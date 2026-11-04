using System;
using Code.Game.Scripts.EntitySystem;
using Game.Core.Contexts;
using Game.Scripts.Items;
using Game.Scripts.Movement;
using Game.Scripts.SaveLoadSystem;
using Game.Scripts.Upgrades;
using RG.DefinitionSystem.Core;
using UnityEngine;

namespace Game.Scripts.Player
{
    public sealed class PlayerAdapter : EntityAdapter<Entity>, IPersistent
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

        public void Save(GameSaveData data)
        {
            if (Entity == null) return;

            data.Player = new SaveData
            {
                Pose = new Pose(transform.position, transform.rotation),
                Abilities = Entity.GetComponent<PlayerAbilitiesComponent>()?.Save(),
                Hands = Entity.GetComponent<ItemHandsComponent>()?.Save()
            };
        }

        public void Load(GameSaveData data)
        {
            var saved = data.Player;
            if (saved == null || Entity == null) return;

            Entity.GetComponent<PlayerAbilitiesComponent>()?.Load(saved.Abilities);
            Entity.GetComponent<ItemHandsComponent>()?.Load(saved.Hands, GameServiceLocator.Resolve<SortingProgress>().Find);
            Entity.GetComponent<IMovementComponent>()?.Teleport(saved.Pose.position, saved.Pose.rotation);
        }

        [Serializable]
        public class SaveData
        {
            public Pose Pose;
            public PlayerAbilitiesComponent.SaveData Abilities;
            public ItemHandsComponent.SaveData Hands;
        }
    }
}
