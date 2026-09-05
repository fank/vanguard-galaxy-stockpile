#!/usr/bin/env python3
"""Owner-local packaging; --check validates archive layout without game assets."""
import argparse
from pathlib import Path
import stat
import zipfile

NAMES = {"VGStockpile.dll", "Newtonsoft.Json.dll", "README.md", "LICENSE", "THIRD_PARTY_NOTICES.md"}


def validate(path):
    with zipfile.ZipFile(path) as archive:
        entries = archive.infolist()
        if len(entries) != len(NAMES) or {e.filename for e in entries} != {"VGStockpile/" + n for n in NAMES}:
            raise ValueError("Release file allowlist mismatch")
        if any(stat.S_ISLNK(e.external_attr >> 16) or e.file_size == 0 for e in entries):
            raise ValueError("Empty file or link in release")
        if archive.testzip() is not None:
            raise ValueError("Archive CRC failure")
    print("PASS: release layout, not binary provenance")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", type=Path)
    parser.add_argument("--configuration", default="Release")
    args = parser.parse_args()
    if args.check:
        validate(args.check)
    else:
        root = Path(__file__).resolve().parents[1]
        output = root / "VGStockpile/bin" / args.configuration / "netstandard2.1"
        if {p.name for p in output.glob("*.dll")} != {"VGStockpile.dll", "Newtonsoft.Json.dll"}:
            raise ValueError("Unexpected DLL set; clean and rebuild")
        target = root / "dist/VGStockpile.zip"
        target.parent.mkdir(exist_ok=True)
        with zipfile.ZipFile(target, "w", zipfile.ZIP_DEFLATED) as archive:
            for name in sorted(NAMES):
                source = (output if name.endswith(".dll") else root) / name
                if source.is_symlink():
                    raise ValueError("Do not package linked inputs")
                archive.write(source, "VGStockpile/" + name)
        validate(target)
        print(target)
