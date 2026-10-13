using Game.Core.Contexts;
using KvinterGames;

namespace Code.Game.Core
{
    public class G
    {
        public static AudioService AudioService => GameServiceLocator.Resolve<AudioService>();
    }
}