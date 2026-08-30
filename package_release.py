from __future__ import annotations

import hashlib
import json
import shutil
import struct
import zipfile
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parent
PROJECT = ROOT / "src" / "PingFlud.WinUI" / "PingFlud.WinUI.csproj"
PROJECT_ROOT = ET.parse(PROJECT).getroot()
VERSION = PROJECT_ROOT.findtext(".//Version")
WINDOWS_APP_SDK_VERSION = next(
    (
        reference.attrib["Version"]
        for reference in PROJECT_ROOT.findall(".//PackageReference")
        if reference.attrib.get("Include") == "Microsoft.WindowsAppSDK"
    ),
    None,
)
if not VERSION or not WINDOWS_APP_SDK_VERSION:
    raise RuntimeError("PingFlud.WinUI.csproj must define Version and Microsoft.WindowsAppSDK.")

RELEASE = ROOT / "release"
if RELEASE.exists():
    shutil.rmtree(RELEASE)
RELEASE.mkdir()

EXPECTED_MACHINE = {"win-x86": 0x014C, "win-x64": 0x8664, "win-arm64": 0xAA64}
FLAVORS = {
    "compact": ROOT / "artifacts" / "winui-compact",
    "portable": ROOT / "artifacts" / "winui-portable",
}
SDK_PACKAGE = Path.home() / ".nuget" / "packages" / "microsoft.windowsappsdk" / WINDOWS_APP_SDK_VERSION.lower()
SDK_NOTICE_FILES = (SDK_PACKAGE / "license.txt", SDK_PACKAGE / "NOTICE.txt")
if not all(path.is_file() for path in SDK_NOTICE_FILES):
    raise RuntimeError("Restore Microsoft.WindowsAppSDK before packaging the release.")

records: list[dict[str, object]] = []
for flavor, artifact_root in FLAVORS.items():
    for rid, expected_machine in EXPECTED_MACHINE.items():
        publish_dir = artifact_root / rid
        executable = publish_dir / "PingFlud.exe"
        if not executable.is_file():
            raise RuntimeError(f"Missing published executable: {executable}")

        data = executable.read_bytes()
        pe_offset = struct.unpack_from("<I", data, 0x3C)[0]
        machine = struct.unpack_from("<H", data, pe_offset + 4)[0]
        if machine != expected_machine:
            raise RuntimeError(
                f"{flavor}/{rid} produced PE machine 0x{machine:04x}, expected 0x{expected_machine:04x}"
            )

        archive = RELEASE / f"PingFlud-{VERSION}-{rid}-{flavor}.zip"
        files = sorted(path for path in publish_dir.rglob("*") if path.is_file())
        with zipfile.ZipFile(archive, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as output:
            for path in files:
                output.write(path, path.relative_to(publish_dir).as_posix())
            for document in ("README.md", "CHANGELOG.md", "SECURITY.md", "LICENSE", "THIRD_PARTY_NOTICES.md"):
                output.write(ROOT / document, document)
            output.write(SDK_NOTICE_FILES[0], "third_party/windowsappsdk/LICENSE.txt")
            output.write(SDK_NOTICE_FILES[1], "third_party/windowsappsdk/NOTICE.txt")
            for notice in sorted(path for path in (ROOT / "third_party" / "dotnet").rglob("*") if path.is_file()):
                output.write(notice, notice.relative_to(ROOT).as_posix())

        records.append(
            {
                "flavor": flavor,
                "rid": rid,
                "pe_machine": f"0x{machine:04x}",
                "file_count": len(files),
                "unpacked_bytes": sum(path.stat().st_size for path in files),
                "exe_sha256": hashlib.sha256(data).hexdigest(),
                "zip": archive.name,
                "zip_bytes": archive.stat().st_size,
                "zip_sha256": hashlib.sha256(archive.read_bytes()).hexdigest(),
                "runtime_requirement": (
                    "none (self-contained)"
                    if flavor == "portable"
                    else ".NET 8 Desktop Runtime and Windows App Runtime 1.8 matching the architecture"
                ),
            }
        )

source_archive = RELEASE / f"PingFlud-{VERSION}-source.zip"
source_roots = [
    ROOT / ".gitignore",
    ROOT / "README.md",
    ROOT / "CHANGELOG.md",
    ROOT / "SECURITY.md",
    ROOT / "LICENSE",
    ROOT / "THIRD_PARTY_NOTICES.md",
    ROOT / "PingFlud.sln",
    ROOT / "build-all.cmd",
    ROOT / "build-all.ps1",
    ROOT / "package_release.py",
]
source_roots += [
    path
    for base in (ROOT / ".github", ROOT / "src", ROOT / "tests", ROOT / "third_party")
    for path in base.rglob("*")
    if path.is_file() and "bin" not in path.parts and "obj" not in path.parts
]
with zipfile.ZipFile(source_archive, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as output:
    for path in sorted(source_roots):
        output.write(path, path.relative_to(ROOT).as_posix())
source_sha256 = hashlib.sha256(source_archive.read_bytes()).hexdigest()

manifest = {
    "product": "Ping Flud",
    "version": VERSION,
    "windows_app_sdk_version": WINDOWS_APP_SDK_VERSION,
    "developer": "OffByOneHuman",
    "packaging_note": "Compact runtime-dependent and compressed self-contained WinUI 3 distributions.",
    "artifacts": records,
    "source": {
        "zip": source_archive.name,
        "zip_bytes": source_archive.stat().st_size,
        "zip_sha256": source_sha256,
    },
}
(RELEASE / "checksums.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
(RELEASE / "SHA256SUMS.txt").write_text(
    "".join(f"{item['zip_sha256']}  {item['zip']}\n" for item in records)
    + f"{source_sha256}  {source_archive.name}\n",
    encoding="ascii",
)
print(json.dumps(manifest, indent=2))
