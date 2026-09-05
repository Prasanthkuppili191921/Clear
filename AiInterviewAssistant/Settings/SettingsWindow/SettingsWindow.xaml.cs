using AiInterviewAssistant.Privacy;
using AiInterviewAssistant.Settings.AI;
using AiInterviewAssistant.Settings.Appearance;
using AiInterviewAssistant.Settings.General;
using AiInterviewAssistant.Settings.Resume;
using AiInterviewAssistant.Settings.Voice;
using AiInterviewAssistant.Settings.HotKeysReadonly;

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Window = System.Windows.Window;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.Forms.MessageBox;

namespace AiInterviewAssistant
{
    public partial class SettingsWindow : Window
    {
        // =========================================================
        // EVENTS
        // =========================================================

        public event EventHandler CloseRequested;
        public event EventHandler SaveRequested;


        // =========================================================
        // MAIN WINDOW
        // =========================================================

        private readonly MainWindow _mainWindow;


        // =========================================================
        // SHARED SETTINGS
        // =========================================================

        private readonly AppSettings _settings;


        // =========================================================
        // PRIVACY
        // =========================================================

        private readonly PrivacyManager _privacyManager;


        // =========================================================
        // CURRENT TAB
        // =========================================================

        private UserControl currentTab;


        // =========================================================
        // TAB INSTANCES
        // =========================================================

        private ResumeTab _resumeTab;
        private GeneralTab _generalTab;
        private AITab _aiTab;
        private VoiceTab _voiceTab;
        private HotKeysReadonly _hotKeysReadonly;
        private AppearanceTab _appearanceTab;


        // =========================================================
        // NAVIGATION COLORS
        // =========================================================

        private readonly Brush ActiveTabBackground =
            new SolidColorBrush(
                Color.FromRgb(
                    48,
                    68,
                    94));

        private readonly Brush ActiveTabForeground =
            new SolidColorBrush(
                Color.FromRgb(
                    244,
                    246,
                    250));

        private readonly Brush NormalTabBackground =
            Brushes.Transparent;

        private readonly Brush DarkNormalTabForeground =
            new SolidColorBrush(
                Color.FromRgb(
                    174,
                    181,
                    194));

        private readonly Brush LightNormalTabForeground =
            new SolidColorBrush(
                Color.FromRgb(
                    55,
                    61,
                    72));

        private Brush NormalTabForeground;


        // =========================================================
        // UNSAVED CHANGES
        // =========================================================

        private bool _hasUnsavedChanges;
        private bool _isTrackingChanges;
        private bool _isSaving;


        // =========================================================
        // CHANGE TRACKING
        // =========================================================

        private bool _changeTrackingRegistered;
        private bool _suppressChangeTracking;

        private string _lastMicrophone;
        private string _lastOutputDevice;


        // =========================================================
        // USER INTERACTION
        // =========================================================

        private bool _userInteraction;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public SettingsWindow(
            MainWindow owner)
        {
            InitializeComponent();

            // -----------------------------------------------------
            // OWNER
            // -----------------------------------------------------

            _mainWindow = owner;
            Owner = owner;


            // -----------------------------------------------------
            // PRIVACY
            // -----------------------------------------------------

            _privacyManager =
                new PrivacyManager(this);


            // -----------------------------------------------------
            // SETTINGS
            // -----------------------------------------------------

            _settings =
                SettingsService.Load()
                ?? new AppSettings();


            // -----------------------------------------------------
            // APPEARANCE
            // -----------------------------------------------------

            AppearanceManager.RegisterWindow(this);

            AppearanceManager.Apply(
                this,
                _settings);


            // -----------------------------------------------------
            // NAVIGATION
            // -----------------------------------------------------

            UpdateNavigationColors();


            // -----------------------------------------------------
            // LOADED
            // -----------------------------------------------------

            Loaded +=
                SettingsWindow_Loaded;


            // -----------------------------------------------------
            // MOUSE DRAG
            // -----------------------------------------------------

            MouseLeftButtonDown +=
                SettingsWindow_MouseLeftButtonDown;
        }


        // =========================================================
        // SETTINGS WINDOW MOUSE DRAG
        // =========================================================

        private void SettingsWindow_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton !=
                    MouseButton.Left)
                {
                    return;
                }

                DependencyObject source =
                    e.OriginalSource as DependencyObject;

                if (IsInsideInteractiveControl(source))
                {
                    return;
                }

                DragMove();
            }
            catch
            {
            }
        }


        // =========================================================
        // CHECK INTERACTIVE CONTROL
        // =========================================================

        private bool IsInsideInteractiveControl(
            DependencyObject source)
        {
            try
            {
                DependencyObject current =
                    source;

                while (current != null)
                {
                    if (current is Button)
                        return true;

                    if (current is TextBox)
                        return true;

                    if (current is ComboBox)
                        return true;

                    if (current is CheckBox)
                        return true;

                    if (current is RadioButton)
                        return true;

                    if (current is Slider)
                        return true;

                    if (current is ListBoxItem)
                        return true;

                    if (current is
                        System.Windows.Controls.Primitives.ScrollBar)
                    {
                        return true;
                    }

                    if (current is ScrollViewer)
                        return true;

                    current =
                        VisualTreeHelper.GetParent(
                            current);
                }
            }
            catch
            {
            }

            return false;
        }


        // =========================================================
        // SOURCE INITIALIZED
        // =========================================================

        protected override void OnSourceInitialized(
            EventArgs e)
        {
            base.OnSourceInitialized(e);

            // =====================================================
            // COMMON WINDOW BEHAVIOR
            // =====================================================

            WindowBehaviorManager.Attach(this);
        }


        // =========================================================
        // LOADED
        // =========================================================

        private void SettingsWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                _isTrackingChanges = false;
                _suppressChangeTracking = true;
                _userInteraction = false;
                _hasUnsavedChanges = false;


                // -------------------------------------------------
                // APPLY APPEARANCE
                // -------------------------------------------------

                AppearanceManager.Apply(
                    this,
                    _settings);

                UpdateNavigationColors();


                // -------------------------------------------------
                // DEFAULT TAB = RESUME
                // -------------------------------------------------

                ShowResumeTab();


                // -------------------------------------------------
                // PRIVACY
                // -------------------------------------------------

                ApplyScreenCaptureProtection(
                    _mainWindow != null &&
                    _mainWindow.IsHideFromCaptureEnabled);


                // -------------------------------------------------
                // INITIALIZATION COMPLETE
                // -------------------------------------------------

                _hasUnsavedChanges = false;
                _userInteraction = false;
                _suppressChangeTracking = false;
                _isTrackingChanges = true;


                // -------------------------------------------------
                // REGISTER CHANGE TRACKING
                // -------------------------------------------------

                RegisterChangeTracking();
            }
            catch
            {
            }
        }


        // =========================================================
        // INTERNAL REFRESH
        // =========================================================

        public void BeginInternalRefresh()
        {
            _suppressChangeTracking = true;
            _userInteraction = false;
        }


        public void EndInternalRefresh()
        {
            _userInteraction = false;
            _suppressChangeTracking = false;
        }


        // =========================================================
        // CHANGE TRACKING
        // =========================================================

        private void RegisterChangeTracking()
        {
            if (SettingsContent == null)
                return;

            if (_changeTrackingRegistered)
                return;

            _changeTrackingRegistered = true;


            SettingsContent.AddHandler(
                UIElement.PreviewMouseDownEvent,
                new MouseButtonEventHandler(
                    SettingsContent_PreviewMouseDown),
                true);


            SettingsContent.AddHandler(
                UIElement.PreviewKeyDownEvent,
                new KeyEventHandler(
                    SettingsContent_PreviewKeyDown),
                true);


            SettingsContent.AddHandler(
                TextBox.TextChangedEvent,
                new TextChangedEventHandler(
                    SettingsContent_TextChanged));


            SettingsContent.AddHandler(
                System.Windows.Controls.Primitives.Selector
                    .SelectionChangedEvent,
                new SelectionChangedEventHandler(
                    SettingsContent_SelectionChanged));


            SettingsContent.AddHandler(
                System.Windows.Controls.Primitives.ToggleButton
                    .CheckedEvent,
                new RoutedEventHandler(
                    SettingsContent_ToggleChanged));


            SettingsContent.AddHandler(
                System.Windows.Controls.Primitives.ToggleButton
                    .UncheckedEvent,
                new RoutedEventHandler(
                    SettingsContent_ToggleChanged));


            SettingsContent.AddHandler(
                System.Windows.Controls.Primitives.RangeBase
                    .ValueChangedEvent,
                new RoutedPropertyChangedEventHandler<double>(
                    SettingsContent_ValueChanged));
        }


        // =========================================================
        // USER MOUSE
        // =========================================================

        private void SettingsContent_PreviewMouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (!_isTrackingChanges)
                return;

            if (_suppressChangeTracking)
                return;

            DependencyObject source =
                e.OriginalSource as DependencyObject;

            if (FindParentComboBox(source) != null)
                return;

            _userInteraction = true;
        }


        // =========================================================
        // FIND PARENT COMBOBOX
        // =========================================================

        private ComboBox FindParentComboBox(
            DependencyObject child)
        {
            while (child != null)
            {
                ComboBox comboBox =
                    child as ComboBox;

                if (comboBox != null)
                    return comboBox;

                child =
                    VisualTreeHelper.GetParent(
                        child);
            }

            return null;
        }


        // =========================================================
        // USER KEYBOARD
        // =========================================================

        private void SettingsContent_PreviewKeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (!_isTrackingChanges)
                return;

            if (_suppressChangeTracking)
                return;

            _userInteraction = true;
        }


        // =========================================================
        // TEXT CHANGED
        // =========================================================

        private void SettingsContent_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            MarkSettingsChanged();
        }


        // =========================================================
        // SELECTION CHANGED
        // =========================================================

        private void SettingsContent_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!_isTrackingChanges)
                return;

            if (_isSaving)
                return;

            if (_suppressChangeTracking)
                return;

            if (e == null)
                return;

            if (e.AddedItems == null ||
                e.AddedItems.Count == 0)
            {
                return;
            }

            if (e.RemovedItems == null ||
                e.RemovedItems.Count == 0)
            {
                return;
            }

            object addedItem =
                e.AddedItems[
                    e.AddedItems.Count - 1];

            object removedItem =
                e.RemovedItems[
                    e.RemovedItems.Count - 1];

            if (Equals(
                    addedItem,
                    removedItem))
            {
                return;
            }

            MarkSettingsChanged();
        }


        // =========================================================
        // TOGGLE CHANGED
        // =========================================================

        private void SettingsContent_ToggleChanged(
            object sender,
            RoutedEventArgs e)
        {
            MarkSettingsChanged();
        }


        // =========================================================
        // VALUE CHANGED
        // =========================================================

        private void SettingsContent_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            MarkSettingsChanged();
        }


        // =========================================================
        // MARK SETTINGS CHANGED
        // =========================================================

        private void MarkSettingsChanged()
        {
            if (!_isTrackingChanges)
                return;

            if (_isSaving)
                return;

            if (_suppressChangeTracking)
                return;

            if (!_userInteraction)
                return;

            _hasUnsavedChanges = true;
            _userInteraction = false;
        }


        // =========================================================
        // PRIVACY
        // =========================================================

        public void ApplyScreenCaptureProtection(
            bool enabled)
        {
            try
            {
                if (_privacyManager == null)
                    return;

                if (enabled)
                {
                    _privacyManager
                        .EnableScreenCaptureProtection();

                    ScreenCaptureProtection
                        .Enable(this);
                }
                else
                {
                    _privacyManager
                        .DisableScreenCaptureProtection();

                    ScreenCaptureProtection
                        .Disable(this);
                }
            }
            catch
            {
            }
        }


        // =========================================================
        // NAVIGATION COLORS
        // =========================================================

        private void UpdateNavigationColors()
        {
            NormalTabForeground =
                DarkNormalTabForeground;

            Button[] buttons =
            {
                ResumeNavButton,
                GeneralNavButton,
                AiNavButton,
                VoiceNavButton,
                HotkeysNavButton,
                AppearanceNavButton
            };

            foreach (Button button in buttons)
            {
                if (button == null)
                    continue;

                button.Background =
                    NormalTabBackground;

                button.Foreground =
                    NormalTabForeground;
            }
        }


        // =========================================================
        // RESUME
        // =========================================================

        private void ShowResumeTab()
        {
            try
            {
                _suppressChangeTracking = true;
                _userInteraction = false;

                if (_resumeTab == null)
                {
                    _resumeTab =
                        new ResumeTab(_settings);
                }

                currentTab =
                    _resumeTab;

                SettingsContent.Content =
                    currentTab;

                SetActiveNavButton(
                    ResumeNavButton);
            }
            finally
            {
                _userInteraction = false;
                _suppressChangeTracking = false;
            }
        }


        // =========================================================
        // GENERAL
        // =========================================================

        private void ShowGeneralTab()
        {
            try
            {
                _suppressChangeTracking = true;
                _userInteraction = false;

                if (_generalTab == null)
                {
                    _generalTab =
                        new GeneralTab(_settings);
                }

                currentTab =
                    _generalTab;

                SettingsContent.Content =
                    currentTab;

                SetActiveNavButton(
                    GeneralNavButton);
            }
            finally
            {
                _userInteraction = false;
                _suppressChangeTracking = false;
            }
        }


        // =========================================================
        // AI
        // =========================================================

        private void ShowAITab()
        {
            try
            {
                _suppressChangeTracking = true;
                _userInteraction = false;

                if (_aiTab == null)
                {
                    _aiTab =
                        new AITab(_settings);
                }

                currentTab =
                    _aiTab;

                SettingsContent.Content =
                    currentTab;

                SetActiveNavButton(
                    AiNavButton);
            }
            finally
            {
                _userInteraction = false;
                _suppressChangeTracking = false;
            }
        }


        // =========================================================
        // VOICE
        // =========================================================

        private void ShowVoiceTab()
        {
            try
            {
                _suppressChangeTracking = true;
                _userInteraction = false;

                if (_voiceTab == null)
                {
                    _voiceTab =
                        new VoiceTab(_settings);
                }

                currentTab =
                    _voiceTab;

                SettingsContent.Content =
                    currentTab;

                SetActiveNavButton(
                    VoiceNavButton);
            }
            finally
            {
                _userInteraction = false;
                _suppressChangeTracking = false;
            }
        }


        // =========================================================
        // HOTKEYS READONLY
        // =========================================================

        private void ShowHotkeysTab()
        {
            try
            {
                _suppressChangeTracking = true;
                _userInteraction = false;

                if (_hotKeysReadonly == null)
                {
                    _hotKeysReadonly =
                        new HotKeysReadonly();
                }

                currentTab =
                    _hotKeysReadonly;

                SettingsContent.Content =
                    currentTab;

                SetActiveNavButton(
                    HotkeysNavButton);
            }
            finally
            {
                _userInteraction = false;
                _suppressChangeTracking = false;
            }
        }


        // =========================================================
        // APPEARANCE
        // =========================================================

        private void ShowAppearanceTab()
        {
            try
            {
                _suppressChangeTracking = true;
                _userInteraction = false;

                if (_appearanceTab == null)
                {
                    _appearanceTab =
                        new AppearanceTab(_settings);
                }

                currentTab =
                    _appearanceTab;

                SettingsContent.Content =
                    currentTab;

                SetActiveNavButton(
                    AppearanceNavButton);
            }
            finally
            {
                _userInteraction = false;
                _suppressChangeTracking = false;
            }
        }


        // =========================================================
        // ACTIVE NAVIGATION BUTTON
        // =========================================================

        private void SetActiveNavButton(
            Button activeButton)
        {
            Button[] buttons =
            {
                ResumeNavButton,
                GeneralNavButton,
                AiNavButton,
                VoiceNavButton,
                HotkeysNavButton,
                AppearanceNavButton
            };

            foreach (Button button in buttons)
            {
                if (button == null)
                    continue;

                button.Background =
                    NormalTabBackground;

                button.Foreground =
                    NormalTabForeground;
            }

            if (activeButton != null)
            {
                activeButton.Background =
                    ActiveTabBackground;

                activeButton.Foreground =
                    ActiveTabForeground;
            }
        }


        // =========================================================
        // NAVIGATION EVENTS
        // =========================================================

        private void ResumeNavButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowResumeTab();
        }


        private void GeneralNavButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowGeneralTab();
        }


        private void AiNavButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowAITab();
        }


        private void VoiceNavButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowVoiceTab();
        }


        private void HotkeysNavButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowHotkeysTab();
        }


        private void AppearanceNavButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowAppearanceTab();
        }


        // =========================================================
        // CLOSE
        // =========================================================

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                CloseRequested?.Invoke(
                    this,
                    EventArgs.Empty);
            }
            catch
            {
            }

            Close();
        }


        // =========================================================
        // SAVE
        // =========================================================

        private void SaveButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isSaving)
                return;

            try
            {
                _isSaving = true;
                _suppressChangeTracking = true;

                SaveCurrentTab();

                SettingsService.Save(_settings);

                _hasUnsavedChanges = false;

                SaveRequested?.Invoke(
                    this,
                    EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to save settings.\n\n" +
                    ex.Message,
                    "Settings",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
            finally
            {
                _suppressChangeTracking = false;
                _userInteraction = false;
                _isSaving = false;
            }
        }


        // =========================================================
        // SAVE CURRENT TAB
        // =========================================================

        private void SaveCurrentTab()
        {
            if (currentTab == null)
                return;

            if (currentTab == _resumeTab)
            {
                _resumeTab.SaveSettings();
                return;
            }

            if (currentTab == _generalTab)
            {
                _generalTab.SaveSettings();
                return;
            }

            if (currentTab == _aiTab)
            {
                _aiTab.SaveSettings();
                return;
            }

            if (currentTab == _voiceTab)
            {
                _voiceTab.SaveSettings();
                return;
            }

            if (currentTab == _appearanceTab)
            {
                _appearanceTab.SaveSettings();
                return;
            }
        }


        // =========================================================
        // WINDOW CLOSE
        // =========================================================

        protected override void OnClosed(
            EventArgs e)
        {
            try
            {
                AppearanceManager.UnregisterWindow(this);
            }
            catch
            {
            }

            base.OnClosed(e);
        }
    }
}