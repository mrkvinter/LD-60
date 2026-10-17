using Code.Game.Scripts.EntitySystem;
using UnityEngine;

namespace Game.Scripts.Player
{
    public sealed class CameraAnchorComponent : IEntityComponent
    {
        public Transform Transform { get; }

        public CameraAnchorComponent(Transform transform)
        {
            Transform = transform;
        }
    }
}
