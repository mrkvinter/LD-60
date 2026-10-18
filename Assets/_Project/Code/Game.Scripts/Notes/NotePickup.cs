using Code.Game.Core;
using Game.Scripts.Player;
using Game.Scripts.UI;
using KvinterGames;
using RG.DefinitionSystem.Core;
using UnityEngine;

namespace Game.Scripts.Notes
{
    /// <summary>A note lying in the level: walking into it opens it in the hand, then it disappears. Not an item.</summary>
    public sealed class NotePickup : MonoBehaviour
    {
        private const float PlayerCenterHeight = 0.9f;

        [SerializeField] private DefRef<NoteDef> note;
        [SerializeField, Min(0.1f)] private float radius = 1f;

        private PlayerAdapter player;
        private NoteView view;

        private void Update()
        {
            if (player == null) player = FindAnyObjectByType<PlayerAdapter>();
            if (view == null) view = FindAnyObjectByType<NoteView>();
            if (player == null || view == null || note.IsEmpty) return;

            var center = player.transform.position + Vector3.up * PlayerCenterHeight;
            if ((center - transform.position).sqrMagnitude > radius * radius) return;

            // Another note or a menu may be open; this one waits until the player walks in again.
            if (!view.TryShow(DefManager.GetDef(note))) return;

            G.AudioService.Sound("TakeNote").WithRandomPitch(0.05f).Play();
            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.4f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
