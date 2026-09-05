using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.IO;

public class LocalVoiceCapture : IDisposable
{
    private WasapiCapture _capture;
    private MemoryStream _audioBuffer;
    private WaveFormat _waveFormat;

    public bool IsRecording { get; private set; }

    public WaveFormat WaveFormat
    {
        get
        {
            return _waveFormat;
        }
    }

    public event EventHandler<WaveInEventArgs> DataAvailable;

    public event EventHandler<StoppedEventArgs> RecordingStopped;


    // =========================================================
    // START
    // =========================================================

    public void Start()
    {
        var enumerator =
            new MMDeviceEnumerator();

        var device =
            enumerator.GetDefaultAudioEndpoint(
                DataFlow.Capture,
                Role.Communications);

        if (device == null)
        {
            throw new InvalidOperationException(
                "No microphone input device was found.");
        }

        _capture =
            new WasapiCapture(device);

        // IMPORTANT:
        // Capture format is available after WasapiCapture
        // has been created.
        _waveFormat =
            _capture.WaveFormat;

        if (_waveFormat == null)
        {
            throw new InvalidOperationException(
                "Microphone audio format could not be determined.");
        }

        _audioBuffer =
            new MemoryStream();

        _capture.DataAvailable +=
            Capture_DataAvailable;

        _capture.RecordingStopped +=
            Capture_RecordingStopped;

        _capture.StartRecording();

        IsRecording = true;
    }


    // =========================================================
    // AUDIO DATA
    // =========================================================

    private void Capture_DataAvailable(
        object sender,
        WaveInEventArgs e)
    {
        if (e != null &&
            e.BytesRecorded > 0 &&
            _audioBuffer != null)
        {
            _audioBuffer.Write(
                e.Buffer,
                0,
                e.BytesRecorded);
        }

        DataAvailable?.Invoke(
            this,
            e);
    }


    // =========================================================
    // STOP EVENT
    // =========================================================

    private void Capture_RecordingStopped(
        object sender,
        StoppedEventArgs e)
    {
        IsRecording = false;

        RecordingStopped?.Invoke(
            this,
            e);
    }


    // =========================================================
    // STOP
    // =========================================================

    public void Stop()
    {
        if (_capture != null &&
            IsRecording)
        {
            _capture.StopRecording();
        }
    }


    // =========================================================
    // GET AUDIO
    // =========================================================

    public byte[] GetAudioBytes()
    {
        if (_audioBuffer == null ||
            _audioBuffer.Length == 0)
        {
            return Array.Empty<byte>();
        }

        return _audioBuffer.ToArray();
    }


    // =========================================================
    // DISPOSE
    // =========================================================

    public void Dispose()
    {
        IsRecording = false;

        if (_capture != null)
        {
            _capture.DataAvailable -=
                Capture_DataAvailable;

            _capture.RecordingStopped -=
                Capture_RecordingStopped;

            _capture.Dispose();

            _capture = null;
        }

        if (_audioBuffer != null)
        {
            _audioBuffer.Dispose();

            _audioBuffer = null;
        }

        _waveFormat = null;
    }
}