using System.Collections.Generic;
using Attune.Signals;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Attune
{
    /// <summary>
    /// The Keyboard zones source (design, Sources): any key is a Press, the left and right halves of
    /// the board Steer, and its four quadrants are four gestures, so a fist, an elbow or a mouth stick
    /// on a big zone plays. Escape and F2 belong to the menus and the presenter, and count for none.
    /// Keys right of the main block are on the right half.
    /// </summary>
    public sealed class KeyboardZonesSource : InputSource
    {
        private static readonly Dictionary<Key, Vector2> _places = BuildPlaces();

        private float _target; // the half last pressed and still held: −1, 0 or 1
        private float _steer;

        /// <inheritdoc/>
        public override string Id => SourceIds.Keyboard;

        /// <inheritdoc/>
        public override string Name => "Keyboard zones";

        /// <inheritdoc/>
        public override string Description => "Any key presses. The halves steer; the quarters are four gestures.";

        /// <inheritdoc/>
        public override Channels Supplies => Channels.Press | Channels.Steer | Channels.Gesture;

        /// <inheritdoc/>
        public override bool IsAvailable => Keyboard.current != null;

        /// <inheritdoc/>
        public override IReadOnlyList<string> GestureSet => KeyboardZones.Quadrants;

        /// <summary>True for a key the menus or the presenter own, which is never a press.</summary>
        public static bool IsReserved(Key key) => key == Key.Escape || key == Key.F2;

        /// <summary>Where a key sits: x across the main block (0 to 1, past 1 to its right), y from the function row (0) to the space bar (1).</summary>
        public static Vector2 Place(Key key)
        {
            if (_places.TryGetValue(key, out Vector2 place))
            {
                return place;
            }

            switch (key)
            {
                case Key.UpArrow:
                case Key.DownArrow:
                case Key.LeftArrow:
                case Key.RightArrow:
                    return new Vector2(1.1f, 0.9f);
                case Key.Insert:
                case Key.Home:
                case Key.PageUp:
                case Key.Delete:
                case Key.End:
                case Key.PageDown:
                case Key.PrintScreen:
                case Key.ScrollLock:
                case Key.Pause:
                case Key.NumLock:
                case Key.NumpadDivide:
                case Key.NumpadMultiply:
                case Key.NumpadMinus:
                case Key.Numpad7:
                case Key.Numpad8:
                case Key.Numpad9:
                    return new Vector2(1.2f, 0.3f);
                default:
                    return new Vector2(1.2f, 0.8f);
            }
        }

        /// <inheritdoc/>
        public override void Begin(Profile profile)
        {
            _target = 0f;
            _steer = 0f;
        }

        /// <inheritdoc/>
        public override void Read(SourceReading reading, float dt)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                reading.PressDown = false;
                reading.Steer = 0f;
                return;
            }

            bool any = false;
            bool heldOnTargetSide = false;
            foreach (KeyControl control in keyboard.allKeys)
            {
                if (control == null || IsReserved(control.keyCode) || !control.isPressed)
                {
                    continue;
                }

                any = true;
                Vector2 place = Place(control.keyCode);
                if (control.wasPressedThisFrame)
                {
                    _target = KeyboardZones.Half(place.x);
                    reading.Gestures.Add(KeyboardZones.Quadrant(place.x, place.y));
                }

                if (KeyboardZones.Half(place.x) == (int)_target)
                {
                    heldOnTargetSide = true;
                }
            }

            // The most recent half held steers; letting go of it centres, even with a key held on the other side.
            if (!heldOnTargetSide)
            {
                _target = 0f;
            }

            reading.PressDown = any;
            float seconds = AttuneTuning.Current.KeyboardSteerSeconds;
            _steer = seconds <= 0f ? _target : Mathf.MoveTowards(_steer, _target, dt / seconds);
            reading.Steer = _steer;
        }

        // The main block of a standard board, row by row, each key with its width in key units; every row is 15 wide.
        private static Dictionary<Key, Vector2> BuildPlaces()
        {
            var places = new Dictionary<Key, Vector2>();

            void Row(float y, params (Key Key, float Width)[] keys)
            {
                float x = 0f;
                foreach ((Key key, float width) in keys)
                {
                    places[key] = new Vector2((x + width / 2f) / 15f, y);
                    x += width;
                }
            }

            Row(0f, (Key.None, 2f), (Key.F1, 1f), (Key.F2, 1f), (Key.F3, 1f), (Key.F4, 1f), (Key.None, 0.5f), (Key.F5, 1f), (Key.F6, 1f),
                (Key.F7, 1f), (Key.F8, 1f), (Key.None, 0.5f), (Key.F9, 1f), (Key.F10, 1f), (Key.F11, 1f), (Key.F12, 1f));
            Row(0.2f, (Key.Backquote, 1f), (Key.Digit1, 1f), (Key.Digit2, 1f), (Key.Digit3, 1f), (Key.Digit4, 1f), (Key.Digit5, 1f),
                (Key.Digit6, 1f), (Key.Digit7, 1f), (Key.Digit8, 1f), (Key.Digit9, 1f), (Key.Digit0, 1f), (Key.Minus, 1f), (Key.Equals, 1f),
                (Key.Backspace, 2f));
            Row(0.4f, (Key.Tab, 1.5f), (Key.Q, 1f), (Key.W, 1f), (Key.E, 1f), (Key.R, 1f), (Key.T, 1f), (Key.Y, 1f), (Key.U, 1f),
                (Key.I, 1f), (Key.O, 1f), (Key.P, 1f), (Key.LeftBracket, 1f), (Key.RightBracket, 1f), (Key.Backslash, 1.5f));
            Row(0.6f, (Key.CapsLock, 1.75f), (Key.A, 1f), (Key.S, 1f), (Key.D, 1f), (Key.F, 1f), (Key.G, 1f), (Key.H, 1f), (Key.J, 1f),
                (Key.K, 1f), (Key.L, 1f), (Key.Semicolon, 1f), (Key.Quote, 1f), (Key.Enter, 2.25f));
            Row(0.8f, (Key.LeftShift, 2.25f), (Key.Z, 1f), (Key.X, 1f), (Key.C, 1f), (Key.V, 1f), (Key.B, 1f), (Key.N, 1f), (Key.M, 1f),
                (Key.Comma, 1f), (Key.Period, 1f), (Key.Slash, 1f), (Key.RightShift, 2.75f));
            Row(1f, (Key.LeftCtrl, 1.25f), (Key.LeftMeta, 1.25f), (Key.LeftAlt, 1.25f), (Key.Space, 6.25f), (Key.RightAlt, 1.25f),
                (Key.RightMeta, 1.25f), (Key.ContextMenu, 1.25f), (Key.RightCtrl, 1.25f));
            places.Remove(Key.None);
            return places;
        }
    }
}
