using SherpaOnnx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace AiInterviewAssistant
{
    internal sealed class SileroVadSession : IDisposable
    {
        private const int SampleRate = 16000;
        private const int WindowSize = 512;

        private const float Threshold = 0.50f;
        private const float MinSilenceDuration = 0.25f;
        private const float MinSpeechDuration = 0.25f;
        private const float MaxSpeechDuration = 8.0f;

        private const string ModelFileName =
            "silero_vad.onnx";

        private VoiceActivityDetector vad;

        private readonly List<float> pendingSamples =
            new List<float>();

        private WaveFormatInfo format;

        private bool started;

        private bool disposed;

        public event EventHandler<byte[]> SpeechSegmentReady;

        public bool IsSpeechDetected
        {
            get
            {
                if (!started ||
                    disposed ||
                    vad == null)
                {
                    return false;
                }

                try
                {
                    return vad.IsSpeechDetected();
                }
                catch
                {
                    return false;
                }
            }
        }

        // =========================================================
        // START
        // =========================================================

        public void Start(
            NAudio.Wave.WaveFormat sourceFormat)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(
                    nameof(SileroVadSession));
            }

            if (started)
            {
                return;
            }

            string modelPath =
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Models",
                    ModelFileName);

            if (!File.Exists(modelPath))
            {
                throw new FileNotFoundException(
                    "Silero VAD model was not found.",
                    modelPath);
            }

            // -----------------------------------------------------
            // Current Voice recorder is WASAPI loopback.
            //
            // We convert incoming audio to 16 kHz mono PCM16
            // before feeding Silero.
            // -----------------------------------------------------

            if (sourceFormat == null)
            {
                throw new ArgumentNullException(
                    nameof(sourceFormat));
            }

            format = new WaveFormatInfo
            {
                SampleRate = sourceFormat.SampleRate,
                Channels = sourceFormat.Channels,
                BitsPerSample = sourceFormat.BitsPerSample,
                Encoding = sourceFormat.Encoding
            };

            VadModelConfig config =
                new VadModelConfig();

            config.SileroVad.Model =
                modelPath;

            config.SileroVad.Threshold =
                Threshold;

            config.SileroVad.MinSilenceDuration =
                MinSilenceDuration;

            config.SileroVad.MinSpeechDuration =
                MinSpeechDuration;

            config.SileroVad.MaxSpeechDuration =
                MaxSpeechDuration;

            config.SileroVad.WindowSize =
                WindowSize;

            config.SampleRate =
                SampleRate;

            config.NumThreads =
                1;

            config.Provider =
                "cpu";

            config.Debug =
                0;

            vad =
                new VoiceActivityDetector(
                    config,
                    30);

            pendingSamples.Clear();

            started = true;

            Debug.WriteLine(
                "SILERO VAD SESSION: STARTED");

            Debug.WriteLine(
                "SOURCE FORMAT: " +
                sourceFormat);

            Debug.WriteLine(
                "VAD FORMAT: 16000 Hz / MONO / PCM16");
        }

        // =========================================================
        // ACCEPT AUDIO
        // =========================================================

        public void AcceptAudio(
            byte[] buffer,
            int bytesRecorded)
        {
            if (!started ||
                disposed ||
                vad == null)
            {
                return;
            }

            if (buffer == null ||
                bytesRecorded <= 0)
            {
                return;
            }

            try
            {
                // -------------------------------------------------
                // Convert incoming WASAPI audio to 16k mono float.
                // -------------------------------------------------

                float[] samples =
                    ConvertTo16KMonoFloat(
                        buffer,
                        bytesRecorded);

                if (samples == null ||
                    samples.Length == 0)
                {
                    return;
                }

                pendingSamples.AddRange(
                    samples);

                // -------------------------------------------------
                // Feed complete 512-sample frames.
                // -------------------------------------------------

                while (
                    pendingSamples.Count >=
                    WindowSize)
                {
                    float[] frame =
                        new float[WindowSize];

                    pendingSamples.CopyTo(
                        0,
                        frame,
                        0,
                        WindowSize);

                    pendingSamples.RemoveRange(
                        0,
                        WindowSize);

                    vad.AcceptWaveform(
                        frame);

                    DrainSpeechSegments();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "SILERO VAD SESSION AUDIO ERROR: " +
                    ex);
            }
        }

        // =========================================================
        // DRAIN SPEECH
        // =========================================================

        private void DrainSpeechSegments()
        {
            if (vad == null)
            {
                return;
            }

            while (!vad.IsEmpty())
            {
                SpeechSegment segment =
                    vad.Front();

                if (segment != null &&
                    segment.Samples != null &&
                    segment.Samples.Length > 0)
                {
                    byte[] wav =
                        CreatePcm16MonoWav(
                            Float32ToPcm16(
                                segment.Samples),
                            SampleRate);

                    if (wav != null &&
                        wav.Length > 44)
                    {
                        Debug.WriteLine(
                            "SILERO VAD SESSION: " +
                            "SPEECH SEGMENT = " +
                            wav.Length +
                            " bytes");

                        SpeechSegmentReady?.Invoke(
                            this,
                            wav);
                    }
                }

                vad.Pop();
            }
        }

        // =========================================================
        // STOP
        // =========================================================

        public void Stop()
        {
            if (!started)
            {
                return;
            }

            try
            {
                // Flush remaining samples.
                if (pendingSamples.Count > 0)
                {
                    float[] frame =
                        new float[WindowSize];

                    int count =
                        Math.Min(
                            pendingSamples.Count,
                            WindowSize);

                    pendingSamples.CopyTo(
                        0,
                        frame,
                        0,
                        count);

                    vad?.AcceptWaveform(
                        frame);
                }

                vad?.Flush();

                DrainSpeechSegments();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "SILERO VAD SESSION STOP ERROR: " +
                    ex);
            }
            finally
            {
                pendingSamples.Clear();

                if (vad != null)
                {
                    vad.Dispose();
                    vad = null;
                }

                started = false;

                Debug.WriteLine(
                    "SILERO VAD SESSION: STOPPED");
            }
        }

        // =========================================================
        // AUDIO CONVERSION
        // =========================================================

        private float[] ConvertTo16KMonoFloat(
            byte[] buffer,
            int bytesRecorded)
        {
            if (format == null)
            {
                return null;
            }

            int channels =
                Math.Max(
                    1,
                    format.Channels);

            int bits =
                format.BitsPerSample;

            if (bits != 16 &&
                bits != 32)
            {
                Debug.WriteLine(
                    "SILERO VAD: Unsupported bits = " +
                    bits);

                return null;
            }

            int bytesPerSample =
                bits / 8;

            int frameSize =
                channels *
                bytesPerSample;

            if (frameSize <= 0)
            {
                return null;
            }

            int sourceFrameCount =
                bytesRecorded /
                frameSize;

            if (sourceFrameCount <= 0)
            {
                return null;
            }

            float[] mono =
                new float[sourceFrameCount];

            for (int frame = 0;
                 frame < sourceFrameCount;
                 frame++)
            {
                double sum = 0;

                int frameOffset =
                    frame *
                    frameSize;

                for (int channel = 0;
                     channel < channels;
                     channel++)
                {
                    int offset =
                        frameOffset +
                        channel *
                        bytesPerSample;

                    float value;

                    if (bits == 32)
                    {
                        value =
                            BitConverter.ToSingle(
                                buffer,
                                offset);
                    }
                    else
                    {
                        short pcm =
                            BitConverter.ToInt16(
                                buffer,
                                offset);

                        value =
                            pcm /
                            32768.0f;
                    }

                    if (float.IsNaN(value) ||
                        float.IsInfinity(value))
                    {
                        value = 0;
                    }

                    value =
                        Math.Max(
                            -1.0f,
                            Math.Min(
                                1.0f,
                                value));

                    sum += value;
                }

                mono[frame] =
                    (float)(
                        sum /
                        channels);
            }

            // -----------------------------------------------------
            // Resample to 16 kHz.
            // -----------------------------------------------------

            if (format.SampleRate ==
                SampleRate)
            {
                return mono;
            }

            return ResampleLinear(
                mono,
                format.SampleRate,
                SampleRate);
        }

        // =========================================================
        // LINEAR RESAMPLER
        // =========================================================

        private float[] ResampleLinear(
            float[] input,
            int sourceRate,
            int targetRate)
        {
            if (input == null ||
                input.Length == 0)
            {
                return new float[0];
            }

            if (sourceRate <= 0 ||
                targetRate <= 0)
            {
                return input;
            }

            int outputLength =
                (int)Math.Round(
                    input.Length *
                    (double)targetRate /
                    sourceRate);

            if (outputLength <= 0)
            {
                return new float[0];
            }

            float[] output =
                new float[outputLength];

            double ratio =
                (double)sourceRate /
                targetRate;

            for (int i = 0;
                 i < outputLength;
                 i++)
            {
                double position =
                    i * ratio;

                int index =
                    (int)position;

                double fraction =
                    position - index;

                if (index >=
                    input.Length - 1)
                {
                    output[i] =
                        input[input.Length - 1];
                }
                else
                {
                    output[i] =
                        (float)(
                            input[index] *
                            (1.0 - fraction) +
                            input[index + 1] *
                            fraction);
                }
            }

            return output;
        }

        // =========================================================
        // FLOAT -> PCM16
        // =========================================================

        private byte[] Float32ToPcm16(
            float[] samples)
        {
            byte[] bytes =
                new byte[
                    samples.Length * 2];

            for (int i = 0;
                 i < samples.Length;
                 i++)
            {
                float value =
                    Math.Max(
                        -1.0f,
                        Math.Min(
                            1.0f,
                            samples[i]));

                short pcm =
                    (short)Math.Round(
                        value *
                        32767.0f);

                byte[] temp =
                    BitConverter.GetBytes(
                        pcm);

                bytes[i * 2] =
                    temp[0];

                bytes[(i * 2) + 1] =
                    temp[1];
            }

            return bytes;
        }

        // =========================================================
        // WAV
        // =========================================================

        private byte[] CreatePcm16MonoWav(
            byte[] pcmBytes,
            int sampleRate)
        {
            const short audioFormat = 1;
            const short channels = 1;
            const short bitsPerSample = 16;

            int byteRate =
                sampleRate *
                channels *
                (bitsPerSample / 8);

            short blockAlign =
                (short)(
                    channels *
                    (bitsPerSample / 8));

            int fileSize =
                36 +
                pcmBytes.Length;

            using (MemoryStream stream =
                   new MemoryStream(
                       fileSize + 8))
            using (BinaryWriter writer =
                   new BinaryWriter(stream))
            {
                writer.Write(
                    0x46464952);

                writer.Write(
                    fileSize);

                writer.Write(
                    0x45564157);

                writer.Write(
                    0x20746D66);

                writer.Write(
                    16);

                writer.Write(
                    audioFormat);

                writer.Write(
                    channels);

                writer.Write(
                    sampleRate);

                writer.Write(
                    byteRate);

                writer.Write(
                    blockAlign);

                writer.Write(
                    bitsPerSample);

                writer.Write(
                    0x61746164);

                writer.Write(
                    pcmBytes.Length);

                writer.Write(
                    pcmBytes);

                writer.Flush();

                return stream.ToArray();
            }
        }

        // =========================================================
        // DISPOSE
        // =========================================================

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            Stop();
        }

        // =========================================================
        // FORMAT HOLDER
        // =========================================================

        private sealed class WaveFormatInfo
        {
            public int SampleRate;

            public int Channels;

            public int BitsPerSample;

            public NAudio.Wave.WaveFormatEncoding Encoding;
        }
    }
}