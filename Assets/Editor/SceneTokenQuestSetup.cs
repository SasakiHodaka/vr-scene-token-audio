using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

public static class SceneTokenQuestSetup
{
    [MenuItem("Tools/Scene Token Audio/Configure Quest OpenXR")]
    public static void Configure()
    {
        const string path = "Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset";
        Directory.CreateDirectory("Assets/XR/Settings");
        AssetDatabase.Refresh();
        if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget perTarget))
        {
            perTarget = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(path);
            if (!perTarget)
            {
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, path);
            }
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
        }
        if (!perTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
            perTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
        var settings = perTarget.SettingsForBuildTarget(BuildTargetGroup.Android);
        settings.InitManagerOnStart = true;
        if (!XRPackageMetadataStore.AssignLoader(settings.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", BuildTargetGroup.Android))
            throw new InvalidOperationException("Cannot assign Android OpenXR loader.");
        var openXR = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        if (!openXR) throw new InvalidOperationException("OpenXR Android settings were not generated.");
        foreach (OpenXRFeature feature in openXR.GetFeatures<OpenXRFeature>())
        {
            string featureName = feature.GetType().Name;
            if (featureName == "MetaQuestFeature" || featureName == "OculusTouchControllerProfile" || featureName == "MetaQuestTouchPlusControllerProfile")
                feature.enabled = true;
        }
        openXR.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
        PlayerSettings.companyName = "SceneTokenResearch";
        PlayerSettings.productName = "VR Scene Token Audio";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.scenetokenresearch.vrscenetokenaudio");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        PlayerSettings.colorSpace = ColorSpace.Linear;
        // OpenXR requires the Input System. No legacy Input.Get* calls are used by this module.
        var playerSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var handler = playerSettings.FindProperty("activeInputHandler");
        if (handler != null) { handler.intValue = 1; playerSettings.ApplyModifiedPropertiesWithoutUndo(); }
        EditorUtility.SetDirty(settings);
        EditorUtility.SetDirty(settings.Manager);
        EditorUtility.SetDirty(perTarget);
        EditorUtility.SetDirty(openXR);
        AssetDatabase.SaveAssets();
        Debug.Log("Quest OpenXR configured for Android. Device validation/build remains required. Standalone XR stays unconfigured for desktop tests.");
    }
}
