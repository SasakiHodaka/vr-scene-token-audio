using System.IO;
using SceneTokenAudio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneTokenTestSceneBuilder
{
    [MenuItem("Tools/Scene Token Audio/Create Test Scenes")]
    public static void CreateAll()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory("Assets/Scenes");
        Create("BasicAudioTest", AudioControlMode.Baseline, false);
        Create("SceneTokenTest", AudioControlMode.Baseline, false);
        Create("AdaptiveAudioTest", AudioControlMode.Adaptive, false);
        Create("NetworkAudioTest", AudioControlMode.Baseline, true);
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene("Assets/Scenes/BasicAudioTest.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/SceneTokenTest.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/AdaptiveAudioTest.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/NetworkAudioTest.unity", false)
        };
        AssetDatabase.SaveAssets();
        Debug.Log("Scene Token test scenes generated. NetworkAudioTest is a local scaffold, not a Photon test.");
    }
    private static GameObject Cube(string name, Vector3 position, Vector3 scale)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name = name;
        item.transform.position = position;
        item.transform.localScale = scale;
        return item;
    }
    private static void Create(string name, AudioControlMode mode, bool networkScaffold)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var rig = new GameObject("ListenerRig");
        var listener = new GameObject("Listener", typeof(Camera), typeof(AudioListener));
        listener.tag = "MainCamera";
        listener.transform.SetParent(rig.transform, false);
        listener.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        listener.GetComponent<Camera>().backgroundColor = new Color(0.13f, 0.16f, 0.2f);
        listener.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        var left = Cube("LeftController", new Vector3(-0.25f, 1.2f, 0.4f), Vector3.one * 0.1f);
        var right = Cube("RightController", new Vector3(0.25f, 1.2f, 0.4f), Vector3.one * 0.1f);
        left.transform.SetParent(rig.transform, true);
        right.transform.SetParent(rig.transform, true);
        Object.DestroyImmediate(left.GetComponent<Collider>());
        Object.DestroyImmediate(right.GetComponent<Collider>());
        var wall = Cube("Wall", new Vector3(0f, 1.6f, 2f), new Vector3(2f, 3.2f, 0.25f));
        Cube("Floor", new Vector3(0f, -0.1f, 5f), new Vector3(20f, 0.2f, 20f));
        var light = new GameObject("Directional Light", typeof(Light));
        light.GetComponent<Light>().type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var sourceObject = Cube("AudioSource", new Vector3(0f, 1.6f, 4f), Vector3.one * 0.25f);
        var audio = sourceObject.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.volume = 0.5f;
        audio.spatialBlend = 1f;
        audio.rolloffMode = AudioRolloffMode.Logarithmic;
        audio.minDistance = 1f;
        audio.maxDistance = 30f;
        audio.dopplerLevel = 0f;
        sourceObject.AddComponent<GeneratedTestTone>();
        var renderer = sourceObject.AddComponent<SpatialAudioRendererAdapter>();
        renderer.audioSource = audio;
        var task = new GameObject("TaskStateController").AddComponent<TaskStateController>();
        var system = new GameObject("SceneTokenAudioSystem");
        var detector = system.AddComponent<OcclusionDetector>();
        var collector = sourceObject.AddComponent<SceneStateCollector>();
        collector.listener = listener.transform;
        collector.audioSource = audio;
        collector.sourceId = "test-source-1";
        collector.task = task;
        collector.occlusionDetector = detector;
        collector.rendererAdapter = renderer;
        var logger = system.AddComponent<AudioEvaluationLogger>();
        var manager = system.AddComponent<SceneTokenAudioManager>();
        manager.sources = new[] { collector };
        manager.logger = logger;
        manager.mode = mode;
        var ui = system.AddComponent<SceneTokenAudioDebugUI>();
        ui.manager = manager;
        ui.task = task;
        ui.wall = wall.transform;
        ui.source = sourceObject.transform;
        var xr = rig.AddComponent<LocalXRTestRig>();
        xr.head = listener.transform;
        xr.leftHand = left.transform;
        xr.rightHand = right.transform;
        xr.manager = manager;
        xr.task = task;
        xr.wall = wall.transform;
        if (networkScaffold) new GameObject("PHOTON_NOT_CONNECTED__LOCAL_SCAFFOLD_ONLY");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/" + name + ".unity");
    }
}
