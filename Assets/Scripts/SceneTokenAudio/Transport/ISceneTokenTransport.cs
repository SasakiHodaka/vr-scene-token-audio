using System;

namespace SceneTokenAudio
{
    public interface ISceneTokenTransport
    {
        event Action<SceneToken> Received;
        void Publish(SceneToken token);
    }
}
