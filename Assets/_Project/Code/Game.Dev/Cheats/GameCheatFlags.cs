using Code.Prefs;

namespace Code.Cheats
{
    public class GameCheatFlags : CheatFlags
    {
        public readonly BoolPlayerPref SkipIntro = new($"{nameof(GameCheatFlags)}.SkipIntro");
        public readonly BoolPlayerPref DevEnabled = new($"{nameof(GameCheatFlags)}.DevEnabled");

    }
}