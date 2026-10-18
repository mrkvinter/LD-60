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
using Game.Scripts.UI;
using RG.DefinitionSystem.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Scripts.Upgrades
{
    /// <summary>
    /// Screen overlay: the name of what the crosshair is on, a popup for earned points and the upgrade panel on Tab
    /// (the UpgradePanel prefab from HudSettingsDef). While the panel is open the cursor is free and the player stands
    /// still. Points and ability hints are on the note in the hand (TipsView).
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

        private UpgradeService upgrades;
        private CursorService cursor;
        private PlayerSettingsDef playerSettings;
        private Entity player;
        private AimComponent aim;
        private PlayerInputComponent input;
        private Transform view;

        private TMP_Text hoverText;
        private UpgradePanelView panel;
        private TMP_Text pointsText;
        private CanvasGroup pointsPopup;
        private Sequence pointsFade;
        private int pointsShown;

        private bool IsPanelOpen => panel != null && panel.gameObject.activeSelf;

        private void Awake()
        {
            upgrades = GameServiceLocator.Resolve<UpgradeService>();
            cursor = GameServiceLocator.Resolve<CursorService>();
            playerSettings = DefManager.GetSingletonDef<PlayerSettingsDef>();

            BuildCanvas();
            CreatePanel();
            upgrades.PointsEarned += ShowPointsEarned;

            Bind("<Keyboard>/tab", TogglePanel);
            for (var i = 0; i < kinds.Length; i++)
            {
                var kind = kinds[i];
                Bind($"<Keyboard>/{i + 1}", () =>
                {
                    if (IsPanelOpen) upgrades.TryBuy(kind);
                });
            }
        }

        private void OnDestroy()
        {
            foreach (var action in actions) action.Dispose();
            actions.Clear();

            upgrades.PointsEarned -= ShowPointsEarned;
            pointsFade?.Kill();

            if (IsPanelOpen) ClosePanel();
        }

        private void Update()
        {
            if (player == null && !FindPlayer()) return;

            hoverText.text = DescribeAimed();
            if (IsPanelOpen) RefreshPanel();
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
            var accent = ColorUtility.ToHtmlStringRGB(panel.Accent);
            if (panel.Title != null) panel.Title.text = $"UPGRADES   <color=#{accent}>{upgrades.Points}</color> PTS";

            for (var i = 0; i < kinds.Length && i < panel.Rows.Count; i++)
            {
                var row = panel.Rows[i];
                if (row == null) continue;

                var kind = kinds[i];
                var next = upgrades.Next(kind);
                if (row.Text != null)
                {
                    row.Text.text = $"<color=#{accent}>[{i + 1}]</color>  {titles[kind]}   " +
                                    $"<color=#{accent}>LV {upgrades.Level(kind)}/{upgrades.MaxLevel(kind)}</color>\n" +
                                    $"<size=75%><alpha=#99>{Describe(kind, upgrades.Current(kind))}  >  {Describe(kind, next)}</size>";
                }

                if (row.BuyButton != null) row.BuyButton.interactable = upgrades.CanBuy(kind);
                if (row.BuyLabel != null) row.BuyLabel.text = next == null ? "MAX" : $"BUY  {next.Cost} PTS";
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
            if (panel == null) return;

            if (IsPanelOpen) ClosePanel();
            else if (input is not { IsBlocked: true }) OpenPanel();
        }

        private void OpenPanel()
        {
            panel.gameObject.SetActive(true);
            if (input != null) input.IsBlocked = true;
            cursor.ReleaseLock();
            RefreshPanel();
        }

        private void ClosePanel()
        {
            panel.gameObject.SetActive(false);
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

            var popupFont = DefManager.GetSingletonDef<HudSettingsDef>().PopupFont;

            hoverText = Text(transform, "Hover", 30, TextAlignmentOptions.Top);
            Place(hoverText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(900f, 60f), new Vector2(0.5f, 1f));
            if (popupFont != null) hoverText.font = popupFont;

            pointsText = Text(transform, "Points Popup", 56, TextAlignmentOptions.Top);
            Place(pointsText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(900f, 160f), new Vector2(0.5f, 1f));
            if (popupFont != null) pointsText.font = popupFont;
            pointsPopup = pointsText.gameObject.AddComponent<CanvasGroup>();
            pointsPopup.alpha = 0f;
            pointsPopup.interactable = false;
            pointsPopup.blocksRaycasts = false;
        }

        private void CreatePanel()
        {
            var prefab = DefManager.GetSingletonDef<HudSettingsDef>().UpgradePanel;
            panel = prefab != null ? Instantiate(prefab, transform).GetComponent<UpgradePanelView>() : null;
            if (panel == null)
            {
                Debug.LogError($"{nameof(HudSettingsDef)}.{nameof(HudSettingsDef.UpgradePanel)} has no {nameof(UpgradePanelView)}; Tab opens nothing.");
                return;
            }

            for (var i = 0; i < kinds.Length && i < panel.Rows.Count; i++)
            {
                var kind = kinds[i];
                var button = panel.Rows[i] != null ? panel.Rows[i].BuyButton : null;
                if (button != null) button.onClick.AddListener(() => upgrades.TryBuy(kind));
            }

            panel.gameObject.SetActive(false);
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
