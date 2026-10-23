namespace Attune.Signals
{
    /// <summary>What a press was, by how long it was held (design, Channels). The thresholds are the profile's.</summary>
    public enum PressKind
    {
        /// <summary>Shorter than the Hold threshold.</summary>
        Tap,

        /// <summary>At least the Hold threshold, shorter than the Long hold threshold.</summary>
        Hold,

        /// <summary>At least the Long hold threshold. With the Switch source it always goes back (ruling A20).</summary>
        LongHold,
    }
}
