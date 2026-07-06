using NUnit.Framework;

namespace BrainlessLabs.Neon.Tests
{
    public class WhiffFxTests
    {
        [Test] public void PeaksAtStart() => Assert.AreEqual(1f, WhiffFx.WeightAt(0f, 0.3f), 0.001f);
        [Test] public void ZeroAtEnd() => Assert.AreEqual(0f, WhiffFx.WeightAt(0.3f, 0.3f), 0.001f);
        [Test] public void HalfwayIsHalf() => Assert.AreEqual(0.5f, WhiffFx.WeightAt(0.15f, 0.3f), 0.001f);
        [Test] public void ClampsPastEnd() => Assert.AreEqual(0f, WhiffFx.WeightAt(1f, 0.3f), 0.001f);
        [Test] public void ZeroDurationIsSafe() => Assert.AreEqual(0f, WhiffFx.WeightAt(0f, 0f), 0.001f);
    }
}
