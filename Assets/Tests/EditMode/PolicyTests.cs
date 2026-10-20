using NUnit.Framework;

namespace SceneTokenAudio.Tests
{
    public sealed class PolicyTests
    {
        private static SceneToken Token(float occlusion = 1f, float priority = 0.2f) => new SceneToken
        {
            timestamp = 10d, sourceId = "source-1", distance = 4f,
            relativeDirection = new TokenDirection(0f, 0f, 1f), occlusion = occlusion,
            taskPhase = TaskPhase.Preparation, priority = priority, timeToDeadline = -1f
        };
        [TestCase(0f, 1f, 22000f)]
        [TestCase(0.5f, 0.6f, 12000f)]
        [TestCase(1f, 0.2f, 2000f)]
        public void BaselineReflectsObstruction(float obstruction, float gain, float cutoff)
        {
            var actual = new BaselinePolicy().Evaluate(Token(obstruction));
            Assert.That(actual.occlusionGain, Is.EqualTo(gain).Within(0.0001f));
            Assert.That(actual.lowPassHz, Is.EqualTo(cutoff).Within(0.1f));
        }
        [Test]
        public void AlwaysClearIgnoresTaskAndPriority()
        {
            var first = Token();
            var second = Token(1f, 1f);
            second.taskPhase = TaskPhase.Emergency;
            second.timeToDeadline = 0f;
            var policy = new AlwaysClearPolicy();
            Assert.That(policy.Evaluate(first).occlusionGain, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(policy.Evaluate(second).occlusionGain, Is.EqualTo(policy.Evaluate(first).occlusionGain));
        }
        [Test]
        public void OrdinarySpeechMatchesBaseline()
        {
            var token = Token();
            token.taskPhase = TaskPhase.Verification;
            token.timeToDeadline = 0f;
            Assert.That(new AdaptivePolicy().Evaluate(token).occlusionGain, Is.EqualTo(new BaselinePolicy().Evaluate(token).occlusionGain));
        }
        [Test]
        public void ImportantInstructionGetsReliefAndDeadlineRaisesIt()
        {
            var token = Token(1f, 0.7f);
            var policy = new AdaptivePolicy();
            float ordinary = new BaselinePolicy().Evaluate(token).occlusionGain;
            float instruction = policy.Evaluate(token).occlusionGain;
            Assert.That(instruction, Is.GreaterThan(ordinary));
            token.timeToDeadline = 5f;
            Assert.That(policy.Evaluate(token).occlusionGain, Is.GreaterThan(instruction));
        }
        [Test]
        public void VerificationRaisesInstructionPriority()
        {
            var token = Token(1f, 0.7f);
            var policy = new AdaptivePolicy();
            float original = policy.Evaluate(token).occlusionGain;
            token.taskPhase = TaskPhase.Verification;
            Assert.That(policy.Evaluate(token).occlusionGain, Is.GreaterThan(original));
        }
        [Test]
        public void EmergencyPrioritizesClarity()
        {
            var token = Token();
            token.taskPhase = TaskPhase.Emergency;
            var actual = new AdaptivePolicy().Evaluate(token);
            Assert.That(actual.occlusionGain, Is.EqualTo(1f));
            Assert.That(actual.lowPassHz, Is.EqualTo(22000f));
        }
        [TestCase(AudioControlMode.Baseline)]
        [TestCase(AudioControlMode.AlwaysClear)]
        [TestCase(AudioControlMode.Adaptive)]
        public void UnobstructedSignalAndDirectionArePreserved(AudioControlMode mode)
        {
            var token = Token(0f, 1f);
            var actual = new AdaptiveAudioController().Evaluate(token, mode);
            Assert.That(actual.gainCorrection * actual.occlusionGain, Is.EqualTo(1f));
            Assert.That(actual.lowPassHz, Is.EqualTo(22000f));
            Assert.That(token.relativeDirection.z, Is.EqualTo(1f));
        }
        [Test]
        public void DistanceIsOwnedByRendererNotMultipliedAgain()
        {
            var token = Token();
            var controller = new AdaptiveAudioController();
            foreach (AudioControlMode mode in System.Enum.GetValues(typeof(AudioControlMode)))
            {
                var first = controller.Evaluate(token, mode);
                token.distance = 20f;
                Assert.That(controller.Evaluate(token, mode).gainCorrection, Is.EqualTo(first.gainCorrection));
            }
        }
        [Test]
        public void UnknownOcclusionUsesDocumentedFallback()
        {
            Assert.That(new BaselinePolicy().Evaluate(Token(-1f)).occlusionGain, Is.EqualTo(0.2f).Within(0.0001f));
        }
    }
}
