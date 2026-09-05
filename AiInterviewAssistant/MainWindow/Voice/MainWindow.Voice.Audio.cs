using NAudio.Wave;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace AiInterviewAssistant
{
    public partial class MainWindow
    {

        // =========================================================
        // CREATE WHISPER-OPTIMIZED WAV BYTES
        // =========================================================

        private byte[] CreateWavBytes(byte[] rawAudio)
        {
            if (rawAudio == null ||
                rawAudio.Length == 0)
            {
                return null;
            }

            try
            {
                // =====================================================
                // SOURCE FORMAT
                // =====================================================

                WaveFormat sourceFormat =
                    voiceRecordingFormat;

                if (sourceFormat == null)
                {
                    Debug.WriteLine(
                        "CreateWavBytes: source format is NULL.");

                    return null;
                }

                int sourceSampleRate =
                    sourceFormat.SampleRate;

                int sourceChannels =
                    Math.Max(
                        1,
                        sourceFormat.Channels);

                int sourceBits =
                    sourceFormat.BitsPerSample;

                WaveFormatEncoding encoding =
                    sourceFormat.Encoding;


                Debug.WriteLine(
                    "SOURCE FORMAT = " +
                    encoding +
                    " | " +
                    sourceSampleRate +
                    "Hz | " +
                    sourceBits +
                    "bit | " +
                    sourceChannels +
                    "ch");


                // =====================================================
                // SUPPORTED WASAPI FORMATS
                // =====================================================

                if (!(
                    (encoding == WaveFormatEncoding.IeeeFloat &&
                     sourceBits == 32)
                    ||
                    (encoding == WaveFormatEncoding.Pcm &&
                     (sourceBits == 16 ||
                      sourceBits == 24 ||
                      sourceBits == 32))
                ))
                {
                    Debug.WriteLine(
                        "Unsupported source audio format.");

                    return null;
                }


                // =====================================================
                // SOURCE FRAME
                // =====================================================

                int sourceBytesPerSample =
                    sourceBits / 8;

                int sourceFrameSize =
                    sourceBytesPerSample *
                    sourceChannels;

                if (sourceFrameSize <= 0)
                {
                    return null;
                }


                int sourceFrameCount =
                    rawAudio.Length /
                    sourceFrameSize;

                if (sourceFrameCount <= 0)
                {
                    return null;
                }


                // =====================================================
                // TARGET FORMAT
                // =====================================================

                const int targetSampleRate =
                    16000;

                const int targetChannels =
                    1;

                const int targetBits =
                    16;


                // =====================================================
                // SOURCE → MONO FLOAT32
                // =====================================================

                float[] sourceSamples =
                    new float[sourceFrameCount];


                for (int i = 0;
                     i < sourceFrameCount;
                     i++)
                {
                    float sample =
                        ReadMonoSample(
                            rawAudio,
                            i,
                            sourceBytesPerSample,
                            sourceChannels,
                            encoding,
                            sourceBits,
                            sourceFrameSize);


                    // =================================================
                    // SAFE CLAMP
                    // =================================================

                    if (sample > 1f)
                    {
                        sample = 1f;
                    }
                    else if (sample < -1f)
                    {
                        sample = -1f;
                    }


                    sourceSamples[i] =
                        sample;
                }


                // =====================================================
                // LIBSOXR HIGH-QUALITY RESAMPLING
                // =====================================================

                Stopwatch resampleTimer =
                    Stopwatch.StartNew();


                float[] samples =
                    SoxrResampler.ResampleFloat32(
                        sourceSamples,
                        sourceSampleRate,
                        targetSampleRate);


                resampleTimer.Stop();


                sourceSamples = null;


                if (samples == null ||
                    samples.Length == 0)
                {
                    Debug.WriteLine(
                        "libsoxr returned no samples.");

                    return null;
                }


                Debug.WriteLine(
                    "LIBSOXR RESAMPLING = " +
                    sourceSampleRate +
                    "Hz → " +
                    targetSampleRate +
                    "Hz");


                Debug.WriteLine(
                    "LIBSOXR OUTPUT SAMPLES = " +
                    samples.Length);


                Debug.WriteLine(
                    "LIBSOXR TIME = " +
                    resampleTimer.ElapsedMilliseconds +
                    " ms");


                // =====================================================
                // REMOVE DC OFFSET
                // =====================================================

                double dcSum =
                    0.0;


                for (int i = 0;
                     i < samples.Length;
                     i++)
                {
                    dcSum +=
                        samples[i];
                }


                float dcOffset =
                    samples.Length > 0
                        ? (float)(
                            dcSum /
                            samples.Length)
                        : 0f;


                Debug.WriteLine(
                    "DC OFFSET = " +
                    dcOffset.ToString("0.000000"));


                if (Math.Abs(dcOffset) >
                    0.00001f)
                {
                    for (int i = 0;
                         i < samples.Length;
                         i++)
                    {
                        samples[i] -=
                            dcOffset;
                    }
                }


                // =====================================================
                // MEASURE AUDIO LEVEL
                // =====================================================

                double sumSquares =
                    0.0;

                float peak =
                    0f;

                int nonSilentSamples =
                    0;


                for (int i = 0;
                     i < samples.Length;
                     i++)
                {
                    float value =
                        samples[i];

                    float absolute =
                        Math.Abs(value);


                    if (absolute > peak)
                    {
                        peak =
                            absolute;
                    }


                    // Ignore extremely tiny background values
                    // when calculating RMS.
                    if (absolute >
                        0.001f)
                    {
                        sumSquares +=
                            value * value;

                        nonSilentSamples++;
                    }
                }


                double rms =
                    0.0;


                if (nonSilentSamples > 0)
                {
                    rms =
                        Math.Sqrt(
                            sumSquares /
                            nonSilentSamples);
                }


                Debug.WriteLine(
                    "PRE-GAIN RMS = " +
                    rms.ToString("0.000000"));


                Debug.WriteLine(
                    "PRE-GAIN PEAK = " +
                    peak.ToString("0.000000"));


                // =====================================================
                // WHISPER-FRIENDLY NORMALIZATION
                // =====================================================
                //
                // Normal voice:
                //     almost untouched.
                //
                // Quiet voice:
                //     gently amplified.
                //
                // Very quiet voice:
                //     maximum 3x.
                //
                // Loud voice:
                //     never amplified.
                // =====================================================

                float gain =
                    1.0f;


                if (rms > 0.00001)
                {
                    const double targetRms =
                        0.095;


                    gain =
                        (float)(
                            targetRms /
                            rms);


                    // Never attenuate naturally good audio.
                    if (gain < 1.0f)
                    {
                        gain =
                            1.0f;
                    }


                    // Prevent excessive amplification.
                    if (gain > 3.0f)
                    {
                        gain =
                            3.0f;
                    }
                }


                Debug.WriteLine(
                    "WHISPER GAIN = " +
                    gain.ToString("0.00") +
                    "x");


                // =====================================================
                // APPLY GAIN
                // =====================================================

                if (gain > 1.0f)
                {
                    for (int i = 0;
                         i < samples.Length;
                         i++)
                    {
                        samples[i] *=
                            gain;
                    }
                }


                // =====================================================
                // MEASURE POST-GAIN PEAK
                // =====================================================

                float peakAfterGain =
                    0f;


                for (int i = 0;
                     i < samples.Length;
                     i++)
                {
                    float absolute =
                        Math.Abs(
                            samples[i]);


                    if (absolute >
                        peakAfterGain)
                    {
                        peakAfterGain =
                            absolute;
                    }
                }


                Debug.WriteLine(
                    "POST-GAIN PEAK = " +
                    peakAfterGain.ToString("0.000000"));


                // =====================================================
                // GENTLE PEAK LIMITER
                // =====================================================
                //
                // No compressor.
                //
                // Natural speech dynamics are preserved.
                // Only protect against clipping.
                // =====================================================

                const float safePeak =
                    0.90f;


                if (peakAfterGain >
                    safePeak)
                {
                    float limiterGain =
                        safePeak /
                        peakAfterGain;


                    for (int i = 0;
                         i < samples.Length;
                         i++)
                    {
                        samples[i] *=
                            limiterGain;
                    }


                    Debug.WriteLine(
                        "LIMITER GAIN = " +
                        limiterGain.ToString("0.000000"));
                }


                // =====================================================
                // VERY LIGHT NOISE FLOOR
                // =====================================================
                //
                // Only remove extremely tiny values.
                //
                // This is NOT a speech gate.
                // Quiet syllables should remain untouched.
                // =====================================================

                const float noiseFloor =
                    0.0008f;


                for (int i = 0;
                     i < samples.Length;
                     i++)
                {
                    if (Math.Abs(samples[i]) <
                        noiseFloor)
                    {
                        samples[i] =
                            0f;
                    }
                }


                // =====================================================
                // FINAL CLAMP
                // =====================================================

                for (int i = 0;
                     i < samples.Length;
                     i++)
                {
                    if (samples[i] > 1f)
                    {
                        samples[i] =
                            1f;
                    }
                    else if (samples[i] < -1f)
                    {
                        samples[i] =
                            -1f;
                    }
                }


                // =====================================================
                // FLOAT32 → PCM16
                // =====================================================

                int pcmBytes =
                    samples.Length *
                    2;


                byte[] pcmData =
                    new byte[pcmBytes];


                for (int i = 0;
                     i < samples.Length;
                     i++)
                {
                    float sample =
                        samples[i];


                    short pcmValue;


                    if (sample <= -1f)
                    {
                        pcmValue =
                            short.MinValue;
                    }
                    else if (sample >= 1f)
                    {
                        pcmValue =
                            short.MaxValue;
                    }
                    else
                    {
                        pcmValue =
                            (short)
                            Math.Round(
                                sample *
                                32767f);
                    }


                    int offset =
                        i * 2;


                    pcmData[offset] =
                        (byte)(
                            pcmValue &
                            0xFF);


                    pcmData[offset + 1] =
                        (byte)(
                            (pcmValue >> 8) &
                            0xFF);
                }


                // =====================================================
                // CREATE WAV
                // =====================================================

                WaveFormat targetFormat =
                    new WaveFormat(
                        targetSampleRate,
                        targetBits,
                        targetChannels);


                using (MemoryStream output =
                       new MemoryStream(
                           44 +
                           pcmData.Length))
                {
                    using (WaveFileWriter writer =
                           new WaveFileWriter(
                               output,
                               targetFormat))
                    {
                        writer.Write(
                            pcmData,
                            0,
                            pcmData.Length);

                        writer.Flush();
                    }


                    byte[] result =
                        output.ToArray();


                    Debug.WriteLine(
                        "WHISPER WAV CREATED = " +
                        result.Length +
                        " bytes");


                    Debug.WriteLine(
                        "TARGET FORMAT = " +
                        "16000Hz | 16bit | Mono");


                    return result;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "CreateWavBytes ERROR:");

                Debug.WriteLine(
                    ex.ToString());

                return null;
            }
        }


        // =========================================================
        // READ MONO SAMPLE
        // =========================================================

        private float ReadMonoSample(
                 byte[] audio,
                 int frameIndex,
                 int bytesPerSample,
                 int channels,
                 WaveFormatEncoding encoding,
                 int bitsPerSample,
                 int frameSize)
        {
            int frameOffset =
                frameIndex * frameSize;

            float sum = 0f;

            int actualChannels =
                Math.Max(
                    1,
                    channels);

            for (int channel = 0;
                 channel < actualChannels;
                 channel++)
            {
                int offset =
                    frameOffset +
                    channel *
                    bytesPerSample;

                float sample;

                // =====================================================
                // IEEE FLOAT 32-BIT
                // =====================================================

                if (encoding ==
                        WaveFormatEncoding.IeeeFloat &&
                    bitsPerSample == 32)
                {
                    sample =
                        BitConverter.ToSingle(
                            audio,
                            offset);
                }

                // =====================================================
                // PCM 16-BIT
                // =====================================================

                else if (
                    encoding ==
                        WaveFormatEncoding.Pcm &&
                    bitsPerSample == 16)
                {
                    short value =
                        BitConverter.ToInt16(
                            audio,
                            offset);

                    sample =
                        value / 32768f;
                }

                // =====================================================
                // PCM 24-BIT
                // =====================================================

                else if (
                    encoding ==
                        WaveFormatEncoding.Pcm &&
                    bitsPerSample == 24)
                {
                    int value =
                        audio[offset] |
                        (audio[offset + 1] << 8) |
                        (audio[offset + 2] << 16);

                    if ((value & 0x800000) != 0)
                    {
                        value |=
                            unchecked(
                                (int)0xFF000000);
                    }

                    sample =
                        value / 8388608f;
                }

                // =====================================================
                // PCM 32-BIT
                // =====================================================

                else if (
                    encoding ==
                        WaveFormatEncoding.Pcm &&
                    bitsPerSample == 32)
                {
                    int value =
                        BitConverter.ToInt32(
                            audio,
                            offset);

                    sample =
                        value / 2147483648f;
                }

                else
                {
                    sample = 0f;
                }

                sum += sample;
            }

            // =========================================================
            // STEREO -> MONO
            // =========================================================

            float monoSample =
                sum / actualChannels;

            // =========================================================
            // CLAMP
            // =========================================================

            if (monoSample > 1f)
            {
                monoSample = 1f;
            }
            else if (monoSample < -1f)
            {
                monoSample = -1f;
            }

            return monoSample;
        }
    }
}