using NUnit.Framework;

namespace BrainlessLabs.Neon.Tests
{
    public class StatSystemTests
    {
        [Test]
        public void PlayerAndRunSheets_AreIndependent()
        {
            IStatSystem statSystem = new StatSystem();
            statSystem.Player.SetBase(StatId.DamageMultiplier, 1f);
            statSystem.Run.SetBase(StatId.DamageMultiplier, 5f);

            Assert.AreEqual(1f, statSystem.Player.GetValue(StatId.DamageMultiplier));
            Assert.AreEqual(5f, statSystem.Run.GetValue(StatId.DamageMultiplier));
        }

        [Test]
        public void Sheets_AreStableInstances()
        {
            IStatSystem statSystem = new StatSystem();

            Assert.AreSame(statSystem.Player, statSystem.Player);
            Assert.AreNotSame(statSystem.Player, statSystem.Run);
        }
    }
}
