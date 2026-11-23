using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace OutsideTheBox
{
    public sealed class MagnetInput : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;
        public event Action<RailSlot> SlotChanged;
        public event Action FirstDragged;
        public event Action<bool> DragStateChanged;
        public bool Enabled
        {
            get => inputEnabled;
            set
            {
                inputEnabled = value;
                if (!inputEnabled && dragging)
                {
                    dragging = false;
                    DragStateChanged?.Invoke(false);
                }
            }
        }

        private bool inputEnabled = true;
        private bool dragging;
        private RailSlot current;
        private bool hasCurrent;
        private Camera worldCamera;

        public void Bind(BoardView view)
        {
            boardView = view;
            ResetInteraction();
            worldCamera = Camera.main;
        }

        public void ResetInteraction()
        {
            if (dragging) DragStateChanged?.Invoke(false);
            dragging = false;
            hasCurrent = false;
            current = default;
            worldCamera = Camera.main;
        }

        private void Update()
        {
            if (!Enabled || boardView == null || boardView.Level == null) return;
            if (!TryGetPointer(out var screenPosition, out var pressed, out var released)) return;
            if (worldCamera == null) worldCamera = Camera.main;
            if (worldCamera == null) return;
            var world = worldCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -worldCamera.transform.position.z));
            if (pressed)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(GetPointerId())) return;
                if (!boardView.CanBeginMagnetDrag(world)) return;
                dragging = true;
                boardView.SetHint(null);
                DragStateChanged?.Invoke(true);
                FirstDragged?.Invoke();
            }
            if (released && dragging)
            {
                dragging = false;
                DragStateChanged?.Invoke(false);
            }
            if (!dragging) return;
            boardView.SetMagnetFromPointer(world);
            var slot = boardView.CurrentRailSlot;
            if (!hasCurrent || slot != current)
            {
                current = slot;
                hasCurrent = true;
                boardView.SetHint(null);
                SlotChanged?.Invoke(slot);
            }
        }

        private static int GetPointerId()
        {
            var touchscreen = Touchscreen.current;
            return touchscreen != null && touchscreen.primaryTouch.press.isPressed
                ? touchscreen.primaryTouch.touchId.ReadValue()
                : -1;
        }

        private static bool TryGetPointer(out Vector2 position, out bool pressed, out bool released)
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen != null && (touchscreen.primaryTouch.press.isPressed ||
                                        touchscreen.primaryTouch.press.wasPressedThisFrame ||
                                        touchscreen.primaryTouch.press.wasReleasedThisFrame))
            {
                var touch = touchscreen.primaryTouch;
                position = touch.position.ReadValue();
                pressed = touch.press.wasPressedThisFrame;
                released = touch.press.wasReleasedThisFrame;
                return true;
            }
            var pointer = Pointer.current;
            if (pointer == null)
            {
                position = default; pressed = released = false; return false;
            }
            position = pointer.position.ReadValue();
            pressed = pointer.press.wasPressedThisFrame;
            released = pointer.press.wasReleasedThisFrame;
            return pressed || released || pointer.press.isPressed;
        }
    }
}
