using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Upgrades
{
    public sealed class UpgradeRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private Button buyButton;
        [SerializeField] private TMP_Text buyLabel;

        public TMP_Text Text => text;
        public Button BuyButton => buyButton;
        public TMP_Text BuyLabel => buyLabel;
    }
}
