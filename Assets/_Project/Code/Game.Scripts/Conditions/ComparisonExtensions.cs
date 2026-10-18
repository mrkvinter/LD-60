using Code.Game.Core.ExecutorSystem.Conditions;

namespace Game.Scripts.Conditions
{
    public static class ComparisonExtensions
    {
        /// <summary>Whether <paramref name="value"/> stands to <paramref name="target"/> as the comparison asks; None never does.</summary>
        public static bool Matches(this ComparisonType comparison, int value, int target) => comparison switch
        {
            ComparisonType.Equals => value == target,
            ComparisonType.MoreThan => value > target,
            ComparisonType.LessThan => value < target,
            ComparisonType.LessOrEqual => value <= target,
            ComparisonType.MoreOrEqual => value >= target,
            _ => false
        };
    }
}
