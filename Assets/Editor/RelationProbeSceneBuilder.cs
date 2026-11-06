using System.Linq;
using SceneTokenAudio.Research;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RelationProbeSceneBuilder
{
    [MenuItem("Tools/Scene Token Research/Create Local Relation Demo")]
    public static void Create()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Camera", typeof(Camera));
        camera.transform.position = new Vector3(3f, 2.5f, -2f);
        camera.transform.LookAt(new Vector3(0f, 1f, 2f));
        camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        var light = new GameObject("Light", typeof(Light));
        light.GetComponent<Light>().type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        new GameObject("RelationProbe").AddComponent<RelationProbeDemo>();
        const string path = "Assets/Scenes/RelationProbe.unity";
        EditorSceneManager.SaveScene(scene, path);
        if (!EditorBuildSettings.scenes.Any(s => s.path == path))
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(path, true) }).ToArray();
        AssetDatabase.SaveAssets();
    }
}
