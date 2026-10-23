namespace Attune
{
    /// <summary>
    /// Where MicrophoneInput's sound comes from: the microphone (MicrophoneFeed), or samples played
    /// as if they were (SampleFeed: the mic file, or the check's test sounds). Sound is read as it
    /// arrives and kept only in memory (ruling A7).
    /// </summary>
    public abstract class AudioFeed
    {
        /// <summary>Its samples a second, once started.</summary>
        public abstract int SampleRate { get; }

        /// <summary>True for a real microphone.</summary>
        public abstract bool IsLive { get; }

        /// <summary>What the indicator says while it is on: "Microphone on".</summary>
        public abstract string Label { get; }

        /// <summary>What is wrong, in words a player can act on, or null.</summary>
        public virtual string Problem => null;

        /// <summary>Starts it; false if it couldn't (Problem says why).</summary>
        public abstract bool Start();

        /// <summary>Stops it.</summary>
        public abstract void Stop();

        /// <summary>dt seconds have passed since the last frame.</summary>
        public virtual void Advance(float dt)
        {
        }

        /// <summary>Copies samples that arrived since the last read into into, up to its length; returns how many (0 when none).</summary>
        public abstract int Read(float[] into);
    }
}
