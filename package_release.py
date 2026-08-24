from __future__ import annotations

import hashlib
import json
import shutil
import struct
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent
VERSION = "1.4.1"
RELEASE = ROOT / "release"
RELEASE.mkdir(exist_ok=True)
for previous in RELEASE.glob(f"PingFlud-{VERSION}-*"):
    if previous.is_dir():
        shutil.rmtree(previous)
    else:
        previous.unlink()
for manifest_name in ("checksums.json", "SHA256SUMS.txt"):
    manifest_path = RELEASE / manifest_name
    if manifest_path.exists():
        manifest_path.unlink()
records: list[dict[str, object]] = []
EXPECTED_MACHINE = {"win-x86": 0x014C, "win-x64": 0x8664, "win-arm64": 0xAA64}

for flavor in ("portable", "lite"):
    for rid in ("win-x86", "win-x64", "win-arm64"):
        publish_dir = ROOT / "artifacts" / flavor / rid
        executable = publish_dir / "PingFlud.exe"
        data = executable.read_bytes()
        pe_offset = struct.unpack_from("<I", data, 0x3C)[0]
        machine = struct.unpack_from("<H", data, pe_offset + 4)[0]
        if machine != EXPECTED_MACHINE[rid]:
            raise RuntimeError(f"{rid} produced PE machine 0x{machine:04x}, expected 0x{EXPECTED_MACHINE[rid]:04x}")
        archive = RELEASE / f"PingFlud-{VERSION}-{rid}-{flavor}.zip"
        files = sorted(path for path in publish_dir.rglob("*") if path.is_file())
        with zipfile.ZipFile(archive, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as output:
            for path in files:
                output.write(path, path.relative_to(publish_dir).as_posix())
            output.write(ROOT / "README.md", "README.md")
            output.write(ROOT / "CHANGELOG.md", "CHANGELOG.md")
            output.write(ROOT / "SECURITY.md", "SECURITY.md")
            output.write(ROOT / "LICENSE", "LICENSE")
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
                "runtime_requirement": "none" if flavor == "portable" else ".NET 8 Desktop Runtime matching the architecture",
            }
        )

source_archive = RELEASE / f"PingFlud-{VERSION}-source.zip"
source_roots = [
    ROOT / "README.md", ROOT / "CHANGELOG.md", ROOT / "SECURITY.md", ROOT / "LICENSE", ROOT / "PingFlud.sln",
    ROOT / "build-all.cmd", ROOT / "build-all.ps1", ROOT / "package_release.py"
]
source_roots += [
    path for base in (ROOT / "src", ROOT / "tests", ROOT / "docs")
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
    "developer": "OffByOneHuman",
    "packaging_note": "Normal unpacked publish layout avoids compressed single-file bundling heuristics.",
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
