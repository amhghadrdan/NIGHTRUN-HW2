#!/usr/bin/env python3
"""Prepare the local PDF and Windows installer in delivery/."""
import argparse
from pathlib import Path
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--report', type=Path, default=ROOT / 'delivery/REPORT.pdf')
parser.add_argument('--makensis', default=shutil.which('makensis'), help='Path to the NSIS compiler')
args = parser.parse_args()

report = args.report.resolve()
if not report.is_file() or not report.read_bytes().startswith(b'%PDF-'):
    raise SystemExit('Supply the final PDF with --report /path/to/report.pdf.')
if not args.makensis:
    raise SystemExit('Install NSIS or supply its compiler with --makensis.')
windows = ROOT / 'Builds/Windows'
for name in ('NIGHTRUN.exe', 'UnityPlayer.dll'):
    if not (windows / name).is_file():
        raise SystemExit(f'Missing Windows runtime file: {name}')
for name in ('NIGHTRUN_Data', 'MonoBleedingEdge'):
    if not (windows / name).is_dir() or not any((windows / name).rglob('*')):
        raise SystemExit(f'Missing Windows runtime directory: {name}')

delivery = ROOT / 'delivery'
delivery.mkdir(exist_ok=True)
if report != delivery / 'REPORT.pdf':
    shutil.copy2(report, delivery / 'REPORT.pdf')
shutil.copy2(ROOT / 'THIRD_PARTY.txt', windows / 'THIRD_PARTY.txt')
subprocess.run([args.makensis, str(ROOT / 'Tools/windows-installer.nsi')], cwd=ROOT, check=True)
if not (delivery / 'NIGHTRUN-Setup.exe').is_file():
    raise SystemExit('NSIS did not produce NIGHTRUN-Setup.exe.')
print(f'Delivery ready: {delivery}')
