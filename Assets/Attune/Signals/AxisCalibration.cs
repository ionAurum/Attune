using System;

namespace Attune.Signals
{
    /// <summary>
    /// One axis of a source, calibrated to the player's own range (design, Calibration): where it
    /// rests, how far it comfortably reaches each way, and how much it wanders at rest. Normalising
    /// is (value − rest) / (reach − rest), clamped, with a dead zone around rest, so a 5° tilt and a
    /// 30° tilt can both be full input. Public fields, so the profile's JSON holds them.
    /// </summary>
    [Serializable]
    public sealed class AxisCalibration
    {
        private const float MinSpan = 1e-5f;

        /// <summary>Which axis this is: "Steer", "Level".</summary>
        public string Axis = "";

        /// <summary>Where the axis rests, in the source's own units.</summary>
        public float Rest;

        /// <summary>The comfortable reach below rest (Steer's left), in the source's units.</summary>
        public float Low = -1f;

        /// <summary>The comfortable reach above rest (Steer's right, Level's full), in the source's units.</summary>
        public float High = 1f;

        /// <summary>How far the axis wanders at rest, in the source's units.</summary>
        public float Noise;

        /// <summary>True once the player has calibrated it; false for a source's defaults.</summary>
        public bool IsSet;

        /// <summary>An axis with these values.</summary>
        public static AxisCalibration Make(string axis, float rest, float low, float high, float noise = 0f, bool isSet = false) =>
            new AxisCalibration { Axis = axis, Rest = rest, Low = low, High = high, Noise = noise, IsSet = isSet };

        /// <summary>value as a Steer, −1 to 1: rest is 0, the reach each way is 1, with the dead zone.</summary>
        public float NormaliseSteer(float value, float deadZone)
        {
            float normal;
            if (value >= Rest)
            {
                float span = High - Rest;
                normal = Math.Abs(span) < MinSpan ? 0f : (value - Rest) / span;
            }
            else
            {
                float span = Rest - Low;
                normal = Math.Abs(span) < MinSpan ? 0f : -(Rest - value) / span;
            }

            return ApplyDeadZone(Clamp(normal, -1f, 1f), EffectiveDeadZone(deadZone));
        }

        /// <summary>value as a Level, 0 to 1: rest is 0, the reach above rest (High) is 1, with the dead zone.</summary>
        public float NormaliseLevel(float value, float deadZone)
        {
            float span = High - Rest;
            float normal = Math.Abs(span) < MinSpan ? 0f : (value - Rest) / span;
            return ApplyDeadZone(Clamp(normal, 0f, 1f), EffectiveDeadZone(deadZone));
        }

        /// <summary>
        /// The dead zone used: the profile's, or the noise measured at rest as a share of the smaller
        /// reach if that is wider, so wandering at rest never reads as input.
        /// </summary>
        public float EffectiveDeadZone(float deadZone)
        {
            float span = Math.Min(Math.Abs(High - Rest), Math.Abs(Rest - Low));
            float noise = span < MinSpan ? 0f : Math.Abs(Noise) / span;
            return Clamp(Math.Max(deadZone, noise), 0f, 0.9f);
        }

        /// <summary>A short summary for the feedback file and the overlay: "rest 0.02, reach -0.31 to 0.28".</summary>
        public string Summary() =>
            string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} rest {1:0.###}, reach {2:0.###} to {3:0.###}{4}",
                Axis, Rest, Low, High, IsSet ? "" : " (default)");

        /// <summary>A copy that can be changed without changing this one.</summary>
        public AxisCalibration Copy() => (AxisCalibration)MemberwiseClone();

        /// <summary>
        /// Sets into to this axis with its rest raised by by (ruling A35, Noisy room: the Breath source
        /// ignores anything under three times the room's loudness), never to within a tenth of the
        /// span of High, so full input can still be reached. Returns into.
        /// </summary>
        public AxisCalibration RaisedRest(float by, AxisCalibration into)
        {
            into.Axis = Axis;
            into.Low = Low;
            into.High = High;
            into.Noise = Noise;
            into.IsSet = IsSet;
            float span = High - Rest;
            into.Rest = span > MinSpan ? Math.Min(Rest + Math.Max(0f, by), High - span * 0.1f) : Rest;
            return into;
        }

        /// <summary>
        /// Takes the dead zone dz out of a normalised value n (−1 to 1): inside it is 0, and the rest
        /// of the range is stretched so the reach is still 1.
        /// </summary>
        public static float ApplyDeadZone(float n, float dz)
        {
            float magnitude = Math.Abs(n);
            if (magnitude <= dz || dz >= 1f)
            {
                return 0f;
            }

            return Math.Sign(n) * (magnitude - dz) / (1f - dz);
        }

        private static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;
    }
}
