using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace AiInterviewAssistant
{
    public partial class AppMessage : Window
    {
        // =========================================================
        // RESULT
        // =========================================================

        private MessageBoxResult _result =
            MessageBoxResult.None;


        // =========================================================
        // SCREEN CAPTURE PROTECTION
        // =========================================================

        [DllImport(
            "user32.dll",
            SetLastError = true)]
        private static extern bool SetWindowDisplayAffinity(
            IntPtr hWnd,
            uint dwAffinity);


        private const uint WDA_EXCLUDEFROMCAPTURE =
            0x00000011;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public AppMessage(
            string message,
            string title,
            MessageBoxButton buttons,
            MessageBoxImage icon)
        {
            InitializeComponent();


            // -----------------------------------------------------
            // KEYBOARD
            // -----------------------------------------------------

            PreviewKeyDown +=
                AppMessage_PreviewKeyDown;


            // -----------------------------------------------------
            // WINDOW INITIALIZATION
            // -----------------------------------------------------

            SourceInitialized +=
                AppMessage_SourceInitialized;


            // -----------------------------------------------------
            // CONTENT
            // -----------------------------------------------------

            TitleTextBlock.Text =
                title ?? "Message";


            MessageTextBlock.Text =
                message ?? string.Empty;


            // -----------------------------------------------------
            // BUTTONS
            // -----------------------------------------------------

            ConfigureButtons(buttons);

            if (buttons == MessageBoxButton.OK)
            {
                DispatcherTimer timer =
                    new DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(1)
                    };

                timer.Tick += (sender, args) =>
                {
                    timer.Stop();

                    _result =
                        MessageBoxResult.OK;

                    DialogResult =
                        true;
                };

                timer.Start();
            }


            // -----------------------------------------------------
            // ICON
            // -----------------------------------------------------

            ConfigureIcon(icon);
        }


        // =========================================================
        // SOURCE INITIALIZED
        // =========================================================

        private void AppMessage_SourceInitialized(
            object sender,
            EventArgs e)
        {
            ApplyScreenCaptureProtection();
        }


        // =========================================================
        // SCREEN CAPTURE PROTECTION
        // =========================================================

        private void ApplyScreenCaptureProtection()
        {
            try
            {
                IntPtr hwnd =
                    new WindowInteropHelper(this).Handle;


                if (hwnd == IntPtr.Zero)
                    return;


                SetWindowDisplayAffinity(
                    hwnd,
                    WDA_EXCLUDEFROMCAPTURE);
            }
            catch
            {
                // Privacy protection must never
                // crash the application.
            }
        }


        // =========================================================
        // STATIC SHOW
        // =========================================================

        public static MessageBoxResult Show(
            string message,
            string title = "Message",
            MessageBoxButton buttons =
                MessageBoxButton.OK,
            MessageBoxImage icon =
                MessageBoxImage.Information,
            Window owner = null)
        {
            AppMessage dialog =
                new AppMessage(
                    message,
                    title,
                    buttons,
                    icon);


            // =====================================================
            // FIND ACTIVE APPLICATION WINDOW
            // =====================================================

            if (owner == null)
            {
                owner =
                    Application.Current?
                        .Windows
                        .OfType<Window>()
                        .FirstOrDefault(
                            window =>
                                window != dialog &&
                                window.IsActive &&
                                window.Visibility ==
                                    Visibility.Visible);
            }


            // =====================================================
            // OWNER
            // =====================================================

            if (owner != null)
            {
                dialog.Owner =
                    owner;

                dialog.WindowStartupLocation =
                    WindowStartupLocation.CenterOwner;
            }
            else
            {
                dialog.WindowStartupLocation =
                    WindowStartupLocation.CenterScreen;
            }


            // =====================================================
            // SHOW
            // =====================================================

            dialog.ShowDialog();


            return dialog._result;
        }


        // =========================================================
        // ERROR
        // =========================================================

        public static void ShowError(
            string message,
            string title = "Error",
            Window owner = null)
        {
            Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error,
                owner);
        }


        // =========================================================
        // INFORMATION
        // =========================================================

        public static void ShowInfo(
            string message,
            string title = "Information",
            Window owner = null)
        {
            Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Information,
                owner);
        }


        // =========================================================
        // QUESTION
        // =========================================================

        public static bool ShowQuestion(
            string message,
            string title = "Confirmation",
            Window owner = null)
        {
            return
                Show(
                    message,
                    title,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    owner)
                ==
                MessageBoxResult.Yes;
        }


        // =========================================================
        // YES / NO / CANCEL
        // =========================================================

        public static MessageBoxResult ShowYesNoCancel(
            string message,
            string title = "Confirmation",
            Window owner = null)
        {
            return
                Show(
                    message,
                    title,
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question,
                    owner);
        }


        // =========================================================
        // BUTTON CONFIGURATION
        // =========================================================

        private void ConfigureButtons(
            MessageBoxButton buttons)
        {
            // -----------------------------------------------------
            // RESET
            // -----------------------------------------------------

            OkButton.Visibility =
                Visibility.Collapsed;

            YesButton.Visibility =
                Visibility.Collapsed;

            NoButton.Visibility =
                Visibility.Collapsed;

            CancelButton.Visibility =
                Visibility.Collapsed;


            // -----------------------------------------------------
            // CONFIGURE
            // -----------------------------------------------------

            switch (buttons)
            {
                case MessageBoxButton.OK:

                    OkButton.Visibility =
                        Visibility.Visible;

                    break;


                case MessageBoxButton.OKCancel:

                    OkButton.Visibility =
                        Visibility.Visible;

                    CancelButton.Visibility =
                        Visibility.Visible;

                    break;


                case MessageBoxButton.YesNo:

                    YesButton.Visibility =
                        Visibility.Visible;

                    NoButton.Visibility =
                        Visibility.Visible;

                    break;


                case MessageBoxButton.YesNoCancel:

                    YesButton.Visibility =
                        Visibility.Visible;

                    NoButton.Visibility =
                        Visibility.Visible;

                    CancelButton.Visibility =
                        Visibility.Visible;

                    break;
            }
        }


        // =========================================================
        // ICON
        // =========================================================

        private void ConfigureIcon(
            MessageBoxImage icon)
        {
            switch (icon)
            {
                case MessageBoxImage.Error:

                    IconTextBlock.Text =
                        "✕";

                    IconTextBlock.Foreground =
                        new SolidColorBrush(
                            Color.FromRgb(
                                255,
                                95,
                                95));

                    break;


                case MessageBoxImage.Warning:

                    IconTextBlock.Text =
                        "⚠";

                    IconTextBlock.Foreground =
                        new SolidColorBrush(
                            Color.FromRgb(
                                255,
                                190,
                                70));

                    break;


                case MessageBoxImage.Question:

                    IconTextBlock.Text =
                        "?";

                    IconTextBlock.Foreground =
                        new SolidColorBrush(
                            Color.FromRgb(
                                77,
                                141,
                                255));

                    break;


                case MessageBoxImage.Information:

                    IconTextBlock.Text =
                        "i";

                    IconTextBlock.Foreground =
                        new SolidColorBrush(
                            Color.FromRgb(
                                77,
                                141,
                                255));

                    break;


                default:

                    IconTextBlock.Text =
                        "✓";

                    IconTextBlock.Foreground =
                        new SolidColorBrush(
                            Color.FromRgb(
                                77,
                                141,
                                255));

                    break;
            }
        }


        // =========================================================
        // OK
        // =========================================================

        private void OkButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _result =
                MessageBoxResult.OK;

            DialogResult =
                true;
        }


        // =========================================================
        // YES
        // =========================================================

        private void YesButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _result =
                MessageBoxResult.Yes;

            DialogResult =
                true;
        }


        // =========================================================
        // NO
        // =========================================================

        private void NoButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _result =
                MessageBoxResult.No;

            DialogResult =
                false;
        }


        // =========================================================
        // CANCEL
        // =========================================================

        private void CancelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _result =
                MessageBoxResult.Cancel;

            DialogResult =
                false;
        }


        // =========================================================
        // ENTER KEY
        // =========================================================

        private void AppMessage_PreviewKeyDown(
            object sender,
            KeyEventArgs e)
        {
            // -----------------------------------------------------
            // ONLY NORMAL ENTER
            // -----------------------------------------------------

            if (e.Key != Key.Enter)
                return;


            // -----------------------------------------------------
            // CTRL + ENTER
            // -----------------------------------------------------
            // Must NOT trigger popup button.
            // -----------------------------------------------------

            if (Keyboard.Modifiers.HasFlag(
                    ModifierKeys.Control))
            {
                return;
            }


            // -----------------------------------------------------
            // ALT / SHIFT + ENTER
            // -----------------------------------------------------

            if (Keyboard.Modifiers.HasFlag(
                    ModifierKeys.Alt) ||
                Keyboard.Modifiers.HasFlag(
                    ModifierKeys.Shift))
            {
                return;
            }


            // =====================================================
            // OK
            // =====================================================

            if (OkButton.Visibility ==
                    Visibility.Visible &&
                OkButton.IsEnabled)
            {
                OkButton_Click(
                    OkButton,
                    new RoutedEventArgs());

                e.Handled =
                    true;

                return;
            }


            // =====================================================
            // YES
            // =====================================================

            if (YesButton.Visibility ==
                    Visibility.Visible &&
                YesButton.IsEnabled)
            {
                YesButton_Click(
                    YesButton,
                    new RoutedEventArgs());

                e.Handled =
                    true;

                return;
            }
        }
    }
}