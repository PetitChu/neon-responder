using Unity.Collections;
using Unity.Mathematics;

namespace BrainlessLabs.Neon.Simulation
{
    /// <summary>Pure, Burst-compatible squad assignment: nearest hero with a free slot, and id lookup.</summary>
    public static class HeroAssignment
    {
        public static int PickNearestOpenHero(float2 pos, in NativeArray<HeroSlot> heroes, in NativeArray<int> followerCounts)
        {
            int best = -1;
            float bestSq = float.MaxValue;
            for (int i = 0; i < heroes.Length; i++)
            {
                if (heroes[i].Cap - followerCounts[i] <= 0) continue;
                float d = math.lengthsq(heroes[i].Position - pos);
                if (d < bestSq) { bestSq = d; best = i; }
            }
            return best;
        }

        public static int IndexOfHero(int heroId, in NativeArray<HeroSlot> heroes)
        {
            for (int i = 0; i < heroes.Length; i++)
                if (heroes[i].Id == heroId) return i;
            return -1;
        }
    }
}
