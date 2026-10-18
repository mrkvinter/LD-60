using Code.Game.Scripts.EntitySystem;
using Game.Scripts.Upgrades;
using RG.DefinitionSystem.Core;
using UnityEngine;

namespace Game.Scripts.Player
{
    public sealed class PlayerAdapter : EntityAdapter<Entity>
    {
        [SerializeField] private Transform cameraAnchor;

        protected override Entity CreateEntity()
        {
            var player = new Entity();
            player.AddComponent(new IsPlayerComponent());
            player.AddComponent(new CameraAnchorComponent(cameraAnchor));
            player.AddComponent(new PlayerAbilitiesComponent(DefManager.GetSingletonDef<PlayerSettingsDef>().StartAbilities));
            player.AddComponent(new HandsAbilitiesComponent(player));

            return player;
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            Entity?.GetComponent<HandsAbilitiesComponent>()?.Dispose();
        }
    }
}
