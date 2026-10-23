using System.Collections.Generic;
using Attune.Signals;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Attune
{
    /// <summary>
    /// The Pointer source (design, Sources): the mouse or trackpad. Point is where the pointer is;
    /// Steer is its x from where it rests (the Rest step of calibration, or where it was when the
    /// source began), so a small movement can be full input; any button is a Press. Steer's raw value
    /// is x as a share of the screen's width.
    /// </summary>
    public sealed class PointerSource : InputSource
    {
        private static readonly CalibrationAxis[] _axes = { new("Steer", "right", "left") };

        private float _startX = 0.5f;

        /// <inheritdoc/>
        public override string Id => SourceIds.Pointer;

        /// <inheritdoc/>
        public override string Name => "Pointer";

        /// <inheritdoc/>
        public override string Description => "Mouse or trackpad. Point anywhere; move sideways to steer; any button presses.";

        /// <inheritdoc/>
        public override Channels Supplies => Channels.Point | Channels.Steer | Channels.Press;

        /// <inheritdoc/>
        public override bool IsAvailable => Mouse.current != null;

        /// <inheritdoc/>
        public override IReadOnlyList<CalibrationAxis> Axes => _axes;

        /// <inheritdoc/>
        public override AttuneTuning.Smoothing? Smoothing => AttuneTuning.Current.PointerSmoothing;

        /// <inheritdoc/>
        public override AxisCalibration DefaultCalibration(string axis)
        {
            float reach = AttuneTuning.Current.PointerReach;
            return AxisCalibration.Make(axis, _startX, _startX - reach, _startX + reach);
        }

        /// <inheritdoc/>
        public override void Begin(Profile profile) => _startX = ReadPoint().x;

        /// <inheritdoc/>
        public override void Read(SourceReading reading, float dt)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                reading.PressDown = false;
                return;
            }

            Vector2 point = ReadPoint();
            reading.Point = point;
            reading.Steer = point.x;
            reading.PressDown = mouse.leftButton.isPressed || mouse.rightButton.isPressed || mouse.middleButton.isPressed;
        }

        private static Vector2 ReadPoint()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return new Vector2(0.5f, 0.5f);
            }

            Vector2 position = mouse.position.ReadValue();
            return new Vector2(Mathf.Clamp01(position.x / Screen.width), Mathf.Clamp01(position.y / Screen.height));
        }
    }
}
