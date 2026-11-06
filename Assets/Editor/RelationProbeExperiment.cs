using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using SceneTokenAudio.Research;
using UnityEditor;
using UnityEngine;

public static class RelationProbeExperiment
{
    private static string F(double x) => x.ToString("R", CultureInfo.InvariantCulture);

    public static void RunBatch()
    {
        RelationProbeSceneBuilder.Create();
        Run();
    }

    [MenuItem("Tools/Scene Token Research/Run Synthetic Relation Probe")]
    public static void Run()
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var input = JsonUtility.FromJson<RelationFrames>(File.ReadAllText(Path.Combine(root, "Evaluation/relation-probe/input.json")));
        // Oracle is read only here; sender and receiver APIs never receive it.
        var expected = JsonUtility.FromJson<RelationFrames>(File.ReadAllText(Path.Combine(root, "Evaluation/relation-probe/expected.json")));
        if (input.frames.Length != expected.frames.Length) throw new ArgumentException("Oracle length mismatch");
        string output = Path.Combine(root, "Logs/relation-probe");
        Directory.CreateDirectory(output);
        var rows = new List<string> {
            "format,policy,budget_bytes,repeat,queries,exact,exact_rate,false_single,status_confusion,incomplete_rejected,payload_bytes,over_budget_frames,encode_ms,receive_ms,loopback_ms"
        };
        foreach (Representation format in Enum.GetValues(typeof(Representation)))
        foreach (Reduction policy in Enum.GetValues(typeof(Reduction)))
        foreach (int budget in new[] { 256, 384, 512, 768, 1024, 2048 })
        for (int repeat = -3; repeat < 20; repeat++)
        {
            var receiver = new RelationReceiver();
            int queries = 0, exact = 0, falseSingle = 0, confusion = 0, bytes = 0, overBudget = 0;
            double encodeMs = 0, receiveMs = 0;
            for (int i = 0; i < input.frames.Length; i++)
            {
                if (input.frames[i].sessionId != expected.frames[i].sessionId || input.frames[i].availableAt != expected.frames[i].availableAt)
                    throw new ArgumentException("Oracle checkpoint mismatch");
                long started = Stopwatch.GetTimestamp();
                string packet = RelationWire.Send(input.frames[i], format, policy, budget);
                long encoded = Stopwatch.GetTimestamp();
                receiver.Receive(packet);
                long received = Stopwatch.GetTimestamp();
                encodeMs += (encoded - started) * 1000d / Stopwatch.Frequency;
                receiveMs += (received - encoded) * 1000d / Stopwatch.Frequency;
                int payload = RelationWire.Bytes(packet);
                bytes += payload;
                if (payload > budget) overBudget++;
                foreach (var truth in expected.frames[i].relations)
                {
                    queries++;
                    var actual = receiver.Get(expected.frames[i].sessionId, truth.id);
                    if (RelationReceiver.Equivalent(actual, truth)) exact++;
                    if (actual == null) continue; // Missing results reduce exact_rate; do not count as correct.
                    if (truth.values.Length > 1 && actual.status == "evaluated" && actual.values.Length == 1) falseSingle++;
                    if (actual.status != truth.status) confusion++;
                }
            }
            if (repeat < 0) continue;
            rows.Add(string.Join(",", new[] { format.ToString(), policy.ToString(), budget.ToString(), repeat.ToString(),
                queries.ToString(), exact.ToString(), F((double)exact / queries), falseSingle.ToString(), confusion.ToString(),
                receiver.IncompleteRejected.ToString(), bytes.ToString(), overBudget.ToString(), F(encodeMs), F(receiveMs), F(encodeMs + receiveMs) }));
        }
        File.WriteAllLines(Path.Combine(output, "results.csv"), rows, new System.Text.UTF8Encoding(false));
        File.WriteAllText(Path.Combine(output, "environment.txt"),
            "Synthetic S4/S5 loopback probe; not human data or network latency.\n" +
            "Unity=" + Application.unityVersion + "\nOS=" + SystemInfo.operatingSystem + "\nCPU=" + SystemInfo.processorType +
            "\nWarmups=3 per configuration; repetitions=20 on the SAME trace, not 20 independent scenarios.\n" +
            "Budget applies per frame. Full is unbudgeted reference; over_budget_frames makes violations explicit.\n" +
            "Bytes include complete UTF-8 envelopes; no transport headers. No history fetches or loss model.\n" +
            "Latency columns measure encode/reduction and decode/reconstruction CPU for all five frames, not network update delay.\n" +
            "Priority is an input annotation, not a trained selector. RankedItems is not Park & Yoon replication.\n" +
            "GenericClosed and TokenClosed intentionally identical. Representation overhead is not a new semantic principle.\n");
        UnityEngine.Debug.Log("Relation probe wrote " + (rows.Count - 1) + " rows to " + output);
    }
}
