"""Archive one Unity probe run and summarize repetitions; no third-party packages."""
import csv
import hashlib
import math
from pathlib import Path
import shutil
import statistics

root = Path(__file__).resolve().parent.parent
source = root / "Logs" / "relation-probe"
target = root / "Evaluation" / "relation-probe" / "recorded"
target.mkdir(parents=True, exist_ok=True)
rows = list(csv.DictReader((source / "results.csv").open(encoding="utf-8-sig", newline="")))
groups = {}
for row in rows:
    key = tuple(row[k] for k in ("format", "policy", "budget_bytes"))
    groups.setdefault(key, []).append(row)
quality = ["queries", "exact", "exact_rate", "false_single", "status_confusion",
           "incomplete_rejected", "payload_bytes", "over_budget_frames"]
header = ["format", "policy", "budget_bytes", "repeats", *quality,
          "encode_median_ms", "receive_median_ms", "loopback_median_ms", "loopback_p95_ms"]
with (target / "summary.csv").open("w", encoding="utf-8", newline="") as stream:
    writer = csv.writer(stream)
    writer.writerow(header)
    for key, group in groups.items():
        if len(group) != 20:
            raise ValueError("Unexpected repetition count")
        if any(len({r[k] for r in group}) != 1 for k in quality):
            raise ValueError("Quality/bytes changed between deterministic repetitions")
        times = sorted(float(r["loopback_ms"]) for r in group)
        writer.writerow([*key, len(group), *(group[0][k] for k in quality),
                         statistics.median(float(r["encode_ms"]) for r in group),
                         statistics.median(float(r["receive_ms"]) for r in group),
                         statistics.median(times), times[math.ceil(0.95 * len(times)) - 1]])
shutil.copyfile(source / "results.csv", target / "raw.csv")
shutil.copyfile(source / "environment.txt", target / "environment.txt")
files = ["Evaluation/relation-probe/input.json", "Evaluation/relation-probe/expected.json",
         "Assets/Scripts/SceneTokenAudio/Research/SemanticRelations.cs", "Assets/Editor/RelationProbeExperiment.cs"]
(target / "sha256.txt").write_text("\n".join(
    f"{hashlib.sha256((root / file).read_bytes()).hexdigest()}  {file}" for file in files) + "\n", encoding="utf-8")
print(f"Archived {len(rows)} rows, {len(groups)} configurations to {target}")
