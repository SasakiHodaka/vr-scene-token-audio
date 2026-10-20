namespace SceneTokenAudio
{
    public static class SceneTokenGenerator
    {
        public static SceneToken Generate(SceneState state)
        {
            var direction = state.relativeDirection.normalized;
            return new SceneToken
            {
                timestamp = state.timestamp,
                sourceId = state.sourceId,
                distance = state.distance,
                relativeDirection = new TokenDirection(direction.x, direction.y, direction.z),
                occlusion = state.occlusion,
                taskPhase = state.taskPhase,
                priority = state.priority,
                timeToDeadline = state.timeToDeadline
            };
        }
    }
}
