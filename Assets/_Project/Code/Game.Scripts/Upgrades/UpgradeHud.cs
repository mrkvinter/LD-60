using System;
using System.Collections.Generic;
using Code.Cursor;
using Code.Game.Scripts.EntitySystem;
using DG.Tweening;
using Game.Core.Contexts;
using Game.Scripts.Construction;
using Game.Scripts.Interaction;
using Game.Scripts.Items;
using Game.Scripts.Player;
using RG.DefinitionSystem.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Scripts.Upgrades
{
    /// <summary>
    /// Screen overlay built in code: the name of what the crosshair is on, a popup for earned points and the upgrade
    /// panel on Tab. While the panel is open the cursor is free and the player stands still. Points and ability hints
    /// are on the note in the hand (TipsView).
    /// </summary>
    public sealed class UpgradeHud : MonoBehaviour
    {
        private const float PointsFadeTime = 0.2f;
        private const float PointsHoldTime = 2f;

        private static readonly UpgradeKind[] kinds =
            { UpgradeKind.HandsCapacity, UpgradeKind.Highlight, UpgradeKind.Distribute, UpgradeKind.Pull };

        private static readonly Dictionary<UpgradeKind, string> titles = new()
        {
            [UpgradeKind.HandsCapacity] = "Hands capacity",
            [UpgradeKind.Highlight] = "Highlight same items [Q]",
            [UpgradeKind.Distribute] = "Sort hands to racks [R]",
            [UpgradeKind.Pull] = "Pull same items [F]"
        };

        private readonly RaycastHit[] hits = new RaycastHit[16];
        private readonly List<InputAction> actions = new();
        private readonly TMP_Text[] rowTexts = new TMP_Text[4];
        private readonly Button[] rowButtons = new Button[4];
        private readonly TMP_Text[] rowButtonTexts = new TMP_Text[4];

        private UpgradeService upgrades;
        private CursorService cursor;
        private PlayerSettingsDef playerSettings;
        private Entity player;
        private AimComponent aim;
        private PlayerInputComponent input;
        private Transform view;

        private TMP_Text hoverText;
        private TMP_Text panelTitle;
        private GameObject panel;
        private TMP_Text pointsText;
        private CanvasGroup pointsPopup;
        private Sequence pointsFade;
        private int pointsShown;

        private void Awake()
        {
            upgrades = GameServiceLocator.Resolve<UpgradeService>();
            cursor = GameServiceLocator.Resolve<CursorService>();
            playerSettings = DefManager.GetSingletonDef<PlayerSettingsDef>();

            BuildCanvas();
            upgrades.PointsEarned += ShowPointsEarned;

            Bind("<Keyboard>/tab", TogglePanel);
            for (var i = 0; i < kinds.Length; i++)
            {
                var kind = kinds[i];
                Bind($"<Keyboard>/{i + 1}", () =>
                {
                    if (panel.activeSelf) upgrades.TryBuy(kind);
                });
            }
        }

        private void OnDestroy()
        {
            foreach (var action in actions) action.Dispose();
            actions.Clear();

            upgrades.PointsEarned -= ShowPointsEarned;
            pointsFade?.Kill();

            if (panel != null && panel.activeSelf) ClosePanel();
        }

        private void Update()
        {
            if (player == null && !FindPlayer()) return;

            hoverText.text = DescribeAimed();
            if (panel.activeSelf) RefreshPanel();
        }

        private bool FindPlayer()
        {
            var adapter = FindFirstObjectByType<PlayerAdapter>();
            if (adapter == null || adapter.Entity == null) return false;

            player = adapter.Entity;
            aim = player.GetComponent<AimComponent>();
            input = player.GetComponent<PlayerInputComponent>();
            view = player.GetComponent<CameraAnchorComponent>()?.Transform;
            return true;
        }

        // The aimed item first; otherwise the rack or construction site the crosshair is on.
        private string DescribeAimed()
        {
            if (aim?.Target != null) return aim.Target.Def.Name;
            if (view == null) return string.Empty;

            var count = Physics.RaycastNonAlloc(view.position, view.forward, hits, playerSettings.InteractDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            Array.Sort(hits, 0, count, Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance)));

            for (var i = 0; i < count; i++)
            {
                var entity = hits[i].collider.GetComponentInParent<IEntityAdapter>()?.BaseEntity;
                if (ReferenceEquals(entity, player)) continue;
                if (hits[i].collider.isTrigger && entity is not ConstructionSite) continue;

                return entity switch
                {
                    Rack rack => rack.StoredDef != null
                        ? $"{rack.Type} rack: {rack.StoredDef.Name} {rack.Count}/{rack.Capacity}"
                        : $"{rack.Type} rack: empty",
                    ConstructionSite { IsBuilt: false } site => $"{site.Name} (needs building)",
                    _ => string.Empty
                };
            }

            return string.Empty;
        }

        // Points that come while the popup is up add to it, so filling several racks at once reads as one sum.
        private void ShowPointsEarned(int points)
        {
            pointsShown += points;
            pointsText.text = $"+{pointsShown} {(pointsShown == 1 ? "POINT" : "POINTS")}\n<size=50%>[TAB] Upgrade</size>";

            pointsFade?.Kill();
            pointsText.rectTransform.localScale = Vector3.one;
            pointsFade = DOTween.Sequence()
                .Append(DOTween.To(() => pointsPopup.alpha, value => pointsPopup.alpha = value, 1f, PointsFadeTime))
                .Join(pointsText.rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.35f, 6))
                .AppendInterval(PointsHoldTime)
                .Append(DOTween.To(() => pointsPopup.alpha, value => pointsPopup.alpha = value, 0f, PointsFadeTime * 2f))
                .OnComplete(() => pointsShown = 0);
        }

        private void RefreshPanel()
        {
            panelTitle.text = $"Upgrades    points: {upgrades.Points}";
            for (var i = 0; i < kinds.Length; i++)
            {
                var kind = kinds[i];
                var next = upgrades.Next(kind);
                rowTexts[i].text = $"{i + 1}. {titles[kind]}   Lv {upgrades.Level(kind)}/{upgrades.MaxLevel(kind)}\n" +
                                   $"<size=80%>{Describe(kind, upgrades.Current(kind))}  >  {Describe(kind, next)}</size>";
                rowButtons[i].interactable = upgrades.CanBuy(kind);
                rowButtonTexts[i].text = next == null ? "Max" : $"Buy ({next.Cost})";
            }
        }

        private string Describe(UpgradeKind kind, UpgradeLevel level)
        {
            if (level == null) return kind == UpgradeKind.HandsCapacity ? $"{CapacityAt(upgrades.Level(kind))} slots" : "-";

            return kind switch
            {
                UpgradeKind.HandsCapacity => $"{CapacityAt(Array.IndexOf(Levels(kind), level) + 1)} slots",
                UpgradeKind.Highlight => $"{level.Amount:0}s, cooldown {level.Cooldown:0}s",
                UpgradeKind.Distribute => $"cooldown {level.Cooldown:0}s",
                UpgradeKind.Pull => $"{level.Amount:0} items, cooldown {level.Cooldown:0}s",
                _ => string.Empty
            };
        }

        private int CapacityAt(int level)
        {
            var table = Levels(UpgradeKind.HandsCapacity);
            var capacity = playerSettings.HandsCapacity;
            for (var i = 0; i < level && i < table.Length; i++) capacity += Mathf.RoundToInt(table[i].Amount);
            return capacity;
        }

        private static UpgradeLevel[] Levels(UpgradeKind kind) => DefManager.GetSingletonDef<UpgradeSettingsDef>().Levels(kind);

        // Another window (a note) holding the player keeps the panel shut: both would fight over the cursor.
        private void TogglePanel()
        {
            if (panel.activeSelf) ClosePanel();
            else if (input is not { IsBlocked: true }) OpenPanel();
        }

        private void OpenPanel()
        {
            panel.SetActive(true);
            if (input != null) input.IsBlocked = true;
            cursor.ReleaseLock();
            RefreshPanel();
        }

        private void ClosePanel()
        {
            panel.SetActive(false);
            if (input != null) input.IsBlocked = false;
            cursor.RequestLock();
        }

        private void Bind(string binding, Action perform)
        {
            var action = new InputAction(binding: binding);
            action.performed += _ => perform();
            action.Enable();
            actions.Add(action);
        }

        private void BuildCanvas()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            hoverText = Text(transform, "Hover", 30, TextAlignmentOptions.Top);
            Place(hoverText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(900f, 60f), new Vector2(0.5f, 1f));

            pointsText = Text(transform, "Points Popup", 56, TextAlignmentOptions.Top);
            Place(pointsText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(900f, 160f), new Vector2(0.5f, 1f));
            pointsPopup = pointsText.gameObject.AddComponent<CanvasGroup>();
            pointsPopup.alpha = 0f;
            pointsPopup.interactable = false;
            pointsPopup.blocksRaycasts = false;

            panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            panel.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.1f, 0.88f);
            Place((RectTransform)panel.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 560f), new Vector2(0.5f, 0.5f));

            panelTitle = Text(panel.transform, "Title", 40, TextAlignmentOptions.Top);
            Place(panelTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(900f, 60f), new Vector2(0.5f, 1f));

            for (var i = 0; i < kinds.Length; i++)
            {
                var y = -110f - i * 100f;
                rowTexts[i] = Text(panel.transform, $"Row {i + 1}", 28, TextAlignmentOptions.MidlineLeft);
                Place(rowTexts[i].rectTransform, new Vector2(0f, 1f), new Vector2(40f, y), new Vector2(680f, 90f), new Vector2(0f, 1f));

                var kind = kinds[i];
                var button = new GameObject($"Buy {i + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
                button.transform.SetParent(panel.transform, false);
                button.GetComponent<Image>().color = new Color(0.25f, 0.55f, 0.85f, 1f);
                Place((RectTransform)button.transform, new Vector2(1f, 1f), new Vector2(-40f, y - 15f), new Vector2(200f, 60f), new Vector2(1f, 1f));
                rowButtons[i] = button.GetComponent<Button>();
                rowButtons[i].onClick.AddListener(() => upgrades.TryBuy(kind));
                rowButtonTexts[i] = Text(button.transform, "Label", 28, TextAlignmentOptions.Center);
                var labelRect = rowButtonTexts[i].rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            }

            var hint = Text(panel.transform, "Hint", 24, TextAlignmentOptions.Bottom);
            hint.text = "Fill a rack with trash to earn a point.   1-4 or click to buy,   Tab to close.";
            Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(900f, 40f), new Vector2(0.5f, 0f));

            panel.SetActive(false);
        }

        private static TMP_Text Text(Transform parent, string name, float size, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
