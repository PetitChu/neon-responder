using NUnit.Framework;

namespace BrainlessLabs.Neon.Tests
{
    public class TestAssemblySmokeTests
    {
        [Test]
        public void TestAssembly_References_NeonRuntime()
        {
            // SceneType lives in BrainlessLabs.Neon — proves the asmdef reference resolves.
            Assert.IsNotNull(typeof(SceneType));
        }
    }
}
