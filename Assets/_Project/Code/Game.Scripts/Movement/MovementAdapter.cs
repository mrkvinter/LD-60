using Code.Game.Scripts.EntitySystem;
using Sirenix.OdinInspector;

namespace Game.Scripts.Movement
{
    public abstract class MovementAdapter : EntityComponentAdapter<IMovementComponent>
    {
        [ShowInInspector] public bool IsEnabled => Component?.IsEnabled ?? false;
    }
}
