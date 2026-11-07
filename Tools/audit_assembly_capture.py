"""Audit recorder file consistency, never human intent or independent ground truth.

Usage: python Tools/audit_assembly_capture.py CAPTURE_DIRECTORY [--output NEW_DIRECTORY]
Only the standard library is required. Input files are never modified.
"""
import argparse
import collections
import csv
import hashlib
import json
import math
from pathlib import Path


FILES = ("capture-info.json", "observations.ndjson", "events-audio.csv", "sender-frames.ndjson")
PHASES = ("Unknown", "Preparation", "Assembly", "Verification", "Emergency")
META = {
    "session_id": "sessionId", "recorder_id": "recorderId", "event_id": "eventId",
    "clock_id": "clockId", "sequence": "sequence", "event_kind": "eventKind",
    "actor_id": "actorId", "actor_evidence": "actorEvidence",
    "actor_audio_source_id": "actorAudioSourceId", "object_id": "objectId",
    "target_id": "targetId", "task_id": "taskId", "attempt_id": "attemptId",
    "utterance_id": "utteranceId", "origin": "origin", "evidence_detail": "evidenceDetail",
}
AUDIO_COLUMNS = ("listener_id", "audio_source_kind", "audio_timestamp_s", "audio_source_id",
                 "distance_m", "direction_x", "direction_y", "direction_z", "occlusion",
                 "task_phase", "priority", "time_to_deadline_s")
HEADER = ("schema_version", *list(META)[:5], "observed_at_s", *list(META)[5:],
          "object_x", "object_y", "object_z", "audio_available", "actor_audio_match", *AUDIO_COLUMNS)


def audit(directory):
    directory = Path(directory)
    errors, warnings = [], []
    report = {"scope": "file_consistency_only", "human_operation_verified": False,
              "independent_accuracy_evaluated": False, "errors": errors, "warnings": warnings,
              "auditor_schema_version": 1,
              "auditor_sha256": hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
              "sha256": {}, "event_count": 0, "csv_row_count": 0}
    for name in FILES:
        path = directory / name
        if not path.is_file():
            errors.append(f"Missing file: {name}")
        else:
            report["sha256"][name] = hashlib.sha256(path.read_bytes()).hexdigest()
    if errors:
        return report, []

    def read_json(path, lines=False):
        try:
            with path.open(encoding="utf-8-sig") as stream:
                return [json.loads(line) for line in stream if line.strip()] if lines else json.load(stream)
        except (ValueError, UnicodeError) as exc:
            errors.append(f"Invalid JSON in {path.name}: {exc}")
            return [] if lines else {}

    info = read_json(directory / FILES[0])
    observations = read_json(directory / FILES[1], True)
    frames = read_json(directory / FILES[3], True)
    try:
        with (directory / FILES[2]).open(encoding="utf-8-sig", newline="") as stream:
            reader = csv.DictReader(stream, strict=True)
            rows = list(reader)
            if tuple(reader.fieldnames or ()) != HEADER:
                errors.append("CSV header differs from recorder schema v1")
    except (csv.Error, UnicodeError) as exc:
        errors.append(f"Invalid CSV: {exc}")
        rows = []
    if errors:
        return report, []

    def same_number(actual, expected, where):
        try:
            if isinstance(actual, bool) or isinstance(expected, bool):
                raise ValueError()
            a, b = float(actual), float(expected)
            if not math.isfinite(a) or not math.isfinite(b) or not math.isclose(a, b, rel_tol=2e-6, abs_tol=1e-6):
                raise ValueError()
        except (ValueError, TypeError, OverflowError):
            errors.append(f"Numeric mismatch/nonfinite value: {where}")

    def equal(actual, expected, where):
        if actual != expected:
            errors.append(f"Mismatch: {where}")

    grouped = collections.defaultdict(list)
    for row in rows:
        if None in row or any(value is None for value in row.values()):
            errors.append("CSV row has wrong number of fields")
        grouped[row.get("event_id")].append(row)
    seen, annotations = set(), []
    counts, source_kinds = collections.Counter(), collections.Counter()
    last_sequence, last_time = None, None
    audio_events = 0
    if not observations:
        errors.append("No observations: an empty capture cannot verify acquisition")
    if not isinstance(info, dict):
        errors.append("capture-info.json must be an object")
        return report, []
    if len(frames) != len(observations):
        errors.append("Frame count differs from observation count")
    for index, observation in enumerate(observations):
        try:
            o = observation
            event_id = o["eventId"]
            if not isinstance(event_id, str) or not event_id or event_id in seen:
                errors.append(f"Missing/duplicate event ID at observation {index + 1}")
            seen.add(event_id)
            equal(o["schemaVersion"], 1, f"{event_id}: schema")
            for key in ("sessionId", "recorderId", "clockId"):
                if not isinstance(info.get(key), str) or not info[key].strip():
                    errors.append(f"Missing capture identity: {key}")
                equal(o[key], info.get(key), f"{event_id}: {key}")
            sequence, at = o["sequence"], o["observedAt"]
            if type(sequence) is not int or sequence < 1 or (last_sequence is not None and sequence != last_sequence + 1):
                errors.append(f"Nonconsecutive/invalid sequence: {event_id}")
            if type(at) not in (int, float) or not math.isfinite(at) or at < 0 or (last_time is not None and at < last_time):
                errors.append(f"Invalid/backwards observation time: {event_id}")
            last_sequence, last_time = sequence, at
            equal(event_id, f"{o['clockId']}/{sequence}", f"{event_id}: event identity")
            contexts = o["audioContexts"]
            if not isinstance(contexts, list) or any(not isinstance(c, dict) for c in contexts):
                raise TypeError("audioContexts must be an array of objects")
            event_rows = grouped.pop(event_id, [])
            equal(len(event_rows), max(1, len(contexts)), f"{event_id}: CSV multiplicity")
            counts[o["eventKind"]] += 1
            audio_events += bool(contexts)
            for row, context in zip(event_rows, contexts or [None]):
                equal(row["schema_version"], "1", f"{event_id}: CSV schema")
                for column, key in META.items():
                    equal(row[column], str(o[key]), f"{event_id}: {column}")
                same_number(row["observed_at_s"], at, f"{event_id}: observed time")
                for axis in "xyz":
                    if o["hasObjectPose"]:
                        same_number(row[f"object_{axis}"], o["objectPosition"][axis], f"{event_id}: object {axis}")
                    else:
                        equal(row[f"object_{axis}"], "", f"{event_id}: unavailable object pose")
                equal(row["audio_available"], "true" if context else "false", f"{event_id}: audio availability")
                if context is None:
                    equal(row["actor_audio_match"], "false", f"{event_id}: unavailable audio match")
                    for column in AUDIO_COLUMNS:
                        equal(row[column], "", f"{event_id}: missing audio {column}")
                    continue
                token = context["token"]
                source_kinds[context["sourceKind"]] += 1
                for column, expected in (("listener_id", context["listenerId"]), ("audio_source_kind", context["sourceKind"]),
                                         ("audio_source_id", token["sourceId"])):
                    equal(row[column], expected, f"{event_id}: {column}")
                phase = token["taskPhase"]
                if type(phase) is not int or not 0 <= phase < len(PHASES):
                    raise ValueError("Invalid task phase")
                equal(row["task_phase"], PHASES[phase], f"{event_id}: phase")
                for column, key in (("audio_timestamp_s", "timestamp"), ("distance_m", "distance"),
                                    ("occlusion", "occlusion"), ("priority", "priority"), ("time_to_deadline_s", "timeToDeadline")):
                    same_number(row[column], token[key], f"{event_id}: {column}")
                for axis in "xyz":
                    same_number(row[f"direction_{axis}"], token["relativeDirection"][axis], f"{event_id}: direction {axis}")
                if token["timestamp"] < at or token["distance"] < 0:
                    errors.append(f"Invalid audio sample time/distance: {event_id}")
                distance = math.dist([context["sourcePosition"][axis] for axis in "xyz"],
                                     [context["listenerPosition"][axis] for axis in "xyz"])
                same_number(token["distance"], distance, f"{event_id}: measured distance")
                matched = o["actorAudioSourceId"] != "unknown" and o["actorAudioSourceId"] == token["sourceId"]
                equal(row["actor_audio_match"], str(matched).lower(), f"{event_id}: actor/audio link")
            if index < len(frames):
                frame = frames[index]
                equal(frame["sessionId"], o["sessionId"], f"{event_id}: frame session")
                available = frame["availableAt"]
                if type(available) not in (int, float) or not math.isfinite(available) or available < at:
                    errors.append(f"Invalid frame availability: {event_id}")
                relations = frame["relations"]
                expected_ids = {event_id + "/" + role for role in ("object", "target", "attempt")}
                equal({r["id"] for r in relations}, expected_ids, f"{event_id}: frame relation IDs")
                equal(len(relations), 3, f"{event_id}: frame relation count")
                for r in relations:
                    role = r["id"].rsplit("/", 1)[-1]
                    equal(r["values"], [o[role + "Id"]], f"{event_id}: frame value")
                    equal(r["kind"], o["eventKind"] + ":" + role, f"{event_id}: frame kind")
                    equal(r["actorId"], o["actorId"], f"{event_id}: frame actor")
                    equal(r["utteranceId"], o["utteranceId"], f"{event_id}: frame utterance")
                    equal(r["status"], "evaluated", f"{event_id}: frame status")
                    same_number(r["observedAt"], at, f"{event_id}: frame time")
            annotations.append({"event_id": event_id, "observed_event_kind": o["eventKind"],
                                "independent_evidence_ref": "", "reviewer": "", "review_status": "unreviewed",
                                "expected_actor_id": "", "expected_object_id": "", "expected_target_id": "",
                                "expected_event_kind": "", "notes": ""})
        except (KeyError, TypeError, ValueError, IndexError, OverflowError) as exc:
            errors.append(f"Malformed observation/frame {index + 1}: {exc}")
    if grouped:
        errors.append("CSV contains events absent from raw observations")
    if observations and not audio_events:
        warnings.append("No audio contexts: listener/source wiring needs human review")
    if source_kinds.get("tracked_head_pose"):
        warnings.append("Tracked head pose is not a recorded voice signal")
    warnings.append("File agreement cannot establish human provenance, operation completeness or accuracy")
    report.update(event_count=len(observations), csv_row_count=len(rows), event_kinds=dict(counts),
                  audio_context_source_kinds=dict(source_kinds), events_with_audio=audio_events,
                  snapshot_events=sum(n for kind, n in counts.items() if kind.endswith("_snapshot")),
                  unknown_actor_events=sum(o.get("actorId") == "unknown" for o in observations if isinstance(o, dict)))
    return report, annotations


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", type=Path)
    parser.add_argument("--output", type=Path, help="Create a NEW directory for audit.json and unreviewed annotations.csv")
    args = parser.parse_args()
    try:
        report, annotations = audit(args.directory)
        if args.output:
            capture = args.directory.resolve()
            output = args.output.resolve()
            if output == capture or capture in output.parents:
                parser.error("Output must be outside the input capture directory")
            output.mkdir(parents=True, exist_ok=False)
            (output / "audit.json").write_text(json.dumps(report, ensure_ascii=False, indent=2, allow_nan=False) + "\n", encoding="utf-8")
            if annotations:
                with (output / "annotations.csv").open("w", encoding="utf-8-sig", newline="") as stream:
                    writer = csv.DictWriter(stream, fieldnames=list(annotations[0]))
                    writer.writeheader()
                    writer.writerows(annotations)
        print(json.dumps(report, ensure_ascii=False, indent=2, allow_nan=False))
        return 1 if report["errors"] else 0
    except (OSError, UnicodeError) as exc:
        parser.exit(2, f"Capture audit failed: {exc}\n")


if __name__ == "__main__":
    raise SystemExit(main())
