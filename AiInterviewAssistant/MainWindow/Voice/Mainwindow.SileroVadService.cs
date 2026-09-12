using SherpaOnnx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace AiInterviewAssistant
{
    internal static class SileroVadService
    {
        private const int SampleRate = 16000;
        private const int WindowSize = 512;

        private const float Threshold = 0.50f;
        private const float MinSilenceDuration = 0.30f;
        private const float MinSpeechDuration = 0.20f;
        private const float MaxSpeechDuration = 8.0f;

        private const string ModelFileName = "silero_vad.onnx";

        public static byte[] RemoveSilence(byte[] wavBytes)
        {
            return Process(wavBytes);
        }

        public static byte[] Process(byte[] wavBytes)
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
                    "SILERO VAD: model not found: " +
                    modelPath);

                // Fail-open.
                // Existing STT continues working.
                return wavBytes;
            }

            try
            {
                short channels;
                short bitsPerSample;
                int sampleRate;

                byte[] pcmBytes =
                    ReadPcm16Wave(
                        wavBytes,
                        out channels,
                        out bitsPerSample,
                        out sampleRate);

                if (channels != 1 ||
                    bitsPerSample != 16 ||
                    sampleRate != SampleRate)
                {
                    Debug.WriteLine(
                        "SILERO VAD: unsupported WAV format. " +
                        "Channels=" + channels +
                        " Bits=" + bitsPerSample +
                        " Rate=" + sampleRate);

                    return wavBytes;
                }

                float[] samples =
                    Pcm16ToFloat32(pcmBytes);

                if (samples.Length < WindowSize)
                    return wavBytes;

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

                config.SileroVad.WindowSize =
                    WindowSize;

                config.SileroVad.MaxSpeechDuration =
                    MaxSpeechDuration;

                config.SampleRate =
                    SampleRate;

                config.NumThreads =
                    1;

                config.Provider =
                    "cpu";

                config.Debug =
                    0;

                using (VoiceActivityDetector vad =
                       new VoiceActivityDetector(
                           config,
                           60))
                {
                    List<SpeechSegmentInfo> segments =
                        new List<SpeechSegmentInfo>();

                    for (
                        int offset = 0;
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

                        vad.AcceptWaveform(
                            frame);

                        while (!vad.IsEmpty())
                        {
                            SpeechSegment segment =
                                vad.Front();

                            if (segment != null &&
                                segment.Samples != null &&
                                segment.Samples.Length > 0)
                            {
                                segments.Add(
                                    new SpeechSegmentInfo
                                    {
                                        Start =
                                            segment.Start,

                                        Samples =
                                            segment.Samples
                                    });
                            }

                            vad.Pop();
                        }
                    }

                    vad.Flush();

                    while (!vad.IsEmpty())
                    {
                        SpeechSegment segment =
                            vad.Front();

                        if (segment != null &&
                            segment.Samples != null &&
                            segment.Samples.Length > 0)
                        {
                            segments.Add(
                                new SpeechSegmentInfo
                                {
                                    Start =
                                        segment.Start,

                                    Samples =
                                        segment.Samples
                                });
                        }

                        vad.Pop();
                    }

                    if (segments.Count == 0)
                    {
                        Debug.WriteLine(
                            "SILERO VAD: NO SPEECH");

                        return null;
                    }

                    int totalSamples = 0;

                    foreach (
                        SpeechSegmentInfo segment
                        in segments)
                    {
                        totalSamples +=
                            segment.Samples.Length;
                    }

                    float[] speechSamples =
                        new float[totalSamples];

                    int writeOffset = 0;

                    foreach (
                        SpeechSegmentInfo segment
                        in segments)
                    {
                        Array.Copy(
                            segment.Samples,
                            0,
                            speechSamples,
                            writeOffset,
                            segment.Samples.Length);

                        writeOffset +=
                            segment.Samples.Length;
                    }

                    byte[] speechPcm =
                        Float32ToPcm16(
                            speechSamples);

                    byte[] result =
                        CreatePcm16MonoWav(
                            speechPcm,
                            SampleRate);

                    Debug.WriteLine(
                        "========================================");

                    Debug.WriteLine(
                        "SILERO VAD RESULT");

                    Debug.WriteLine(
                        "Original WAV = " +
                        wavBytes.Length +
                        " bytes");

                    Debug.WriteLine(
                        "Speech WAV = " +
                        result.Length +
                        " bytes");

                    Debug.WriteLine(
                        "Original duration = " +
                        (samples.Length /
                         (double)SampleRate)
                        .ToString("F2") +
                        " sec");

                    Debug.WriteLine(
                        "Speech duration = " +
                        (speechSamples.Length /
                         (double)SampleRate)
                        .ToString("F2") +
                        " sec");

                    Debug.WriteLine(
                        "========================================");

                    return result;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "SILERO VAD ERROR:");

                Debug.WriteLine(
                    ex.ToString());

                // VERY IMPORTANT:
                // VAD failure must never break existing STT.
                return wavBytes;
            }
        }

        private sealed class SpeechSegmentInfo
        {
            public int Start;

            public float[] Samples;
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
                   new MemoryStream(
                       wavBytes,
                       false))
            using (BinaryReader reader =
                   new BinaryReader(stream))
            {
                if (reader.ReadUInt32() !=
                    0x46464952)
                {
                    throw new InvalidDataException(
                        "Invalid RIFF header.");
                }

                reader.ReadUInt32();

                if (reader.ReadUInt32() !=
                    0x45564157)
                {
                    throw new InvalidDataException(
                        "Invalid WAVE header.");
                }

                bool formatFound = false;
                bool dataFound = false;

                byte[] data = null;

                while (
                    stream.Position + 8 <=
                    stream.Length)
                {
                    uint chunkId =
                        reader.ReadUInt32();

                    int chunkSize =
                        reader.ReadInt32();

                    if (chunkSize < 0 ||
                        stream.Position +
                        chunkSize >
                        stream.Length)
                    {
                        throw new InvalidDataException(
                            "Invalid WAV chunk.");
                    }

                    // "fmt "
                    if (chunkId ==
                        0x20746D66)
                    {
                        short audioFormat =
                            reader.ReadInt16();

                        channels =
                            reader.ReadInt16();

                        sampleRate =
                            reader.ReadInt32();

                        reader.ReadInt32();
                        reader.ReadInt16();

                        bitsPerSample =
                            reader.ReadInt16();

                        if (audioFormat != 1)
                        {
                            throw new InvalidDataException(
                                "WAV is not PCM.");
                        }

                        int remaining =
                            chunkSize - 16;

                        if (remaining > 0)
                        {
                            stream.Seek(
                                remaining,
                                SeekOrigin.Current);
                        }

                        formatFound = true;
                    }
                    // "data"
                    else if (
                        chunkId ==
                        0x61746164)
                    {
                        data =
                            reader.ReadBytes(
                                chunkSize);

                        dataFound = true;
                    }
                    else
                    {
                        stream.Seek(
                            chunkSize,
                            SeekOrigin.Current);
                    }

                    if ((chunkSize & 1) != 0 &&
                        stream.Position <
                        stream.Length)
                    {
                        stream.Seek(
                            1,
                            SeekOrigin.Current);
                    }
                }

                if (!formatFound ||
                    !dataFound ||
                    data == null)
                {
                    throw new InvalidDataException(
                        "WAV format/data missing.");
                }

                return data;
            }
        }

        private static float[] Pcm16ToFloat32(
            byte[] pcmBytes)
        {
            int count =
                pcmBytes.Length / 2;

            float[] samples =
                new float[count];

            for (int i = 0; i < count; i++)
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
            byte[] pcm =
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

                short sample =
                    (short)Math.Round(
                        value * 32767.0f);

                byte[] bytes =
                    BitConverter.GetBytes(
                        sample);

                pcm[i * 2] =
                    bytes[0];

                pcm[(i * 2) + 1] =
                    bytes[1];
            }

            return pcm;
        }

        private static byte[] CreatePcm16MonoWav(
            byte[] pcmBytes,
            int sampleRate)
        {
            const short audioFormat = 1;
            const short channels = 1;
            const short bitsPerSample = 16;

            short blockAlign =
                (short)(
                    channels *
                    (bitsPerSample / 8));

            int byteRate =
                sampleRate *
                blockAlign;

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

                writer.Write(16);

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
    }
}