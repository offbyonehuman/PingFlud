from __future__ import annotations

import hashlib
import json
import os
import shutil
import struct
import subprocess
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

tracked_paths = subprocess.check_output(
    ["git", "-C", str(ROOT), "ls-files", "-z"]
).split(b"\0")
tracked_names = {raw_path.decode("utf-8") for raw_path in tracked_paths if raw_path}
source_documents = {
    "README.md",
    "CHANGELOG.md",
    "CONTRIBUTING.md",
    "CODE_OF_CONDUCT.md",
    "CREDITS.md",
    "SECURITY.md",
    "LICENSE",
    "THIRD_PARTY_NOTICES.md",
    "package_release.py",
}
source_documents.update(
    path.relative_to(ROOT).as_posix()
    for path in (ROOT / "third_party").rglob("*")
    if path.is_file()
)
missing_source_documents = sorted(source_documents - tracked_names)
if missing_source_documents:
    raise RuntimeError(
        "Source packaging requires these files to be staged or committed: " + ", ".join(missing_source_documents)
    )

EXPECTED_MACHINE = {"win-x86": 0x014C, "win-x64": 0x8664, "win-arm64": 0xAA64}
FLAVORS = {
    "compact": ROOT / "artifacts" / "winui-compact",
    "portable": ROOT / "artifacts" / "winui-portable",
}
NUGET_PACKAGES = Path(os.environ.get("NUGET_PACKAGES", str(Path.home() / ".nuget" / "packages")))
ASSETS_FILE = PROJECT.parent / "obj" / "project.assets.json"
BUILD_ONLY_PACKAGE_IDS = {
    "microsoft.net.illink.tasks",
    "microsoft.windows.sdk.buildtools",
    "microsoft.windows.sdk.buildtools.msix",
}
NOTICE_NAME_PARTS = ("license", "notice", "copyright", "copying")


def committed_notice_root(package_id: str, version: str) -> Path:
    package_key = package_id.lower()
    if package_key == "system.drawing.common":
        return ROOT / "third_party" / "dotnet" / "system.drawing.common"
    if package_key == "microsoft.win32.systemevents":
        return ROOT / "third_party" / "dotnet" / "systemevents"
    if package_key == "system.numerics.tensors":
        return ROOT / "third_party" / "dotnet" / "system.numerics.tensors"
    if package_key == "microsoft.web.webview2":
        return ROOT / "third_party" / "webview2"
    if package_key == "microsoft.windowsappsdk":
        return ROOT / "third_party" / "windowsappsdk"
    if package_key.startswith("microsoft.windowsappsdk."):
        return ROOT / "third_party" / "windowsappsdk" / "packages" / package_id / version
    return ROOT / "third_party" / "nuget" / package_id / version


def committed_notice_path(package_id: str, version: str, relative_path: Path) -> Path:
    relative = Path("LICENSE.txt") if (
        package_id.lower() == "microsoft.windowsappsdk" and relative_path.as_posix().lower() == "license.txt"
    ) else relative_path
    return committed_notice_root(package_id, version) / relative


def resolved_notice_pairs() -> list[tuple[Path, Path, str, str]]:
    if not ASSETS_FILE.is_file():
        raise RuntimeError("Restore the WinUI project before packaging the release.")
    assets = json.loads(ASSETS_FILE.read_text(encoding="utf-8"))
    libraries = assets.get("libraries", {})
    package_refs = {
        library
        for target_name, target in assets.get("targets", {}).items()
        if "/win-" in target_name
        for library in target
        if "/" in library and libraries.get(library, {}).get("type") == "package"
    }
    pairs: list[tuple[Path, Path, str, str]] = []
    for library in sorted(package_refs):
        package_id, version = library.split("/", 1)
        if package_id.lower() in BUILD_ONLY_PACKAGE_IDS:
            continue
        package_dir = NUGET_PACKAGES / package_id.lower() / version.lower()
        if not package_dir.is_dir():
            raise RuntimeError(f"Restore the package containing {package_id} {version} before packaging the release.")
        for package_notice in sorted(
            path
            for path in package_dir.rglob("*")
            if path.is_file() and any(part in path.name.lower() for part in NOTICE_NAME_PARTS)
        ):
            relative_path = package_notice.relative_to(package_dir)
            pairs.append(
                (
                    package_notice,
                    committed_notice_path(package_id, version, relative_path),
                    package_id,
                    version,
                )
            )
    return pairs


NOTICE_PAIRS = resolved_notice_pairs()
for package_notice, committed_notice, package_id, version in NOTICE_PAIRS:
    if not committed_notice.is_file():
        raise RuntimeError(f"Missing committed third-party notice for {package_id} {version}: {committed_notice}")
    if package_notice.read_bytes() != committed_notice.read_bytes():
        raise RuntimeError(f"Committed third-party notice does not match {package_id} {version}: {committed_notice}")

RELEASE = ROOT / "release"
if RELEASE.exists():
    shutil.rmtree(RELEASE)
RELEASE.mkdir()

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
        files = sorted(path for path in publish_dir.rglob("*") if path.is_file() and path.suffix.lower() != ".pdb")
        with zipfile.ZipFile(archive, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as output:
            for path in files:
                output.write(path, path.relative_to(publish_dir).as_posix())
            for document in (
                "README.md",
                "CHANGELOG.md",
                "CONTRIBUTING.md",
                "CODE_OF_CONDUCT.md",
                "CREDITS.md",
                "SECURITY.md",
                "LICENSE",
                "THIRD_PARTY_NOTICES.md",
            ):
                output.write(ROOT / document, document)
            for notice in sorted(path for path in (ROOT / "third_party").rglob("*") if path.is_file()):
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
with zipfile.ZipFile(source_archive, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as output:
    for raw_path in sorted(path for path in tracked_paths if path):
        relative_path = Path(raw_path.decode("utf-8"))
        path = ROOT / relative_path
        if not path.is_file():
            raise RuntimeError(f"Tracked source file is missing: {relative_path}")
        output.write(path, relative_path.as_posix())
source_sha256 = hashlib.sha256(source_archive.read_bytes()).hexdigest()

manifest = {
    "product": "Ping Flud",
    "version": VERSION,
    "windows_app_sdk_version": WINDOWS_APP_SDK_VERSION,
    "developer": "OffByOneHuman",
    "packaging_note": "Compact runtime-dependent and compressed self-contained WinUI 3 distributions; optional symbols are omitted from release archives.",
    "redistributed_notice_files": sorted(
        path.relative_to(ROOT).as_posix() for _, path, _, _ in NOTICE_PAIRS
    ),
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
