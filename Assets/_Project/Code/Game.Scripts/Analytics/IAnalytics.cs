using System.Collections.Generic;

namespace Game.Scripts.Analytics
{
    public interface IAnalytics
    {
        void Track(string eventName, Dictionary<string, object> properties);
        void Flush();
    }
}
