using System;
using System.Collections.Generic;
using Code.Game.Scripts.EntitySystem;
using Game.Core.Contexts;
using Game.Scripts.Items;
using Game.Scripts.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace Game.Scripts.Upgrades
{
    /// <summary>The bought abilities that work on the items in hand, each on its own key and cooldown.</summary>
    public sealed class HandsAbilitiesComponent : IEntityComponent, IInitializable, ITickable, IDisposable
    {
        private readonly Entity owner;
        private readonly Dictionary<UpgradeKind, AbilityCooldown> cooldowns = new()
        {
            [UpgradeKind.Highlight] = new AbilityCooldown(),
            [UpgradeKind.Distribute] = new AbilityCooldown(),
            [UpgradeKind.Pull] = new AbilityCooldown()
        };

        private readonly List<BaseItem> marked = new();
        private readonly List<InputAction> actions = new();

        private ItemHandsComponent hands;
        private PlayerInputComponent input;
        private Transform view;
        private UpgradeService upgrades;
        private SortingProgress progress;

        public float HighlightLeft { get; private set; }

        public HandsAbilitiesComponent(Entity owner)
        {
            this.owner = owner;
        }

        public void Initialize()
        {
            hands = owner.GetComponent<ItemHandsComponent>();
            input = owner.GetComponent<PlayerInputComponent>();
            view = owner.GetComponent<CameraAnchorComponent>()?.Transform;
            upgrades = GameServiceLocator.Resolve<UpgradeService>();
            progress = GameServiceLocator.Resolve<SortingProgress>();

            Bind("<Keyboard>/q", () => TryHighlight());
            Bind("<Keyboard>/r", () => TryDistribute());
            Bind("<Keyboard>/f", () => TryPull());

            upgrades.Changed += ApplyCapacity;
            ApplyCapacity();
        }

        public void Dispose()
        {
            foreach (var action in actions) action.Dispose();
            actions.Clear();

            if (upgrades != null) upgrades.Changed -= ApplyCapacity;
            ClearMarks();
        }

        public void Tick()
        {
            foreach (var cooldown in cooldowns.Values) cooldown.Tick(Time.deltaTime);

            if (HighlightLeft <= 0f) return;

            HighlightLeft -= Time.deltaTime;
            if (HighlightLeft <= 0f)
            {
                ClearMarks();
                return;
            }

            // Picked up or stored items are found already.
            for (var i = marked.Count - 1; i >= 0; i--)
            {
                if (marked[i].Container == null) continue;

                marked[i].SetMarked(false);
                marked.RemoveAt(i);
            }
        }

        public AbilityCooldown Cooldown(UpgradeKind kind) => cooldowns.TryGetValue(kind, out var cooldown) ? cooldown : null;

        public bool TryHighlight()
        {
            var level = Ready(UpgradeKind.Highlight);
            var def = hands?.Active?.Def;
            if (level == null || def == null) return false;

            ClearMarks();
            foreach (var item in progress.Items)
            {
                if (item.Container != null || !item.IsActive || !Equals(item.Def, def)) continue;

                item.SetMarked(true);
                marked.Add(item);
            }

            HighlightLeft = level.Amount;
            cooldowns[UpgradeKind.Highlight].Start(level.Cooldown);
            return true;
        }

        public bool TryDistribute()
        {
            var level = Ready(UpgradeKind.Distribute);
            if (level == null || hands == null) return false;

            if (HandsAbilities.Distribute(hands, progress.Racks, view != null ? view.position : null) == 0) return false;

            cooldowns[UpgradeKind.Distribute].Start(level.Cooldown);
            return true;
        }

        public bool TryPull()
        {
            var level = Ready(UpgradeKind.Pull);
            var def = hands?.Active?.Def;
            if (level == null || def == null) return false;

            var origin = view != null ? view.position : Vector3.zero;
            if (HandsAbilities.Pull(hands, progress.Items, def, Mathf.RoundToInt(level.Amount), origin) == 0) return false;

            cooldowns[UpgradeKind.Pull].Start(level.Cooldown);
            return true;
        }

        // The bought level when the ability is off cooldown and the player is not in a menu.
        private UpgradeLevel Ready(UpgradeKind kind)
        {
            if (input is { IsBlocked: true } || !cooldowns[kind].IsReady) return null;

            return upgrades?.Current(kind);
        }

        private void Bind(string binding, Action perform)
        {
            var action = new InputAction(binding: binding);
            action.performed += _ => perform();
            action.Enable();
            actions.Add(action);
        }

        private void ApplyCapacity()
        {
            if (hands != null) hands.BonusCapacity = Mathf.RoundToInt(upgrades.TotalAmount(UpgradeKind.HandsCapacity));
        }

        private void ClearMarks()
        {
            foreach (var item in marked) item.SetMarked(false);
            marked.Clear();
            HighlightLeft = 0f;
        }
    }
}
