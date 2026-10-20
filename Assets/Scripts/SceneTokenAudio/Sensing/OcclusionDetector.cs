using UnityEngine;

namespace SceneTokenAudio
{
    public sealed class OcclusionDetector : MonoBehaviour
    {
        public LayerMask obstacleLayers = ~0;
        // Binary direct-path obstruction, not an acoustic transmission simulation.
        public float Measure(Transform listener, Transform source)
        {
            if (!listener || !source) return -1f;
            Vector3 path = source.position - listener.position;
            if (path.sqrMagnitude < 0.000001f) return 0f;
            RaycastHit[] hits = Physics.RaycastAll(listener.position, path.normalized, path.magnitude,
                obstacleLayers, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform == source || hit.transform.IsChildOf(source) || source.IsChildOf(hit.transform)) continue;
                if (hit.transform == listener || hit.transform.IsChildOf(listener) || listener.IsChildOf(hit.transform)) continue;
                return 1f;
            }
            // Raycasts do not report a collider containing the origin. Check listener/source endpoints as well.
            foreach (Vector3 point in new[] { listener.position, source.position })
                foreach (Collider collider in Physics.OverlapSphere(point, 0.001f, obstacleLayers, QueryTriggerInteraction.Ignore))
                {
                    Transform other = collider.transform;
                    if (other == source || other.IsChildOf(source) || source.IsChildOf(other)) continue;
                    if (other == listener || other.IsChildOf(listener) || listener.IsChildOf(other)) continue;
                    return 1f;
                }
            return 0f;
        }
    }
}
