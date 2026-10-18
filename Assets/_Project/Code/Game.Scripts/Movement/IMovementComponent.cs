using Code.Game.Scripts.EntitySystem;
using UnityEngine;

namespace Game.Scripts.Movement
{
    public interface IMovementComponent : IEntityComponent
    {
        bool IsEnabled { get; set; }

        void Teleport(Vector3 position, Quaternion rotation);
    }
}
