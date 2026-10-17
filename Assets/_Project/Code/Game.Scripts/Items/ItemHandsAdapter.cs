using System.Collections.Generic;
using Code.Game.Scripts.EntitySystem;
using Game.Scripts.Player;
using RG.DefinitionSystem.Core;
using UnityEngine;

namespace Game.Scripts.Items
{
    public sealed class ItemHandsAdapter : EntityComponentAdapter<ItemHandsComponent>, IItemHandsRig
    {
        [SerializeField] private Transform anchor;

        [Tooltip("Where the item in hand slides to when hidden on switch, in anchor space.")]
        [SerializeField] private Vector3 swapOffset = new(0f, -0.25f, 0f);

        private readonly Dictionary<ItemDef, GameObject> ghosts = new();
        private GameObject shownGhost;

        public Transform Anchor => anchor;
        public Vector3 SwapOffset => swapOffset;

        public void ShowGhost(BaseItem item, Transform parent, Vector3 localPosition)
        {
            var ghost = GetGhost(item);
            if (shownGhost != ghost) HideGhost();
            if (ghost == null) return;

            ghost.transform.SetParent(parent, false);
            ghost.transform.SetLocalPositionAndRotation(localPosition, Quaternion.identity);
            ghost.SetActive(true);
            shownGhost = ghost;
        }

        public void HideGhost()
        {
            if (shownGhost != null) shownGhost.SetActive(false);
            shownGhost = null;
        }

        protected override ItemHandsComponent CreateComponent() =>
            new(Entity, this, DefManager.GetSingletonDef<PlayerSettingsDef>());

        private GameObject GetGhost(BaseItem item)
        {
            if (ghosts.TryGetValue(item.Def, out var ghost) && ghost != null) return ghost;

            var material = DefManager.GetSingletonDef<ItemSettingsDef>().GhostMaterial;
            ghost = material != null && item.View is ItemAdapter view ? view.CreateGhost(material) : null;
            if (ghost != null) ghost.SetActive(false);

            ghosts[item.Def] = ghost;
            return ghost;
        }

        private void OnDestroy()
        {
            Component?.Dispose();

            foreach (var ghost in ghosts.Values)
            {
                if (ghost != null) Destroy(ghost);
            }
        }
    }
}
