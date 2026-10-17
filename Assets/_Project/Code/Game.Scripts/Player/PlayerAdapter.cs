using Code.Game.Scripts.EntitySystem;
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

            return player;
        }
    }
}
