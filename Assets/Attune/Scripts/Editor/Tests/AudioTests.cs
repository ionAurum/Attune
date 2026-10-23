using System;
using System.Collections.Generic;
using System.IO;
using Attune.Signals;
using NUnit.Framework;

namespace Attune.Tests
{
    /// <summary>
    /// M5's signal maths (design, Checks): the loudness detector and the noise floor, the pitch
    /// detector on synthetic tones from 80 to 800 Hz within 2%, with and without noise, the glide
    /// between notes (ruling A34), Noisy room (A35), a hum's calibrated range, and the mic file's
    /// WAV reader.
    /// </summary>
    public class AudioTests
    {
        private const int Rate = 48000;

        // A tone of hz with harmonics overtones falling as 1/n, plus white noise of that amplitude.
        private static float[] Tone(float hz, float seconds, float amplitude, int harmonics = 1, float noise = 0f, int seed = 1)
        {
            var random = new Random(seed);
            var samples = new float[(int)(seconds * Rate)];
            for (int i = 0; i < samples.Length; i++)
            {
                double t = (double)i / Rate;
                double value = 0;
                for (int h = 1; h <= harmonics; h++)
                {
                    value += amplitude / h * Math.Sin(2 * Math.PI * h * hz * t);
                }

                samples[i] = (float)(value + noise * (random.NextDouble() * 2 - 1));
            }

            return samples;
        }

        private static float[] Noise(float seconds, float amplitude, int seed = 2) => Tone(1f, seconds, 0f, 1, amplitude, seed);

        //---------- Loudness

        [Test]
        public void LoudnessIsTheRmsInDecibels()
        {
            float[] sine = Tone(440f, 0.1f, 0.5f);
            Assert.That(LoudnessDetector.Decibels(sine, 0, sine.Length), Is.EqualTo(-9.03f).Within(0.1f), "a sine of 0.5 has an RMS of 0.354");
            Assert.That(LoudnessDetector.Decibels(new float[480], 0, 480), Is.EqualTo(LoudnessDetector.Silence));
            Assert.That(LoudnessDetector.Decibels(sine, 0, 0), Is.EqualTo(LoudnessDetector.Silence), "no samples is silence");
            Assert.That(LoudnessDetector.Times(3f), Is.EqualTo(9.54f).Within(0.01f));
        }

        [Test]
        public void AMicrophonesOffsetIsNotSound()
        {
            float[] quiet = Tone(200f, 0.1f, 0.01f);
            for (int i = 0; i < quiet.Length; i++)
            {
                quiet[i] += 0.3f;
            }

            Assert.That(LoudnessDetector.Decibels(quiet, 0, quiet.Length), Is.EqualTo(-43.0f).Within(0.2f), "the offset is taken out");
        }

        [Test]
        public void ABreathStandsAboveTheRoomsNoiseFloor()
        {
            float[] room = Noise(0.05f, 0.001f);
            float[] breath = Noise(0.05f, 0.1f, seed: 3);
            float floor = LoudnessDetector.Decibels(room, 0, room.Length);
            float puff = LoudnessDetector.Decibels(breath, 0, breath.Length);
            Assert.That(puff - floor, Is.EqualTo(40f).Within(1.5f), "a hundred times the amplitude is 40 dB");

            // Calibrated from that room to that breath, the room is Level 0 and the breath full.
            AxisCalibration axis = AxisCalibration.Make("Level", floor, floor, puff, noise: 1f);
            Assert.That(axis.NormaliseLevel(floor + 0.5f, 0.1f), Is.EqualTo(0f));
            Assert.That(axis.NormaliseLevel(puff, 0.1f), Is.EqualTo(1f));
            Assert.That(axis.NormaliseLevel((floor + puff) / 2f, 0f), Is.EqualTo(0.5f).Within(0.01f));
        }

        [Test]
        public void NoisyRoomIgnoresWhatIsUnderThreeTimesTheRoom()
        {
            AxisCalibration axis = AxisCalibration.Make("Level", -60f, -60f, -20f, isSet: true);
            AxisCalibration raised = axis.RaisedRest(LoudnessDetector.Times(3f), new AxisCalibration());
            Assert.That(raised.Rest, Is.EqualTo(-50.46f).Within(0.01f));
            Assert.That(raised.High, Is.EqualTo(-20f));
            Assert.That(axis.Rest, Is.EqualTo(-60f), "the profile's calibration is left as it was");
            Assert.That(axis.NormaliseLevel(-52f, 0f), Is.GreaterThan(0f));
            Assert.That(raised.NormaliseLevel(-52f, 0f), Is.EqualTo(0f), "a sound under three times the room is ignored");
            Assert.That(raised.NormaliseLevel(-20f, 0f), Is.EqualTo(1f), "a full breath is still full");

            AxisCalibration capped = axis.RaisedRest(100f, new AxisCalibration());
            Assert.That(capped.Rest, Is.EqualTo(-24f).Within(1e-4f), "never within a tenth of the span of full");
        }

        //---------- Pitch

        [Test]
        public void PitchIsWithinTwoPercentFrom80To800Hz()
        {
            var detector = new PitchDetector(Rate);
            foreach (float hz in new[] { 80f, 100f, 150f, 220f, 330f, 440f, 600f, 800f })
            {
                float[] tone = Tone(hz, 0.1f, 0.5f);
                float found = detector.Detect(tone, 0, tone.Length, out float clarity);
                Assert.That(found, Is.EqualTo(hz).Within(hz * 0.02f), $"a {hz} Hz sine");
                Assert.That(clarity, Is.GreaterThan(0.9f));
            }
        }

        [Test]
        public void PitchIsWithinTwoPercentWithOvertonesAndNoise()
        {
            var detector = new PitchDetector(Rate);
            foreach (float hz in new[] { 80f, 110f, 196f, 262f, 392f, 523f, 800f })
            {
                float[] tone = Tone(hz, 0.1f, 0.5f, harmonics: 4, noise: 0.1f, seed: (int)hz);
                float found = detector.Detect(tone, 0, tone.Length, out _);
                Assert.That(found, Is.EqualTo(hz).Within(hz * 0.02f), $"a hum-like {hz} Hz tone in noise");
            }
        }

        [Test]
        public void NoiseAndSilenceHaveNoPitch()
        {
            var detector = new PitchDetector(Rate);
            float[] noise = Noise(0.1f, 0.3f);
            Assert.That(detector.Detect(noise, 0, noise.Length, out _), Is.EqualTo(0f), "a breath's noise is no note");
            float[] silence = new float[4800];
            Assert.That(detector.Detect(silence, 0, silence.Length, out _), Is.EqualTo(0f));
            float[] tone = Tone(220f, 0.1f, 0.5f);
            Assert.That(detector.Detect(tone, 0, detector.SamplesNeeded - 1, out _), Is.EqualTo(0f), "too few samples");
        }

        [Test]
        public void SemitonesCountTheScaleEvenly()
        {
            Assert.That(PitchDetector.Semitones(110f), Is.EqualTo(12f).Within(1e-4f), "an octave above 55 Hz");
            Assert.That(PitchDetector.Semitones(440f) - PitchDetector.Semitones(220f), Is.EqualTo(12f).Within(1e-4f));
            Assert.That(PitchDetector.Hertz(PitchDetector.Semitones(196f)), Is.EqualTo(196f).Within(0.01f));
        }

        //---------- Between notes (ruling A34)

        [Test]
        public void SilenceGlidesTheLevelDownAtTheFallRate()
        {
            var glide = new LevelGlide();
            Assert.That(glide.IsGliding, Is.True, "silent until it hears something");
            Assert.That(glide.Update(false, 0.8f, 0.02f, 2f), Is.EqualTo(0.8f));
            Assert.That(glide.IsGliding, Is.False);
            Assert.That(glide.Update(true, 0f, 0.5f, 2f), Is.EqualTo(0.55f).Within(1e-5f), "a quarter of the way down in half the fall time");
            Assert.That(glide.IsGliding, Is.True);
            Assert.That(glide.Update(true, 0f, 10f, 2f), Is.EqualTo(0f), "down to rest, and no further");
            Assert.That(glide.Update(false, 0.3f, 0.02f, 2f), Is.EqualTo(0.3f), "a new note is heard at once");
            glide.Reset();
            Assert.That(glide.Level, Is.EqualTo(0f));
        }

        //---------- A hum's range

        [Test]
        public void AHumsRangeIsTheMiddleOfEachHeldNote()
        {
            var low = new List<float> { 12f, 12.2f, 11.9f, 12.1f, 24f }; // one stray octave
            var high = new List<float> { 20f, 19.8f, 20.1f, 20.2f, 8f };
            Assert.That(Calibrate.Range(low, high, 3, 2f, out float from, out float to), Is.True);
            Assert.That(from, Is.EqualTo(12.1f).Within(1e-4f));
            Assert.That(to, Is.EqualTo(20f).Within(1e-4f));

            Assert.That(Calibrate.Range(low, new List<float> { 20f }, 3, 2f, out _, out _), Is.False, "a note too short to hear");
            Assert.That(Calibrate.Range(low, new List<float> { 13f, 13f, 13f }, 3, 2f, out _, out _), Is.False, "two notes too close");
        }

        [Test]
        public void ACalibratedHumGoesFromLowestToHighest()
        {
            AxisCalibration axis = AxisCalibration.Make("Level", PitchDetector.Semitones(110f), PitchDetector.Semitones(110f),
                PitchDetector.Semitones(440f), isSet: true);
            Assert.That(axis.NormaliseLevel(PitchDetector.Semitones(110f), 0f), Is.EqualTo(0f));
            Assert.That(axis.NormaliseLevel(PitchDetector.Semitones(220f), 0f), Is.EqualTo(0.5f).Within(1e-4f), "an octave up is halfway");
            Assert.That(axis.NormaliseLevel(PitchDetector.Semitones(880f), 0f), Is.EqualTo(1f), "above the highest stays full");
        }

        //---------- The mic file

        [Test]
        public void TheWavReaderReads16BitStereoAndFloatMono()
        {
            byte[] stereo = Wav(1, 2, 16, 22050, writer =>
            {
                writer.Write((short)16384);
                writer.Write((short)-16384); // a frame that mixes to 0
                writer.Write((short)32767);
                writer.Write((short)32767);
            });
            Assert.That(WavReader.TryRead(stereo, out float[] samples, out int rate, out string problem), Is.True, problem);
            Assert.That(rate, Is.EqualTo(22050));
            Assert.That(samples.Length, Is.EqualTo(2));
            Assert.That(samples[0], Is.EqualTo(0f).Within(1e-4f));
            Assert.That(samples[1], Is.EqualTo(1f).Within(1e-3f));

            byte[] floats = Wav(3, 1, 32, 48000, writer =>
            {
                writer.Write(0.25f);
                writer.Write(-0.5f);
            });
            Assert.That(WavReader.TryRead(floats, out samples, out rate, out problem), Is.True, problem);
            Assert.That(rate, Is.EqualTo(48000));
            Assert.That(samples, Is.EqualTo(new[] { 0.25f, -0.5f }));
        }

        [Test]
        public void TheWavReaderRefusesWhatIsntOne()
        {
            Assert.That(WavReader.TryRead(new byte[] { 1, 2, 3 }, out _, out _, out string problem), Is.False);
            Assert.That(problem, Is.Not.Null);
            byte[] adpcm = Wav(2, 1, 4, 8000, writer => writer.Write(0));
            Assert.That(WavReader.TryRead(adpcm, out _, out _, out problem), Is.False, "a compressed format");
        }

        // A WAV file's bytes: its format chunk, then data written by data.
        private static byte[] Wav(short format, short channels, short bits, int rate, Action<BinaryWriter> data)
        {
            var body = new MemoryStream();
            using (var writer = new BinaryWriter(body))
            {
                data(writer);
            }

            byte[] samples = body.ToArray();
            var file = new MemoryStream();
            using (var writer = new BinaryWriter(file))
            {
                writer.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
                writer.Write(36 + samples.Length);
                writer.Write(new[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' });
                writer.Write(new[] { (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
                writer.Write(16);
                writer.Write(format);
                writer.Write(channels);
                writer.Write(rate);
                writer.Write(rate * channels * Math.Max(1, bits / 8));
                writer.Write((short)(channels * Math.Max(1, bits / 8)));
                writer.Write(bits);
                writer.Write(new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
                writer.Write(samples.Length);
                writer.Write(samples);
            }

            return file.ToArray();
        }
    }
}
