using System;
using Code.Game.Core.ExecutorSystem.Conditions;
using Game.Core.Contexts;
using Game.Scripts.Construction;
using RG.DefinitionSystem.Core;
using UnityEngine;

namespace Game.Scripts.Conditions
{
    [Serializable]
    public class BuildingBuiltCondition : Condition
    {
        private static ConstructionProgress Progress => GameServiceLocator.Resolve<ConstructionProgress>();

        [SerializeField] private DefRef<BuildingDef> building;
        [SerializeField] private ComparisonType comparisonType = ComparisonType.MoreOrEqual;
        [SerializeField, Min(0)] private int count = 1;

        // The game scope may already be torn down when the last tracked object goes away.
        private ConstructionProgress tracked;

        public override string Description => $"{building.Id} built {comparisonType} {count}";

        public override bool Execute() => comparisonType.Matches(Progress.BuiltCount(building.Id), count);

        public override void OnStartTracking()
        {
            tracked = Progress;
            tracked.Changed += RaiseConditionChanged;
        }

        public override void OnDisposeTracking()
        {
            if (tracked != null) tracked.Changed -= RaiseConditionChanged;
            tracked = null;
        }
    }
}
