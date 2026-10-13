namespace Game.Core
{
    public interface IGameDirector
    {
        void RestartGame();
        void SaveGame();
        void LoadLastSave();
    }
}