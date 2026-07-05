using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BrainlessLabs.Neon.Simulation;

namespace BrainlessLabs.Neon.Tests
{
    public class HeroAssignmentTests
    {
        static NativeArray<HeroSlot> Heroes(params (int id, float2 pos, int cap)[] hs)
        {
            var a = new NativeArray<HeroSlot>(hs.Length, Allocator.Temp);
            for (int i = 0; i < hs.Length; i++) a[i] = new HeroSlot { Id = hs[i].id, Position = hs[i].pos, Cap = hs[i].cap };
            return a;
        }

        [Test]
        public void PicksNearestHeroWithAnOpenSlot()
        {
            var heroes = Heroes((1, new float2(1, 0), 4), (2, new float2(5, 0), 4));
            var counts = new NativeArray<int>(new[] { 0, 0 }, Allocator.Temp);
            try { Assert.AreEqual(0, HeroAssignment.PickNearestOpenHero(new float2(0, 0), heroes, counts)); }
            finally { heroes.Dispose(); counts.Dispose(); }
        }

        [Test]
        public void SkipsFullHeroesEvenIfNearer()
        {
            var heroes = Heroes((1, new float2(1, 0), 4), (2, new float2(5, 0), 4));
            var counts = new NativeArray<int>(new[] { 4, 0 }, Allocator.Temp); // hero 1 full
            try { Assert.AreEqual(1, HeroAssignment.PickNearestOpenHero(new float2(0, 0), heroes, counts)); }
            finally { heroes.Dispose(); counts.Dispose(); }
        }

        [Test]
        public void ReturnsMinusOneWhenAllFull()
        {
            var heroes = Heroes((1, new float2(1, 0), 2));
            var counts = new NativeArray<int>(new[] { 2 }, Allocator.Temp);
            try { Assert.AreEqual(-1, HeroAssignment.PickNearestOpenHero(new float2(0, 0), heroes, counts)); }
            finally { heroes.Dispose(); counts.Dispose(); }
        }

        [Test]
        public void IndexOfHero_finds_and_misses()
        {
            var heroes = Heroes((7, float2.zero, 3), (9, new float2(2, 0), 3));
            try
            {
                Assert.AreEqual(1, HeroAssignment.IndexOfHero(9, heroes));
                Assert.AreEqual(-1, HeroAssignment.IndexOfHero(42, heroes));
            }
            finally { heroes.Dispose(); }
        }
    }
}
