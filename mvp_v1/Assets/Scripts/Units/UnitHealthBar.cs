using UnityEngine;

namespace GoblinSiege.Units
{
    // ═══════════════════════════════════════════════════════════════════════════
    // UnitHealthBar — a tiny floating HP bar over EVERY combat unit (Warren's ask).
    // ═══════════════════════════════════════════════════════════════════════════
    // WHAT IT DOES
    //   Shows a small green→red bar just above each unit's head so the player can
    //   read, at a glance, how hurt every goblin, human, and the Warlord is. The bar
    //   is built entirely in code (two quads: a dark backdrop + a colored fill), so
    //   it needs zero art and is WebGL-safe.
    //
    // WHY A SEPARATE, UN-PARENTED VISUAL
    //   Units have wildly different local scales (a 0.55× goblin primitive, a 1.04×
    //   Warlord, a 0.71× human). If the bar were a CHILD of the unit it would inherit
    //   that scale and look tiny or huge. Instead the bar lives on its OWN root
    //   GameObject that simply FOLLOWS the unit every LateUpdate and billboards to the
    //   camera — so every bar is the exact same on-screen size regardless of the unit.
    //
    // LIFECYCLE
    //   This component is added to every Unit in Unit.Awake. It creates its bar in
    //   Start (once the unit's real stats are applied) and destroys it when the unit
    //   is disabled/destroyed (death-pop deactivates the unit), so no orphan bars are
    //   ever left floating in the scene (G3: tidy, no leaks).
    //
    // GUARDRAILS
    //   G2 (flat play): the bar only affects visuals, never physics/targeting.
    //   G3 (perf): shared unlit materials cached statically; per-bar color via a
    //     MaterialPropertyBlock (no per-frame material allocation). No GetComponent in
    //     Update. G5 (frame-rate independent): pure transform follow, no time math.
    // ═══════════════════════════════════════════════════════════════════════════
    [RequireComponent(typeof(Unit))]
    public sealed class UnitHealthBar : MonoBehaviour
    {
        // On-screen size of the bar, in world metres.
        private const float BarWidth  = 1.0f;
        private const float BarHeight = 0.2f;
        // Thickness of the black frame drawn around every bar (world metres). This
        // is what gives each bar a crisp, clearly-visible start and end edge so
        // neighbouring bars never blur together.
        private const float Border    = 0.045f;
        // How far above the unit's feet-pivot the bar floats. Units are ~1.8 m tall;
        // a little extra clears the tallest (the Warlord) without floating too high.
        private const float HeightAboveUnit = 2.15f;

        // ONE shared unlit material for every bar piece (G3). Per-piece color is
        // applied with a MaterialPropertyBlock so this material is never duplicated.
        private static Material _sharedBarMat;

        private Unit _unit;
        private Transform _camTf;

        private Transform _root;      // billboarded holder that follows the unit
        private Transform _fillTf;    // the colored fill quad (scaled by hp fraction)
        private Renderer _fillRenderer;
        private MaterialPropertyBlock _mpb;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            _unit = GetComponent<Unit>();
        }

        private void Start()
        {
            BuildBar();
        }

        private void BuildBar()
        {
            _mpb = new MaterialPropertyBlock();

            // Inner area (inside the black frame) that the track + fill live in.
            float innerW = BarWidth  - Border * 2f;
            float innerH = BarHeight - Border * 2f;

            // Holder root — its own object so unit scale never distorts the bar.
            var rootGo = new GameObject($"{name}_HealthBar");
            _root = rootGo.transform;

            // 1) Black frame — full size, sits at the back. Because the track and
            //    fill are smaller and drawn in front, this leaves a crisp dark
            //    border on all four sides so every bar's edges are obvious.
            Transform frame = MakeQuad("HP_Frame", _root, new Color(0f, 0f, 0f, 1f));
            frame.localPosition = Vector3.zero;
            frame.localScale = new Vector3(BarWidth, BarHeight, 1f);

            // 2) Dark track — the "empty" part of the bar, so you can always see how
            //    much health is missing against a clean background.
            Transform track = MakeQuad("HP_Track", _root, new Color(0.12f, 0.12f, 0.12f, 1f));
            track.localPosition = new Vector3(0f, 0f, -0.01f);
            track.localScale = new Vector3(innerW, innerH, 1f);

            // 3) Colored fill — sits in front and shrinks toward the LEFT edge as the
            //    unit loses health (green → yellow → red).
            _fillTf = MakeQuad("HP_Fill", _root, Color.green);
            _fillTf.localPosition = new Vector3(0f, 0f, -0.02f);
            _fillTf.localScale = new Vector3(innerW, innerH, 1f);
            _fillRenderer = _fillTf.GetComponent<Renderer>();

            if (Camera.main != null) _camTf = Camera.main.transform;
        }

        // Builds one unlit, collider-free quad tinted via MaterialPropertyBlock.
        private Transform MakeQuad(string quadName, Transform parent, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = quadName;
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rend = go.GetComponent<Renderer>();
            rend.sharedMaterial = GetSharedBarMaterial();
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;

            var block = new MaterialPropertyBlock();
            block.SetColor(ColorId, color);
            block.SetColor(BaseColorId, color);
            rend.SetPropertyBlock(block);

            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static Material GetSharedBarMaterial()
        {
            // Unity-null check survives domain reloads/play-stop.
            if (_sharedBarMat != null) return _sharedBarMat;

            // Sprites/Default is unlit AND double-sided (no back-face culling), so a
            // billboarded quad is always visible no matter which way it faces the
            // camera. Fall back across pipelines if it's ever unavailable.
            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
            _sharedBarMat = new Material(sh);
            return _sharedBarMat;
        }

        private void LateUpdate()
        {
            if (_root == null) return;

            // Hide the bar the instant the unit dies or leaves play.
            if (_unit == null || !_unit.IsAlive)
            {
                if (_root.gameObject.activeSelf) _root.gameObject.SetActive(false);
                return;
            }

            // Alive: make sure the bar is showing (handles pooled units revived for a
            // new raid after having been hidden on death).
            if (!_root.gameObject.activeSelf) _root.gameObject.SetActive(true);

            // Follow the unit and float above its head.
            _root.position = transform.position + Vector3.up * HeightAboveUnit;

            // Billboard: face the camera (re-acquire if the camera appears late).
            if (_camTf == null)
            {
                if (Camera.main == null) return;
                _camTf = Camera.main.transform;
            }
            _root.rotation = Quaternion.LookRotation(_root.position - _camTf.position);

            // Update fill: shrink toward the LEFT edge inside the frame.
            float innerW = BarWidth  - Border * 2f;
            float innerH = BarHeight - Border * 2f;
            float frac = _unit.MaxHp > 0f ? Mathf.Clamp01(_unit.Hp / _unit.MaxHp) : 0f;
            _fillTf.localScale = new Vector3(innerW * frac, innerH, 1f);
            _fillTf.localPosition = new Vector3(-innerW * 0.5f * (1f - frac), 0f, -0.02f);

            Color c = frac > 0.5f
                ? Color.Lerp(Color.yellow, Color.green, (frac - 0.5f) * 2f)
                : Color.Lerp(Color.red, Color.yellow, frac * 2f);
            _fillRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(ColorId, c);
            _mpb.SetColor(BaseColorId, c);
            _fillRenderer.SetPropertyBlock(_mpb);
        }

        private void OnDisable()
        {
            if (_root != null) _root.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_root != null) Destroy(_root.gameObject);
        }
    }
}
