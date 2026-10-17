using System;
using System.Collections.Generic;
using Code.Game.Scripts.EntitySystem;
using Game.Scripts.Items;
using Game.Scripts.Player;
using UnityEngine;
using VContainer.Unity;

namespace Game.Scripts.Interaction
{
    public sealed class AimComponent : IEntityComponent, IInitializable, ITickable
    {
        private const float ProbeRadius = 0.01f;
        private const int MaxHits = 64;

        private static readonly IComparer<RaycastHit> byDistance =
            Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));

        private readonly Entity owner;
        private readonly PlayerSettingsDef settings;
        private readonly RaycastHit[] hits = new RaycastHit[MaxHits];

        private Transform origin;

        public BaseItem Target { get; private set; }

        public event Action<BaseItem> TargetChanged;

        public AimComponent(Entity owner, PlayerSettingsDef settings)
        {
            this.owner = owner;
            this.settings = settings;
        }

        public void Initialize()
        {
            origin = owner.GetComponent<CameraAnchorComponent>()?.Transform;
        }

        public void Tick()
        {
            var next = origin != null ? Probe() : null;
            if (ReferenceEquals(next, Target)) return;

            Target?.SetHighlighted(false);
            Target = next;
            Target?.SetHighlighted(true);

            TargetChanged?.Invoke(Target);
        }

        private BaseItem Probe()
        {
            var count = Physics.SphereCastNonAlloc(origin.position, ProbeRadius, origin.forward, hits,
                settings.InteractDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, 0, count, byDistance);

            for (var i = 0; i < count; i++)
            {
                var entity = hits[i].collider.GetComponentInParent<IEntityAdapter>()?.BaseEntity;
                if (ReferenceEquals(entity, owner)) continue;

                return entity is BaseItem { IsActive: true } item ? item : null;
            }

            return null;
        }
    }
}
