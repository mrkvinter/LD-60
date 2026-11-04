using System;
using System.Collections.Generic;
using Game.Scripts.Construction;
using Game.Scripts.Items;
using Game.Scripts.Player;
using Game.Scripts.Upgrades;

namespace Game.Scripts.SaveLoadSystem
{
    [Serializable]
    public class GameSaveData : ISaveData
    {
        public const int CurrentVersion = 1;

        public int Version;

        public PlayerAdapter.SaveData Player;
        public UpgradeService.SaveData Upgrades;

        public Dictionary<int, BaseItem.SaveData> Items = new();
        public Dictionary<string, Rack.SaveData> Racks = new();
        public Dictionary<string, ConstructionSite.SaveData> Sites = new();

        public List<string> ReadNotes = new();
        public List<string> PickedSyringes = new();

        int ISaveData.Version
        {
            get => Version;
            set => Version = value;
        }
    }

    public interface IPersistent
    {
        void Save(GameSaveData gameSaveData);
        void Load(GameSaveData gameSaveData);
    }

    /// <summary>Runs once every persistent has loaded, for state that depends on what others loaded.</summary>
    public interface IPostLoad
    {
        void PostLoad();
    }
}
