using Unity.Burst;
using Unity.Entities;

namespace BrainlessLabs.Neon.Simulation
{
    /// <summary>Consumes bridge damage/kill commands. (Spec's SwarmChipSystem folded in — see plan deviations.)</summary>
    [BurstCompile]
    public partial struct SwarmDamageSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SwarmWorldState>();
            state.RequireForUpdate<SwarmDamageCommand>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var damageBuffer = SystemAPI.GetSingletonBuffer<SwarmDamageCommand>();
            for (int i = 0; i < damageBuffer.Length; i++)
            {
                var command = damageBuffer[i];
                if (!state.EntityManager.Exists(command.Target)) continue;
                if (!state.EntityManager.HasComponent<SwarmHealth>(command.Target)) continue;

                var health = state.EntityManager.GetComponentData<SwarmHealth>(command.Target);
                health.Current -= command.Amount;
                // Chip pushes toward Finish-Ready but never kills (spec §5.1):
                // without this floor, chip (8) skips the ready band (≤6 of 24)
                // and the loop starves. Only verbs (IsChip = 0) finish chaff.
                if (command.IsChip == 1 && health.Current < 1) health.Current = 1;
                state.EntityManager.SetComponentData(command.Target, health);
            }
            damageBuffer.Clear();

            var killBuffer = SystemAPI.GetSingletonBuffer<SwarmKillCommand>();
            for (int i = 0; i < killBuffer.Length; i++)
            {
                var command = killBuffer[i];
                if (!state.EntityManager.Exists(command.Target)) continue;
                if (!state.EntityManager.HasComponent<SwarmHealth>(command.Target)) continue;

                var health = state.EntityManager.GetComponentData<SwarmHealth>(command.Target);
                health.Current = 0;
                state.EntityManager.SetComponentData(command.Target, health);
            }
            killBuffer.Clear();
        }
    }
}
