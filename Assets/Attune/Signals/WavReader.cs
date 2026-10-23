using System;
using System.Text;

namespace Attune.Signals
{
    /// <summary>
    /// Static: reads a WAV file's bytes into mono samples, for the mic file (design, Checks:
    /// -attuneMicFile feeds a recording through the real Breath and Hum pipeline). PCM at 8, 16, 24
    /// or 32 bits, or 32-bit float, any number of channels (mixed to one). It only reads: nothing is
    /// ever written.
    /// </summary>
    public static class WavReader
    {
        /// <summary>The samples, −1 to 1, and their rate; false with problem saying why if the bytes aren't a WAV it reads.</summary>
        public static bool TryRead(byte[] bytes, out float[] samples, out int sampleRate, out string problem)
        {
            samples = null;
            sampleRate = 0;
            problem = null;
            if (bytes == null || bytes.Length < 12 || Tag(bytes, 0) != "RIFF" || Tag(bytes, 8) != "WAVE")
            {
                problem = "not a WAV file";
                return false;
            }

            int format = 0, channels = 0, bits = 0, dataAt = -1, dataLength = 0;
            int at = 12;
            while (at + 8 <= bytes.Length)
            {
                string tag = Tag(bytes, at);
                int length = BitConverter.ToInt32(bytes, at + 4);
                int body = at + 8;
                if (length < 0 || body + length > bytes.Length)
                {
                    length = bytes.Length - body; // a recorder that stopped before fixing the length
                }

                if (tag == "fmt " && length >= 16)
                {
                    format = BitConverter.ToInt16(bytes, body);
                    channels = BitConverter.ToInt16(bytes, body + 2);
                    sampleRate = BitConverter.ToInt32(bytes, body + 4);
                    bits = BitConverter.ToInt16(bytes, body + 14);
                    if (format == 0xFFFE && length >= 26)
                    {
                        format = BitConverter.ToInt16(bytes, body + 24); // WAVE_FORMAT_EXTENSIBLE: the real format is in the sub-format
                    }
                }
                else if (tag == "data")
                {
                    dataAt = body;
                    dataLength = length;
                    break;
                }

                at = body + length + (length & 1);
            }

            if (dataAt < 0 || channels <= 0 || sampleRate <= 0)
            {
                problem = "no format or no sound in it";
                return false;
            }

            bool pcm = format == 1 && (bits == 8 || bits == 16 || bits == 24 || bits == 32);
            bool floats = format == 3 && bits == 32;
            if (!pcm && !floats)
            {
                problem = $"format {format} at {bits} bits isn't one it reads (PCM, or 32-bit float)";
                return false;
            }

            int bytesPerSample = bits / 8;
            int frames = dataLength / (bytesPerSample * channels);
            samples = new float[frames];
            for (int frame = 0; frame < frames; frame++)
            {
                float sum = 0f;
                for (int channel = 0; channel < channels; channel++)
                {
                    int offset = dataAt + (frame * channels + channel) * bytesPerSample;
                    sum += floats ? BitConverter.ToSingle(bytes, offset) : Pcm(bytes, offset, bits);
                }

                samples[frame] = sum / channels;
            }

            return true;
        }

        private static float Pcm(byte[] bytes, int offset, int bits)
        {
            switch (bits)
            {
                case 8:
                    return (bytes[offset] - 128) / 128f;
                case 16:
                    return BitConverter.ToInt16(bytes, offset) / 32768f;
                case 24:
                    int value = bytes[offset] | (bytes[offset + 1] << 8) | ((sbyte)bytes[offset + 2] << 16);
                    return value / 8388608f;
                default:
                    return BitConverter.ToInt32(bytes, offset) / 2147483648f;
            }
        }

        private static string Tag(byte[] bytes, int at) => Encoding.ASCII.GetString(bytes, at, 4);
    }
}
