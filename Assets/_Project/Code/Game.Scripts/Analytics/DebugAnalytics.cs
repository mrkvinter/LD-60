using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Scripts.Analytics
{
    public sealed class DebugAnalytics : IAnalytics
    {
        public void Track(string eventName, Dictionary<string, object> properties)
        {
            var text = properties == null ? string.Empty : string.Join(", ", properties.Select(p => $"{p.Key}={p.Value}"));
            Debug.Log($"[Analytics] {eventName} {text}");
        }

        public void Flush() { }
    }
}
