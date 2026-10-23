using UnityEngine;

namespace Attune
{
    /// <summary>
    /// The real microphone, the system's default one, through Unity's Microphone: it records into a
    /// one-second looping clip, and Read hands on what arrived, a few milliseconds at a time. Nothing
    /// is kept beyond that clip and MicrophoneInput's short buffer, and nothing is written or sent
    /// (ruling A7). If Windows' privacy settings block desktop apps from the microphone, it starts
    /// but hears only silence; Problem then says where to change that (design, Risks).
    /// </summary>
    public sealed class MicrophoneFeed : AudioFeed
    {
        private const int ClipSeconds = 1;
        private const float SilentSeconds = 1.5f; // nothing at all for this long, and it says why
        private const string PrivacyHint =
            "Windows may be blocking it: Settings > Privacy & security > Microphone, then turn on Let desktop apps access your microphone.";

        private AudioClip _clip;
        private float[] _chunk;
        private int _rate, _readAt;
        private float _running;
        private bool _heard;
        private string _problem;

        /// <inheritdoc/>
        public override int SampleRate => _rate;

        /// <inheritdoc/>
        public override bool IsLive => true;

        /// <inheritdoc/>
        public override string Label => "Microphone on";

        /// <inheritdoc/>
        public override string Problem => _problem;

        /// <summary>True if the system has a microphone.</summary>
        public static bool HasDevice => Microphone.devices.Length > 0;

        /// <inheritdoc/>
        public override bool Start()
        {
            _problem = null;
            _heard = false;
            _running = 0f;
            if (!HasDevice)
            {
                _problem = "No microphone found. Plug one in, then choose Breath or Hum again.";
                return false;
            }

            int rate = AttuneTuning.Current.MicrophoneRate;
            Microphone.GetDeviceCaps(null, out int min, out int max);
            if (max > 0)
            {
                rate = Mathf.Clamp(rate, Mathf.Max(min, 8000), max);
            }

            _clip = Microphone.Start(null, true, ClipSeconds, rate);
            if (_clip == null)
            {
                _problem = "The microphone wouldn't start. " + PrivacyHint;
                return false;
            }

            _rate = _clip.frequency;
            _chunk = new float[Chunk(_rate)];
            _readAt = Microphone.GetPosition(null) / _chunk.Length * _chunk.Length;
            Debug.Log($"Microphone started at {_rate} Hz."); // the rate only: never the device's name (ruling A27)
            return true;
        }

        /// <inheritdoc/>
        public override void Stop()
        {
            if (_clip == null)
            {
                return;
            }

            Microphone.End(null);
            Object.Destroy(_clip);
            _clip = null;
        }

        /// <inheritdoc/>
        public override void Advance(float dt)
        {
            if (_clip == null)
            {
                return;
            }

            _running += dt;
            if (!_heard && _running >= SilentSeconds && _problem == null)
            {
                _problem = "Nothing is coming from the microphone. " + PrivacyHint;
            }
        }

        /// <inheritdoc/>
        public override int Read(float[] into)
        {
            if (_clip == null || into.Length < _chunk.Length)
            {
                return 0;
            }

            int length = _clip.samples;
            int position = Microphone.GetPosition(null);
            int waiting = (position - _readAt + length) % length;
            if (waiting > length / 2)
            {
                // Fallen behind by half the clip (a long frame): skip to the newest whole chunk.
                _readAt = (position - _chunk.Length + length) % length / _chunk.Length * _chunk.Length;
                waiting = (position - _readAt + length) % length;
            }

            if (waiting < _chunk.Length)
            {
                return 0;
            }

            // The clip is a whole number of chunks, so a chunk never runs past its end.
            _clip.GetData(_chunk, _readAt);
            _readAt = (_readAt + _chunk.Length) % length;
            for (int i = 0; i < _chunk.Length; i++)
            {
                float sample = _chunk[i];
                into[i] = sample;
                if (sample != 0f && !_heard)
                {
                    _heard = true;
                    _problem = null;
                }
            }

            return _chunk.Length;
        }

        // About 10 ms of samples, that divides the second-long clip exactly.
        private static int Chunk(int rate)
        {
            for (int size = Mathf.Max(1, rate / 100); size > 1; size--)
            {
                if (rate % size == 0)
                {
                    return size;
                }
            }

            return 1;
        }
    }
}
