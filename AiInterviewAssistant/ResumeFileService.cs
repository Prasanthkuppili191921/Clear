using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UglyToad.PdfPig;

namespace AiInterviewAssistant
{
    /// <summary>
    /// Shared resume text extraction for PDF, DOCX, and TXT files.
    /// </summary>
    public static class ResumeFileService
    {
        public static string ExtractText(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Resume file path is required.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("The selected resume file could not be found.", filePath);

            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            switch (extension)
            {
                case ".txt":
                    return File.ReadAllText(filePath, Encoding.UTF8);

                case ".pdf":
                    return ExtractPdfText(filePath);

                case ".docx":
                    return ExtractDocxText(filePath);

                default:
                    throw new NotSupportedException(
                        "Unsupported resume format. Please select a PDF, DOCX, or TXT file.");
            }
        }

        private static string ExtractPdfText(string filePath)
        {
            var text = new StringBuilder();

            using (var document = PdfDocument.Open(filePath))
            {
                foreach (var page in document.GetPages())
                {
                    if (!string.IsNullOrWhiteSpace(page.Text))
                    {
                        if (text.Length > 0)
                            text.AppendLine();

                        text.Append(page.Text);
                    }
                }
            }

            return text.ToString();
        }

        private static string ExtractDocxText(string filePath)
        {
            using (Package package = Package.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                Uri documentUri = new Uri("/word/document.xml", UriKind.Relative);

                if (!package.PartExists(documentUri))
                    throw new InvalidDataException("The selected DOCX file does not contain a Word document part.");

                PackagePart documentPart = package.GetPart(documentUri);

                using (Stream stream = documentPart.GetStream(FileMode.Open, FileAccess.Read))
                {
                    XDocument document = XDocument.Load(stream);

                    XNamespace wordNamespace =
                        "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

                    var paragraphs = document
                        .Descendants(wordNamespace + "p")
                        .Select(paragraph => string.Concat(
                            paragraph.Descendants(wordNamespace + "t")
                                     .Select(node => (string)node)))
                        .Where(paragraph => !string.IsNullOrWhiteSpace(paragraph));

                    return string.Join(Environment.NewLine, paragraphs);
                }
            }
        }
    }
}
