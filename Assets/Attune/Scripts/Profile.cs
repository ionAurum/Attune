using System;
using System.Collections.Generic;
using Attune.Signals;

namespace Attune
{
    /// <summary>
    /// A player's profile (design, Profiles; ruling A9): a name, the source they play with, the
    /// calibration of every source they have calibrated (so a live swap uses that source's own), the
    /// switch they chose, and the timing values. Saved as JSON through Settings by ProfileStore.
    /// Public fields, for JsonUtility.
    /// </summary>
    [Serializable]
    public sealed class Profile
    {
        /// <summary>One source's calibrated axes.</summary>
        [Serializable]
        public sealed class SourceCalibration
        {
            /// <summary>The source's Id.</summary>
            public string Source = "";

            /// <summary>Its axes.</summary>
            public List<AxisCalibration> Axes = new();
        }

        /// <summary>The profile's number, which its settings key ends in (Attune.Profile.&lt;n&gt;).</summary>
        public int Id;

        /// <summary>The name shown on the hub.</summary>
        public string Name = "";

        /// <summary>The Id of the source the player plays with.</summary>
        public string Source = SourceIds.Keyboard;

        /// <summary>The Switch source's control, as an Input System path: "&lt;Keyboard&gt;/space".</summary>
        public string SwitchControl = "<Keyboard>/space";

        /// <summary>The timing values.</summary>
        public TimingValues Timing = new();

        /// <summary>Noisy room (ruling A35): the Breath source ignores anything under three times the room's loudness.</summary>
        public bool NoisyRoom;

        /// <summary>Each calibrated source's axes.</summary>
        public List<SourceCalibration> Calibrations = new();

        /// <summary>The player's calibration of one source's axis, or null if they haven't calibrated it.</summary>
        public AxisCalibration Calibration(string source, string axis)
        {
            foreach (SourceCalibration calibration in Calibrations)
            {
                if (calibration.Source != source)
                {
                    continue;
                }

                foreach (AxisCalibration found in calibration.Axes)
                {
                    if (found.Axis == axis)
                    {
                        return found;
                    }
                }
            }

            return null;
        }

        /// <summary>Keeps a calibration of one source's axis, in place of any it had.</summary>
        public void SetCalibration(string source, AxisCalibration axis)
        {
            SourceCalibration entry = Calibrations.Find(calibration => calibration.Source == source);
            if (entry == null)
            {
                entry = new SourceCalibration { Source = source };
                Calibrations.Add(entry);
            }

            entry.Axes.RemoveAll(found => found.Axis == axis.Axis);
            entry.Axes.Add(axis.Copy());
        }

        /// <summary>A short summary of the source's calibration and the timing, for the feedback file.</summary>
        public string Summary(string source)
        {
            var parts = new List<string>();
            SourceCalibration entry = Calibrations.Find(calibration => calibration.Source == source);
            if (entry != null)
            {
                foreach (AxisCalibration axis in entry.Axes)
                {
                    parts.Add(axis.Summary());
                }
            }
            else
            {
                parts.Add("not calibrated");
            }

            parts.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "hold {0:0.##} s, long hold {1:0.##} s, scan {2:0.##} s",
                Timing.HoldSeconds, Timing.LongHoldSeconds, Timing.ScanStepSeconds));
            if (NoisyRoom && source == SourceIds.Breath)
            {
                parts.Add("noisy room");
            }

            return string.Join("; ", parts);
        }
    }
}
