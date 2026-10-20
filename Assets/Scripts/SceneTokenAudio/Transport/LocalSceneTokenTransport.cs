using System;

namespace SceneTokenAudio
{
    // Listener-local dispatch only. This is intentionally not a network implementation.
    public sealed class LocalSceneTokenTransport : ISceneTokenTransport
    {
        public event Action<SceneToken> Received;
        public void Publish(SceneToken token)
        {
            SceneTokenSerializer.Validate(token);
            Received?.Invoke(token);
        }
    }
}
