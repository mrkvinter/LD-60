using Code.Game.Scripts.EntitySystem;
using Game.Scripts.Player;
using RG.DefinitionSystem.Core;

namespace Game.Scripts.Interaction
{
    public sealed class AimAdapter : EntityComponentAdapter<AimComponent>
    {
        protected override AimComponent CreateComponent() =>
            new(Entity, DefManager.GetSingletonDef<PlayerSettingsDef>());
    }
}
