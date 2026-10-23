using System;
using Attune.Signals;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;

namespace Attune
{
    /// <summary>
    /// The Switch source (design, Sources): one chosen key or mouse button, which is how USB switch
    /// interfaces usually appear, and a sip-and-puff device that sends keys. It supplies only Press;
    /// the adapters give it the rest, and the menus are scanned while it is active (design,
    /// Scanning). The control is the profile's (Profile.SwitchControl), chosen with Capture.
    /// </summary>
    public sealed class SwitchSource : InputSource
    {
        private string _path;
        private ButtonControl _control;
        private IDisposable _capture;

        /// <inheritdoc/>
        public override string Id => SourceIds.Switch;

        /// <inheritdoc/>
        public override string Name => "Switch";

        /// <inheritdoc/>
        public override string Description => "One key, mouse button or switch. The menus scan; tap to choose, long hold to go back.";

        /// <inheritdoc/>
        public override Channels Supplies => Channels.Press;

        /// <inheritdoc/>
        public override bool ScansMenus => true;

        /// <summary>The control's path, as the profile keeps it.</summary>
        public string Path => _path;

        /// <summary>True while Capture is waiting for a press.</summary>
        public bool IsCapturing => _capture != null;

        /// <summary>A control's name for people: "Space", "Left Button".</summary>
        public static string Describe(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "none";
            }

            string name = InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
            return path.StartsWith("<Mouse>", StringComparison.Ordinal) ? "Mouse " + name : name;
        }

        /// <summary>
        /// A control's path by its device's layout, "&lt;Keyboard&gt;/k", not the device's name
        /// ("/Keyboard/k"), so the switch is found again on a device plugged in later.
        /// </summary>
        public static string PathOf(InputControl control)
        {
            string device = control.device.path;
            string inDevice = control.path.StartsWith(device, StringComparison.Ordinal) ? control.path.Substring(device.Length) : "/" + control.name;
            return "<" + control.device.layout + ">" + inDevice;
        }

        /// <summary>True for a control the menus or the presenter own, which can't be the switch.</summary>
        public static bool IsReserved(InputControl control)
        {
            if (control is KeyControl key)
            {
                return KeyboardZonesSource.IsReserved(key.keyCode);
            }

            if (control.device is Gamepad gamepad)
            {
                return control == gamepad.startButton || control == gamepad.selectButton || control == gamepad.buttonEast;
            }

            return false;
        }

        /// <inheritdoc/>
        public override void Begin(Profile profile)
        {
            _path = profile != null ? profile.SwitchControl : "<Keyboard>/space";
            _control = null;
        }

        /// <inheritdoc/>
        public override void End() => StopCapture();

        /// <inheritdoc/>
        public override void Read(SourceReading reading, float dt)
        {
            if (_control == null || !_control.device.added)
            {
                _control = string.IsNullOrEmpty(_path) ? null : InputSystem.FindControl(_path) as ButtonControl;
            }

            reading.PressDown = _control != null && _control.isPressed;
        }

        /// <summary>
        /// Waits for the next press of any key or button the menus don't own, and calls chosen with its
        /// path. Pressing it makes it the switch; StopCapture gives up waiting.
        /// </summary>
        public void Capture(Action<string> chosen)
        {
            StopCapture();
            _capture = InputSystem.onAnyButtonPress.Call(control =>
            {
                if (IsReserved(control))
                {
                    return;
                }

                string path = PathOf(control);
                StopCapture();
                _path = path;
                _control = null;
                chosen?.Invoke(path);
            });
        }

        /// <summary>Stops waiting for a press.</summary>
        public void StopCapture()
        {
            _capture?.Dispose();
            _capture = null;
        }

        /// <summary>Uses path as the switch from now on, as a self-test does.</summary>
        public void Use(string path)
        {
            _path = path;
            _control = null;
        }

        /// <summary>For a self-test: the switch's key, if it is a key, so the test can press it as a device would.</summary>
        public Key KeyOfSwitch()
        {
            if (InputSystem.FindControl(_path) is KeyControl key)
            {
                return key.keyCode;
            }

            return Key.None;
        }
    }
}
