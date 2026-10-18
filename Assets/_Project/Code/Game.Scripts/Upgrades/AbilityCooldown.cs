using UnityEngine;

namespace Game.Scripts.Upgrades
{
    public sealed class AbilityCooldown
    {
        public float Remaining { get; private set; }
        public bool IsReady => Remaining <= 0f;

        public void Start(float seconds) => Remaining = Mathf.Max(Remaining, seconds);

        public void Tick(float deltaTime) => Remaining = Mathf.Max(0f, Remaining - deltaTime);
    }
}
