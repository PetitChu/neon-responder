using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using BrainlessLabs.Neon.Simulation;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// Scene authoring for ambient walkway paths. Each element of `Paths` is a parent
    /// Transform whose ORDERED CHILDREN are the waypoints. Draws gizmos for editing; at
    /// runtime, bakes all paths into a single WalkwayPathsTag singleton the sim reads.
    /// Plan A ships the capability + a smoke path; Plan C authors Level 01's real paths.
    /// </summary>
    public class WalkwayPathAuthoring : MonoBehaviour
    {
        [Tooltip("Each entry = one path; its child transforms (in order) are the waypoints.")]
        public Transform[] Paths;

        private void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || Paths == null || Paths.Length == 0) return;

            var em = world.EntityManager;
            // All components in one CreateEntity: each AddBuffer would be a structural
            // change invalidating previously acquired buffer handles.
            var entity = em.CreateEntity(
                typeof(WalkwayPathsTag), typeof(WalkwayPoint), typeof(WalkwayPathRange));
            var pointBuf = em.GetBuffer<WalkwayPoint>(entity);
            var rangeBuf = em.GetBuffer<WalkwayPathRange>(entity);

            foreach (var root in Paths)
            {
                if (root == null || root.childCount < 2) continue;
                int start = pointBuf.Length;
                for (int i = 0; i < root.childCount; i++)
                {
                    var p = root.GetChild(i).position;
                    pointBuf.Add(new WalkwayPoint { Value = new float2(p.x, p.y) });
                }
                rangeBuf.Add(new WalkwayPathRange { Start = start, Count = root.childCount });
            }
        }

        private void OnDrawGizmos()
        {
            if (Paths == null) return;
            Gizmos.color = Color.cyan;
            foreach (var root in Paths)
            {
                if (root == null || root.childCount < 2) continue;
                for (int i = 0; i < root.childCount; i++)
                {
                    var a = root.GetChild(i).position;
                    var b = root.GetChild((i + 1) % root.childCount).position;
                    Gizmos.DrawSphere(a, 0.12f);
                    Gizmos.DrawLine(a, b);
                }
            }
        }
    }
}
