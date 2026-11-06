using UnityEngine;

namespace SceneTokenAudio.Research
{
    // Local controlled demo. The pointer is scripted; it is not measured human intent.
    public sealed class RelationProbeDemo : MonoBehaviour
    {
        private RelationObservationSource source;
        private Transform pointer, part1, part2;
        private int sample;
        private string explanation = "Select a controlled observation. No human-intent inference.";

        private void Start()
        {
            source = gameObject.AddComponent<RelationObservationSource>();
            pointer = new GameObject("ScriptedPointer").transform;
            pointer.SetParent(transform, false);
            pointer.position = new Vector3(0f, 1f, 0f);
            part1 = Part("M1", new Vector3(0f, 1f, 2f));
            part2 = Part("M2", new Vector3(0f, 1f, 3f));
            source.pointer = pointer;
            source.parts = new[] {
                new ObservedPart { id = "M1", collider = part1.GetComponent<Collider>() },
                new ObservedPart { id = "M2", collider = part2.GetComponent<Collider>() }
            };
        }

        private Transform Part(string id, Vector3 position)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            part.name = id;
            part.transform.SetParent(transform, false);
            part.transform.position = position;
            part.transform.localScale = Vector3.one * 0.2f;
            return part.transform;
        }

        private void Observe()
        {
            double time = Time.realtimeSinceStartupAsDouble;
            source.ObservePointing("U1", "Q" + (++sample), time, time);
        }

        private void OnGUI()
        {
            if (!source) return;
            GUILayout.BeginArea(new Rect(12, 12, 720, 650), GUI.skin.box);
            GUILayout.Label("Scene Token relation probe: local synthetic demo");
            GUILayout.Label("Utterance U1 by A: B, install this motor.");
            if (GUILayout.Button("Missing pointer observation"))
            { source.pointer = null; Observe(); explanation = "insufficient; candidates not computed"; }
            if (GUILayout.Button("Both motors intersect pointer"))
            {
                source.pointer = pointer;
                part1.position = new Vector3(0f, 1f, 2f); part2.position = new Vector3(0f, 1f, 3f);
                Observe(); explanation = "M1 and M2 are geometric candidates, not intended targets";
            }
            if (GUILayout.Button("Move M2 aside and observe"))
            { source.pointer = pointer; part2.position = new Vector3(1f, 1f, 3f); Observe(); explanation = "M1 alone intersects"; }
            if (GUILayout.Button("Move M1 without creating a new pointing observation"))
            { part1.position = new Vector3(-1f, 1f, 2f); explanation = "Past pointing results are unchanged"; }
            if (GUILayout.Button("Record observed B / M1 / Install / Succeeded"))
            {
                source.actorId = "B";
                double time = Time.realtimeSinceStartupAsDouble;
                source.ObserveOperation("not_applicable", "O" + (++sample), "M1", "Install", "Succeeded", time, time);
                source.actorId = "A";
                explanation = "Operation result explicitly injected by button; no U1 causal link inferred";
            }
            GUILayout.Label(explanation);
            GUILayout.Label("UTF-8 payload bytes: " + (source.LatestSent == null ? 0 : RelationWire.Bytes(source.LatestSent)));
            GUILayout.Label("Sender JSON"); GUILayout.TextArea(source.LatestSent ?? "");
            GUILayout.Label("Receiver reconstructed result"); GUILayout.TextArea(source.LatestReceived ?? "");
            GUILayout.EndArea();
        }
    }
}
