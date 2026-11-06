using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace SceneTokenAudio
{
    public sealed class SceneTokenAudioManager : MonoBehaviour
    {
        public SceneStateCollector[] sources = new SceneStateCollector[0];
        public AudioEvaluationLogger logger;
        public AudioControlMode mode = AudioControlMode.Baseline;
        [Range(1f, 120f)] public float updateHz = 20f;
        public bool controlEnabled = true;
        public bool observationEnabled = true;
        public SceneToken LatestToken { get; private set; }
        public AudioControlParameters LatestParameters { get; private set; }
        public double LatestProcessingMs { get; private set; }
        public int SamplesProcessed { get; private set; }
        public string LastError { get; private set; }
        public readonly AdaptiveAudioController Controller = new AdaptiveAudioController();
        private ISceneTokenTransport transport;
        private readonly Dictionary<string, SceneStateCollector> routes = new Dictionary<string, SceneStateCollector>();
        private double nextSample;
        private bool wasControlling;
        private SceneState currentState;
        private double sampleStart, collectionMs;
        private static double Milliseconds => Stopwatch.GetTimestamp() * 1000d / Stopwatch.Frequency;

        private void OnEnable()
        {
            transport = new LocalSceneTokenTransport();
            transport.Received += Receive;
            nextSample = 0d;
        }
        private void Update()
        {
            if (!observationEnabled)
            {
                RestoreAll();
                if (logger) logger.Close();
                wasControlling = false;
                return;
            }
            if (wasControlling && !controlEnabled) RestoreAll();
            wasControlling = controlEnabled;
            double now = Time.realtimeSinceStartupAsDouble;
            if (now < nextSample) return;
            // Skip missed ticks instead of producing bursts of stale observations.
            nextSample = now + 1d / Mathf.Clamp(updateHz, 1f, 120f);
            Tick();
        }
        public void Tick()
        {
            if (!observationEnabled || transport == null) return;
            if (!controlEnabled) RestoreAll();
            foreach (var oldSource in routes.Values)
                if (oldSource && (!oldSource.isActiveAndEnabled || System.Array.IndexOf(sources, oldSource) < 0) && oldSource.rendererAdapter)
                    oldSource.rendererAdapter.Restore();
            routes.Clear();
            foreach (var source in sources)
            {
                if (!source || !source.isActiveAndEnabled) continue;
                if (string.IsNullOrWhiteSpace(source.sourceId) || routes.ContainsKey(source.sourceId))
                {
                    LastError = "Missing or duplicate sourceId; no samples applied.";
                    RestoreAll();
                    return;
                }
                routes.Add(source.sourceId, source);
            }
            LastError = null;
            foreach (var source in sources)
            {
                if (!source || !source.isActiveAndEnabled) { if (source && source.rendererAdapter) source.rendererAdapter.Restore(); continue; }
                sampleStart = Milliseconds;
                if (!source.TryCollect(out currentState)) { if (source.rendererAdapter) source.rendererAdapter.Restore(); continue; }
                collectionMs = Milliseconds - sampleStart;
                try { transport.Publish(SceneTokenGenerator.Generate(currentState)); }
                catch (System.ArgumentException exception)
                {
                    LastError = exception.Message;
                    if (source.rendererAdapter) source.rendererAdapter.Restore();
                }
            }
        }
        private void Receive(SceneToken token)
        {
            if (!routes.TryGetValue(token.sourceId, out var source)) return;
            AudioControlParameters parameters = default;
            double controlMs = 0d;
            bool applied = false;
            if (controlEnabled && source.rendererAdapter)
            {
                double controlStart = Milliseconds;
                parameters = Controller.Evaluate(token, mode);
                controlMs = Milliseconds - controlStart;
                applied = source.rendererAdapter.Apply(parameters);
            }
            LatestToken = token;
            LatestParameters = parameters;
            LatestProcessingMs = Milliseconds - sampleStart;
            SamplesProcessed++;
            if (logger)
            {
                if (applied) logger.Write(currentState, token, mode, parameters, source.rendererAdapter, collectionMs, controlMs, LatestProcessingMs);
                else logger.WriteObservation(currentState, token, collectionMs, LatestProcessingMs);
            }
        }
        public void SetControlEnabled(bool value)
        {
            controlEnabled = value;
            if (!value) { RestoreAll(); if (logger) logger.Close(); }
            nextSample = 0d;
        }
        public void SetObservationEnabled(bool value)
        {
            observationEnabled = value;
            if (!value) { RestoreAll(); if (logger) logger.Close(); }
            nextSample = 0d;
        }
        private void RestoreAll()
        {
            foreach (var source in sources) if (source && source.rendererAdapter) source.rendererAdapter.Restore();
            foreach (var source in routes.Values) if (source && source.rendererAdapter) source.rendererAdapter.Restore();
        }
        private void OnDisable()
        {
            RestoreAll();
            if (transport != null) transport.Received -= Receive;
            transport = null;
            if (logger) logger.Close();
        }
    }
}
