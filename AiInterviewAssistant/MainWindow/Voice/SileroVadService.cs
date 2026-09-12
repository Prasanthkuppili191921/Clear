using SherpaOnnx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace AiInterviewAssistant
{
    /// <summary>
    /// Removes non-speech portions from the existing 16 kHz / mono / PCM16 WAV
    /// before the audio is sent to the configured paid STT model.
    ///
    /// This service is intentionally independent of ChatGPT WebView handling.
    /// The caller must only invoke it when _chatGPTView == false.
    /// </summary>
    internal static class SileroVadService
    {
        private const int SampleRate = 16000;
        private const int WindowSize = 512;

        private const float Threshold = 0.50f;
        private const float MinSilenceDuration = 0.25f;
        private const float MinSpeechDuration = 0.25f;
        private const float MaxSpeechDuration = 8.0f;

        private const string ModelFileName = "silero_vad.onnx";

        public static byte[] RemoveSilence(byte[] wavBytes)
        {
            if (wavBytes == null || wavBytes.Length <= 44)
                return wavBytes;

            string modelPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Models",
                ModelFileName);

            if (!File.Exists(modelPath))
            {
                Debug.WriteLine(
                    "SILERO VAD: model not found. Keeping original WAV: " +
                    modelPath);

                // Fail open so the existing STT flow is never broken merely
                // because the optional VAD model has not been installed yet.
                return wavBytes;
            }

            try
            {
                short channels;
                short bitsPerSample;
                int sampleRate;
                byte[] pcmBytes = ReadPcm16Wave(
                    wavBytes,
                    out channels,
                    out bitsPerSample,
                    out sampleRate);

                if (channels != 1 ||
                    bitsPerSample != 16 ||
                    sampleRate != SampleRate)
                {
                    Debug.WriteLine(
                        "SILERO VAD: unexpected WAV format. " +
                        "channels=" + channels +
                        ", bits=" + bitsPerSample +
                        ", rate=" + sampleRate);

                    return wavBytes;
                }

                float[] samples =
                    Pcm16ToFloat32(pcmBytes);

                if (samples.Length < WindowSize)
                    return wavBytes;

                VadModelConfig config =
                    new VadModelConfig();

                config.SileroVad.Model = modelPath;
                config.SileroVad.Threshold = Threshold;
                config.SileroVad.MinSilenceDuration = MinSilenceDuration;
                config.SileroVad.MinSpeechDuration = MinSpeechDuration;
                config.SileroVad.MaxSpeechDuration = MaxSpeechDuration;
                config.SileroVad.WindowSize = WindowSize;
                config.SampleRate = SampleRate;
                config.NumThreads = 1;
                config.Provider = "cpu";
                config.Debug = 0;

                using (VoiceActivityDetector vad =
                       new VoiceActivityDetector(config, 30))
                {
                    List<float[]> speechSegments =
                        new List<float[]>();

                    for (int offset = 0;
                         offset + WindowSize <= samples.Length;
                         offset += WindowSize)
                    {
                        float[] frame =
                            new float[WindowSize];

                        Array.Copy(
                            samples,
                            offset,
                            frame,
                            0,
                            WindowSize);

                        vad.AcceptWaveform(frame);

                        DrainSegments(
                            vad,
                            speechSegments);
                    }

                    vad.Flush();

                    DrainSegments(
                        vad,
                        speechSegments);

                    if (speechSegments.Count == 0)
                    {
                        Debug.WriteLine(
                            "SILERO VAD: no speech detected.");

                        return null;
                    }

                    int totalSpeechSamples = 0;

                    foreach (float[] segment in speechSegments)
                    {
                        totalSpeechSamples += segment.Length;
                    }

                    float[] speechSamples =
                        new float[totalSpeechSamples];

                    int writeOffset = 0;

                    foreach (float[] segment in speechSegments)
                    {
                        Array.Copy(
                            segment,
                            0,
                            speechSamples,
                            writeOffset,
                            segment.Length);

                        writeOffset += segment.Length;
                    }

                    byte[] trimmedPcm =
                        Float32ToPcm16(speechSamples);

                    byte[] trimmedWav =
                        CreatePcm16MonoWav(
                            trimmedPcm,
                            SampleRate);

                    Debug.WriteLine(
                        "SILERO VAD: original samples=" +
                        samples.Length +
                        ", speech samples=" +
                        speechSamples.Length +
                        ", original WAV=" +
                        wavBytes.Length +
                        ", trimmed WAV=" +
                        trimmedWav.Length);

                    return trimmedWav;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "SILERO VAD ERROR: " +
                    ex);

                // Fail open. The existing paid STT path remains available.
                return wavBytes;
            }
        }

        private static void DrainSegments(
            VoiceActivityDetector vad,
            List<float[]> destination)
        {
            while (!vad.IsEmpty())
            {
                SpeechSegment segment =
                    vad.Front();

                if (segment != null &&
                    segment.Samples != null &&
                    segment.Samples.Length > 0)
                {
                    destination.Add(
                        segment.Samples);
                }

                vad.Pop();
            }
        }

        private static byte[] ReadPcm16Wave(
            byte[] wavBytes,
            out short channels,
            out short bitsPerSample,
            out int sampleRate)
        {
            channels = 0;
            bitsPerSample = 0;
            sampleRate = 0;

            using (MemoryStream stream =
                   new MemoryStream(wavBytes, false))
            using (BinaryReader reader =
                   new BinaryReader(stream))
            {
                if (reader.ReadUInt32() != 0x46464952) // RIFF
                    throw new InvalidDataException("Invalid RIFF header.");

                reader.ReadUInt32();

                if (reader.ReadUInt32() != 0x45564157) // WAVE
                    throw new InvalidDataException("Invalid WAVE header.");

                bool foundFormat = false;
                bool foundData = false;
                byte[] data = null;

                while (stream.Position + 8 <= stream.Length)
                {
                    uint chunkId = reader.ReadUInt32();
                    int chunkSize = reader.ReadInt32();

                    if (chunkSize < 0 ||
                        stream.Position + chunkSize > stream.Length)
                    {
                        throw new InvalidDataException(
                            "Invalid WAV chunk size.");
                    }

                    if (chunkId == 0x20746D66) // fmt 
                    {
                        short audioFormat = reader.ReadInt16();
                        channels = reader.ReadInt16();
                        sampleRate = reader.ReadInt32();
                        reader.ReadInt32();
                        reader.ReadInt16();
                        bitsPerSample = reader.ReadInt16();

                        if (audioFormat != 1)
                        {
                            throw new InvalidDataException(
                                "Only PCM WAV is supported by Silero VAD.");
                        }

                        long remaining =
                            chunkSize - 16;

                        if (remaining > 0)
                            stream.Seek(remaining, SeekOrigin.Current);

                        foundFormat = true;
                    }
                    else if (chunkId == 0x61746164) // data
                    {
                        data = reader.ReadBytes(chunkSize);
                        foundData = true;
                    }
                    else
                    {
                        stream.Seek(chunkSize, SeekOrigin.Current);
                    }

                    if ((chunkSize & 1) != 0 &&
                        stream.Position < stream.Length)
                    {
                        stream.Seek(1, SeekOrigin.Current);
                    }
                }

                if (!foundFormat || !foundData || data == null)
                    throw new InvalidDataException("WAV format/data chunk missing.");

                return data;
            }
        }

        private static float[] Pcm16ToFloat32(
            byte[] pcmBytes)
        {
            int sampleCount =
                pcmBytes.Length / 2;

            float[] samples =
                new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                short value =
                    BitConverter.ToInt16(
                        pcmBytes,
                        i * 2);

                samples[i] =
                    value / 32768.0f;
            }

            return samples;
        }

        private static byte[] Float32ToPcm16(
            float[] samples)
        {
            byte[] pcmBytes =
                new byte[samples.Length * 2];

            for (int i = 0; i < samples.Length; i++)
            {
                float value =
                    Math.Max(
                        -1.0f,
                        Math.Min(
                            1.0f,
                            samples[i]));

                short pcm =
                    (short)Math.Round(
                        value * 32767.0f);

                byte[] bytes =
                    BitConverter.GetBytes(pcm);

                pcmBytes[i * 2] = bytes[0];
                pcmBytes[(i * 2) + 1] = bytes[1];
            }

            return pcmBytes;
        }

        private static byte[] CreatePcm16MonoWav(
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
                (short)(channels *
                        (bitsPerSample / 8));

            int fileSize =
                36 + pcmBytes.Length;

            using (MemoryStream stream =
                   new MemoryStream(fileSize + 8))
            using (BinaryWriter writer =
                   new BinaryWriter(stream))
            {
                writer.Write(0x46464952); // RIFF
                writer.Write(fileSize);
                writer.Write(0x45564157); // WAVE

                writer.Write(0x20746D66); // fmt 
                writer.Write(16);
                writer.Write(audioFormat);
                writer.Write(channels);
                writer.Write(sampleRate);
                writer.Write(byteRate);
                writer.Write(blockAlign);
                writer.Write(bitsPerSample);

                writer.Write(0x61746164); // data
                writer.Write(pcmBytes.Length);
                writer.Write(pcmBytes);

                writer.Flush();
                return stream.ToArray();
            }
        }
    }
}
