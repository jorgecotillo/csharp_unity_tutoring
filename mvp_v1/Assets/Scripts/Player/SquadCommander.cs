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
        private InputAction _selectAll;

        private readonly List<Squad> _selection = new();

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
            _selectAll = _input.actions["SelectAll"];

            _orderAction.performed += OnOrder;
            _selectSquad1.performed += OnSelect1;
            _selectSquad2.performed += OnSelect2;
            _selectSquad3.performed += OnSelect3;
            _selectAll.performed += OnSelectAll;
        }

        private void OnDisable()
        {
            if (_orderAction != null) _orderAction.performed -= OnOrder;
            if (_selectSquad1 != null) _selectSquad1.performed -= OnSelect1;
            if (_selectSquad2 != null) _selectSquad2.performed -= OnSelect2;
            if (_selectSquad3 != null) _selectSquad3.performed -= OnSelect3;
            if (_selectAll != null) _selectAll.performed -= OnSelectAll;
        }

        private void OnSelect1(InputAction.CallbackContext _) => SelectOnly(0);
        private void OnSelect2(InputAction.CallbackContext _) => SelectOnly(1);
        private void OnSelect3(InputAction.CallbackContext _) => SelectOnly(2);

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
        }

        private void SelectOnly(int index)
        {
            if (raid == null || index < 0 || index >= raid.Squads.Count) return;
            Squad s = raid.Squads[index];
            if (s == null || s.IsDestroyed) return;
            ClearSelection();
            s.SetSelected(true);
            _selection.Add(s);
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
            Vector2 world = PointerWorld();
            for (int i = 0; i < _selection.Count; i++)
                if (_selection[i] != null && !_selection[i].IsDestroyed)
                    _selection[i].OrderMoveTo(world);
        }

        private Vector2 PointerWorld()
        {
            Vector2 screen = _pointAction.ReadValue<Vector2>();
            Vector3 w = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -worldCamera.transform.position.z));
            return new Vector2(w.x, w.y);
        }
    }
}
