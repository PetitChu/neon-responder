using Unity.Entities;
using UnityEngine;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Unified target handle across both worlds: an ECS chaff entity OR a
    /// MonoBehaviour hero-tier GameObject. The engagement spine never knows which.
    /// </summary>
    public readonly struct TargetRef
    {
        public readonly Entity Entity;
        public readonly GameObject GameObject;
        public readonly Vector2 Position;

        public bool IsChaff => GameObject == null && Entity != Entity.Null;
        public bool IsValid => GameObject != null || Entity != Entity.Null;

        public TargetRef(Entity entity, Vector2 position)
        {
            Entity = entity;
            GameObject = null;
            Position = position;
        }

        public TargetRef(GameObject gameObject, Vector2 position)
        {
            Entity = Entity.Null;
            GameObject = gameObject;
            Position = position;
        }
    }
}
