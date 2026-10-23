using System;
using System.Collections.Generic;
using Attune.Signals;

namespace Attune
{
    /// <summary>
    /// A source (design, Sources): reads one device and supplies some of the channels. Only sources
    /// touch the Input System, the microphone or the camera; InputRouter holds the active one,
    /// calibrates and smooths what it reads, and runs the adapters for the channels it lacks.
    /// </summary>
    public abstract class InputSource
    {
        /// <summary>Its Id, as profiles save it (SourceIds).</summary>
        public abstract string Id { get; }

        /// <summary>The name players see.</summary>
        public abstract string Name { get; }

        /// <summary>A few words on what it is and how it plays, for the source page.</summary>
        public abstract string Description { get; }

        /// <summary>The channels it supplies itself.</summary>
        public abstract Channels Supplies { get; }

        /// <summary>True if the menus are scanned while it is active, by its presses (the Switch, Breath and Hum sources, ruling A36).</summary>
        public virtual bool ScansMenus => false;

        /// <summary>How the hints name its presses while it scans the menus.</summary>
        public virtual PressWords Words => PressWords.Switch;

        /// <summary>True if what it reads is already in channel units, so no calibration, dead zone or smoothing applies (the Scripted source).</summary>
        public virtual bool PassesThrough => false;

        /// <summary>True if its device is there to be read.</summary>
        public virtual bool IsAvailable => true;

        /// <summary>True if it listens through the microphone (MicrophoneInput), which is on while it is the active source (ruling A36).</summary>
        public virtual bool UsesMicrophone => false;

        /// <summary>The axes calibration measures (Rest, then Reach each way that applies); none for a source of presses.</summary>
        public virtual IReadOnlyList<CalibrationAxis> Axes => Array.Empty<CalibrationAxis>();

        /// <summary>The gestures it makes, in scanning order; none if it supplies no Gesture.</summary>
        public virtual IReadOnlyList<string> GestureSet => Array.Empty<string>();

        /// <summary>The smoothing of its axes, or null for none (a source of keys).</summary>
        public virtual AttuneTuning.Smoothing? Smoothing => null;

        /// <summary>An axis as it is before the player calibrates it.</summary>
        public virtual AxisCalibration DefaultCalibration(string axis) => AxisCalibration.Make(axis, 0f, -1f, 1f);

        /// <summary>The calibration an axis plays with, from the profile's: the same, unless the profile changes it (the Breath source's Noisy room, ruling A35).</summary>
        public virtual AxisCalibration InPlay(AxisCalibration axis, Profile profile) => axis;

        /// <summary>It becomes the active source, for profile.</summary>
        public virtual void Begin(Profile profile)
        {
        }

        /// <summary>It stops being the active source.</summary>
        public virtual void End()
        {
        }

        /// <summary>Reads the device into reading; dt is the time since the last read.</summary>
        public abstract void Read(SourceReading reading, float dt);
    }
}
