using UnityEngine;

namespace SceneTokenAudio.Research
{
    public sealed class ResearchEntityIdentity : MonoBehaviour
    {
        // Assigned in the scene, never derived from a name/category/runtime instance ID.
        public string entityId;
        public string actorAudioSourceId;
    }
}
