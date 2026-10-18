using System;
using Code.Game.Core.ExecutorSystem.Conditions;
using Game.Core.Contexts;
using Game.Scripts.Upgrades;
using UnityEngine;

namespace Game.Scripts.Conditions
{

    [Serializable]
    public class FilledRacksCondition : Condition
    {
        private static UpgradeService Upgrades => GameServiceLocator.Resolve<UpgradeService>();

        [SerializeField] private ComparisonType comparisonType = ComparisonType.MoreOrEqual;
        [SerializeField, Min(0)] private int count = 1;

        // The game scope may already be torn down when the last tracked object goes away.
        private UpgradeService tracked;

        public override string Description => $"Filled racks {comparisonType} {count}";

        public override bool Execute() => comparisonType.Matches(Upgrades.FilledRacks, count);

        public override void OnStartTracking()
        {
            tracked = Upgrades;
            tracked.Changed += RaiseConditionChanged;
        }

        public override void OnDisposeTracking()
        {
            if (tracked != null) tracked.Changed -= RaiseConditionChanged;
            tracked = null;
        }
    }
}
