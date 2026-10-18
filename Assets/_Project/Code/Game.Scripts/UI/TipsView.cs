using System.Text;
using Game.Core.Contexts;
using Game.Scripts.Player;
using Game.Scripts.Upgrades;
using TMPro;
using UnityEngine;

namespace Game.Scripts.UI
{
    /// <summary>The key hints on the note in the hand: the hands keys, points, the bought hand abilities with their cooldowns, the upgrade panel.</summary>
    public sealed class TipsView : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;

        private readonly StringBuilder builder = new();
        private UpgradeService upgrades;
        private HandsAbilitiesComponent abilities;

        private void Reset()
        {
            text = GetComponent<TMP_Text>();
        }

        // The player entity and the services appear once the game state has initialized the scene, after this object wakes.
        private void Update()
        {
            if (text == null) return;
            if (abilities == null && !Bind()) return;

            builder.Clear();
            builder.Append("[LMB] Pick up / Put\n[RMB] Take from shelf\n[G] Throw\n[C] Crouch\n");
            builder.Append("Points: ").Append(upgrades.Points);
            AppendAbility(UpgradeKind.Highlight, "Q", "Highlight");
            AppendAbility(UpgradeKind.Distribute, "R", "Sort");
            AppendAbility(UpgradeKind.Pull, "F", "Pull");
            builder.Append("\n[TAB] Upgrade");
            text.SetText(builder);
        }

        private bool Bind()
        {
            var player = FindAnyObjectByType<PlayerAdapter>();
            abilities = player != null ? player.Entity?.GetComponent<HandsAbilitiesComponent>() : null;
            if (abilities == null) return false;

            upgrades = GameServiceLocator.Resolve<UpgradeService>();
            return true;
        }

        private void AppendAbility(UpgradeKind kind, string key, string title)
        {
            if (upgrades.Level(kind) == 0) return;

            builder.Append("\n[").Append(key).Append("] ").Append(title).Append(' ');
            if (kind == UpgradeKind.Highlight && abilities.HighlightLeft > 0f)
            {
                builder.Append("on ").Append(Mathf.CeilToInt(abilities.HighlightLeft)).Append('s');
                return;
            }

            var cooldown = abilities.Cooldown(kind);
            if (cooldown.IsReady) builder.Append("ready");
            else builder.Append(Mathf.CeilToInt(cooldown.Remaining)).Append('s');
        }
    }
}
