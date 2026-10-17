using System;
using System.Collections.Generic;
using Code.Game.Scripts.EntitySystem;
using Game.Scripts.Interaction;
using Game.Scripts.Player;
using UnityEngine;
using VContainer.Unity;

namespace Game.Scripts.Items
{
    public interface IItemHandsRig
    {
        Transform Anchor { get; }
        Pose GetSlot(int index);

        void ShowGhost(BaseItem item, Transform parent, Vector3 localPosition);
        void HideGhost();
    }

    public sealed class ItemHandsComponent : IEntityComponent, IItemContainer, IInitializable, ITickable, IDisposable
    {
        private const int MaxHits = 16;

        private static readonly IComparer<RaycastHit> byDistance =
            Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));

        private enum HandsAction
        {
            None,
            Take,
            Put,
            Throw
        }

        private readonly Entity owner;
        private readonly IItemHandsRig rig;
        private readonly PlayerSettingsDef settings;
        private readonly List<BaseItem> items = new();
        private readonly RaycastHit[] hits = new RaycastHit[MaxHits];

        private PlayerInputComponent input;
        private AimComponent aim;
        private Transform view;
        private float repeatTimer;
        private HandsAction heldAction;

        public int Count => items.Count;
        public int Capacity => settings.HandsCapacity;
        public ItemMode Mode => ItemMode.Held;
        public IReadOnlyList<BaseItem> Items => items;

        public Rack PutTarget { get; private set; }

        private Vector3? ViewPosition => view != null ? view.position : null;

        public event Action Changed;

        public ItemHandsComponent(Entity owner, IItemHandsRig rig, PlayerSettingsDef settings)
        {
            this.owner = owner;
            this.rig = rig;
            this.settings = settings;
        }

        public void Initialize()
        {
            aim = owner.GetComponent<AimComponent>();
            view = owner.GetComponent<CameraAnchorComponent>()?.Transform;
            input = owner.GetComponent<PlayerInputComponent>();

            if (input == null) return;

            input.InteractPressed += OnInteractPressed;
            input.ThrowPressed += OnThrowPressed;
        }

        public void Dispose()
        {
            if (input == null) return;

            input.InteractPressed -= OnInteractPressed;
            input.ThrowPressed -= OnThrowPressed;
        }

        public void Tick()
        {
            UpdatePutTarget();

            if (heldAction == HandsAction.None || input == null) return;

            var isHeld = heldAction == HandsAction.Throw ? input.IsThrowHeld : input.IsInteractHeld;
            if (!isHeld)
            {
                heldAction = HandsAction.None;
                return;
            }

            repeatTimer -= Time.deltaTime;
            if (repeatTimer > 0f) return;

            repeatTimer += settings.RepeatInterval;
            heldAction = heldAction == HandsAction.Throw ? Throw() : Use(heldAction);
        }

        public void Put(BaseItem item)
        {
            items.Add(item);
            item.SetContainer(this);

            Relayout();
            Changed?.Invoke();
        }

        public BaseItem TakeLast()
        {
            if (items.Count == 0) return null;

            var item = items[^1];
            items.RemoveAt(items.Count - 1);

            Relayout();
            Changed?.Invoke();

            return item;
        }

        public bool TryPutOn(Rack rack)
        {
            if (items.Count == 0 || rack == null || !rack.CanPut(items[^1])) return false;

            rack.Put(TakeLast(), ViewPosition);
            return true;
        }

        public bool TryTake(BaseItem item)
        {
            if (item == null || Count >= Capacity) return false;

            switch (item.Container)
            {
                case null:
                    break;
                case Rack rack when rack.Take(item):
                    break;
                default:
                    return false;
            }

            Put(item);
            return true;
        }

        private void OnInteractPressed()
        {
            repeatTimer = settings.RepeatInterval;
            heldAction = Use(HandsAction.None);
        }

        private void OnThrowPressed()
        {
            repeatTimer = settings.RepeatInterval;
            heldAction = Throw();
        }

        private HandsAction Use(HandsAction action)
        {
            if (action != HandsAction.Take && TryPutOn(PutTarget)) return HandsAction.Put;
            if (action != HandsAction.Put && TryTake(aim?.Target)) return HandsAction.Take;

            return HandsAction.None;
        }

        private HandsAction Throw()
        {
            if (items.Count == 0 || rig.Anchor == null || view == null) return HandsAction.None;

            var item = TakeLast();
            var slot = rig.GetSlot(items.Count);
            item.Release(
                rig.Anchor.TransformPoint(slot.position),
                rig.Anchor.rotation * slot.rotation,
                view.forward * settings.ThrowSpeed);

            return HandsAction.Throw;
        }

        private void UpdatePutTarget()
        {
            var rack = items.Count > 0 ? ProbeRack() : null;
            if (rack != null && rack.TryGetNextSlot(items[^1], ViewPosition, out var slot))
            {
                PutTarget = rack;
                rig.ShowGhost(items[^1], rack.SlotRoot, slot);
                return;
            }

            if (PutTarget == null) return;

            PutTarget = null;
            rig.HideGhost();
        }

        private Rack ProbeRack()
        {
            if (view == null) return null;

            var count = Physics.RaycastNonAlloc(view.position, view.forward, hits, settings.InteractDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, 0, count, byDistance);

            for (var i = 0; i < count; i++)
            {
                var entity = hits[i].collider.GetComponentInParent<IEntityAdapter>()?.BaseEntity;
                if (ReferenceEquals(entity, owner)) continue;

                return entity switch
                {
                    Rack rack => rack,
                    BaseItem { Container: Rack rack } => rack,
                    _ => null
                };
            }

            return null;
        }

        private void Relayout()
        {
            for (var i = 0; i < items.Count; i++)
            {
                var slot = rig.GetSlot(i);
                items[i].Place(rig.Anchor, slot.position, slot.rotation, items[i].Def.HeldScale);
            }
        }
    }
}
