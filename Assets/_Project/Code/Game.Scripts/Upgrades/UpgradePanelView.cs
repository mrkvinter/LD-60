using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Game.Scripts.Upgrades
{
    public sealed class UpgradePanelView : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;

        [Tooltip("In order: hands capacity, highlight, sort to racks, pull.")]
        [SerializeField] private UpgradeRowView[] rows = new UpgradeRowView[4];

        [Tooltip("Keys, levels and points in the texts the code writes.")]
        [SerializeField] private Color accent = new(0.95f, 0.76f, 0.11f, 1f);

        public TMP_Text Title => title;
        public IReadOnlyList<UpgradeRowView> Rows => rows;
        public Color Accent => accent;
    }
}
