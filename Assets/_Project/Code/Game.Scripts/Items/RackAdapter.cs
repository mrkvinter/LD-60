using Code.Game.Scripts.EntitySystem;
using Game.Core.Contexts;
using RG.DefinitionSystem.Core;
using UnityEngine;
#if UNITY_EDITOR
using RG.DefinitionSystem.UnityAdapter;
#endif

namespace Game.Scripts.Items
{
    public sealed class RackAdapter : EntityAdapter<Rack>
    {
        [SerializeField] private RackFilter filter;

        [Tooltip("Middle of the surface the items stand on; their layout is counted from here.")]
        [SerializeField] private Transform slotRoot;

        [Tooltip("Editor only: draws the slots of this item's layout while the rack is selected.")]
        [SerializeField] private DefRef<ItemDef> previewItem;

        public Transform SlotRoot => slotRoot != null ? slotRoot : transform;

        protected override Rack CreateEntity() => new(SlotRoot, filter);

        protected override void OnEntityInitialized()
        {
            GameServiceLocator.Resolve<SortingProgress>().Register(Entity);
            GameServiceLocator.Resolve<ResourceStorage>().Register(Entity);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (previewItem.IsEmpty || DefinitionDatabaseUtility.GetDefinition(previewItem.Id) is not ItemDef def) return;

            var layout = def.RackLayout;
            Gizmos.color = new Color(0.45f, 1f, 0.45f, 0.8f);
            Gizmos.matrix = SlotRoot.localToWorldMatrix;
            for (var i = 0; i < layout.Capacity; i++)
            {
                Gizmos.DrawWireCube(layout.GetPosition(i) + Vector3.up * (layout.Spacing.y * 0.5f), layout.Spacing * 0.95f);
            }
        }
#endif
    }
}
