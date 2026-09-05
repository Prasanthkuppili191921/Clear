using AiInterviewAssistant;
using NAudio.Wave;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace AiInterviewAssistant.Settings.Voice
{
    public partial class VoiceTab : UserControl
    {
        private readonly VoiceBusiness business;

        // =====================================================
        // MICROPHONE TEST
        // =====================================================

        private WaveInEvent testWaveIn;
        private bool testRecording;
        private int testBytesRecorded;

        // =====================================================
        // VOICE ACTIVITY
        // =====================================================

        private DispatcherTimer voiceActivityHideTimer;

        private const double VoiceActivityThreshold = 2.0;

        // =====================================================
        // DEVICE REFRESH
        // =====================================================

        private DispatcherTimer deviceRefreshTimer;

        private bool isRefreshingDevices;

        private const int DeviceRefreshIntervalMilliseconds = 1500;

        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public VoiceTab(AppSettings settings)
        {
            InitializeComponent();

            business =
                new VoiceBusiness(settings);

            LoadSettings();

            HideVoiceActivity();

            StartDeviceRefreshTimer();

            Unloaded +=
                UserControl_Unloaded;
        }

        // =====================================================
        // LOAD SETTINGS
        // =====================================================

        public void LoadSettings()
        {
            try
            {
                VoiceEnabledSwitch.IsChecked =
                    business.VoiceEnabled;

                LoadMicrophones();

                LoadOutputDevices();

                HideVoiceActivity();
            }
            catch
            {
            }
        }

        // =====================================================
        // DEVICE REFRESH TIMER
        // =====================================================

        private void StartDeviceRefreshTimer()
        {
            try
            {
                StopDeviceRefreshTimer();

                deviceRefreshTimer =
                    new DispatcherTimer
                    {
                        Interval =
                            TimeSpan.FromMilliseconds(
                                DeviceRefreshIntervalMilliseconds)
                    };

                deviceRefreshTimer.Tick +=
                    DeviceRefreshTimer_Tick;

                deviceRefreshTimer.Start();
            }
            catch
            {
            }
        }

        // =====================================================
        // DEVICE REFRESH TIMER TICK
        // =====================================================

        private void DeviceRefreshTimer_Tick(
            object sender,
            EventArgs e)
        {
            RefreshAudioDevicesIfRequired();
        }

        // =====================================================
        // REFRESH AUDIO DEVICES
        // =====================================================

        private void RefreshAudioDevicesIfRequired()
        {
            if (isRefreshingDevices)
                return;

            if (testRecording)
                return;

            try
            {
                isRefreshingDevices = true;

                string selectedMicrophone =
                    GetSelectedComboBoxValue(
                        MicrophoneComboBox);

                string selectedOutputDevice =
                    GetSelectedComboBoxValue(
                        OutputDeviceComboBox);

                string[] microphones =
                    business.GetMicrophones();

                string[] outputDevices =
                    business.GetOutputDevices();

                bool microphoneChanged =
                    !AreItemsSame(
                        MicrophoneComboBox,
                        microphones);

                bool outputDeviceChanged =
                    !AreItemsSame(
                        OutputDeviceComboBox,
                        outputDevices);

                if (microphoneChanged)
                {
                    ReloadComboBox(
                        MicrophoneComboBox,
                        microphones,
                        selectedMicrophone);
                }

                if (outputDeviceChanged)
                {
                    ReloadComboBox(
                        OutputDeviceComboBox,
                        outputDevices,
                        selectedOutputDevice);
                }
            }
            catch
            {
            }
            finally
            {
                isRefreshingDevices = false;
            }
        }

        // =====================================================
        // CHECK COMBOBOX ITEMS
        // =====================================================

        private bool AreItemsSame(
            ComboBox comboBox,
            string[] newItems)
        {
            if (comboBox == null)
                return false;

            if (newItems == null)
                return comboBox.Items.Count == 0;

            if (comboBox.Items.Count != newItems.Length)
                return false;

            for (int i = 0;
                 i < newItems.Length;
                 i++)
            {
                string current =
                    comboBox.Items[i]?
                        .ToString();

                if (!string.Equals(
                        current,
                        newItems[i],
                        StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        // =====================================================
        // RELOAD COMBOBOX
        // =====================================================

        private void ReloadComboBox(
            ComboBox comboBox,
            string[] devices,
            string previousSelection)
        {
            if (comboBox == null)
                return;

            comboBox.Items.Clear();

            if (devices != null)
            {
                foreach (string device in devices)
                {
                    if (string.IsNullOrWhiteSpace(device))
                        continue;

                    comboBox.Items.Add(device);
                }
            }

            // -------------------------------------------------
            // Preserve previous selection
            // -------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                    previousSelection))
            {
                comboBox.SelectedItem =
                    previousSelection;
            }

            // -------------------------------------------------
            // Select first available device
            // -------------------------------------------------

            if (comboBox.SelectedIndex < 0 &&
                comboBox.Items.Count > 0)
            {
                comboBox.SelectedIndex = 0;
            }
        }

        // =====================================================
        // GET SELECTED DEVICE
        // =====================================================

        private string GetSelectedComboBoxValue(
            ComboBox comboBox)
        {
            if (comboBox == null)
                return null;

            if (comboBox.SelectedItem == null)
                return null;

            return comboBox.SelectedItem
                .ToString();
        }

        // =====================================================
        // STOP DEVICE REFRESH TIMER
        // =====================================================

        private void StopDeviceRefreshTimer()
        {
            try
            {
                if (deviceRefreshTimer != null)
                {
                    deviceRefreshTimer.Stop();

                    deviceRefreshTimer.Tick -=
                        DeviceRefreshTimer_Tick;

                    deviceRefreshTimer = null;
                }
            }
            catch
            {
            }
        }

        // =====================================================
        // MICROPHONES
        // =====================================================

        private void LoadMicrophones()
        {
            MicrophoneComboBox.Items.Clear();

            try
            {
                string[] microphones =
                    business.GetMicrophones();

                foreach (string microphone in microphones)
                {
                    if (string.IsNullOrWhiteSpace(microphone))
                        continue;

                    MicrophoneComboBox.Items.Add(
                        microphone);
                }

                if (!string.IsNullOrWhiteSpace(
                        business.Microphone))
                {
                    MicrophoneComboBox.SelectedItem =
                        business.Microphone;
                }

                if (MicrophoneComboBox.SelectedIndex < 0 &&
                    MicrophoneComboBox.Items.Count > 0)
                {
                    MicrophoneComboBox.SelectedIndex = 0;
                }
            }
            catch
            {
            }
        }

        // =====================================================
        // OUTPUT DEVICES
        // =====================================================

        private void LoadOutputDevices()
        {
            OutputDeviceComboBox.Items.Clear();

            try
            {
                string[] outputDevices =
                    business.GetOutputDevices();

                foreach (string device in outputDevices)
                {
                    if (string.IsNullOrWhiteSpace(device))
                        continue;

                    OutputDeviceComboBox.Items.Add(
                        device);
                }

                if (!string.IsNullOrWhiteSpace(
                        business.OutputDevice))
                {
                    OutputDeviceComboBox.SelectedItem =
                        business.OutputDevice;
                }

                if (OutputDeviceComboBox.SelectedIndex < 0 &&
                    OutputDeviceComboBox.Items.Count > 0)
                {
                    OutputDeviceComboBox.SelectedIndex = 0;
                }
            }
            catch
            {
            }
        }

        // =====================================================
        // SAVE SETTINGS
        // =====================================================

        public void SaveSettings()
        {
            business.VoiceEnabled =
                VoiceEnabledSwitch.IsChecked == true;

            if (MicrophoneComboBox.SelectedItem != null)
            {
                business.Microphone =
                    MicrophoneComboBox.SelectedItem
                        .ToString();
            }

            if (OutputDeviceComboBox.SelectedItem != null)
            {
                business.OutputDevice =
                    OutputDeviceComboBox.SelectedItem
                        .ToString();
            }
        }

        // =====================================================
        // TEST BUTTON
        // =====================================================

        private void TestVoiceButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                if (testRecording)
                {
                    StopMicrophoneTest();

                    return;
                }

                StartMicrophoneTest();
            }
            catch (Exception ex)
            {
                StopMicrophoneTest();

                AppMessage.Show(
                    "Microphone test error:\n\n" +
                    ex.Message,
                    "Microphone Test",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // =====================================================
        // START TEST
        // =====================================================

        private void StartMicrophoneTest()
        {
            if (MicrophoneComboBox.SelectedIndex < 0)
            {
                AppMessage.Show(
                    "Please select a microphone first.",
                    "Microphone Test",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            int deviceNumber =
                MicrophoneComboBox.SelectedIndex;

            testBytesRecorded = 0;

            HideVoiceActivity();

            testWaveIn =
                new WaveInEvent
                {
                    DeviceNumber =
                        deviceNumber,

                    WaveFormat =
                        new WaveFormat(
                            16000,
                            16,
                            1),

                    BufferMilliseconds = 50
                };

            testWaveIn.DataAvailable +=
                TestWaveIn_DataAvailable;

            testWaveIn.StartRecording();

            testRecording = true;

            TestVoiceButton.Content =
                "⏹  Stop Microphone Test";

            DispatcherTimer timer =
                new DispatcherTimer
                {
                    Interval =
                        TimeSpan.FromSeconds(3)
                };

            timer.Tick +=
                (s, e) =>
                {
                    timer.Stop();

                    if (testRecording)
                    {
                        StopMicrophoneTest();
                    }
                };

            timer.Start();
        }

        // =====================================================
        // AUDIO DATA
        // =====================================================

        private void TestWaveIn_DataAvailable(
            object sender,
            WaveInEventArgs e)
        {
            try
            {
                if (e.BytesRecorded <= 0)
                    return;

                testBytesRecorded +=
                    e.BytesRecorded;

                double sum = 0;

                int sampleCount =
                    e.BytesRecorded / 2;

                if (sampleCount <= 0)
                    return;

                for (int i = 0;
                     i + 1 < e.BytesRecorded;
                     i += 2)
                {
                    short sample =
                        BitConverter.ToInt16(
                            e.Buffer,
                            i);

                    double normalized =
                        sample / 32768.0;

                    sum +=
                        normalized *
                        normalized;
                }

                double rms =
                    Math.Sqrt(
                        sum /
                        sampleCount);

                double level =
                    rms * 100.0 * 8.0;

                if (level > 100)
                    level = 100;

                if (level >= VoiceActivityThreshold)
                {
                    ShowVoiceActivity(level);
                }
                else
                {
                    ScheduleVoiceActivityHide();
                }
            }
            catch
            {
            }
        }

        // =====================================================
        // SHOW ACTIVITY
        // =====================================================

        private void ShowVoiceActivity(
            double level)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        ShowVoiceActivity(level);
                    }));

                return;
            }

            if (!testRecording)
                return;

            if (voiceActivityHideTimer != null)
            {
                voiceActivityHideTimer.Stop();

                voiceActivityHideTimer = null;
            }

            if (MicrophoneActivityPanel.Visibility !=
                Visibility.Visible)
            {
                MicrophoneActivityPanel.Visibility =
                    Visibility.Visible;
            }

            UpdateVoiceBars(level);
        }

        // =====================================================
        // HIDE AFTER SILENCE
        // =====================================================

        private void ScheduleVoiceActivityHide()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        ScheduleVoiceActivityHide();
                    }));

                return;
            }

            if (!testRecording)
                return;

            if (voiceActivityHideTimer == null)
            {
                voiceActivityHideTimer =
                    new DispatcherTimer
                    {
                        Interval =
                            TimeSpan.FromMilliseconds(300)
                    };

                voiceActivityHideTimer.Tick +=
                    (s, e) =>
                    {
                        voiceActivityHideTimer.Stop();

                        voiceActivityHideTimer = null;

                        HideVoiceActivity();
                    };
            }

            voiceActivityHideTimer.Stop();

            voiceActivityHideTimer.Start();
        }

        // =====================================================
        // HIDE ACTIVITY
        // =====================================================

        private void HideVoiceActivity()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        HideVoiceActivity();
                    }));

                return;
            }

            if (voiceActivityHideTimer != null)
            {
                voiceActivityHideTimer.Stop();

                voiceActivityHideTimer = null;
            }

            StopVoiceBars();

            if (MicrophoneActivityPanel != null)
            {
                MicrophoneActivityPanel.Visibility =
                    Visibility.Collapsed;
            }
        }

        // =====================================================
        // UPDATE BARS
        // =====================================================

        private void UpdateVoiceBars(
            double level)
        {
            double normalized =
                level / 100.0;

            if (normalized < 0.05)
                normalized = 0.05;

            if (normalized > 1)
                normalized = 1;

            double h1 =
                8 + (10 * normalized);

            double h2 =
                10 + (18 * normalized);

            double h3 =
                14 + (24 * normalized);

            double h4 =
                18 + (30 * normalized);

            double h5 =
                14 + (24 * normalized);

            double h6 =
                10 + (18 * normalized);

            double h7 =
                8 + (10 * normalized);

            AnimateBar(VoiceBar1, h1);
            AnimateBar(VoiceBar2, h2);
            AnimateBar(VoiceBar3, h3);
            AnimateBar(VoiceBar4, h4);
            AnimateBar(VoiceBar5, h5);
            AnimateBar(VoiceBar6, h6);
            AnimateBar(VoiceBar7, h7);

            UpdateBarColors(level);
        }

        // =====================================================
        // ANIMATE BAR
        // =====================================================

        private void AnimateBar(
            FrameworkElement bar,
            double height)
        {
            if (bar == null)
                return;

            DoubleAnimation animation =
                new DoubleAnimation
                {
                    To = height,

                    Duration =
                        AppearanceManager.GetAnimationDuration(
                            70),

                    EasingFunction =
                        new QuadraticEase
                        {
                            EasingMode =
                                EasingMode.EaseOut
                        }
                };

            bar.BeginAnimation(
                FrameworkElement.HeightProperty,
                animation);
        }

        // =====================================================
        // COLOR
        // =====================================================

        private void UpdateBarColors(
            double level)
        {
            Color color;

            if (level < 25)
            {
                color =
                    Color.FromRgb(
                        77,
                        141,
                        255);
            }
            else if (level < 60)
            {
                color =
                    Color.FromRgb(
                        50,
                        190,
                        255);
            }
            else if (level < 80)
            {
                color =
                    Color.FromRgb(
                        80,
                        220,
                        170);
            }
            else
            {
                color =
                    Color.FromRgb(
                        255,
                        180,
                        70);
            }

            SolidColorBrush brush =
                new SolidColorBrush(color);

            VoiceBar1.Background = brush;
            VoiceBar2.Background = brush;
            VoiceBar3.Background = brush;
            VoiceBar4.Background = brush;
            VoiceBar5.Background = brush;
            VoiceBar6.Background = brush;
            VoiceBar7.Background = brush;
        }

        // =====================================================
        // STOP BARS
        // =====================================================

        private void StopVoiceBars()
        {
            ResetBar(VoiceBar1, 8);
            ResetBar(VoiceBar2, 14);
            ResetBar(VoiceBar3, 20);
            ResetBar(VoiceBar4, 28);
            ResetBar(VoiceBar5, 20);
            ResetBar(VoiceBar6, 14);
            ResetBar(VoiceBar7, 8);
        }

        // =====================================================
        // RESET BAR
        // =====================================================

        private void ResetBar(
            FrameworkElement bar,
            double height)
        {
            if (bar == null)
                return;

            bar.BeginAnimation(
                FrameworkElement.HeightProperty,
                null);

            bar.Height = height;

            if (bar is Border border)
            {
                border.Background =
                    new SolidColorBrush(
                        Color.FromRgb(
                            77,
                            141,
                            255));
            }
        }

        // =====================================================
        // STOP TEST
        // =====================================================

        private void StopMicrophoneTest()
        {
            bool wasRecording =
                testRecording;

            HideVoiceActivity();

            try
            {
                if (testWaveIn != null)
                {
                    testWaveIn.DataAvailable -=
                        TestWaveIn_DataAvailable;

                    try
                    {
                        testWaveIn.StopRecording();
                    }
                    catch
                    {
                    }

                    try
                    {
                        testWaveIn.Dispose();
                    }
                    catch
                    {
                    }

                    testWaveIn = null;
                }
            }
            catch
            {
            }

            testRecording = false;

            if (TestVoiceButton != null)
            {
                TestVoiceButton.Content =
                    "🎤  Test Microphone";
            }

            if (wasRecording)
            {
                if (testBytesRecorded > 0)
                {
                    AppMessage.Show(
                        "Microphone is working correctly.",
                        "Microphone Test",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    AppMessage.Show(
                        "No audio input was detected.\n\n" +
                        "Please check your microphone and Windows microphone permissions.",
                        "Microphone Test",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
        }

        // =====================================================
        // CLEANUP
        // =====================================================

        private void UserControl_Unloaded(
            object sender,
            RoutedEventArgs e)
        {
            StopDeviceRefreshTimer();

            StopMicrophoneTest();

            HideVoiceActivity();
        }
    }
}