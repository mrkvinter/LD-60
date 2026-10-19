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
        Vector3 SwapOffset { get; }

        void ShowGhost(BaseItem item, Transform parent, Vector3 localPosition, Quaternion localRotation);
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
            TakeFromRack,
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
        private int activeIndex;
        private BaseItem shown;

        public int Count => items.Count;
        public int Capacity => settings.HandsCapacity + BonusCapacity;

        public int BonusCapacity { get; set; }
        public ItemMode Mode => ItemMode.Held;
        public IReadOnlyList<BaseItem> Items => items;

        public int ActiveIndex => activeIndex;
        public BaseItem Active => items.Count > 0 ? items[activeIndex] : null;

        public IItemReceiver PutTarget { get; private set; }

        private Vector3? ViewPosition => view != null ? view.position : null;

        public event Action Changed;
        public event Action<IItemReceiver, BaseItem> PutRefused;

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
            input.TakeFromRackPressed += OnTakeFromRackPressed;
            input.ThrowPressed += OnThrowPressed;
            input.Scrolled += OnItemScroll;
        }

        public void Dispose()
        {
            if (input == null) return;

            input.InteractPressed -= OnInteractPressed;
            input.TakeFromRackPressed -= OnTakeFromRackPressed;
            input.ThrowPressed -= OnThrowPressed;
            input.Scrolled -= OnItemScroll;
        }

        public void Tick()
        {
            UpdatePutTarget();

            if (heldAction == HandsAction.None || input == null) return;

            var isHeld = heldAction switch
            {
                HandsAction.Throw => input.IsThrowHeld,
                HandsAction.TakeFromRack => input.IsTakeFromRackHeld,
                _ => input.IsInteractHeld
            };
            if (!isHeld)
            {
                heldAction = HandsAction.None;
                return;
            }

            repeatTimer -= Time.deltaTime;
            if (repeatTimer > 0f) return;

            repeatTimer += settings.RepeatInterval;
            heldAction = heldAction switch
            {
                HandsAction.Throw => Throw(),
                HandsAction.TakeFromRack => TakeFromRack(),
                _ => Use(heldAction)
            };
        }

        public void Put(BaseItem item)
        {
            items.Add(item);
            item.SetContainer(this);
            activeIndex = items.Count - 1;

            ShowActive(swapIn: false);
            Changed?.Invoke();
        }

        public BaseItem TakeActive() => Take(activeIndex);

        public BaseItem TakeLast() => Take(items.Count - 1);

        public bool Remove(BaseItem item)
        {
            var index = items.IndexOf(item);
            if (index < 0) return false;

            Take(index);
            return true;
        }

        public void Cycle(int step)
        {
            if (items.Count < 2 || step == 0) return;

            activeIndex = ((activeIndex + step) % items.Count + items.Count) % items.Count;

            ShowActive(swapIn: true);
            Changed?.Invoke();
        }

        public bool TryPutOn(IItemReceiver receiver)
        {
            if (Active == null || receiver == null || !receiver.CanPut(Active)) return false;

            receiver.Put(TakeActive(), ViewPosition);
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

            if (heldAction == HandsAction.None && Active is { } active && ProbeReceiver() is { } receiver && !receiver.CanPut(active))
            {
                PutRefused?.Invoke(receiver, active);
            }
        }

        private void OnTakeFromRackPressed()
        {
            repeatTimer = settings.RepeatInterval;
            heldAction = TakeFromRack();
        }

        private void OnThrowPressed()
        {
            repeatTimer = settings.RepeatInterval;
            heldAction = Throw();
        }

        private void OnItemScroll(int direction)
        {
            Cycle(-direction);
        }

        private HandsAction Use(HandsAction action)
        {
            if (action != HandsAction.Take && TryPutOn(PutTarget)) return HandsAction.Put;
            if (action != HandsAction.Put && aim?.Target is { Container: null } loose && TryTake(loose)) return HandsAction.Take;

            return HandsAction.None;
        }

        private HandsAction TakeFromRack()
        {
            if (Count >= Capacity) return HandsAction.None;
            if (aim?.Target is { Container: Rack } stored) return TryTake(stored) ? HandsAction.TakeFromRack : HandsAction.None;
            if (ProbeReceiver() is not Rack { Count: > 0 } rack) return HandsAction.None;

            Put(rack.TakeNearest(ViewPosition));
            return HandsAction.TakeFromRack;
        }

        private HandsAction Throw()
        {
            if (items.Count == 0 || rig.Anchor == null || view == null) return HandsAction.None;

            TakeActive().Release(rig.Anchor.position, rig.Anchor.rotation, view.forward * settings.ThrowSpeed);

            return HandsAction.Throw;
        }

        private BaseItem Take(int index)
        {
            if (items.Count == 0) return null;

            var item = items[index];
            items.RemoveAt(index);
            item.SetVisible(true);
            if (item == shown) shown = null;

            if (index < activeIndex || activeIndex >= items.Count) activeIndex = Mathf.Max(activeIndex - 1, 0);

            ShowActive(swapIn: true);
            Changed?.Invoke();

            return item;
        }

        private void UpdatePutTarget()
        {
            var active = Active;
            var receiver = active != null ? ProbeReceiver() : null;
            if (receiver is Rack rack && rack.TryGetNextSlot(active, ViewPosition, out var slot))
            {
                PutTarget = rack;
                rig.ShowGhost(active, rack.SlotRoot, slot, rack.LayoutFor(active.Def).SlotRotation);
                return;
            }

            if (receiver is not Rack && receiver != null && receiver.CanPut(active))
            {
                PutTarget = receiver;
                rig.HideGhost();
                return;
            }

            if (PutTarget == null) return;

            PutTarget = null;
            rig.HideGhost();
        }

        private IItemReceiver ProbeReceiver()
        {
            if (view == null) return null;

            var count = Physics.RaycastNonAlloc(view.position, view.forward, hits, settings.InteractDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            Array.Sort(hits, 0, count, byDistance);

            for (var i = 0; i < count; i++)
            {
                var entity = hits[i].collider.GetComponentInParent<IEntityAdapter>()?.BaseEntity;
                if (ReferenceEquals(entity, owner)) continue;
                if (hits[i].collider.isTrigger && entity is not IItemReceiver) continue;

                return entity switch
                {
                    IItemReceiver receiver => receiver,
                    BaseItem { Container: Rack rack } => rack,
                    _ => null
                };
            }

            return null;
        }

        private void ShowActive(bool swapIn)
        {
            var active = Active;
            if (active == shown) return;

            shown?.SwapOut(rig.SwapOffset);
            shown = active;
            if (active == null) return;

            if (swapIn)
            {
                active.SwapIn(rig.Anchor, Vector3.zero, Quaternion.identity, active.Def.HeldScale, rig.SwapOffset);
            }
            else
            {
                active.Place(rig.Anchor, Vector3.zero, Quaternion.identity, active.Def.HeldScale);
            }
        }
    }
}
