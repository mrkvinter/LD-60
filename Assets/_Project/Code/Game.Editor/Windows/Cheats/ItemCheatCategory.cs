using System.Collections.Generic;
using Code.Cheats;
using Code.Game.Scripts.EntitySystem;
using Game.Scripts.Items;
using Game.Scripts.Player;
using UnityEngine;

namespace Code.Game.Editor.Cheats
{
    public class ItemCheatCategory : IGameCheatCategory
    {
        public void Draw()
        {
            var progress = CheatServices.Resolve<SortingProgress>();
            var resources = CheatServices.Resolve<ResourceStorage>();
            GUILayout.Label($"On racks {progress.Stored.CurrentValue} / {progress.Total.CurrentValue}, " +
                            $"resources {resources.Total}");

            if (GUILayout.Button("Lay everything out on racks")) ItemCheats.StoreAll();
            if (GUILayout.Button("Scatter everything over the level")) ItemCheats.ScatterAll();
            if (GUILayout.Button("Fill hands with loose items")) ItemCheats.FillHands();
        }
    }

    public static class ItemCheats
    {
        public static void StoreAll()
        {
            var progress = CheatServices.Resolve<SortingProgress>();
            var leftovers = new List<BaseItem>();

            foreach (var item in CollectAll(progress))
            {
                var rack = FindRack(progress, item);
                if (rack != null) rack.Put(item, true);
                else leftovers.Add(item);
            }

            if (leftovers.Count > 0) CheatServices.Resolve<ItemSpawner>().Scatter(leftovers);
        }

        public static void ScatterAll()
        {
            var progress = CheatServices.Resolve<SortingProgress>();
            CheatServices.Resolve<ItemSpawner>().Scatter(CollectAll(progress));
        }

        public static void FillHands()
        {
            var hands = FindHands();
            if (hands == null) return;

            foreach (var item in LooseItems())
            {
                if (hands.Count >= hands.Capacity) return;

                hands.Put(item);
            }
        }

        private static List<BaseItem> CollectAll(SortingProgress progress)
        {
            var items = LooseItems();

            foreach (var rack in progress.Racks)
            {
                while (rack.Count > 0) items.Add(rack.TakeLast());
            }

            var hands = FindHands();
            while (hands is { Count: > 0 }) items.Add(hands.TakeLast());

            return items;
        }

        private static List<BaseItem> LooseItems()
        {
            var loose = new List<BaseItem>();
            foreach (var entity in CheatServices.Resolve<EntityService>().Entities)
            {
                if (entity is BaseItem { Container: null } item) loose.Add(item);
            }

            return loose;
        }

        private static Rack FindRack(SortingProgress progress, BaseItem item)
        {
            foreach (var rack in progress.Racks)
            {
                if (rack.CanPut(item)) return rack;
            }

            return null;
        }

        private static ItemHandsComponent FindHands()
        {
            foreach (var entity in CheatServices.Resolve<EntityService>().Entities)
            {
                if (entity.GetComponent<IsPlayerComponent>() != null) return entity.GetComponent<ItemHandsComponent>();
            }

            return null;
        }
    }
}
