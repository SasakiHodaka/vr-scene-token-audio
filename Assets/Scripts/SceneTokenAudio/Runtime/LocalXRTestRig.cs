using UnityEngine;
using UnityEngine.XR;

namespace SceneTokenAudio
{
    // Minimal local tracking and locomotion for the test scenes; not a replacement for Fusion's rig.
    public sealed class LocalXRTestRig : MonoBehaviour
    {
        public Transform head, leftHand, rightHand;
        public SceneTokenAudioManager manager;
        public TaskStateController task;
        public Transform wall;
        public float moveSpeed = 1.5f;
        private bool modeHeld, priorityHeld, wallHeld;
        private void Update()
        {
            Track(XRNode.Head, head);
            Track(XRNode.LeftHand, leftHand);
            Track(XRNode.RightHand, rightHand);
            InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (left.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis) && head)
            {
                Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
                Vector3 rightDirection = Vector3.ProjectOnPlane(head.right, Vector3.up).normalized;
                transform.position += (forward * axis.y + rightDirection * axis.x) * (moveSpeed * Time.unscaledDeltaTime);
            }
            bool modePressed = right.TryGetFeatureValue(CommonUsages.primaryButton, out bool modeValue) && modeValue;
            if (modePressed && !modeHeld && manager) manager.mode = (AudioControlMode)(((int)manager.mode + 1) % 3);
            modeHeld = modePressed;
            bool priorityPressed = left.TryGetFeatureValue(CommonUsages.primaryButton, out bool priorityValue) && priorityValue;
            if (priorityPressed && !priorityHeld && task) task.priority = task.priority < 0.6f ? 0.85f : 0.2f;
            priorityHeld = priorityPressed;
            bool wallPressed = right.TryGetFeatureValue(CommonUsages.secondaryButton, out bool wallValue) && wallValue;
            if (wallPressed && !wallHeld && wall)
            {
                Vector3 position = wall.position;
                position.x = Mathf.Abs(position.x) < 1f ? 4f : 0f;
                wall.position = position;
            }
            wallHeld = wallPressed;
        }
        private static void Track(XRNode node, Transform target)
        {
            if (!target) return;
            InputDevice device = InputDevices.GetDeviceAtXRNode(node);
            if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position)) target.localPosition = position;
            if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation)) target.localRotation = rotation;
        }
    }
}
