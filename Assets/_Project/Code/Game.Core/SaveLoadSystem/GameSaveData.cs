using System;

namespace Game.Scripts.SaveLoadSystem
{
    [Serializable]
    public class GameSaveData : ISaveData
    {
        public const int CurrentVersion = 1;

        public int Version;

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
}
