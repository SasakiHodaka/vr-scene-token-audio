using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SceneTokenAudio.Research
{
    // Learning activities/rigs may spawn only after joining. Bind those without editing SDK prefabs.
    public sealed class DroneCaptureRuntimeBinder : MonoBehaviour
    {
        public AssemblyCaptureRecorder recorder;
        public int BoundParts { get; private set; }
        public int BoundActivities { get; private set; }
        private float nextScan;
        private readonly HashSet<Component> bound = new HashSet<Component>();

        public static bool IsType(Component component, string name)
        {
            if (!component) return false;
            for (Type t = component.GetType(); t != null; t = t.BaseType) if (t.FullName == name) return true;
            return false;
        }

        private static bool Spawned(Component component)
        {
            var networkObject = DroneAssemblyEventBridge.Read(component, "Object") as UnityEngine.Object;
            return networkObject && DroneAssemblyEventBridge.Read(networkObject, "IsValid") is bool valid && valid;
        }

        private static ResearchEntityIdentity Identity(Component component, string kind)
        {
            var identity = component.GetComponent<ResearchEntityIdentity>();
            if (!identity) identity = component.gameObject.AddComponent<ResearchEntityIdentity>();
            if (string.IsNullOrWhiteSpace(identity.entityId))
                identity.entityId = kind + "/fusion/" + DroneAssemblyEventBridge.Read(component, "Id");
            return identity;
        }

        private void Update()
        {
            if (!recorder || Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + 0.2f;
            Scan();
        }

        public void Scan()
        {
            var components = FindObjectsByType<Component>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(c => c).ToArray();
            var head = components.FirstOrDefault(c => IsType(c, "Fusion.XR.Shared.Rig.HardwareHeadset"));
            // Replace the lobby camera when the tracked local rig appears.
            if (head) recorder.audioListener = head.transform;
            else if (!recorder.audioListener || !recorder.audioListener.gameObject.activeInHierarchy)
                recorder.audioListener = Camera.main ? Camera.main.transform : null;
            foreach (var point in components.Where(c => IsType(c, "Fusion.Addons.StructureCohesion.AttachmentPoint") && Spawned(c)))
                Identity(point, "attachment-point");
            bound.RemoveWhere(c => !c);
            foreach (var component in components)
            {
                bool part = IsType(component, "Fusion.XR.Shared.Grabbing.NetworkGrabbable");
                bool activity = IsType(component, "Fusion.Addons.Learning.LearnerActivityTracker");
                if ((!part && !activity) || !Spawned(component)) continue;
                var bridge = component.GetComponents<DroneAssemblyEventBridge>().FirstOrDefault(b =>
                    part ? b.networkGrabbable == component : b.activityTracker == component);
                var points = part ? components.Where(p => IsType(p, "Fusion.Addons.StructureCohesion.AttachmentPoint") && Spawned(p) &&
                    p.GetComponentsInParent<Component>().FirstOrDefault(owner => IsType(owner, "Fusion.XR.Shared.Grabbing.NetworkGrabbable")) == component).ToArray()
                    : Array.Empty<Component>();
                if (bound.Contains(component) && bridge)
                {
                    if (part) bridge.RefreshAttachmentPoints(points);
                    continue;
                }
                if (!bridge)
                {
                    bridge = component.gameObject.AddComponent<DroneAssemblyEventBridge>();
                    bridge.recorder = recorder; bridge.partIdentity = Identity(component, part ? "part" : "task");
                    if (part)
                    {
                        bridge.networkGrabbable = component;
                        bridge.attachmentPoints = points;
                    }
                    else bridge.activityTracker = component;
                }
                bridge.Bind();
                if (!string.IsNullOrEmpty(bridge.LastError)) continue;
                if (part) bridge.RefreshAttachmentPoints(points);
                bridge.CaptureInitialState();
                bound.Add(component);
                if (part) BoundParts++; else BoundActivities++;
            }
        }
    }
}
