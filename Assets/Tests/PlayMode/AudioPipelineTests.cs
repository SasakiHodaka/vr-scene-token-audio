using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

namespace SceneTokenAudio.Tests
{
    public sealed class AudioPipelineTests
    {
        [UnityTest]
        public IEnumerator AdaptiveSceneRespondsToWallAndPolicySwitches()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/AdaptiveAudioTest.unity", LoadSceneMode.Additive);
            Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/AdaptiveAudioTest.unity");
            SceneTokenAudioManager manager = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var candidate = root.GetComponent<SceneTokenAudioManager>();
                if (candidate) manager = candidate;
            }
            Assert.That(manager, Is.Not.Null);
            var source = manager.sources[0];
            source.task.priority = 0.2f;
            source.task.taskPhase = TaskPhase.Preparation;
            manager.mode = AudioControlMode.Baseline;
            yield return new WaitForSecondsRealtime(0.15f);
            manager.Tick();
            Assert.That(manager.LatestToken.occlusion, Is.EqualTo(1f));
            Assert.That(manager.LatestParameters.occlusionGain, Is.EqualTo(0.2f).Within(0.0001f));
            manager.mode = AudioControlMode.AlwaysClear;
            manager.Tick();
            Assert.That(manager.LatestParameters.occlusionGain, Is.EqualTo(0.8f).Within(0.0001f));
            manager.mode = AudioControlMode.Adaptive;
            source.task.priority = 1f;
            manager.Tick();
            Assert.That(manager.LatestParameters.occlusionGain, Is.EqualTo(1f));
            var ui = manager.GetComponent<SceneTokenAudioDebugUI>();
            ui.wall.position += Vector3.right * 6f;
            Physics.SyncTransforms();
            manager.mode = AudioControlMode.Baseline;
            manager.Tick();
            Assert.That(manager.LatestToken.occlusion, Is.EqualTo(0f));
            Assert.That(manager.LatestParameters.occlusionGain, Is.EqualTo(1f));
            source.audioSource.transform.position += Vector3.right * 2f;
            manager.Tick();
            Assert.That(manager.LatestToken.relativeDirection.x, Is.GreaterThan(0f));
            string path = manager.logger.FilePath;
            var renderer = source.rendererAdapter;
            manager.sources = new SceneStateCollector[0];
            manager.Tick();
            Assert.That(renderer.IsControlling, Is.False);
            Assert.That(source.audioSource.volume, Is.EqualTo(0.5f));
            manager.SetControlEnabled(false);
            Assert.That(File.ReadAllLines(path).Length, Is.GreaterThan(4));
            yield return SceneManager.UnloadSceneAsync(scene);
            File.Delete(path);
        }
        [UnityTest]
        public IEnumerator RendererSmoothsAndRestoresExistingSettings()
        {
            var item = new GameObject("Audio", typeof(AudioSource), typeof(AudioLowPassFilter));
            var audio = item.GetComponent<AudioSource>();
            var filter = item.GetComponent<AudioLowPassFilter>();
            audio.volume = 0.65f;
            audio.pitch = 1.2f;
            audio.spatialBlend = 1f;
            filter.cutoffFrequency = 15000f;
            filter.enabled = false;
            var renderer = item.AddComponent<SpatialAudioRendererAdapter>();
            renderer.audioSource = audio;
            renderer.smoothingSeconds = 0.03f;
            renderer.Apply(new BaselinePolicy().Evaluate(new SceneToken { occlusion = 1f }));
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(audio.volume, Is.LessThan(0.3f));
            Assert.That(filter.cutoffFrequency, Is.LessThan(5000f));
            Assert.That(audio.pitch, Is.EqualTo(1.2f));
            Assert.That(audio.spatialBlend, Is.EqualTo(1f));
            renderer.enabled = false;
            Assert.That(audio.volume, Is.EqualTo(0.65f));
            Assert.That(filter.enabled, Is.False);
            Assert.That(filter.cutoffFrequency, Is.EqualTo(15000f));
            Object.Destroy(item);
            yield return null;
        }
        [UnityTest]
        public IEnumerator PipelineLogsAndStopRestoresAudio()
        {
            var listener = new GameObject("Listener");
            var item = new GameObject("Source", typeof(AudioSource));
            item.transform.position = Vector3.forward * 4f;
            var system = new GameObject("System");
            var audio = item.GetComponent<AudioSource>();
            audio.volume = 0.7f;
            var renderer = item.AddComponent<SpatialAudioRendererAdapter>();
            renderer.audioSource = audio;
            var task = item.AddComponent<TaskStateController>();
            var collector = item.AddComponent<SceneStateCollector>();
            collector.audioSource = audio;
            collector.listener = listener.transform;
            collector.task = task;
            collector.rendererAdapter = renderer;
            var logger = system.AddComponent<AudioEvaluationLogger>();
            var manager = system.AddComponent<SceneTokenAudioManager>();
            manager.sources = new[] { collector };
            manager.logger = logger;
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(manager.SamplesProcessed, Is.GreaterThanOrEqualTo(2));
            manager.mode = AudioControlMode.Adaptive;
            task.priority = 1f;
            manager.Tick();
            Assert.That(manager.LatestParameters.occlusionGain, Is.EqualTo(1f));
            manager.SetControlEnabled(false);
            Assert.That(audio.volume, Is.EqualTo(0.7f));
            Assert.That(renderer.IsControlling, Is.False);
            string path = logger.FilePath;
            Assert.That(File.Exists(path), Is.True);
            string[] lines = File.ReadAllLines(path);
            Assert.That(lines.Length, Is.GreaterThan(2));
            Assert.That(lines[0], Is.EqualTo(AudioEvaluationLogger.Header));
            Object.Destroy(system); Object.Destroy(item); Object.Destroy(listener);
            File.Delete(path); // Only the unique log created by this test.
            yield return null;
        }
    }
}
