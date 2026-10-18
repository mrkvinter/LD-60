using UnityEngine;

namespace Game.Scripts.Player
{
    /// <summary>Gives the player an ability when they walk into it, then disappears. Not an item.</summary>
    public sealed class SyringePickup : MonoBehaviour
    {
        private const float PlayerCenterHeight = 0.9f;

        [SerializeField] private PlayerAbility ability;
        [SerializeField, Min(0.1f)] private float radius = 1f;

        private PlayerAdapter player;

        public PlayerAbility Ability => ability;

        private void Update()
        {
            if (player == null) player = FindFirstObjectByType<PlayerAdapter>();
            var abilities = player != null ? player.Entity?.GetComponent<PlayerAbilitiesComponent>() : null;
            if (abilities == null) return;

            var center = player.transform.position + Vector3.up * PlayerCenterHeight;
            if ((center - transform.position).sqrMagnitude > radius * radius) return;

            abilities.Unlock(ability);
            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
