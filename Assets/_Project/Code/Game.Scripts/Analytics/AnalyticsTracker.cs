using System;
using System.Collections.Generic;
using Game.Scripts.Construction;
using Game.Scripts.Items;
using Game.Scripts.Notes;
using Game.Scripts.Player;
using Game.Scripts.UI;
using Game.Scripts.Upgrades;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Scripts.Analytics
{
    public sealed class AnalyticsTracker : IDisposable
    {
        private readonly IAnalytics analytics;
        private readonly UpgradeService upgrades;
        private readonly ConstructionProgress construction;
        private readonly SortingProgress sorting;

        private PlayerAbilitiesComponent abilities;
        private NoteView notes;
        private float startTime;
        private bool started;
        private bool ended;
        private bool quitting;

        public AnalyticsTracker(IAnalytics analytics, UpgradeService upgrades, ConstructionProgress construction,
            SortingProgress sorting)
        {
            this.analytics = analytics;
            this.upgrades = upgrades;
            this.construction = construction;
            this.sorting = sorting;
        }

        public void Start()
        {
            if (started) return;

            started = true;
            startTime = Time.realtimeSinceStartup;

            upgrades.RackFilled += OnRackFilled;
            upgrades.Bought += OnBought;
            construction.Built += OnBuilt;

            var player = Object.FindAnyObjectByType<PlayerAdapter>();
            abilities = player != null ? player.Entity?.GetComponent<PlayerAbilitiesComponent>() : null;
            if (abilities != null) abilities.Unlocked += OnUnlocked;

            notes = Object.FindAnyObjectByType<NoteView>(FindObjectsInactive.Include);
            if (notes != null) notes.Shown += OnNoteShown;

            Application.quitting += OnQuitting;
            Track("game_started");
        }

        public void Track(string eventName, params (string Key, object Value)[] properties)
        {
            var payload = new Dictionary<string, object> { ["time_sec"] = Mathf.RoundToInt(Time.realtimeSinceStartup - startTime) };
            foreach (var (key, value) in properties) payload[key] = value;

            analytics.Track(eventName, payload);
        }

        public void Flush() => analytics.Flush();

        public void Dispose()
        {
            if (!started) return;

            EndSession(quitting ? "quit" : "restart");

            upgrades.RackFilled -= OnRackFilled;
            upgrades.Bought -= OnBought;
            construction.Built -= OnBuilt;
            if (abilities != null) abilities.Unlocked -= OnUnlocked;
            if (notes != null) notes.Shown -= OnNoteShown;
            Application.quitting -= OnQuitting;
        }

        private void OnRackFilled(Rack rack) =>
            Track("rack_filled", ("item_id", rack.StoredDef?.Id), ("filled_racks", upgrades.FilledRacks), ("points", upgrades.Points));

        private void OnBought(UpgradeKind kind, int level, int cost) =>
            Track("upgrade_bought", ("upgrade", kind.ToString()), ("level", level), ("cost", cost));

        private void OnBuilt(BuildingDef building) =>
            Track("building_built", ("building_id", building.Id), ("buildings_built", construction.BuiltTotal()));

        private void OnUnlocked(PlayerAbility ability) => Track("ability_unlocked", ("ability", ability.ToString()));

        private void OnNoteShown(NoteDef note) => Track("note_read", ("note_id", note.Id));

        private void OnQuitting()
        {
            quitting = true;
            EndSession("quit");
        }

        private void EndSession(string reason)
        {
            if (ended) return;

            ended = true;
            Track("session_ended",
                ("reason", reason),
                ("points", upgrades.Points),
                ("filled_racks", upgrades.FilledRacks),
                ("stored_items", sorting.Stored.CurrentValue),
                ("buildings_built", construction.BuiltTotal()));
            analytics.Flush();
        }
    }
}
