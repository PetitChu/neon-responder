using NUnit.Framework;

namespace BrainlessLabs.Neon.Tests
{
    public class FeelProfileTests
    {
        private static FeelConfig Config()
        {
            // Distinct shake intensities so selection is unambiguous.
            var punch = new HitProfile(0.08f, 0.04f, 0.10f, 0.15f);
            var kick = new HitProfile(0.06f, 0.06f, 0.16f, 0.20f);
            var weapon = new HitProfile(0.05f, 0.07f, 0.20f, 0.22f);
            var thrown = new HitProfile(0.03f, 0.10f, 0.35f, 0.35f);
            var def = new HitProfile(0.10f, 0.03f, 0.08f, 0.12f);
            var finish = new HitProfile(0.04f, 0.09f, 0.28f, 0.30f);
            var tierUp = new HitProfile(0.15f, 0.08f, 0.22f, 0.30f);
            return new FeelConfig(punch, kick, weapon, thrown, def, finish, tierUp, 0.35f, 0.25f);
        }

        [Test]
        public void Punch_And_GrabPunch_MapToPunch()
        {
            var c = Config();
            Assert.AreEqual(c.Punch.ShakeIntensity, c.ProfileForVerb(ATTACKTYPE.PUNCH).ShakeIntensity, 0.0001f);
            Assert.AreEqual(c.Punch.ShakeIntensity, c.ProfileForVerb(ATTACKTYPE.GRABPUNCH).ShakeIntensity, 0.0001f);
        }

        [Test]
        public void Kick_And_GrabKick_MapToKick()
        {
            var c = Config();
            Assert.AreEqual(c.Kick.ShakeIntensity, c.ProfileForVerb(ATTACKTYPE.KICK).ShakeIntensity, 0.0001f);
            Assert.AreEqual(c.Kick.ShakeIntensity, c.ProfileForVerb(ATTACKTYPE.GRABKICK).ShakeIntensity, 0.0001f);
        }

        [Test]
        public void Throw_IsTheBiggestHit()
        {
            var c = Config();
            var thrown = c.ProfileForVerb(ATTACKTYPE.GRABTHROW);
            Assert.Greater(thrown.ShakeIntensity, c.ProfileForVerb(ATTACKTYPE.PUNCH).ShakeIntensity);
            Assert.Greater(thrown.ShakeIntensity, c.ProfileForVerb(ATTACKTYPE.KICK).ShakeIntensity);
            Assert.Greater(thrown.ShakeIntensity, c.ProfileForVerb(ATTACKTYPE.WEAPON).ShakeIntensity);
        }

        [Test]
        public void Weapon_MapsToWeapon()
        {
            var c = Config();
            Assert.AreEqual(c.Weapon.ShakeIntensity, c.ProfileForVerb(ATTACKTYPE.WEAPON).ShakeIntensity, 0.0001f);
        }

        [Test]
        public void UnknownVerb_FallsBackToDefault()
        {
            var c = Config();
            Assert.AreEqual(c.DefaultHit.ShakeIntensity, c.ProfileForVerb(ATTACKTYPE.NONE).ShakeIntensity, 0.0001f);
            Assert.AreEqual(c.DefaultHit.ShakeIntensity, c.ProfileForVerb(ATTACKTYPE.GROUNDPOUND).ShakeIntensity, 0.0001f);
        }
    }
}
