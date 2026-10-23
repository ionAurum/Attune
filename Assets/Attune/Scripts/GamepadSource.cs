using System.Collections.Generic;
using Attune.Signals;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Attune
{
    /// <summary>
    /// The Gamepad source (design, Sources): the left stick Steers (its x) and moves Point like a
    /// cursor; any face button, shoulder, trigger or stick click is a Press. Start and Select belong
    /// to the menus and the presenter.
    /// </summary>
    public sealed class GamepadSource : InputSource
    {
        private static readonly CalibrationAxis[] _axes = { new("Steer", "right", "left") };

        private Vector2 _point = new(0.5f, 0.5f);

        /// <inheritdoc/>
        public override string Id => SourceIds.Gamepad;

        /// <inheritdoc/>
        public override string Name => "Gamepad";

        /// <inheritdoc/>
        public override string Description => "The left stick steers and points; any button presses.";

        /// <inheritdoc/>
        public override Channels Supplies => Channels.Steer | Channels.Point | Channels.Press;

        /// <inheritdoc/>
        public override bool IsAvailable => Gamepad.current != null;

        /// <inheritdoc/>
        public override IReadOnlyList<CalibrationAxis> Axes => _axes;

        /// <inheritdoc/>
        public override AttuneTuning.Smoothing? Smoothing => AttuneTuning.Current.GamepadSmoothing;

        /// <inheritdoc/>
        public override void Begin(Profile profile) => _point = new Vector2(0.5f, 0.5f);

        /// <inheritdoc/>
        public override void Read(SourceReading reading, float dt)
        {
            Gamepad gamepad = Gamepad.current;
            if (gamepad == null)
            {
                reading.PressDown = false;
                reading.Steer = 0f;
                reading.Point = _point;
                return;
            }

            Vector2 stick = gamepad.leftStick.ReadValue();
            reading.Steer = stick.x;
            _point += stick * (AttuneTuning.Current.GamepadPointSpeed * dt);
            _point = new Vector2(Mathf.Clamp01(_point.x), Mathf.Clamp01(_point.y));
            reading.Point = _point;
            reading.PressDown = gamepad.buttonSouth.isPressed || gamepad.buttonEast.isPressed || gamepad.buttonWest.isPressed ||
                                gamepad.buttonNorth.isPressed || gamepad.leftShoulder.isPressed || gamepad.rightShoulder.isPressed ||
                                gamepad.leftTrigger.isPressed || gamepad.rightTrigger.isPressed ||
                                gamepad.leftStickButton.isPressed || gamepad.rightStickButton.isPressed;
        }
    }
}
