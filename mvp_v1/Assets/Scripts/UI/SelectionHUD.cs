using System.Collections.Generic;
using GoblinSiege.Units;
using UnityEngine;
using UnityEngine.UI;

namespace GoblinSiege.UI
{
    /// <summary>
    /// Bottom-of-screen selection panel. Shows one card per selected squad:
    /// the squad type, alive/total count, and one color-coded HP dot per unit
    /// (green = healthy, yellow = wounded, red = critical, grey = dead).
    ///
    /// Lifecycle:
    ///   Call Refresh(selection) whenever the player's selection changes — this
    ///   rebuilds the card layout and caches the dot Images for per-frame HP color
    ///   updates in Update(). The layout rebuild costs one frame; the color updates
    ///   are just a Color.Lerp + property set per dot, so essentially free.
    ///
    /// WebGL-SAFE: only UGUI, built-in font, no TMP, no System.IO.
    /// GUARDRAIL G3: only allocates on selection change (layout rebuild), never
    ///   every frame. Shared materials are NOT used here (each dot needs its own
    ///   per-instance color), but there are at most ~15 dots — well within budget.
    /// </summary>
    public class SelectionHUD : MonoBehaviour
    {
        private RectTransform _panel;
        private Font _font;

        // Cached per-selection data for Update() HP coloring.
        private readonly List<Image>      _dots  = new();
        private readonly List<GoblinUnit> _units = new();

        // Card root GameObjects are destroyed and rebuilt on each Refresh call.
        private readonly List<GameObject> _cards = new();

        private const float PanelHeight = 96f;
        private const float CardWidth   = 170f;
        private const float CardPad     = 10f;

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildPanel();
        }

        // ─────────────────────────────────────────────────────────────────────
        // HP-dot color update — runs every frame, touches only the cached dots.
        // No allocations, no layout work; just set a color on an Image component.
        // ─────────────────────────────────────────────────────────────────────
        private void Update()
        {
            for (int i = 0; i < _dots.Count; i++)
            {
                if (_dots[i] == null) continue;
                GoblinUnit u = _units[i];
                if (u == null || !u.IsAlive)
                {
                    _dots[i].color = new Color(0.25f, 0.25f, 0.25f, 0.55f); // dead grey
                    continue;
                }
                float frac = u.MaxHp > 0f ? Mathf.Clamp01(u.Hp / u.MaxHp) : 0f;
                // green (full) → yellow (half) → red (critical)
                _dots[i].color = frac > 0.5f
                    ? Color.Lerp(new Color(0.95f, 0.85f, 0.1f), new Color(0.2f, 0.9f, 0.2f), (frac - 0.5f) * 2f)
                    : Color.Lerp(new Color(0.9f, 0.15f, 0.15f), new Color(0.95f, 0.85f, 0.1f), frac * 2f);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public API — called by SquadCommander whenever the selection changes.
        // ─────────────────────────────────────────────────────────────────────
        public void Refresh(IReadOnlyList<Squad> selection)
        {
            // Clear old cards + cached dot references.
            foreach (var c in _cards)
                if (c != null) Destroy(c);
            _cards.Clear();
            _dots.Clear();
            _units.Clear();

            if (selection == null || selection.Count == 0) return;

            // Centre the card row inside the panel.
            int validCount = 0;
            for (int i = 0; i < selection.Count; i++)
                if (selection[i] != null && !selection[i].IsDestroyed) validCount++;
            if (validCount == 0) return;

            float totalW  = validCount * CardWidth + (validCount - 1) * CardPad;
            float startX  = -totalW * 0.5f + CardWidth * 0.5f;
            int   placed  = 0;

            for (int i = 0; i < selection.Count; i++)
            {
                Squad sq = selection[i];
                if (sq == null || sq.IsDestroyed) continue;
                float x = startX + placed * (CardWidth + CardPad);
                BuildCard(sq, x);
                placed++;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Layout helpers
        // ─────────────────────────────────────────────────────────────────────
        private void BuildPanel()
        {
            // Own canvas so the selection bar's sorting order is independent.
            var canvasGo = new GameObject("SelectionHUD Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10; // draw above main HUD (order 0)
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            // Full-width panel pinned to the screen bottom.
            var panelGo = new GameObject("SelectionPanel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            var bg    = panelGo.AddComponent<Image>();
            bg.color  = new Color(0.05f, 0.05f, 0.08f, 0.82f);
            _panel    = bg.rectTransform;
            _panel.anchorMin       = new Vector2(0f, 0f);
            _panel.anchorMax       = new Vector2(1f, 0f);
            _panel.pivot           = new Vector2(0.5f, 0f);
            _panel.anchoredPosition = Vector2.zero;
            _panel.sizeDelta       = new Vector2(0f, PanelHeight);
        }

        private void BuildCard(Squad sq, float centreX)
        {
            var cardGo = new GameObject($"Card_{sq.SquadType}");
            cardGo.transform.SetParent(_panel, false);
            _cards.Add(cardGo);

            // Card background — slightly lighter than the panel.
            var cardImg   = cardGo.AddComponent<Image>();
            cardImg.color = new Color(0.15f, 0.15f, 0.20f, 1f);
            var rt = cardImg.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(centreX, 0f);
            rt.sizeDelta        = new Vector2(CardWidth, PanelHeight - CardPad * 2f);

            // Top label: "GRUNT  3 / 5"
            string typeLabel = sq.SquadType.ToString().ToUpper();
            int alive = sq.AliveCount();
            int total = sq.Members.Count;
            AddLabel(cardGo.transform, $"{typeLabel}  {alive} / {total}",
                     anchoredPos: new Vector2(0f, 26f), fontSize: 13,
                     color: new Color(0.9f, 0.85f, 0.5f));

            // Sub-label: "units"
            AddLabel(cardGo.transform, alive > 0 ? "ready" : "wiped",
                     anchoredPos: new Vector2(0f, 10f), fontSize: 10,
                     color: alive > 0 ? new Color(0.55f, 0.85f, 0.55f) : new Color(0.7f, 0.3f, 0.3f));

            // HP dots row.
            BuildHpDots(cardGo.transform, sq);
        }

        private void BuildHpDots(Transform parent, Squad sq)
        {
            const float dotSize  = 12f;
            const float dotGap   = 3f;
            int count = sq.Members.Count;
            float rowWidth = count * dotSize + (count - 1) * dotGap;
            float startX   = -rowWidth * 0.5f + dotSize * 0.5f;
            const float dotY = -20f; // below the labels

            for (int i = 0; i < count; i++)
            {
                GoblinUnit unit = sq.Members[i];

                var dotGo = new GameObject($"Dot{i}");
                dotGo.transform.SetParent(parent, false);
                var img = dotGo.AddComponent<Image>();

                // Initial color — Update() will animate this per frame.
                img.color = (unit != null && unit.IsAlive)
                    ? new Color(0.2f, 0.9f, 0.2f)
                    : new Color(0.25f, 0.25f, 0.25f, 0.55f);

                var drt = img.rectTransform;
                drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
                drt.pivot     = new Vector2(0.5f, 0.5f);
                drt.anchoredPosition = new Vector2(startX + i * (dotSize + dotGap), dotY);
                drt.sizeDelta        = new Vector2(dotSize, dotSize);

                _dots.Add(img);
                _units.Add(unit);
            }
        }

        private void AddLabel(Transform parent, string text, Vector2 anchoredPos, int fontSize, Color color)
        {
            var go = new GameObject("Lbl");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font      = _font;
            t.text      = text;
            t.color     = color;
            t.fontSize  = fontSize;
            t.alignment = TextAnchor.MiddleCenter;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = new Vector2(CardWidth - 8f, 20f);
        }
    }
}
