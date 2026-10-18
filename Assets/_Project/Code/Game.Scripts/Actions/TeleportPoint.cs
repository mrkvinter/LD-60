using UnityEngine;

namespace Game.Scripts.Actions
{
    public sealed class TeleportPoint : MonoBehaviour
    {
        [Tooltip("What actions call this point by. Must be unique in the scene.")]
        [SerializeField] private string id;

        public string Id => id;

        private void Reset()
        {
            id = name;
        }

        public static TeleportPoint Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            foreach (var point in FindObjectsByType<TeleportPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (point.id == id) return point;
            }

            return null;
        }

        private void OnDrawGizmos()
        {
            var position = transform.position;
            var forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
            Gizmos.DrawWireSphere(position + Vector3.up * 0.3f, 0.3f);
            Gizmos.DrawLine(position, position + Vector3.up * 1.8f);
            Gizmos.DrawLine(position + Vector3.up * 1.6f, position + Vector3.up * 1.6f + forward * 0.8f);
        }
    }
}
