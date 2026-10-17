using Code.Game.Scripts.EntitySystem;

namespace Game.Scripts.Movement
{
    public interface IMovementComponent : IEntityComponent
    {
        bool IsEnabled { get; set; }
    }
}
