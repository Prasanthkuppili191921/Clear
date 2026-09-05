using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace AiInterviewAssistant.Settings.Resume
{
    public partial class ResumeTab : UserControl
    {
        private readonly AppSettings _settings;

        private bool _isLoading;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public ResumeTab(AppSettings settings)
        {
            InitializeComponent();

            _settings = settings;

            Loaded += ResumeTab_Loaded;
        }


        // =========================================================
        // LOADED
        // =========================================================

        private void ResumeTab_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            LoadResume();
        }


        // =========================================================
        // LOAD RESUME
        // =========================================================

        private void LoadResume()
        {
            try
            {
                _isLoading = true;

                string resume =
                    GetResumeFromSettings();

                ResumeTextBox.Text =
                    resume ?? string.Empty;

                UpdateResumeUI();
            }
            catch
            {
                ResumeTextBox.Text =
                    string.Empty;

                UpdateResumeUI();
            }
            finally
            {
                _isLoading = false;
            }
        }


        // =========================================================
        // GET RESUME
        // =========================================================

        private string GetResumeFromSettings()
        {
            if (_settings == null)
                return string.Empty;

            return _settings.ResumeText ?? string.Empty;
        }


        // =========================================================
        // SAVE SETTINGS
        // =========================================================

        public void SaveSettings()
        {
            if (_settings == null)
                return;

            _settings.ResumeText =
                ResumeTextBox.Text ?? string.Empty;
        }


        // =========================================================
        // BROWSE FILE
        // =========================================================

        private void BrowseResumeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog dialog =
                    new OpenFileDialog
                    {
                        Title = "Select Resume",

                        Filter =
                            "Resume Files (*.pdf;*.docx;*.txt)|*.pdf;*.docx;*.txt|" +
                            "PDF Files (*.pdf)|*.pdf|" +
                            "Word Documents (*.docx)|*.docx|" +
                            "Text Files (*.txt)|*.txt|" +
                            "All Files (*.*)|*.*",

                        Multiselect = false
                    };


                bool? result =
                    dialog.ShowDialog();


                if (result != true)
                    return;


                string filePath =
                    dialog.FileName;


                string extension =
                    Path.GetExtension(filePath)
                    .ToLowerInvariant();


                string text =
                    ExtractResumeText(
                        filePath,
                        extension);


                if (string.IsNullOrWhiteSpace(text))
                {
                    MessageBox.Show(
                        "Could not extract any text from the selected resume.",
                        "Resume",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }


                _isLoading = true;

                ResumeTextBox.Text =
                    text.Trim();

                _isLoading = false;


                ResumeFileNameText.Text =
                    Path.GetFileName(filePath);

                ResumeStatusText.Text =
                    "Resume imported";


                UpdateResumeUI();
            }
            catch (Exception ex)
            {
                _isLoading = false;

                MessageBox.Show(
                    "Unable to read the resume.\n\n" +
                    ex.Message,
                    "Resume",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        // =========================================================
        // EXTRACT TEXT
        // =========================================================

        private string ExtractResumeText(
            string filePath,
            string extension)
        {
            switch (extension)
            {
                case ".txt":

                    return File.ReadAllText(
                        filePath,
                        Encoding.UTF8);


                case ".pdf":

                    return ExtractPdfText(
                        filePath);


                case ".docx":

                    return ExtractDocxText(
                        filePath);


                default:

                    throw new NotSupportedException(
                        "Unsupported resume file format.");
            }
        }


        // =========================================================
        // PDF
        // =========================================================

        private string ExtractPdfText(
            string filePath)
        {
            StringBuilder builder =
                new StringBuilder();


            using (
                UglyToad.PdfPig.PdfDocument document =
                    UglyToad.PdfPig.PdfDocument.Open(
                        filePath))
            {
                foreach (
                    UglyToad.PdfPig.Content.Page page
                    in document.GetPages())
                {
                    string text =
                        page.Text;

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        builder.AppendLine(text);
                    }
                }
            }


            return builder.ToString();
        }


        // =========================================================
        // DOCX
        // =========================================================

        private string ExtractDocxText(
            string filePath)
        {
            StringBuilder builder =
                new StringBuilder();


            using (
                System.IO.Packaging.Package package =
                    System.IO.Packaging.Package.Open(
                        filePath,
                        FileMode.Open,
                        FileAccess.Read))
            {
                Uri documentUri =
                    new Uri(
                        "/word/document.xml",
                        UriKind.Relative);


                if (!package.PartExists(documentUri))
                    return string.Empty;


                System.IO.Packaging.PackagePart part =
                    package.GetPart(documentUri);


                using (
                    Stream stream =
                        part.GetStream())
                {
                    System.Xml.XmlDocument xml =
                        new System.Xml.XmlDocument();

                    xml.Load(stream);


                    System.Xml.XmlNamespaceManager ns =
                        new System.Xml.XmlNamespaceManager(
                            xml.NameTable);


                    ns.AddNamespace(
                        "w",
                        "http://schemas.openxmlformats.org/wordprocessingml/2006/main");


                    System.Xml.XmlNodeList paragraphs =
                        xml.SelectNodes(
                            "//w:p",
                            ns);


                    if (paragraphs == null)
                        return string.Empty;


                    foreach (
                        System.Xml.XmlNode paragraph
                        in paragraphs)
                    {
                        StringBuilder line =
                            new StringBuilder();


                        System.Xml.XmlNodeList texts =
                            paragraph.SelectNodes(
                                ".//w:t",
                                ns);


                        if (texts != null)
                        {
                            foreach (
                                System.Xml.XmlNode textNode
                                in texts)
                            {
                                line.Append(
                                    textNode.InnerText);
                            }
                        }


                        string value =
                            line.ToString();


                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            builder.AppendLine(
                                value);
                        }
                    }
                }
            }


            return builder.ToString();
        }


        // =========================================================
        // CLEAR
        // =========================================================

        private void ClearResumeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _isLoading = true;

            ResumeTextBox.Clear();

            _isLoading = false;


            ResumeFileNameText.Text =
                string.Empty;


            ResumeStatusText.Text =
                "No resume added";


            UpdateResumeUI();
        }


        // =========================================================
        // TEXT CHANGED
        // =========================================================

        private void ResumeTextBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (_isLoading)
                return;

            UpdateResumeUI();
        }


        // =========================================================
        // UPDATE UI
        // =========================================================

        private void UpdateResumeUI()
        {
            string text =
                ResumeTextBox?.Text
                ?? string.Empty;


            bool hasResume =
                !string.IsNullOrWhiteSpace(text);


            // -----------------------------------------------------
            // EMPTY STATE
            // -----------------------------------------------------

            if (ResumeEmptyState != null)
            {
                ResumeEmptyState.Visibility =
                    hasResume
                        ? Visibility.Collapsed
                        : Visibility.Visible;
            }


            // -----------------------------------------------------
            // CHARACTER COUNT
            // -----------------------------------------------------

            if (ResumeCharacterCountText != null)
            {
                ResumeCharacterCountText.Text =
                    text.Length.ToString("N0") +
                    " characters";
            }


            // -----------------------------------------------------
            // STATUS
            // -----------------------------------------------------

            if (ResumeStatusText != null &&
                string.IsNullOrWhiteSpace(
                    ResumeFileNameText?.Text))
            {
                ResumeStatusText.Text =
                    hasResume
                        ? "Resume added"
                        : "No resume added";
            }
        }
    }
}