using System;
using System.Collections.Generic;
using Code.Game.Scripts.EntitySystem;

namespace Game.Scripts.Player
{
    public enum PlayerAbility
    {
        Sprint,
        Jump,
        DoubleJump
    }

    /// <summary>Movement the player has unlocked, e.g. by syringes. A double jump brings the plain jump with it.</summary>
    public sealed class PlayerAbilitiesComponent : IEntityComponent
    {
        private readonly HashSet<PlayerAbility> unlocked = new();

        public event Action Changed;

        /// <summary>Raised with the ability asked for when the player did not have it yet.</summary>
        public event Action<PlayerAbility> Unlocked;

        public PlayerAbilitiesComponent(IEnumerable<PlayerAbility> start = null)
        {
            if (start == null) return;

            foreach (var ability in start) Add(ability);
        }

        public bool Has(PlayerAbility ability) => unlocked.Contains(ability);

        /// <summary>Returns false when the player already had it.</summary>
        public bool Unlock(PlayerAbility ability)
        {
            if (!Add(ability)) return false;

            Changed?.Invoke();
            Unlocked?.Invoke(ability);
            return true;
        }

        private bool Add(PlayerAbility ability)
        {
            var added = unlocked.Add(ability);
            if (ability == PlayerAbility.DoubleJump) added |= unlocked.Add(PlayerAbility.Jump);
            return added;
        }
    }
}
