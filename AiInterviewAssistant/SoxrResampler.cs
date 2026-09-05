using System;
using System.Runtime.InteropServices;

namespace AiInterviewAssistant
{
    internal static class SoxrResampler
    {
        private const string SoxrDll =
            "Native\\soxr.dll";


        // =========================================================
        // libsoxr
        // Default I/O:
        // FLOAT32 -> FLOAT32
        // Default quality:
        // HQ
        // =========================================================

        [DllImport(
            SoxrDll,
            CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr soxr_create(
            double inputRate,
            double outputRate,
            uint channels,
            out IntPtr error,
            IntPtr ioSpec,
            IntPtr qualitySpec,
            IntPtr runtimeSpec);


        [DllImport(
            SoxrDll,
            CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr soxr_process(
            IntPtr soxr,
            IntPtr input,
            UIntPtr inputLength,
            out UIntPtr inputDone,
            IntPtr output,
            UIntPtr outputLength,
            out UIntPtr outputDone);


        [DllImport(
            SoxrDll,
            CallingConvention = CallingConvention.Cdecl)]
        private static extern void soxr_delete(
            IntPtr soxr);


        // =========================================================
        // FLOAT32 RESAMPLER
        // =========================================================

        public static float[] ResampleFloat32(
            float[] input,
            int inputSampleRate,
            int outputSampleRate)
        {
            if (input == null ||
                input.Length == 0)
            {
                return null;
            }

            if (inputSampleRate <= 0 ||
                outputSampleRate <= 0)
            {
                throw new ArgumentException(
                    "Invalid sample rate.");
            }

            if (inputSampleRate ==
                outputSampleRate)
            {
                float[] copy =
                    new float[input.Length];

                Array.Copy(
                    input,
                    copy,
                    input.Length);

                return copy;
            }


            IntPtr soxr =
                IntPtr.Zero;

            GCHandle inputHandle =
                default(GCHandle);

            GCHandle outputHandle =
                default(GCHandle);

            try
            {
                // =================================================
                // CREATE RESAMPLER
                //
                // NULL ioSpec =
                // FLOAT32 input/output
                //
                // NULL qualitySpec =
                // HQ quality
                // =================================================

                IntPtr createError;

                soxr =
                    soxr_create(
                        inputSampleRate,
                        outputSampleRate,
                        1,
                        out createError,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        IntPtr.Zero);


                if (soxr == IntPtr.Zero)
                {
                    throw new InvalidOperationException(
                        "libsoxr initialization failed.");
                }


                if (createError != IntPtr.Zero)
                {
                    string error =
                        Marshal.PtrToStringAnsi(
                            createError);

                    throw new InvalidOperationException(
                        "libsoxr initialization error: " +
                        error);
                }


                // =================================================
                // OUTPUT SIZE
                // =================================================

                int estimatedSamples =
                    (int)Math.Ceiling(
                        input.Length *
                        (double)outputSampleRate /
                        inputSampleRate);

                // Extra space for resampler filter tail.
                int outputCapacity =
                    estimatedSamples + 256;


                float[] output =
                    new float[outputCapacity];


                // =================================================
                // PIN ARRAYS
                // =================================================

                inputHandle =
                    GCHandle.Alloc(
                        input,
                        GCHandleType.Pinned);

                outputHandle =
                    GCHandle.Alloc(
                        output,
                        GCHandleType.Pinned);


                IntPtr inputPtr =
                    inputHandle.AddrOfPinnedObject();

                IntPtr outputPtr =
                    outputHandle.AddrOfPinnedObject();


                // =================================================
                // PROCESS INPUT
                // =================================================

                UIntPtr inputDone;

                UIntPtr outputDone;


                IntPtr processError =
                    soxr_process(
                        soxr,
                        inputPtr,
                        (UIntPtr)input.Length,
                        out inputDone,
                        outputPtr,
                        (UIntPtr)outputCapacity,
                        out outputDone);


                if (processError != IntPtr.Zero)
                {
                    string error =
                        Marshal.PtrToStringAnsi(
                            processError);

                    throw new InvalidOperationException(
                        "libsoxr processing error: " +
                        error);
                }


                int totalOutput =
                    checked(
                        (int)outputDone.ToUInt64());


                // =================================================
                // FLUSH FILTER
                //
                // This makes sure the final samples held inside
                // the resampler are also emitted.
                // =================================================

                if (totalOutput <
                    outputCapacity)
                {
                    IntPtr flushError =
                        soxr_process(
                            soxr,
                            IntPtr.Zero,
                            UIntPtr.Zero,
                            out inputDone,
                            IntPtr.Add(
                                outputPtr,
                                totalOutput * sizeof(float)),
                            (UIntPtr)(
                                outputCapacity -
                                totalOutput),
                            out outputDone);


                    if (flushError != IntPtr.Zero)
                    {
                        string error =
                            Marshal.PtrToStringAnsi(
                                flushError);

                        throw new InvalidOperationException(
                            "libsoxr flush error: " +
                            error);
                    }


                    totalOutput +=
                        checked(
                            (int)outputDone.ToUInt64());
                }


                if (totalOutput <= 0)
                {
                    throw new InvalidOperationException(
                        "libsoxr produced no output.");
                }


                // =================================================
                // FINAL ARRAY
                // =================================================

                float[] result =
                    new float[totalOutput];


                Array.Copy(
                    output,
                    result,
                    totalOutput);


                return result;
            }
            finally
            {
                if (inputHandle.IsAllocated)
                {
                    inputHandle.Free();
                }

                if (outputHandle.IsAllocated)
                {
                    outputHandle.Free();
                }

                if (soxr != IntPtr.Zero)
                {
                    try
                    {
                        soxr_delete(
                            soxr);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}