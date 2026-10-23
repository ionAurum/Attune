using System;
using Aurumworks;
using UnityEngine;

namespace Attune
{
    /// <summary>
    /// Static: the one microphone the Breath and Hum sources listen through; only they use it. It is
    /// on while one of them is the active source, on the hub too, since their presses scan the menus
    /// (ruling A36), and MicIndicator shows while it is. Sound is analysed as it arrives and kept only in a short buffer of the latest
    /// samples; nothing is written to disk or sent (ruling A7). With -attuneMicFile wav it plays
    /// that recording in place of the microphone (design, Checks); in a self-test it never opens the
    /// real microphone: it plays silence, or the test sound the check gives it (Use).
    /// </summary>
    public static class MicrophoneInput
    {
        /// <summary>The flag that feeds a recording in place of the microphone.</summary>
        public const string FileFlag = "-attuneMicFile";

        private const int BufferLength = 16384;
        private const float DeviceRecheckSeconds = 2f;

        private static readonly float[] _buffer = new float[BufferLength];
        private static readonly float[] _chunk = new float[4096];
        private static long _received; // samples since it came on
        private static AudioFeed _feed, _given;
        private static string _problem;
        private static bool _hasDevice;
        private static float _deviceCheckedAt = float.NegativeInfinity;
        private static float _failedAt = float.NegativeInfinity; // it tries again a little after failing, not every frame

        /// <summary>True while it listens.</summary>
        public static bool IsOn { get; private set; }

        /// <summary>True while the real microphone listens (not a mic file or a test sound).</summary>
        public static bool IsLive => IsOn && _feed != null && _feed.IsLive;

        /// <summary>What the indicator says while it is on.</summary>
        public static string Label => _feed != null ? _feed.Label : "Microphone on";

        /// <summary>What is wrong, in words a player can act on, or null.</summary>
        public static string Problem => _problem ?? _feed?.Problem;

        /// <summary>The samples a second of what it hears; 48000 before it has come on.</summary>
        public static int SampleRate => _feed != null && _feed.SampleRate > 0 ? _feed.SampleRate : 48000;

        /// <summary>How many samples it has heard since it came on.</summary>
        public static long Received => _received;

        /// <summary>True if there is something to listen to: a microphone, a mic file, or a self-test's sound.</summary>
        public static bool HasDevice
        {
            get
            {
                if (_given != null || SelfTest.IsRunning || CommandLine.Has(FileFlag))
                {
                    return true;
                }

                // Asking the system for its devices costs; ask now and then.
                if (Time.unscaledTime - _deviceCheckedAt >= DeviceRecheckSeconds)
                {
                    _deviceCheckedAt = Time.unscaledTime;
                    _hasDevice = MicrophoneFeed.HasDevice;
                }

                return _hasDevice;
            }
        }

        // Statics survive play mode when domain reload is disabled; reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _feed?.Stop();
            _feed = null;
            _given = null;
            _problem = null;
            _received = 0;
            _hasDevice = false;
            _deviceCheckedAt = float.NegativeInfinity;
            _failedAt = float.NegativeInfinity;
            IsOn = false;
            Array.Clear(_buffer, 0, _buffer.Length);
        }

        /// <summary>The check: listen to feed in place of the microphone from the next time it comes on; null goes back.</summary>
        public static void Use(AudioFeed feed)
        {
            Release();
            _given = feed;
            _problem = null;
            _failedAt = float.NegativeInfinity;
        }

        /// <summary>
        /// Called each frame by the active microphone source: comes on (ruling A36), then takes in
        /// what arrived. The source releases it when it stops being the active one.
        /// </summary>
        public static void Pump(float dt)
        {
            if (!IsOn && (Time.unscaledTime - _failedAt < DeviceRecheckSeconds || !TurnOn()))
            {
                return;
            }

            _feed.Advance(dt);
            int count;
            while ((count = _feed.Read(_chunk)) > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    _buffer[(int)(_received++ % BufferLength)] = _chunk[i];
                }
            }
        }

        /// <summary>Stops listening: the microphone source stopped being the active one, or the check changes what it hears.</summary>
        public static void Release()
        {
            if (IsOn)
            {
                _feed?.Stop();
                Debug.Log("Microphone off.");
            }

            IsOn = false;
            _feed = null;
            _received = 0;
        }

        /// <summary>Copies the latest count samples it heard, oldest first, into into; returns how many (fewer before it has heard that many).</summary>
        public static int Latest(float[] into, int count)
        {
            int n = (int)Math.Min(Math.Min(count, into.Length), Math.Min(_received, BufferLength));
            long from = _received - n;
            for (int i = 0; i < n; i++)
            {
                into[i] = _buffer[(int)((from + i) % BufferLength)];
            }

            return n;
        }

        private static bool TurnOn()
        {
            _problem = null;
            _received = 0;
            _feed = _given;
            if (_feed == null && CommandLine.Has(FileFlag))
            {
                string path = CommandLine.Value(FileFlag);
                string problem = null;
                _feed = path != null ? SampleFeed.FromFile(path, out problem) : null;
                if (_feed == null)
                {
                    Debug.LogWarning($"The mic file couldn't be read ({problem ?? "no path given"}): listening to silence.");
                    _feed = SampleFeed.Silence();
                }
            }

            if (_feed == null)
            {
                _feed = SelfTest.IsRunning ? SampleFeed.Silence() : new MicrophoneFeed();
            }

            if (!_feed.Start())
            {
                _problem = _feed.Problem ?? "The microphone wouldn't start.";
                _feed = null;
                IsOn = false;
                _failedAt = Time.unscaledTime;
                return false;
            }

            IsOn = true;
            Debug.Log($"Microphone on ({_feed.Label}).");
            return true;
        }
    }
}
