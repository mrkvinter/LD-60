using System;
using Code.Game.Core.ExecutorSystem.Actions;
using Game.Core.Contexts;
using Game.Scripts.Analytics;
using UnityEngine;

namespace Game.Scripts.Actions
{
    [Serializable]
    public class TrackEventGameAction : GameAction
    {
        [SerializeField] private string eventName = "ending_reached";
        [SerializeField] private string id;

        public override void Execute()
        {
            if (string.IsNullOrEmpty(eventName)) return;

            var tracker = GameServiceLocator.Resolve<AnalyticsTracker>();
            tracker.Track(eventName, ("id", id));
            tracker.Flush();
        }
    }
}
