using System;
using System.IO;
using System.Text;

namespace AiInterviewAssistant
{
    /// <summary>
    /// Shared resume-file extraction used by both Main Window and Resume settings tab.
    /// Supports the same formats as the existing Resume tab: PDF, DOCX, and TXT.
    /// </summary>
    public static class ResumeFileService
    {
        public static string ExtractText(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("A resume file path is required.", nameof(filePath));

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
                    throw new NotSupportedException("Unsupported resume file format.");
            }
        }

        private static string ExtractPdfText(string filePath)
        {
            StringBuilder builder = new StringBuilder();

            using (UglyToad.PdfPig.PdfDocument document =
                UglyToad.PdfPig.PdfDocument.Open(filePath))
            {
                foreach (UglyToad.PdfPig.Content.Page page in document.GetPages())
                {
                    string text = page.Text;
                    if (!string.IsNullOrWhiteSpace(text))
                        builder.AppendLine(text);
                }
            }

            return builder.ToString();
        }

        private static string ExtractDocxText(string filePath)
        {
            StringBuilder builder = new StringBuilder();

            using (System.IO.Packaging.Package package =
                System.IO.Packaging.Package.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                Uri documentUri = new Uri("/word/document.xml", UriKind.Relative);
                if (!package.PartExists(documentUri))
                    return string.Empty;

                System.IO.Packaging.PackagePart part = package.GetPart(documentUri);

                using (Stream stream = part.GetStream())
                {
                    System.Xml.XmlDocument xml = new System.Xml.XmlDocument();
                    xml.Load(stream);

                    System.Xml.XmlNamespaceManager ns =
                        new System.Xml.XmlNamespaceManager(xml.NameTable);
                    ns.AddNamespace(
                        "w",
                        "http://schemas.openxmlformats.org/wordprocessingml/2006/main");

                    System.Xml.XmlNodeList paragraphs = xml.SelectNodes("//w:p", ns);
                    if (paragraphs == null)
                        return string.Empty;

                    foreach (System.Xml.XmlNode paragraph in paragraphs)
                    {
                        StringBuilder line = new StringBuilder();
                        System.Xml.XmlNodeList texts = paragraph.SelectNodes(".//w:t", ns);

                        if (texts != null)
                        {
                            foreach (System.Xml.XmlNode textNode in texts)
                                line.Append(textNode.InnerText);
                        }

                        string value = line.ToString();
                        if (!string.IsNullOrWhiteSpace(value))
                            builder.AppendLine(value);
                    }
                }
            }

            return builder.ToString();
        }
    }
}
