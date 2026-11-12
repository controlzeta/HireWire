"""Coverage ratchet: fails (exit 1) if the newest Cobertura report is below THRESHOLD lines.

Usage:  python coverage-check.py
"""
import glob
import os
import sys
import xml.etree.ElementTree as ET

THRESHOLD = 75.0

files = glob.glob("HireWire.Tests/TestResults/*/coverage.cobertura.xml")
if not files:
    print("No coverage report found. Run: dotnet test HireWire.Tests --settings coverage.runsettings --collect:\"XPlat Code Coverage\"")
    sys.exit(2)

newest = max(files, key=os.path.getmtime)
root = ET.parse(newest).getroot()

total = covered = 0
for pkg in root.iter("package"):
    for cls in pkg.iter("class"):
        for line in cls.iter("line"):
            total += 1
            if int(line.get("hits", "0")) > 0:
                covered += 1

percent = 100.0 * covered / total if total else 0.0
print(f"Coverage report: {newest}")
print(f"Lines covered:   {covered}/{total} ({percent:.1f}%)  — threshold {THRESHOLD}%")

if percent < THRESHOLD:
    print("FAIL: coverage below threshold")
    sys.exit(1)
print("OK")
