using System;
using System.Collections.Generic;
using GoblinSiege.Systems;
using GoblinSiege.Units;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GoblinSiege.Player
{
    /// <summary>
    /// Real-time squad command (spec section 3): select a squad with number keys (or click),
    /// then right-click / Order to move it in formation toward the pointer.
    /// You command BANDS, not individual units. NEW INPUT SYSTEM ONLY.
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public class SquadCommander : MonoBehaviour
    {
        [SerializeField] private RaidManager raid;
        [SerializeField] private Camera worldCamera;

        private PlayerInput _input;
        private InputAction _pointAction;
        private InputAction _orderAction;
        private InputAction _selectSquad1;
        private InputAction _selectSquad2;
        private InputAction _selectSquad3;
        private InputAction _selectSquad4;
        private InputAction _selectAll;

        private readonly List<Squad> _selection = new();

        /// <summary>Fires after the selection changes. Arg: current selection (read-only).</summary>
        public event Action<IReadOnlyList<Squad>> OnSelectionChanged;

        /// <summary>Runtime injection for RaidBootstrap (no scene wiring).</summary>
        public void Setup(RaidManager raidRef, Camera cam)
        {
            raid = raidRef;
            worldCamera = cam;
        }

        private void Awake()
        {
            _input = GetComponent<PlayerInput>();
            if (worldCamera == null) worldCamera = Camera.main;
        }

        private void OnEnable()
        {
            _pointAction = _input.actions["Point"];
            _orderAction = _input.actions["Order"];
            _selectSquad1 = _input.actions["SelectSquad1"];
            _selectSquad2 = _input.actions["SelectSquad2"];
            _selectSquad3 = _input.actions["SelectSquad3"];
            // Squad #4 (the Sapper band) — keys 1-3 could never reach it before.
            _selectSquad4 = _input.actions["SelectSquad4"];
            _selectAll = _input.actions["SelectAll"];

            _orderAction.performed += OnOrder;
            _selectSquad1.performed += OnSelect1;
            _selectSquad2.performed += OnSelect2;
            _selectSquad3.performed += OnSelect3;
            if (_selectSquad4 != null) _selectSquad4.performed += OnSelect4;
            _selectAll.performed += OnSelectAll;
        }

        private void OnDisable()
        {
            if (_orderAction != null) _orderAction.performed -= OnOrder;
            if (_selectSquad1 != null) _selectSquad1.performed -= OnSelect1;
            if (_selectSquad2 != null) _selectSquad2.performed -= OnSelect2;
            if (_selectSquad3 != null) _selectSquad3.performed -= OnSelect3;
            if (_selectSquad4 != null) _selectSquad4.performed -= OnSelect4;
            if (_selectAll != null) _selectAll.performed -= OnSelectAll;
        }

        private void OnSelect1(InputAction.CallbackContext _) => SelectOnly(0);
        private void OnSelect2(InputAction.CallbackContext _) => SelectOnly(1);
        private void OnSelect3(InputAction.CallbackContext _) => SelectOnly(2);
        private void OnSelect4(InputAction.CallbackContext _) => SelectOnly(3);

        private void OnSelectAll(InputAction.CallbackContext _)
        {
            ClearSelection();
            if (raid == null) return;
            for (int i = 0; i < raid.Squads.Count; i++)
            {
                Squad s = raid.Squads[i];
                if (s != null && !s.IsDestroyed)
                {
                    s.SetSelected(true);
                    _selection.Add(s);
                }
            }
            OnSelectionChanged?.Invoke(_selection);
        }

        private void SelectOnly(int index)
        {
            if (raid == null || index < 0 || index >= raid.Squads.Count) return;
            Squad s = raid.Squads[index];
            if (s == null || s.IsDestroyed) return;
            ClearSelection();
            s.SetSelected(true);
            _selection.Add(s);
            OnSelectionChanged?.Invoke(_selection);
        }

        private void ClearSelection()
        {
            for (int i = 0; i < _selection.Count; i++)
                if (_selection[i] != null) _selection[i].SetSelected(false);
            _selection.Clear();
        }

        private void OnOrder(InputAction.CallbackContext _)
        {
            if (_selection.Count == 0 || worldCamera == null) return;
            if (!TryPointerGround(out Vector3 world)) return;
            for (int i = 0; i < _selection.Count; i++)
                if (_selection[i] != null && !_selection[i].IsDestroyed)
                    _selection[i].OrderMoveTo(world);
        }

        // ─────────────────────────────────────────────────────────────────────
        // TryPointerGround — screen pointer → XZ ground point (3D_MIGRATION §Phase B)
        // ─────────────────────────────────────────────────────────────────────
        // The old 2D code used Camera.ScreenToWorldPoint, which only makes sense for
        // an orthographic top-down camera. Now the camera is a TILTED perspective
        // RTS cam (G1), so we cast a ray from the pointer INTO the scene and find
        // where it meets the flat ground.
        //
        // We intersect a MATH Plane (Vector3.up through the origin, i.e. y = 0)
        // rather than a physics collider — no ground collider required, allocation-
        // free, and it always returns the exact XZ point the player clicked (G2).
        // ─────────────────────────────────────────────────────────────────────
        private bool TryPointerGround(out Vector3 world)
        {
            world = Vector3.zero;
            Vector2 screen = _pointAction.ReadValue<Vector2>();
            Ray ray = worldCamera.ScreenPointToRay(screen);
            Plane ground = new Plane(Vector3.up, Vector3.zero);
            if (!ground.Raycast(ray, out float enter)) return false;
            world = ray.GetPoint(enter);
            return true;
        }
    }
}
