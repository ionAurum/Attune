using System;

namespace Attune.Signals
{
    /// <summary>
    /// A profile's timing values (design, Calibration, Press timing; ruling A11: every one can be
    /// changed). The defaults are generous. Public fields, so the profile's JSON holds them.
    /// </summary>
    [Serializable]
    public sealed class TimingValues
    {
        /// <summary>A press held this long or longer is a Hold, not a Tap (seconds).</summary>
        public float HoldSeconds = 0.8f;

        /// <summary>A press held this long or longer is a Long hold (seconds).</summary>
        public float LongHoldSeconds = 2f;

        /// <summary>How long resting on a target selects it (seconds).</summary>
        public float DwellSeconds = 1.5f;

        /// <summary>How long scanning rests on each option (seconds).</summary>
        public float ScanStepSeconds = 1.5f;

        /// <summary>How many rounds scanning makes with no press before it pauses; 0 never pauses.</summary>
        public int ScanRounds = 3;

        /// <summary>Whether scanning starts from the top again after a pick or a pause, or carries on from where it was.</summary>
        public bool ScanFromTop = true;

        /// <summary>Press → Level: how long holding takes to raise the level from 0 to 1 (seconds).</summary>
        public float RiseSeconds = 1.5f;

        /// <summary>Press → Level: how long the level takes to fall from 1 to 0 once released (seconds).</summary>
        public float FallSeconds = 2.5f;

        /// <summary>The dead zone around rest, as a share of the calibrated range (0 to 1).</summary>
        public float DeadZone = 0.1f;

        /// <summary>Level → Press: the level a press starts at.</summary>
        public float PressOnLevel = 0.5f;

        /// <summary>Level → Press: the level it ends below; under PressOnLevel, so a level that wavers doesn't flicker.</summary>
        public float PressOffLevel = 0.35f;

        /// <summary>What a press held for seconds was.</summary>
        public PressKind Classify(float seconds)
        {
            if (seconds >= LongHoldSeconds)
            {
                return PressKind.LongHold;
            }

            return seconds >= HoldSeconds ? PressKind.Hold : PressKind.Tap;
        }

        /// <summary>Puts every value back inside the range that makes sense, after a hand-edited save or an old one.</summary>
        public void Clamp()
        {
            HoldSeconds = Math.Max(0.1f, HoldSeconds);
            LongHoldSeconds = Math.Max(HoldSeconds + 0.1f, LongHoldSeconds);
            DwellSeconds = Math.Max(0.2f, DwellSeconds);
            ScanStepSeconds = Math.Max(0.2f, ScanStepSeconds);
            ScanRounds = Math.Max(0, ScanRounds);
            RiseSeconds = Math.Max(0.05f, RiseSeconds);
            FallSeconds = Math.Max(0.05f, FallSeconds);
            DeadZone = Math.Min(0.9f, Math.Max(0f, DeadZone));
            PressOnLevel = Math.Min(1f, Math.Max(0.05f, PressOnLevel));
            PressOffLevel = Math.Min(PressOnLevel - 0.01f, Math.Max(0f, PressOffLevel));
        }

        /// <summary>A copy that can be changed without changing this one.</summary>
        public TimingValues Copy() => (TimingValues)MemberwiseClone();
    }
}
