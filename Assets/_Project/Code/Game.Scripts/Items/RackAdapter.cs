using Code.Game.Scripts.EntitySystem;
using Game.Core.Contexts;
using Game.Scripts.Upgrades;
using RG.DefinitionSystem.Core;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using RG.DefinitionSystem.UnityAdapter;
#endif

namespace Game.Scripts.Items
{
    public sealed class RackAdapter : EntityAdapter<Rack>
    {
        [Tooltip("Takes only items of this type. None means any.")]
        [SerializeField] private RackType type;

        [SerializeField] private RackFilter filter;

        [Tooltip("Middle of the surface the items stand on; their layout is counted from here.")]
        [SerializeField] private Transform slotRoot;

        [Tooltip("Lays every kind out in the grid below instead of its own, for racks built around one size of item.")]
        [SerializeField] private bool ownLayout;

        [SerializeField, ShowIf(nameof(ownLayout))] private RackLayout layout = new();

        [Tooltip("Editor only: draws the slots of this item's layout while the rack is selected.")]
        [SerializeField, HideIf(nameof(ownLayout))] private DefRef<ItemDef> previewItem;

        public Transform SlotRoot => slotRoot != null ? slotRoot : transform;

        protected override Rack CreateEntity() => new(SlotRoot, type, filter, ownLayout ? layout : null);

        protected override void OnEntityInitialized()
        {
            GameServiceLocator.Resolve<SortingProgress>().Register(Entity);
            GameServiceLocator.Resolve<ResourceStorage>().Register(Entity);
            GameServiceLocator.Resolve<UpgradeService>().Register(Entity);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var preview = ownLayout ? layout
                : !previewItem.IsEmpty && DefinitionDatabaseUtility.GetDefinition(previewItem.Id) is ItemDef def ? def.RackLayout
                : null;
            if (preview == null) return;

            Gizmos.color = new Color(0.45f, 1f, 0.45f, 0.8f);
            Gizmos.matrix = SlotRoot.localToWorldMatrix;
            for (var i = 0; i < preview.Capacity; i++)
            {
                Gizmos.DrawWireCube(preview.GetPosition(i) + Vector3.up * (preview.Spacing.y * 0.5f), preview.Spacing * 0.95f);
            }
        }
#endif
    }
}
