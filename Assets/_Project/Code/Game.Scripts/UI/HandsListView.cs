using System.Text;
using Game.Scripts.Items;
using Game.Scripts.Player;
using TMPro;
using UnityEngine;

namespace Game.Scripts.UI
{
    /// <summary>Writes what the player carries on the note in the hand; the active item, the one scrolled to, is marked.</summary>
    public sealed class HandsListView : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private string title = "IN HANDS";
        [SerializeField] private Color activeColor = new(0.8f, 0.3f, 0.05f, 1f);

        [Tooltip("Item lines shown at most; a longer list scrolls to keep the active item in view.")]
        [SerializeField, Min(1)] private int maxLines = 15;

        private readonly StringBuilder builder = new();
        private ItemHandsComponent hands;

        private void Reset()
        {
            text = GetComponentInChildren<TMP_Text>();
        }

        // The player entity appears once the game state has initialized the scene, after this object wakes.
        private void Update()
        {
            if (hands != null) return;

            var player = FindAnyObjectByType<PlayerAdapter>();
            hands = player != null ? player.Entity?.GetComponent<ItemHandsComponent>() : null;
            if (hands == null) return;

            hands.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (hands != null) hands.Changed -= Refresh;
        }

        private void Refresh()
        {
            if (text == null) return;

            builder.Clear();
            builder.Append("<size=150%>").Append(title).Append(' ').Append(hands.Count).Append('/')
                .Append(hands.Capacity).Append("</size>\n\n");

            if (hands.Count == 0)
            {
                builder.Append("- empty -");
                text.text = builder.ToString();
                return;
            }

            var first = Mathf.Clamp(hands.ActiveIndex - maxLines / 2, 0, Mathf.Max(0, hands.Count - maxLines));
            var last = Mathf.Min(hands.Count, first + maxLines);
            var color = ColorUtility.ToHtmlStringRGB(activeColor);

            if (first > 0) builder.Append("...\n");
            for (var i = first; i < last; i++)
            {
                var name = hands.Items[i].Def.Name;
                if (i == hands.ActiveIndex)
                {
                    builder.Append("<b><color=#").Append(color).Append(">> ").Append(name).Append("</color></b>\n");
                }
                else
                {
                    builder.Append("- ").Append(name).Append('\n');
                }
            }

            if (last < hands.Count) builder.Append("...");

            text.text = builder.ToString();
        }
    }
}
