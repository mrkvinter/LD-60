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

        public TMP_Text Title => title;
        public IReadOnlyList<UpgradeRowView> Rows => rows;
    }
}
