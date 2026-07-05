using NUnit.Framework;
using UnityEngine;
using BrainlessLabs.Neon;

namespace BrainlessLabs.Neon.Tests
{
    public class SwarmDensityTests
    {
        [Test]
        public void No_curve_falls_back_to_flat_cap_times_nastiness()
        {
            int cap = SwarmDensity.ResolveChaffCap(flatCap: 100, curve: null, progression: 0.5f, nastiness: 1.2f);
            Assert.AreEqual(120, cap);
        }

        [Test]
        public void Curve_defines_absolute_cap_over_progression()
        {
            var curve = new AnimationCurve(new Keyframe(0f, 20f), new Keyframe(1f, 150f));
            int lo = SwarmDensity.ResolveChaffCap(999, curve, 0f, 1f);
            int hi = SwarmDensity.ResolveChaffCap(999, curve, 1f, 1f);
            Assert.AreEqual(20, lo);
            Assert.AreEqual(150, hi);
        }

        [Test]
        public void Result_is_clamped_to_the_proxy_pool_ceiling()
        {
            var curve = new AnimationCurve(new Keyframe(0f, 150f), new Keyframe(1f, 150f));
            Assert.AreEqual(150, SwarmDensity.ResolveChaffCap(999, curve, 1f, 2f)); // 150*2 → clamp 150
        }
    }
}
