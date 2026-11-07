"""Synthetic file-consistency fixtures; these are not human operation data."""
import copy
import csv
import importlib.util
import json
import re
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

MODULE_PATH = Path(__file__).resolve().parents[1] / "audit_assembly_capture.py"
SPEC = importlib.util.spec_from_file_location("capture_audit", MODULE_PATH)
AUDIT = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(AUDIT)


class CaptureAuditTests(unittest.TestCase):
    def test_csv_contract_matches_actual_csharp_recorder(self):
        source = MODULE_PATH.parents[1] / "Assets/Scripts/SceneTokenAudio/Research/AssemblyCaptureRecorder.cs"
        header = re.search(r'public const string CsvHeader = "([^"]+)";', source.read_text(encoding="utf-8"))
        self.assertIsNotNone(header)
        self.assertEqual(tuple(header[1].split(",")), AUDIT.HEADER)

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name) / "capture"
        self.root.mkdir()
        self.info = dict(sessionId="synthetic", recorderId="test", clockId="synthetic/test/clock")
        self.o = dict(schemaVersion=1, **self.info, eventId=self.info["clockId"] + "/1", sequence=1,
                      observedAt=1.25, eventKind="grab_confirmed", actorId="A", actorEvidence="test_callback",
                      actorAudioSourceId="A", objectId="M1", targetId="not_applicable", taskId="unknown",
                      attemptId="M1/grab-episode/1", utteranceId="unknown", origin="synthetic_test",
                      evidenceDetail='comma, quote " and\nnewline', hasObjectPose=True,
                      objectPosition=dict(x=0, y=1, z=0), objectRotation=dict(x=0, y=0, z=0, w=1), audioContexts=[])
        self.context = dict(listenerId="local", sourceKind="tracked_head_pose", sourcePosition=dict(x=0, y=0, z=4),
                            listenerPosition=dict(x=0, y=0, z=0), token=dict(timestamp=1.26, sourceId="A", distance=4,
                            relativeDirection=dict(x=0, y=0, z=1), occlusion=-1, taskPhase=0, priority=0, timeToDeadline=-1))

    def write(self, audio=True, second_source=False):
        self.o["audioContexts"] = [copy.deepcopy(self.context)] if audio else []
        if second_source:
            other = copy.deepcopy(self.context)
            other["token"]["sourceId"] = "B"
            self.o["audioContexts"].append(other)
        self.write_json("capture-info.json", self.info)
        self.write_json("observations.ndjson", self.o)
        self.frame = dict(sessionId="synthetic", availableAt=1.27, relations=[dict(
            id=self.o["eventId"] + "/" + role, utteranceId="unknown", actorId="A",
            kind="grab_confirmed:" + role, status="evaluated", observedAt=1.25,
            values=[self.o[role + "Id"]]) for role in ("object", "target", "attempt")])
        self.write_json("sender-frames.ndjson", self.frame)
        self.rows = []
        for context in self.o["audioContexts"] or [None]:
            row = {column: str(self.o[key]) for column, key in AUDIT.META.items()}
            row.update(schema_version="1", observed_at_s="1.25", object_x="0", object_y="1", object_z="0",
                       audio_available=str(context is not None).lower(),
                       actor_audio_match=str(context is not None and context["token"]["sourceId"] == "A").lower())
            row.update(dict.fromkeys(AUDIT.AUDIO_COLUMNS, ""))
            if context:
                row.update(listener_id="local", audio_source_kind="tracked_head_pose", audio_timestamp_s="1.26",
                           audio_source_id=context["token"]["sourceId"], distance_m="4", direction_x="0", direction_y="0",
                           direction_z="1", occlusion="-1", task_phase="Unknown", priority="0", time_to_deadline_s="-1")
            self.rows.append(row)
        self.write_csv()

    def write_json(self, name, value):
        (self.root / name).write_text(json.dumps(value) + "\n", encoding="utf-8")

    def write_csv(self):
        with (self.root / "events-audio.csv").open("w", encoding="utf-8-sig", newline="") as stream:
            writer = csv.DictWriter(stream, fieldnames=AUDIT.HEADER)
            writer.writeheader()
            writer.writerows(self.rows)

    def test_multiple_sources_are_one_event_and_annotations_have_no_answers(self):
        self.write(second_source=True)
        before = {p.name: p.read_bytes() for p in self.root.iterdir()}
        report, annotations = AUDIT.audit(self.root)
        self.assertEqual(report["errors"], [])
        self.assertEqual(report["event_count"], 1)
        self.assertEqual(report["csv_row_count"], 2)
        self.assertFalse(report["human_operation_verified"])
        self.assertFalse(report["independent_accuracy_evaluated"])
        self.assertEqual(annotations[0]["expected_actor_id"], "")
        self.assertEqual(annotations[0]["review_status"], "unreviewed")
        self.assertEqual(before, {p.name: p.read_bytes() for p in self.root.iterdir()})

    def test_missing_audio_remains_blank(self):
        self.write(audio=False)
        report, _ = AUDIT.audit(self.root)
        self.assertEqual(report["errors"], [])
        self.assertEqual(report["events_with_audio"], 0)
        self.assertTrue(any("No audio" in w for w in report["warnings"]))
        self.rows[0]["distance_m"] = "0"
        self.write_csv()
        self.assertTrue(AUDIT.audit(self.root)[0]["errors"])

    def test_wrong_actor_link_frame_value_and_missing_rows_are_rejected(self):
        for mutation in ("actor", "frame", "rows", "clock", "sequence", "time", "distance", "phase"):
            with self.subTest(mutation=mutation):
                self.write()
                if mutation == "actor": self.rows[0]["actor_audio_match"] = "false"
                if mutation == "frame": self.frame["relations"][0]["values"] = ["other"]
                if mutation == "rows": self.rows.clear()
                if mutation == "clock": self.info["clockId"] = "other"
                if mutation == "sequence": self.o["sequence"] = 2
                if mutation == "time": self.o["observedAt"] = float("nan")
                if mutation == "distance": self.rows[0]["distance_m"] = "NaN"
                if mutation == "phase": self.o["audioContexts"][0]["token"]["taskPhase"] = 99
                self.write_csv()
                self.write_json("observations.ndjson", self.o)
                self.write_json("capture-info.json", self.info)
                self.write_json("sender-frames.ndjson", self.frame)
                self.assertTrue(AUDIT.audit(self.root)[0]["errors"])
                # Reset identities/time for subsequent subtests.
                self.info["clockId"] = "synthetic/test/clock"
                self.o["sequence"], self.o["observedAt"] = 1, 1.25

    def test_duplicate_observations_and_truncated_frames_are_rejected(self):
        self.write()
        path = self.root / "observations.ndjson"
        path.write_text(path.read_text(encoding="utf-8") * 2, encoding="utf-8")
        report, _ = AUDIT.audit(self.root)
        self.assertTrue(any("duplicate" in e for e in report["errors"]))
        self.assertTrue(any("Frame count" in e for e in report["errors"]))

    def test_missing_and_invalid_files(self):
        self.assertTrue(AUDIT.audit(self.root)[0]["errors"])
        self.write()
        (self.root / "observations.ndjson").write_text('{"truncated":', encoding="utf-8")
        self.assertTrue(AUDIT.audit(self.root)[0]["errors"])
        self.write()
        self.write_json("observations.ndjson", None)
        self.assertTrue(AUDIT.audit(self.root)[0]["errors"])

    def test_cli_refuses_overwrite_and_capture_subdirectories(self):
        self.write()
        output = Path(self.temp.name) / "audit"
        command = [sys.executable, str(MODULE_PATH), str(self.root), "--output", str(output)]
        self.assertEqual(subprocess.run(command, capture_output=True).returncode, 0)
        annotation_bytes = (output / "annotations.csv").read_bytes()
        self.assertNotEqual(subprocess.run(command, capture_output=True).returncode, 0)
        self.assertEqual((output / "annotations.csv").read_bytes(), annotation_bytes)
        command[-1] = str(self.root / "audit")
        self.assertNotEqual(subprocess.run(command, capture_output=True).returncode, 0)
        self.assertFalse((self.root / "audit").exists())


if __name__ == "__main__":
    unittest.main()
