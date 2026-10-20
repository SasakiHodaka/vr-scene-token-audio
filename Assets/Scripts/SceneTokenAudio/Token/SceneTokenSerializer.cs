using System;
using UnityEngine;

namespace SceneTokenAudio
{
    public static class SceneTokenSerializer
    {
        public static string Serialize(SceneToken token)
        {
            Validate(token);
            return JsonUtility.ToJson(token);
        }
        public static SceneToken Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Empty token JSON", nameof(json));
            // Required fields distinguish a missing observation from valid zero-valued observations.
            foreach (string field in new[] { "timestamp", "sourceId", "distance", "relativeDirection", "occlusion", "taskPhase", "priority", "timeToDeadline" })
                if (json.IndexOf("\"" + field + "\"", StringComparison.Ordinal) < 0)
                    throw new ArgumentException("Missing token field: " + field, nameof(json));
            SceneToken token = JsonUtility.FromJson<SceneToken>(json);
            Validate(token);
            return token;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public static void Validate(SceneToken token)
        {
            float directionSquared = token.relativeDirection.x * token.relativeDirection.x + token.relativeDirection.y * token.relativeDirection.y + token.relativeDirection.z * token.relativeDirection.z;
            if (string.IsNullOrWhiteSpace(token.sourceId) || token.sourceId.Length > 128 ||
                double.IsNaN(token.timestamp) || double.IsInfinity(token.timestamp) || token.timestamp < 0d ||
                !Finite(token.distance) || token.distance < 0f || !Finite(directionSquared) ||
                (token.distance > 0.0001f ? System.Math.Abs(directionSquared - 1f) > 0.002f : directionSquared > 0.002f) ||
                !Finite(token.occlusion) || (token.occlusion != -1f && (token.occlusion < 0f || token.occlusion > 1f)) ||
                !Enum.IsDefined(typeof(TaskPhase), token.taskPhase) || !Finite(token.priority) || token.priority < 0f || token.priority > 1f ||
                !Finite(token.timeToDeadline) || (token.timeToDeadline < 0f && token.timeToDeadline != -1f))
                throw new ArgumentException("Invalid Scene Token observation");
        }
    }
}
