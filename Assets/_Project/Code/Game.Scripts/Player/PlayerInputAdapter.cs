using Code.Cursor;
using Code.Game.Scripts.EntitySystem;
using Game.Core.Contexts;

namespace Game.Scripts.Player
{
    public sealed class PlayerInputAdapter : EntityComponentAdapter<PlayerInputComponent>
    {
        public override int InitializationOrder => -10;

        protected override PlayerInputComponent CreateComponent() =>
            new(GameServiceLocator.Resolve<CursorService>());

        private void OnDestroy()
        {
            Component?.Dispose();
        }
    }
}
