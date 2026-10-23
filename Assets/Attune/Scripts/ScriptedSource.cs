using System.Collections.Generic;
using Attune.Signals;
using UnityEngine;

namespace Attune
{
    /// <summary>
    /// The Scripted source (design, Sources): a script of timed values, for the self-test and demo
    /// playback. It supplies every channel. Values can also be set straight away (Set, Press,
    /// Gesture). Only a self-test offers it.
    /// </summary>
    public sealed class ScriptedSource : InputSource
    {
        /// <summary>One timed value: at At seconds into the script, Channel becomes Value (Point: Value, Y; Press: down if Value > 0.5; Gesture: Name).</summary>
        public readonly struct Key
        {
            /// <summary>A timed value.</summary>
            public Key(float at, Channels channel, float value, float y = 0f, string name = null)
            {
                At = at;
                Channel = channel;
                Value = value;
                Y = y;
                Name = name;
            }

            /// <summary>When, in seconds from the start of the script.</summary>
            public float At { get; }

            /// <summary>Which channel.</summary>
            public Channels Channel { get; }

            /// <summary>The value: Level, Steer, Point's x, or Press (down above 0.5).</summary>
            public float Value { get; }

            /// <summary>Point's y.</summary>
            public float Y { get; }

            /// <summary>A gesture's name.</summary>
            public string Name { get; }
        }

        private readonly List<Key> _script = new();
        private readonly List<string> _gestures = new();
        private int _next;
        private float _time;
        private float _level, _steer;
        private Vector2 _point = new(0.5f, 0.5f);
        private bool _down;

        /// <inheritdoc/>
        public override string Id => SourceIds.Scripted;

        /// <inheritdoc/>
        public override string Name => "Scripted";

        /// <inheritdoc/>
        public override string Description => "A script of timed values, for the self-test.";

        /// <inheritdoc/>
        public override Channels Supplies =>
            Channels.Level | Channels.Steer | Channels.Point | Channels.Press | Channels.Gesture;

        /// <inheritdoc/>
        public override IReadOnlyList<string> GestureSet => KeyboardZones.Quadrants;

        /// <inheritdoc/>
        public override bool PassesThrough => true;

        /// <summary>True while a script still has values to come.</summary>
        public bool IsPlaying => _next < _script.Count;

        /// <summary>Plays a script from its start; keys need not be in order.</summary>
        public void Play(IEnumerable<Key> keys)
        {
            _script.Clear();
            _script.AddRange(keys);
            _script.Sort((a, b) => a.At.CompareTo(b.At));
            _next = 0;
            _time = 0f;
        }

        /// <summary>Sets the axes now.</summary>
        public void Set(float level, float steer, Vector2 point)
        {
            _level = level;
            _steer = steer;
            _point = point;
        }

        /// <summary>Holds the press down, or lets it go.</summary>
        public void Press(bool down) => _down = down;

        /// <summary>Makes a gesture on the next read.</summary>
        public void Gesture(string name) => _gestures.Add(name);

        /// <inheritdoc/>
        public override void Begin(Profile profile)
        {
            _down = false;
            _gestures.Clear();
        }

        /// <inheritdoc/>
        public override void Read(SourceReading reading, float dt)
        {
            _time += dt;
            while (_next < _script.Count && _script[_next].At <= _time)
            {
                Apply(_script[_next++]);
            }

            reading.Level = _level;
            reading.Steer = _steer;
            reading.Point = _point;
            reading.PressDown = _down;
            reading.Gestures.AddRange(_gestures);
            _gestures.Clear();
        }

        private void Apply(Key key)
        {
            switch (key.Channel)
            {
                case Channels.Level:
                    _level = key.Value;
                    break;
                case Channels.Steer:
                    _steer = key.Value;
                    break;
                case Channels.Point:
                    _point = new Vector2(key.Value, key.Y);
                    break;
                case Channels.Press:
                    _down = key.Value > 0.5f;
                    break;
                case Channels.Gesture:
                    _gestures.Add(key.Name);
                    break;
            }
        }
    }
}
