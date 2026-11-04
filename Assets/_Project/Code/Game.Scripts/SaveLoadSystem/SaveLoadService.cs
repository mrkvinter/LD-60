using System.Collections.Generic;

namespace Game.Scripts.SaveLoadSystem
{
    public class SaveLoadService : SaveLoadService<GameSaveData>, ISaveLoadService
    {
        public SaveLoadService(ISaveSerializer serializer, ISaveStorage storage,
            IEnumerable<ISaveMigration<GameSaveData>> migrations = null)
            : base(serializer, storage, GameSaveData.CurrentVersion, migrations)
        {
        }
    }
}
