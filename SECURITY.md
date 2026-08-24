# Security and antivirus notes

## Reported heuristic detection

An older unsigned, compressed single-file archive was reported as detected by one VirusTotal vendor:

```text
SHA-256: af36fa2473d9de1ca7230a7aac492cc81ae46f8aa7068f2001e0c17d5b03ea7c
```

One detection is not proof of malware or proof of a false positive. The report applies only to that exact hash.

## Current release hardening

Ping Flud 1.4.1 uses a conventional unpacked .NET publish layout instead of embedding and compressing the runtime inside one executable. This makes the managed assemblies and official .NET runtime files directly inspectable by antivirus engines.

The project also provides:

- complete MIT-licensed source;
- reproducible build scripts;
- SHA-256 checksums for every archive;
- explicit product, version, and developer metadata;
- no elevation request;
- no process injection, registry modification, shell execution, dynamic assembly loading, or remote-code download logic.

Before packaging, the full automated test suite is run. The release directory is scanned locally with Microsoft Defender using:

```bat
"%ProgramFiles%\Windows Defender\MpCmdRun.exe" -Scan -ScanType 3 -File "C:\path\to\release" -DisableRemediation
```

## Remaining trust limitation

The executables are not Authenticode-signed. Unsigned network utilities can receive heuristic or reputation warnings even when built from clean source. OffByOneHuman should sign production releases with a trusted Authenticode certificate when one is available.

## Reporting a current detection

1. Verify the file's SHA-256 against `SHA256SUMS.txt`.
2. Confirm that the detection is for the current hash, not an older release.
3. Submit the exact current file to the detecting vendor's false-positive portal.
4. Include the source archive, build instructions, and checksum manifest.
5. Do not advise users to disable antivirus protection or create broad exclusions.
