using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SceneTokenAudio;
using SceneTokenAudio.Research;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AssemblyCaptureSceneSetup
{
    public const string Original = "Assets/Training/Scene/Drone Training.unity";
    public const string CaptureScene = "Assets/Training/Scene/Drone Training Capture.unity";

    private static bool IsType(Component c, string name)
    {
        if (!c) return false;
        for (Type type = c.GetType(); type != null; type = type.BaseType)
            if (type.FullName == name) return true;
        return false;
    }

    private static ResearchEntityIdentity Identity(Component c, string category)
    {
        var identity = c.GetComponent<ResearchEntityIdentity>();
        if (!identity) identity = c.gameObject.AddComponent<ResearchEntityIdentity>();
        if (string.IsNullOrWhiteSpace(identity.entityId)) identity.entityId = category + "/" + Guid.NewGuid().ToString("N");
        EditorUtility.SetDirty(identity);
        return identity;
    }

    [MenuItem("Tools/Scene Token Research/Prepare Drone Capture Scene")]
    public static void Prepare()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!File.Exists(Original)) throw new FileNotFoundException("Run in the existing Fusion drone project", Original);
        if (File.Exists(CaptureScene))
            throw new InvalidOperationException("Capture scene already exists; keep it and inspect the saved setup instead of overwriting");
        var scene = EditorSceneManager.OpenScene(Original);
        var components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).Where(c => c).ToArray();
        var existing = components.OfType<AssemblyCaptureRecorder>().FirstOrDefault();
        if (existing) throw new InvalidOperationException("Source scene already contains capture instrumentation");
        var system = new GameObject("SceneTokenAssemblyCapture");
        var recorder = system.AddComponent<AssemblyCaptureRecorder>();
        system.AddComponent<DroneCaptureRuntimeBinder>().recorder = recorder;
        system.AddComponent<AssemblyCaptureDebugUI>().recorder = recorder;
        var head = components.FirstOrDefault(c => IsType(c, "Fusion.XR.Shared.Rig.HardwareHeadset"));
        recorder.audioListener = head ? head.transform : components.OfType<Camera>().FirstOrDefault()?.transform;
        recorder.listenerId = "local-head";
        var parts = components.Where(c => IsType(c, "Fusion.XR.Shared.Grabbing.NetworkGrabbable")).ToArray();
        var points = components.Where(c => IsType(c, "Fusion.Addons.StructureCohesion.AttachmentPoint")).ToArray();
        foreach (var point in points) Identity(point, "attachment-point");
        foreach (var part in parts)
        {
            var identity = Identity(part, "part");
            var bridge = part.gameObject.AddComponent<DroneAssemblyEventBridge>();
            bridge.partIdentity = identity;
            bridge.recorder = recorder;
            bridge.networkGrabbable = part;
            bridge.attachmentPoints = points.Where(p => p.GetComponentsInParent<Component>().FirstOrDefault(
                owner => IsType(owner, "Fusion.XR.Shared.Grabbing.NetworkGrabbable")) == part).ToArray();
        }
        int trackers = 0;
        foreach (var tracker in components.Where(c => IsType(c, "Fusion.Addons.Learning.LearnerActivityTracker")))
        {
            var bridge = tracker.gameObject.AddComponent<DroneAssemblyEventBridge>();
            bridge.partIdentity = Identity(tracker, "task"); bridge.recorder = recorder; bridge.activityTracker = tracker; trackers++;
        }
        var ids = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ResearchEntityIdentity>(true)).ToArray();
        if (ids.Select(id => id.entityId).Distinct().Count() != ids.Length) throw new InvalidOperationException("Duplicate identity in scene");
        // Some activities and all user rigs are network-spawned. Runtime binder handles them after connection.
        // Save under a NEW path. The original scene asset is not rewritten.
        if (!EditorSceneManager.SaveScene(scene, CaptureScene)) throw new IOException("Unable to save capture scene");
        AddBuildScene();
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Logs/assembly-capture");
        File.WriteAllText("Logs/assembly-capture/setup.txt", "scene=" + CaptureScene + "\nparts=" + parts.Length +
            "\nattachment_points=" + points.Length + "\nactivity_trackers=" + trackers + "\nidentities=" + ids.Length +
            "\nListener=" + (recorder.audioListener ? recorder.audioListener.name : "runtime-spawned") + "\nOriginal scene left unchanged. No VR operation has been performed by this setup.\n");
        Debug.Log("Drone capture scene prepared: " + parts.Length + " parts, " + points.Length + " points, " + trackers + " activities");
    }

    public static void Verify()
    {
        var scene = EditorSceneManager.OpenScene(CaptureScene);
        AddBuildScene();
        var bridges = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<DroneAssemblyEventBridge>(true)).ToArray();
        int subscriptions = 0;
        foreach (var bridge in bridges)
        {
            bridge.Bind();
            if (!string.IsNullOrEmpty(bridge.LastError)) throw new InvalidOperationException(bridge.name + ": " + bridge.LastError);
            subscriptions += bridge.Subscriptions;
            bridge.Unbind();
        }
        Directory.CreateDirectory("Logs/assembly-capture");
        File.WriteAllText("Logs/assembly-capture/verification.txt", "bridges=" + bridges.Length + "\nsubscriptions=" + subscriptions +
            "\nSDK callback binding checked in EditMode; this is NOT a real operation trace.\n");
        Debug.Log("Verified drone capture bridge subscriptions: " + subscriptions);
    }

    private static void AddBuildScene()
    {
        if (!EditorBuildSettings.scenes.Any(s => s.path == CaptureScene))
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(CaptureScene, true) }).ToArray();
    }
}
