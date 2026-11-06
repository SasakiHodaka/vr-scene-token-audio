using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace SceneTokenAudio.Research
{
    // SDK-independent binding to the inspected Fusion sample APIs. Does not change SDK control logic.
    public sealed class DroneAssemblyEventBridge : MonoBehaviour
    {
        public AssemblyCaptureRecorder recorder;
        public Component networkGrabbable;
        public Component activityTracker;
        public Component[] attachmentPoints = new Component[0];
        public ResearchEntityIdentity partIdentity;
        public string LastError { get; private set; }
        public int Subscriptions { get; private set; }
        private readonly List<Action> unsubscribe = new List<Action>();
        private readonly Dictionary<Component, string> related = new Dictionary<Component, string>();
        private readonly HashSet<string> attachedPairs = new HashSet<string>();
        private string lastActor = "unknown", attempt = "not_applicable", lastActivity;
        private string lastActorAudio = "unknown";
        private int attemptNumber;

        private void Start() => Bind();
        private void OnEnable() { if (started) Bind(); }
        private bool started;
        private void OnDisable() => Unbind();

        public void Bind()
        {
            started = true;
            Unbind();
            LastError = null;
            if (!recorder || !partIdentity || string.IsNullOrWhiteSpace(partIdentity.entityId))
            { LastError = "Recorder and authored part ID are required"; return; }
            try
            {
                if (networkGrabbable)
                {
                    ListenArgument(Read(networkGrabbable, "onDidGrab"), OnGrab);
                    Listen(Read(networkGrabbable, "onDidUngrab"), OnRelease);
                }
                foreach (var point in attachmentPoints ?? new Component[0])
                {
                    if (!point) continue;
                    var captured = point;
                    Listen(Read(point, "onRegisterAttachment"), () => OnAttachment(captured, true));
                    Listen(Read(point, "onRegisterReverseAttachment"), () => OnAttachment(captured, true));
                    Listen(Read(point, "onUnregisterAttachment"), () => OnAttachment(captured, false));
                    Listen(Read(point, "onUnregisterReverseAttachment"), () => OnAttachment(captured, false));
                }
            }
            catch (Exception e) { LastError = e.Message; Unbind(); }
        }

        public static object Read(object source, string member)
        {
            if (source == null) return null;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            var type = source.GetType();
            var property = type.GetProperty(member, flags);
            return property != null ? property.GetValue(source) : type.GetField(member, flags)?.GetValue(source);
        }

        public static string Identity(Component source)
        {
            if (!source) return "unknown";
            var id = source.GetComponentInParent<ResearchEntityIdentity>();
            return id && !string.IsNullOrWhiteSpace(id.entityId) ? id.entityId : "unknown";
        }

        private void Listen(object callback, UnityAction action)
        {
            if (!(callback is UnityEvent ev)) throw new InvalidOperationException("Expected public UnityEvent callback");
            ev.AddListener(action); unsubscribe.Add(() => ev.RemoveListener(action)); Subscriptions++;
        }

        private void ListenArgument(object callback, Action<object> action)
        {
            if (callback == null) throw new InvalidOperationException("Missing onDidGrab callback");
            Type type = callback.GetType();
            while (type != null && !(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(UnityEvent<>))) type = type.BaseType;
            if (type == null) throw new InvalidOperationException("Expected UnityEvent<T> callback");
            var method = typeof(DroneAssemblyEventBridge).GetMethod(nameof(ListenTyped), BindingFlags.NonPublic | BindingFlags.Instance);
            method.MakeGenericMethod(type.GetGenericArguments()[0]).Invoke(this, new[] { callback, action });
        }

        private void ListenTyped<T>(UnityEvent<T> ev, Action<object> action)
        {
            UnityAction<T> listener = value => action(value);
            ev.AddListener(listener); unsubscribe.Add(() => ev.RemoveListener(listener)); Subscriptions++;
        }

        private void OnGrab(object grabber)
            => CaptureGrab(grabber, false);

        private void CaptureGrab(object grabber, bool initial)
        {
            // A new confirmed grab is an episode, NOT proof of an install retry.
            var component = grabber as Component;
            lastActor = Identity(component);
            string actorEvidence = "grabber_callback_identity";
            if (lastActor == "unknown" && component)
            {
                object owner = Read(Read(component, "Object"), "StateAuthority");
                if (owner != null && Read(owner, "IsRealPlayer") is bool real && real)
                { lastActor = "fusion-player/" + owner; actorEvidence = "grabber_state_authority"; }
            }
            var actorIdentity = component ? component.GetComponentInParent<ResearchEntityIdentity>() : null;
            lastActorAudio = actorIdentity ? actorIdentity.actorAudioSourceId : "unknown";
            if (component)
            {
                Component rig = null;
                foreach (var ancestor in component.GetComponentsInParent<Component>())
                    if (ancestor && ancestor.GetType().FullName == "Fusion.XR.Shared.Rig.NetworkRig") { rig = ancestor; break; }
                if (rig)
                    foreach (var child in rig.GetComponentsInChildren<Component>())
                        if (child && child.GetType().FullName == "Fusion.XR.Shared.Rig.NetworkHeadset")
                        { lastActorAudio = recorder.RegisterActorPose(lastActor, child.transform); break; }
            }
            attempt = initial ? "unknown" : partIdentity.entityId + "/grab-episode/" + (++attemptNumber);
            Emit(initial ? "grab_snapshot" : "grab_confirmed", lastActor, actorEvidence, "not_applicable",
                initial ? "network_grabbable.initial_state" : "network_grabbable.onDidGrab");
        }

        private void OnRelease()
            => Emit("release_observed", lastActor, "previous_confirmed_grabber", "not_applicable", "network_grabbable.onDidUngrab");

        private void OnAttachment(Component point, bool register, bool snapshot = false)
        {
            string local = Identity(point);
            string target;
            if (register)
            {
                target = Identity(Read(point, "RelatedPoint") as Component);
                if (target == "unknown")
                    target = Identity(Read(point, "AttachedPoint") as Component);
                related[point] = target;
            }
            else { related.TryGetValue(point, out target); related.Remove(point); }
            target = string.IsNullOrWhiteSpace(target) ? "unknown" : target;
            string pair = string.CompareOrdinal(local, target) < 0 ? local + "|" + target : target + "|" + local;
            if (register ? !attachedPairs.Add(pair) : !attachedPairs.Remove(pair)) return;
            // The sample callback does not carry an actor. Do not promote previous holder to confirmed installer.
            Emit(snapshot ? "attachment_snapshot" : register ? "attachment_registered" : "attachment_unregistered", "unknown", "actor_not_in_callback", target,
                "attachment_point", "point=" + local + ";previous_grabber=" + lastActor);
        }

        private void Update()
        {
            if (!activityTracker || !recorder) return;
            try
            {
                // Networked state is unavailable until the tracker is spawned.
                var networkObject = Read(activityTracker, "Object");
                if (networkObject == null || !(Read(networkObject, "IsValid") is bool valid) || !valid) return;
                string status = Read(activityTracker, "ActivityStatusOfLearner")?.ToString();
                if (string.IsNullOrWhiteSpace(status) || status == lastActivity) return;
                bool initial = lastActivity == null;
                lastActivity = status;
                recorder.Record(initial ? "activity_snapshot" : "activity_status_changed", "unknown", "actor_not_in_status",
                    partIdentity.entityId, "not_applicable", Identity(activityTracker), "not_applicable", "unknown",
                    "learner_activity_tracker", "status=" + status + ";progress=" + Convert.ToString(Read(activityTracker, "Progress"), System.Globalization.CultureInfo.InvariantCulture), transform);
            }
            catch (TargetInvocationException e) { LastError = "Activity observation unavailable: " + e.InnerException?.Message; }
        }

        private void Emit(string kind, string actor, string evidence, string target, string origin, string detail = "")
            => recorder.Record(kind, actor, evidence, partIdentity.entityId, target, "unknown", attempt, "unknown",
                origin, detail, partIdentity.transform, actor == lastActor ? lastActorAudio : "unknown");

        public void Unbind()
        {
            foreach (var remove in unsubscribe) remove(); unsubscribe.Clear(); Subscriptions = 0;
        }

        public void CaptureInitialState()
        {
            if (networkGrabbable && Read(networkGrabbable, "IsGrabbed") is bool grabbed && grabbed)
                CaptureGrab(Read(networkGrabbable, "CurrentGrabber"), true);
            foreach (var point in attachmentPoints ?? new Component[0])
                if (point && Read(point, "RelatedPoint") is Component relatedPoint && relatedPoint)
                    OnAttachment(point, true, true);
        }
    }
}
