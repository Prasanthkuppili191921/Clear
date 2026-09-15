using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace AiInterviewAssistant.Reports
{
    public partial class InterviewReportsWindow : Window
    {
        private readonly string _interviewSessionsFolder;

        private readonly List<InterviewReportItem> _reports =
            new List<InterviewReportItem>();

        private WebBrowser _browser;

        private string _wpfScrollbarCss;


        // =========================================================
        // CUSTOM SELECTION
        // =========================================================

        // Name text color
        private static readonly Brush SelectedNameBrush =
            new SolidColorBrush(
                Color.FromRgb(
                    255,
                    209,
                    102));

        // Name selection background
        private static readonly Brush SelectedNameBackground =
            new SolidColorBrush(
                Color.FromRgb(
                    55,
                    75,
                    95));

        private static readonly Brush NormalDateBrush =
            Brushes.White;

        private static readonly Brush NormalFileBrush =
            Brushes.LightGray;


        // Currently selected name
        private Border _selectedNameBorder;

        private TextBlock _selectedNameText;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public InterviewReportsWindow()
        {
            InitializeComponent();

            _interviewSessionsFolder =
                FindInterviewSessionsFolder();

            LoadWpfScrollbarCss();

            Loaded +=
                InterviewReportsWindow_Loaded;
        }


        // =========================================================
        // LOAD WPF SCROLLBAR CSS
        // =========================================================

        private void LoadWpfScrollbarCss()
        {
            try
            {
                string baseFolder =
                    Path.GetDirectoryName(
                        System.Reflection.Assembly
                            .GetExecutingAssembly()
                            .Location);

                string cssPath =
                    Path.Combine(
                        baseFolder,
                        "InterviewTemplates",
                        "InterviewSessionWpf.css");

                if (File.Exists(cssPath))
                {
                    _wpfScrollbarCss =
                        File.ReadAllText(cssPath);
                }
                else
                {
                    _wpfScrollbarCss =
                        string.Empty;

                    System.Diagnostics.Debug.WriteLine(
                        $"WPF CSS not found: {cssPath}");
                }
            }
            catch (Exception ex)
            {
                _wpfScrollbarCss =
                    string.Empty;

                System.Diagnostics.Debug.WriteLine(
                    $"WPF CSS load error: {ex}");
            }
        }


        // =========================================================
        // LOADED
        // =========================================================

        private void InterviewReportsWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            LoadReports();
        }


        // =========================================================
        // LOAD REPORTS
        // =========================================================

        private void LoadReports()
        {
            try
            {
                ReportsTreeView.Items.Clear();

                _reports.Clear();

                ClearCustomSelection();

                ShowEmptyState();


                if (string.IsNullOrWhiteSpace(
                    _interviewSessionsFolder))
                {
                    return;
                }


                if (!Directory.Exists(
                    _interviewSessionsFolder))
                {
                    return;
                }


                // =====================================================
                // ONLY dd-MM-yyyy FOLDERS
                // =====================================================

                var dateFolders =
                    Directory.GetDirectories(
                        _interviewSessionsFolder,
                        "*",
                        SearchOption.TopDirectoryOnly)
                    .Select(path =>
                        new DirectoryInfo(path))
                    .Select(folder =>
                    {
                        DateTime parsedDate;

                        bool valid =
                            DateTime.TryParseExact(
                                folder.Name,
                                "dd-MM-yyyy",
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.None,
                                out parsedDate);

                        return new
                        {
                            Folder = folder,
                            Date = parsedDate,
                            IsValid = valid
                        };
                    })
                    .Where(x => x.IsValid)
                    .OrderByDescending(x => x.Date)
                    .ToList();


                // =====================================================
                // CREATE TREE
                // =====================================================

                foreach (var dateFolderInfo
                         in dateFolders)
                {
                    DirectoryInfo dateFolder =
                        dateFolderInfo.Folder;


                    // =================================================
                    // ONLY DIRECT HTML FILES
                    // =================================================

                    List<FileInfo> htmlFiles =
                        dateFolder
                            .GetFiles(
                                "*",
                                SearchOption.TopDirectoryOnly)
                            .Where(file =>
                                string.Equals(
                                    file.Extension,
                                    ".html",
                                    StringComparison.OrdinalIgnoreCase))
                            .OrderByDescending(
                                file => file.LastWriteTime)
                            .ToList();


                    // Ignore empty folders

                    if (htmlFiles.Count == 0)
                    {
                        continue;
                    }


                    // =================================================
                    // DATE FOLDER
                    // =================================================

                    TreeViewItem dateItem =
                        CreateDateTreeItem(
                            dateFolder.Name);

                    dateItem.IsExpanded = true;


                    // =================================================
                    // REPORT FILES
                    // =================================================

                    for (int i = 0;
                         i < htmlFiles.Count;
                         i++)
                    {
                        FileInfo report =
                            htmlFiles[i];


                        InterviewReportItem reportItem =
                            new InterviewReportItem
                            {
                                FilePath =
                                    report.FullName,

                                FileName =
                                    report.Name,

                                DateFolder =
                                    dateFolder.Name,

                                IsLast =
                                    i == htmlFiles.Count - 1
                            };


                        TreeViewItem reportTreeItem =
                            CreateReportTreeItem(
                                reportItem);


                        dateItem.Items.Add(
                            reportTreeItem);


                        _reports.Add(
                            reportItem);
                    }


                    ReportsTreeView.Items.Add(
                        dateItem);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Load reports error: {ex}");
            }
        }


        // =========================================================
        // DATE TREE ITEM
        // =========================================================

        private TreeViewItem CreateDateTreeItem(
            string date)
        {
            TreeViewItem item =
                new TreeViewItem
                {
                    Header =
                        CreateDateHeader(date),

                    IsExpanded =
                        true,

                    Cursor =
                        Cursors.Arrow
                };


            // Prevent native TreeViewItem selection
            // except actual date name.

            item.PreviewMouseLeftButtonDown +=
                TreeItem_PreviewMouseLeftButtonDown;


            return item;
        }


        // =========================================================
        // DATE HEADER
        // =========================================================

        private StackPanel CreateDateHeader(
            string date)
        {
            StackPanel panel =
                new StackPanel
                {
                    Orientation =
                        Orientation.Horizontal,

                    HorizontalAlignment =
                        HorizontalAlignment.Left
                };


            // =====================================================
            // LINE
            // =====================================================

            TextBlock line =
                new TextBlock
                {
                    Text =
                        "├── ",

                    Foreground =
                        Brushes.Gray,

                    IsHitTestVisible =
                        true,

                    Cursor =
                        Cursors.Arrow
                };


            line.MouseLeftButtonDown +=
                IgnoreTreeSelectionClick;


            panel.Children.Add(line);


            // =====================================================
            // FOLDER ICON
            // =====================================================

            TextBlock folderIcon =
                new TextBlock
                {
                    Text =
                        "📁",

                    FontSize =
                        13,

                    Margin =
                        new Thickness(
                            0,
                            0,
                            6,
                            0),

                    IsHitTestVisible =
                        true,

                    Cursor =
                        Cursors.Arrow
                };


            folderIcon.MouseLeftButtonDown +=
                IgnoreTreeSelectionClick;


            panel.Children.Add(folderIcon);


            // =====================================================
            // DATE NAME
            // =====================================================

            TextBlock dateText =
                new TextBlock
                {
                    Text =
                        date,

                    Foreground =
                        NormalDateBrush,

                    FontWeight =
                        FontWeights.SemiBold,

                    Tag =
                        "DateName",

                    Cursor =
                        Cursors.Arrow
                };


            // =====================================================
            // ONLY DATE NAME AREA
            // =====================================================

            Border dateBorder =
                new Border
                {
                    Background =
                        Brushes.Transparent,

                    Padding =
                        new Thickness(
                            2,
                            1,
                            2,
                            1),

                    CornerRadius =
                        new CornerRadius(2),

                    HorizontalAlignment =
                        HorizontalAlignment.Left,

                    Tag =
                        "DateNameBorder",

                    Cursor =
                        Cursors.Arrow
                };


            dateBorder.Child =
                dateText;


            dateBorder.MouseLeftButtonDown +=
                DateName_MouseLeftButtonDown;


            panel.Children.Add(
                dateBorder);


            return panel;
        }


        // =========================================================
        // REPORT TREE ITEM
        // =========================================================

        private TreeViewItem CreateReportTreeItem(
            InterviewReportItem report)
        {
            TreeViewItem item =
                new TreeViewItem
                {
                    Foreground =
                        NormalFileBrush,

                    Header =
                        CreateReportHeader(report),

                    Tag =
                        report,

                    Cursor =
                        Cursors.Arrow
                };


            // Prevent native selection.
            // File selection is handled only by filename Border.

            item.PreviewMouseLeftButtonDown +=
                TreeItem_PreviewMouseLeftButtonDown;


            return item;
        }


        // =========================================================
        // REPORT HEADER
        // =========================================================

        private StackPanel CreateReportHeader(
            InterviewReportItem report)
        {
            StackPanel panel =
                new StackPanel
                {
                    Orientation =
                        Orientation.Horizontal,

                    HorizontalAlignment =
                        HorizontalAlignment.Left
                };


            // =====================================================
            // LINE
            // =====================================================

            TextBlock line =
                new TextBlock
                {
                    Text =
                        report.IsLast
                            ? "└── "
                            : "├── ",

                    Foreground =
                        Brushes.Gray,

                    IsHitTestVisible =
                        true,

                    Cursor =
                        Cursors.Arrow
                };


            line.MouseLeftButtonDown +=
                IgnoreTreeSelectionClick;


            panel.Children.Add(line);


            // =====================================================
            // FILE ICON
            // =====================================================

            TextBlock fileIcon =
                new TextBlock
                {
                    Text =
                        "📄",

                    FontSize =
                        13,

                    Margin =
                        new Thickness(
                            0,
                            0,
                            6,
                            0),

                    IsHitTestVisible =
                        true,

                    Cursor =
                        Cursors.Arrow
                };


            fileIcon.MouseLeftButtonDown +=
                IgnoreTreeSelectionClick;


            panel.Children.Add(fileIcon);


            // =====================================================
            // FILE NAME
            // =====================================================

            string displayName =
                Path.GetFileNameWithoutExtension(
                    report.FileName);


            TextBlock fileText =
                new TextBlock
                {
                    Text =
                        displayName,

                    Foreground =
                        NormalFileBrush,

                    Tag =
                        "ReportName",

                    Cursor =
                        Cursors.Arrow
                };


            // =====================================================
            // ONLY FILE NAME AREA
            // =====================================================

            Border fileNameBorder =
                new Border
                {
                    Background =
                        Brushes.Transparent,

                    Padding =
                        new Thickness(
                            2,
                            1,
                            2,
                            1),

                    CornerRadius =
                        new CornerRadius(2),

                    HorizontalAlignment =
                        HorizontalAlignment.Left,

                    Tag =
                        "ReportNameBorder",

                    Cursor =
                        Cursors.Arrow
                };


            fileNameBorder.Child =
                fileText;


            fileNameBorder.MouseLeftButtonDown +=
                FileName_MouseLeftButtonDown;


            panel.Children.Add(
                fileNameBorder);


            return panel;
        }


        // =========================================================
        // TREE ITEM PREVIEW CLICK
        // =========================================================

        private void TreeItem_PreviewMouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            DependencyObject source =
                e.OriginalSource as DependencyObject;


            if (source == null)
            {
                e.Handled = true;
                return;
            }


            // =====================================================
            // NAME BORDER
            // =====================================================

            Border border =
                FindParent<Border>(source);


            if (border != null &&
                (string.Equals(
                    border.Tag as string,
                    "DateNameBorder",
                    StringComparison.Ordinal) ||

                 string.Equals(
                    border.Tag as string,
                    "ReportNameBorder",
                    StringComparison.Ordinal)))
            {
                return;
            }


            // =====================================================
            // EXPANDER
            // =====================================================

            ToggleButton toggle =
                FindParent<ToggleButton>(source);


            if (toggle != null)
            {
                return;
            }


            // =====================================================
            // LINE / ICON / BLANK AREA
            // DO NOT SELECT
            // =====================================================

            e.Handled = true;
        }


        // =========================================================
        // IGNORE LINE / ICON CLICK
        // =========================================================

        private void IgnoreTreeSelectionClick(
            object sender,
            MouseButtonEventArgs e)
        {
            e.Handled = true;
        }


        // =========================================================
        // DATE NAME CLICK
        // =========================================================

        private void DateName_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            e.Handled = true;


            Border border =
                sender as Border;


            if (border == null)
            {
                return;
            }


            TextBlock text =
                border.Child as TextBlock;


            if (text == null)
            {
                return;
            }


            // Clear previous selection

            ClearCustomSelection();


            // Select ONLY date name

            border.Background =
                SelectedNameBackground;

            text.Foreground =
                SelectedNameBrush;


            _selectedNameBorder =
                border;

            _selectedNameText =
                text;


            // Folder selected:
            // Do NOT show any report.

            ShowEmptyState();
        }


        // =========================================================
        // FILE NAME CLICK
        // =========================================================

        private void FileName_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            e.Handled = true;


            Border border =
                sender as Border;


            if (border == null)
            {
                return;
            }


            TextBlock text =
                border.Child as TextBlock;


            if (text == null)
            {
                return;
            }


            // =====================================================
            // FIND TREE VIEW ITEM
            // =====================================================

            TreeViewItem item =
                FindParent<TreeViewItem>(
                    border);


            if (item == null)
            {
                return;
            }


            InterviewReportItem report =
                item.Tag as InterviewReportItem;


            if (report == null)
            {
                return;
            }


            // Clear previous selection

            ClearCustomSelection();


            // Select ONLY filename

            border.Background =
                SelectedNameBackground;

            text.Foreground =
                SelectedNameBrush;


            _selectedNameBorder =
                border;

            _selectedNameText =
                text;


            // Open report

            OpenReport(report);
        }


        // =========================================================
        // CLEAR CUSTOM SELECTION
        // =========================================================

        private void ClearCustomSelection()
        {
            if (_selectedNameBorder != null)
            {
                _selectedNameBorder.Background =
                    Brushes.Transparent;
            }


            if (_selectedNameText != null)
            {
                string tag =
                    _selectedNameText.Tag as string;


                if (string.Equals(
                    tag,
                    "DateName",
                    StringComparison.Ordinal))
                {
                    _selectedNameText.Foreground =
                        NormalDateBrush;
                }
                else
                {
                    _selectedNameText.Foreground =
                        NormalFileBrush;
                }
            }


            _selectedNameBorder =
                null;

            _selectedNameText =
                null;
        }


        // =========================================================
        // FIND PARENT
        // =========================================================

        private T FindParent<T>(
            DependencyObject child)
            where T : DependencyObject
        {
            DependencyObject current =
                child;


            while (current != null)
            {
                T parent =
                    current as T;


                if (parent != null)
                {
                    return parent;
                }


                current =
                    VisualTreeHelper.GetParent(
                        current);
            }


            return null;
        }


        // =========================================================
        // OPEN REPORT
        // =========================================================

        private void OpenReport(
            InterviewReportItem report)
        {
            try
            {
                if (report == null)
                {
                    ShowEmptyState();
                    return;
                }


                if (string.IsNullOrWhiteSpace(
                    report.FilePath))
                {
                    ShowEmptyState();
                    return;
                }


                if (!File.Exists(
                    report.FilePath))
                {
                    ShowEmptyState();
                    return;
                }


                SelectedReportText.Text =
                    Path.GetFileNameWithoutExtension(
                        report.FileName);


                EmptyStatePanel.Visibility =
                    Visibility.Collapsed;


                WebBrowser browser =
                    GetOrCreateBrowser();


                browser.Visibility =
                    Visibility.Visible;


                browser.Navigate(
                    new Uri(
                        report.FilePath,
                        UriKind.Absolute));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Open report error: {ex}");

                ShowEmptyState();
            }
        }


        // =========================================================
        // GET / CREATE WEB BROWSER
        // =========================================================

        private WebBrowser GetOrCreateBrowser()
        {
            if (_browser != null)
            {
                return _browser;
            }


            _browser =
                new WebBrowser
                {
                    Margin =
                        new Thickness(
                            0,
                            42,
                            0,
                            0),

                    HorizontalAlignment =
                        HorizontalAlignment.Stretch,

                    VerticalAlignment =
                        VerticalAlignment.Stretch
                };


            _browser.LoadCompleted +=
                Browser_LoadCompleted;


            ReportContentHost.Children.Add(
                _browser);


            return _browser;
        }


        // =========================================================
        // BROWSER LOAD COMPLETED
        // =========================================================

        private void Browser_LoadCompleted(
            object sender,
            System.Windows.Navigation.NavigationEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(
                    _wpfScrollbarCss))
                {
                    return;
                }


                if (_browser == null)
                {
                    return;
                }


                dynamic document =
                    _browser.Document;


                if (document == null)
                {
                    return;
                }


                dynamic head =
                    document.getElementsByTagName(
                        "head")[0];


                if (head == null)
                {
                    return;
                }


                dynamic style =
                    document.createElement(
                        "style");


                style.type =
                    "text/css";

                style.innerHTML =
                    _wpfScrollbarCss;


                head.appendChild(
                    style);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "WPF SCROLLBAR CSS ERROR: " +
                    ex);
            }
        }


        // =========================================================
        // EMPTY STATE
        // =========================================================

        private void ShowEmptyState()
        {
            SelectedReportText.Text =
                string.Empty;


            EmptyStatePanel.Visibility =
                Visibility.Visible;


            if (_browser != null)
            {
                _browser.Visibility =
                    Visibility.Collapsed;
            }
        }


        // =========================================================
        // FIND INTERVIEW SESSIONS FOLDER
        // =========================================================

        private string FindInterviewSessionsFolder()
        {
            try
            {
                string current =
                    AppDomain.CurrentDomain.BaseDirectory;


                DirectoryInfo directory =
                    new DirectoryInfo(
                        current);


                while (directory != null)
                {
                    // -------------------------------------------------
                    // Current\AiInterviewAssistant\InterviewSessions
                    // -------------------------------------------------

                    string path1 =
                        Path.Combine(
                            directory.FullName,
                            "AiInterviewAssistant",
                            "InterviewSessions");


                    if (Directory.Exists(path1))
                    {
                        return path1;
                    }


                    // -------------------------------------------------
                    // Current\InterviewSessions
                    // -------------------------------------------------

                    string path2 =
                        Path.Combine(
                            directory.FullName,
                            "InterviewSessions");


                    if (Directory.Exists(path2))
                    {
                        return path2;
                    }


                    directory =
                        directory.Parent;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Find InterviewSessions error: {ex}");
            }


            return null;
        }


        // =========================================================
        // REFRESH
        // =========================================================

        private void RefreshButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            LoadReports();
        }


        // =========================================================
        // CLOSE
        // =========================================================

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }


        // =========================================================
        // HEADER DRAG
        // =========================================================

        private void Header_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            try
            {
                if (e.ButtonState ==
                    MouseButtonState.Pressed)
                {
                    DragMove();
                }
            }
            catch
            {
                // Ignore drag exceptions
            }
        }


        // =========================================================
        // REPORT MODEL
        // =========================================================

        private class InterviewReportItem
        {
            public string FilePath { get; set; }

            public string FileName { get; set; }

            public string DateFolder { get; set; }

            public bool IsLast { get; set; }
        }
    }
}