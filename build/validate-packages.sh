#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
package_dir="${1:-$root/artifacts/packages}"
version="${2:-$(dotnet msbuild "$root/src/Orient.Pipelink/Orient.Pipelink.csproj" -getProperty:Version)}"

python3 - "$package_dir" "$version" << 'PY'
import sys, zipfile
from pathlib import Path

package_dir, version = Path(sys.argv[1]), sys.argv[2]
package_id = "Orient.Pipelink"
nupkg = package_dir / f"{package_id}.{version}.nupkg"
snupkg = package_dir / f"{package_id}.{version}.snupkg"

print("packages:")
for path in sorted(package_dir.glob("*nupkg")):
    print(" -", path.name)
for path in (nupkg, snupkg):
    if not path.exists():
        raise SystemExit(f"missing {path.name}")
unexpected = [p.name for p in package_dir.glob("*.nupkg") if not p.name.startswith(package_id + ".")]
if unexpected:
    raise SystemExit("unexpected packages: " + ", ".join(unexpected))

with zipfile.ZipFile(nupkg) as archive:
    entries = archive.namelist()
    nuspec = archive.read(next(e for e in entries if e.endswith(".nuspec"))).decode()
    checks = {
        "icon.png in package": "icon.png" in entries,
        "README.md in package": "README.md" in entries,
        "LICENSE in package": "LICENSE" in entries,
        f"lib/net10.0/{package_id}.dll": f"lib/net10.0/{package_id}.dll" in entries,
        f"lib/net10.0/{package_id}.xml": f"lib/net10.0/{package_id}.xml" in entries,
        f"<id>{package_id}</id>": f"<id>{package_id}</id>" in nuspec,
        f"<version>{version}</version>": f"<version>{version}</version>" in nuspec,
        "<icon>icon.png</icon>": "<icon>icon.png</icon>" in nuspec,
        "<readme>README.md</readme>": "<readme>README.md</readme>" in nuspec,
        "repository url": "https://github.com/bburaksseyhan/pipelink" in nuspec,
        "repository commit": 'commit="' in nuspec,
        "orient tag": "orient" in nuspec,
    }
    failed = [name for name, ok in checks.items() if not ok]
    for name, ok in checks.items():
        print(("ok   " if ok else "FAIL ") + name)
    if failed:
        raise SystemExit("package validation failed: " + ", ".join(failed))
print("package layout ok")
PY
