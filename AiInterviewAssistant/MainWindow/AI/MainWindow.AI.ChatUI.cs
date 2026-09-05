using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace AiInterviewAssistant
{
    public partial class MainWindow
    {
        // =========================================================
        // CURRENT USER QUESTION
        // =========================================================

        private Border _currentUserQuestionBubble;

        // =========================================================
        // UI FONT SETTINGS
        // =========================================================

        private static readonly FontFamily AnswerFont =
            new FontFamily("Tahoma");

        private static readonly FontFamily CodeFont =
            new FontFamily("Cascadia Code");

        private const double AnswerFontSize = 14.0;
        private const double CodeFontSize = 13.5;


        // =========================================================
        // COLORS
        // =========================================================

        private static readonly Color UserBubbleColor =
            Color.FromRgb(52, 53, 65);

        private static readonly Color AIBubbleColor =
            Color.FromRgb(44, 45, 55);

        private static readonly Color CodeBackgroundColor =
            Color.FromRgb(32, 33, 36);

        private static readonly Color CodeBorderColor =
            Color.FromRgb(110, 112, 122);

        private static readonly Color NormalTextColor =
            Color.FromRgb(236, 236, 236);

        private static readonly Color CodeTextColor =
            Color.FromRgb(225, 225, 230);

        private static readonly Color LanguageLabelColor =
            Color.FromRgb(155, 157, 165);


        // =========================================================
        // SYNTAX COLORS
        // =========================================================

        private static readonly Color SyntaxKeywordColor =
            Color.FromRgb(255, 121, 198);

        private static readonly Color SyntaxStringColor =
            Color.FromRgb(166, 227, 161);

        private static readonly Color SyntaxCommentColor =
            Color.FromRgb(120, 130, 140);

        private static readonly Color SyntaxNumberColor =
            Color.FromRgb(189, 147, 249);

        private static readonly Color SyntaxTypeColor =
            Color.FromRgb(139, 233, 253);

        private static readonly Color SyntaxFunctionColor =
            Color.FromRgb(80, 250, 123);

        private static readonly Color SyntaxOperatorColor =
            Color.FromRgb(255, 184, 108);

        private static readonly Color SyntaxPropertyColor =
            Color.FromRgb(139, 233, 253);


        // =========================================================
        // CHAT SCROLL ANCHOR
        // =========================================================

        private Border _chatBottomSpacer;


        // =========================================================
        // ADD MESSAGE
        // =========================================================

        private Border AddMessage(
            string message,
            HorizontalAlignment alignment,
            Color backgroundColor,
            Thickness margin)
        {
            Border bubble =
                new Border
                {
                    Background =
                        new SolidColorBrush(
                            backgroundColor),

                    CornerRadius =
                        new CornerRadius(10),

                    Padding =
                        new Thickness(
                            14,
                            11,
                            14,
                            11),

                    Margin =
                        margin,

                    HorizontalAlignment =
                        alignment,

                    MaxWidth =
                        900
                };


            TextBlock text =
                new TextBlock
                {
                    Text =
                        message ?? "",

                    FontFamily =
                        AnswerFont,

                    FontSize =
                        AnswerFontSize,

                    FontWeight =
                        FontWeights.Normal,

                    TextWrapping =
                        TextWrapping.Wrap,

                    Foreground =
                        new SolidColorBrush(
                            NormalTextColor)
                };


            bubble.Child =
                text;


            EnsureChatBottomSpacer();

            ChatPanel.Children.Insert(
                ChatPanel.Children.Count - 1,
                bubble);

            return bubble;
        }


        // =========================================================
        // USER MESSAGE
        // =========================================================

        private Border AddUserMessage(
            string message)
        {
            Border bubble =
                AddMessage(
                    message,
                    HorizontalAlignment.Right,
                    UserBubbleColor,
                    new Thickness(
                        100,
                        7,
                        8,
                        7));

            // IMPORTANT:
            // This question belongs to the next AI response.
            _currentUserQuestionBubble = bubble;

            ScrollMessageToTop(bubble);

            return bubble;
        }


        // =========================================================
        // UPDATE USER MESSAGE
        // =========================================================

        private void UpdateUserMessage(
                    Border bubble,
                    string message)
        {
            if (bubble == null)
                return;


            if (bubble.Child is TextBlock text)
            {
                text.Text =
                    message ?? "";
            }


            //ChatScrollViewer.ScrollToEnd();
        }


        // =========================================================
        // AI MESSAGE
        // =========================================================

        private Border AddAIMessage(
            string message)
        {
            Border bubble =
                new Border
                {
                    Background =
                        new SolidColorBrush(
                                Color.FromArgb(
                                    55,
                                    255,
                                    255,
                                    255)),

                    CornerRadius =
                        new CornerRadius(10),

                    Padding =
                        new Thickness(
                            14,
                            12,
                            14,
                            12),

                    Margin =
                        new Thickness(
                            0,
                            7,
                            0,
                            7),

                    HorizontalAlignment =
                        HorizontalAlignment.Stretch
                };


            Grid mainGrid =
                new Grid();


            // =====================================================
            // ANSWER CONTENT
            // =====================================================

            ScrollViewer contentScroll =
                new ScrollViewer
                {
                    HorizontalScrollBarVisibility =
                        ScrollBarVisibility.Disabled,

                    VerticalScrollBarVisibility =
                        ScrollBarVisibility.Disabled,

                    CanContentScroll =
                        false
                };


            StackPanel contentPanel =
                new StackPanel
                {
                    Orientation =
                        Orientation.Vertical
                };


            RenderAIContent(
                contentPanel,
                message ?? "");


            contentScroll.Content =
                contentPanel;


            mainGrid.Children.Add(
                contentScroll);


            // =====================================================
            // COPY BUTTON
            // =====================================================

            Button copyButton =
                new Button
                {
                    Width = 26,
                    Height = 26,

                    Padding =
                        new Thickness(0),

                    Background =
                        new SolidColorBrush(
                                Color.FromArgb(
                                    55,
                                    255,
                                    255,
                                    255)),

                    BorderThickness =
                        new Thickness(0),

                    Foreground =
                        new SolidColorBrush(
                            Color.FromRgb(
                                190,
                                190,
                                195)),

                    Cursor =
                        Cursors.Hand,

                    ToolTip =
                        "Copy answer",

                    Visibility =
                        Visibility.Collapsed,

                    HorizontalAlignment =
                        HorizontalAlignment.Right,

                    VerticalAlignment =
                        VerticalAlignment.Top,

                    Margin =
                        new Thickness(
                            0,
                            -5,
                            -5,
                            0),

                    Content =
                        "⧉"
                };


            copyButton.Click +=
                (sender, e) =>
                {
                    try
                    {
                        //Clipboard.SetText(
                        //    message ?? "");

                        Clipboard.SetText(
                            copyButton.Tag?.ToString() ?? "");


                        copyButton.Opacity =
                            0.45;


                        Task.Delay(600)
                            .ContinueWith(_ =>
                            {
                                Dispatcher.BeginInvoke(
                                    new Action(() =>
                                    {
                                        copyButton.Opacity =
                                            1.0;
                                    }));
                            });
                    }
                    catch (Exception ex)
                    {
                        AppMessage.Show(
                            "Copy failed:\n\n" +
                            ex.Message);
                    }
                };


            mainGrid.Children.Add(
                copyButton);


            // =====================================================
            // HOVER
            // =====================================================

            bubble.MouseEnter +=
                (sender, e) =>
                {
                    copyButton.Visibility =
                        Visibility.Visible;
                };


            bubble.MouseLeave +=
                (sender, e) =>
                {
                    copyButton.Visibility =
                        Visibility.Collapsed;
                };


            bubble.Child =
                mainGrid;


            // =========================================================
            // INSERT AI RESPONSE DIRECTLY AFTER ITS QUESTION
            // =========================================================

            if (_currentUserQuestionBubble != null)
            {
                int questionIndex =
                    ChatPanel.Children.IndexOf(
                        _currentUserQuestionBubble);

                if (questionIndex >= 0)
                {
                    ChatPanel.Children.Insert(
                        questionIndex + 1,
                        bubble);
                }
                else
                {
                    ChatPanel.Children.Add(
                        bubble);
                }
            }
            else
            {
                ChatPanel.Children.Add(
                    bubble);
            }


            return bubble;
        }


        // =========================================================
        // RENDER AI CONTENT
        // =========================================================

        private void RenderAIContent(
            StackPanel panel,
            string message)
        {
            if (panel == null)
                return;


            panel.Children.Clear();


            if (string.IsNullOrEmpty(message))
                return;


            // =====================================================
            // SPLIT MARKDOWN CODE BLOCKS
            // =====================================================

            string[] parts =
                message.Split(
                    new[] { "```" },
                    StringSplitOptions.None);


            for (int i = 0;
                 i < parts.Length;
                 i++)
            {
                string part =
                    parts[i] ?? "";


                // =================================================
                // CODE BLOCK
                // =================================================

                if (i % 2 == 1)
                {
                    AddAvalonCodeBlock(
                        panel,
                        part);

                    continue;
                }


                // =================================================
                // NORMAL TEXT
                // =================================================

                AddNormalText(
                    panel,
                    part);
            }


            //ChatScrollViewer.ScrollToEnd();
        }


        // =========================================================
        // NORMAL TEXT
        // =========================================================

        private void AddNormalText(
            StackPanel panel,
            string text)
        {
            if (panel == null)
                return;


            if (string.IsNullOrWhiteSpace(text))
                return;


            TextBlock normalText =
                new TextBlock
                {
                    Text =
                        text.Trim(),

                    FontFamily =
                        AnswerFont,

                    FontSize =
                        AnswerFontSize,

                    FontWeight =
                        FontWeights.Normal,

                    Foreground =
                        new SolidColorBrush(
                            NormalTextColor),

                    TextWrapping =
                        TextWrapping.Wrap,

                    LineHeight =
                        22,

                    Margin =
                        new Thickness(
                            0,
                            2,
                            0,
                            4)
                };


            panel.Children.Add(
                normalText);
        }


        // =========================================================
        // AVALONEDIT CODE BLOCK
        // =========================================================

        private void AddAvalonCodeBlock(
            StackPanel panel,
            string rawCode)
        {
            if (panel == null)
                return;


            if (rawCode == null)
                rawCode = "";


            // =====================================================
            // NORMALIZE
            // =====================================================

            string language = "";

            string code =
                rawCode.TrimStart(
                    '\r',
                    '\n',
                    ' ',
                    '\t');


            // =====================================================
            // DETECT EXPLICIT LANGUAGE
            // =====================================================

            int newlineIndex =
                code.IndexOf('\n');


            if (newlineIndex >= 0)
            {
                string firstLine =
                    code.Substring(
                        0,
                        newlineIndex)
                        .Trim();


                if (IsLanguageIdentifier(
                        firstLine))
                {
                    language =
                        NormalizeLanguage(
                            firstLine);

                    code =
                        code.Substring(
                            newlineIndex + 1);
                }
            }


            // =====================================================
            // FALLBACK LANGUAGE DETECTION
            //
            // This is important when AI returns:
            //
            // ```
            // SELECT ...
            // ```
            //
            // instead of:
            //
            // ```sql
            // SELECT ...
            // ```
            // =====================================================

            if (string.IsNullOrWhiteSpace(language))
            {
                language =
                    DetectLanguageFromCode(
                        code);
            }


            // =====================================================
            // CLEAN CODE
            // =====================================================

            code =
                code.Trim(
                    '\r',
                    '\n');


            // =====================================================
            // CODE BORDER
            // =====================================================

            Border codeBorder =
                new Border
                {
                    Background =
                        Brushes.Transparent,

                    CornerRadius =
                        new CornerRadius(8),

                    BorderBrush =
                        new SolidColorBrush(
                            CodeBorderColor),

                    BorderThickness =
                        new Thickness(1),

                    Padding =
                        new Thickness(0),

                    Margin =
                        new Thickness(
                            0,
                            7,
                            0,
                            7)
                };


            Grid codeGrid =
                new Grid();


            codeBorder.Child =
                codeGrid;


            // =====================================================
            // LANGUAGE HEADER
            // =====================================================

            if (!string.IsNullOrWhiteSpace(
                    language))
            {
                TextBlock languageText =
                    new TextBlock
                    {
                        Text =
                            language.ToUpperInvariant(),

                        FontFamily =
                            AnswerFont,

                        FontSize =
                            10,

                        FontWeight =
                            FontWeights.SemiBold,

                        Foreground =
                            new SolidColorBrush(
                                LanguageLabelColor),

                        Margin =
                            new Thickness(
                                12,
                                8,
                                12,
                                4)
                    };


                codeGrid.RowDefinitions.Add(
                    new RowDefinition
                    {
                        Height =
                            GridLength.Auto
                    });


                Grid.SetRow(
                    languageText,
                    0);


                codeGrid.Children.Add(
                    languageText);
            }


            // =====================================================
            // AVALONEDIT
            // =====================================================

            TextEditor editor =
                new TextEditor
                {
                    Text =
                        code,

                    FontFamily =
                        CodeFont,

                    FontSize =
                        CodeFontSize,

                    Foreground =
                        new SolidColorBrush(
                            CodeTextColor),

                    Background =
                        new SolidColorBrush(
                                Color.FromArgb(
                                    55,
                                    255,
                                    255,
                                    255)),

                    BorderThickness =
                        new Thickness(0),

                    Padding =
                        new Thickness(
                            12,
                            8,
                            12,
                            8),

                    IsReadOnly =
                        true,

                    ShowLineNumbers =
                        false,

                    HorizontalScrollBarVisibility =
                        ScrollBarVisibility.Auto,

                    VerticalScrollBarVisibility =
                        ScrollBarVisibility.Disabled,

                    HorizontalAlignment =
                        HorizontalAlignment.Stretch,

                    VerticalAlignment =
                        VerticalAlignment.Top,

                    Focusable =
                        false,

                    IsHitTestVisible =
                        true
                };


            // =====================================================
            // SYNTAX HIGHLIGHTING
            // =====================================================

            ApplySyntaxHighlighting(
                editor,
                language);


            // =====================================================
            // FORCE CUSTOM RENDERING COLORS
            // =====================================================

            ApplyReadableSyntaxColors(
                editor,
                language);


            // =====================================================
            // CODE HEIGHT
            // =====================================================

            int lineCount =
                Math.Max(
                    1,
                    code.Split(
                        new[] { '\n' })
                        .Length);


            double codeHeight =
                (lineCount * 20.0) + 24.0;


            if (codeHeight < 48)
                codeHeight = 48;


            if (codeHeight > 500)
                codeHeight = 500;


            editor.Height =
                codeHeight;


            // =====================================================
            // GRID ROW
            // =====================================================

            int editorRow =
                string.IsNullOrWhiteSpace(language)
                    ? 0
                    : 1;


            while (codeGrid.RowDefinitions.Count <=
                   editorRow)
            {
                codeGrid.RowDefinitions.Add(
                    new RowDefinition
                    {
                        Height =
                            GridLength.Auto
                    });
            }


            Grid.SetRow(
                editor,
                editorRow);


            codeGrid.Children.Add(
                editor);


            panel.Children.Add(
                codeBorder);
        }


        // =========================================================
        // APPLY BUILT-IN SYNTAX HIGHLIGHTING
        // =========================================================

        private void ApplySyntaxHighlighting(
    TextEditor editor,
    string language)
        {
            if (editor == null)
                return;

            if (string.IsNullOrWhiteSpace(language))
                return;

            string normalized =
                language.Trim()
                        .ToLowerInvariant();

            try
            {
                IHighlightingDefinition definition = null;

                // =================================================
                // SQL
                // =================================================

                if (normalized == "sql" ||
                    normalized == "tsql" ||
                    normalized == "mysql" ||
                    normalized == "postgresql" ||
                    normalized == "postgres" ||
                    normalized == "plsql")
                {
                    definition =
                        HighlightingManager.Instance
                            .GetDefinitionByExtension(".sql");

                    if (definition == null)
                    {
                        definition =
                            HighlightingManager.Instance
                                .GetDefinition("SQL");
                    }
                }

                // =================================================
                // C#
                // =================================================

                else if (normalized == "csharp" ||
                         normalized == "cs" ||
                         normalized == "c#")
                {
                    definition =
                        HighlightingManager.Instance
                            .GetDefinitionByExtension(".cs");

                    if (definition == null)
                    {
                        definition =
                            HighlightingManager.Instance
                                .GetDefinition("C#");
                    }
                }

                // =================================================
                // JSON
                // =================================================

                else if (normalized == "json")
                {
                    definition =
                        HighlightingManager.Instance
                            .GetDefinitionByExtension(".json");

                    if (definition == null)
                    {
                        definition =
                            HighlightingManager.Instance
                                .GetDefinition("Json");
                    }
                }

                // =================================================
                // XML / XAML
                // =================================================

                else if (normalized == "xml" ||
                         normalized == "xaml")
                {
                    definition =
                        HighlightingManager.Instance
                            .GetDefinitionByExtension(".xml");

                    if (definition == null)
                    {
                        definition =
                            HighlightingManager.Instance
                                .GetDefinition("XML");
                    }
                }

                // =================================================
                // JAVASCRIPT
                // =================================================

                else if (normalized == "javascript" ||
                         normalized == "js")
                {
                    definition =
                        HighlightingManager.Instance
                            .GetDefinitionByExtension(".js");

                    if (definition == null)
                    {
                        definition =
                            HighlightingManager.Instance
                                .GetDefinition("JavaScript");
                    }
                }

                // =================================================
                // TYPESCRIPT
                // =================================================

                else if (normalized == "typescript" ||
                         normalized == "ts")
                {
                    definition =
                        HighlightingManager.Instance
                            .GetDefinitionByExtension(".ts");

                    if (definition == null)
                    {
                        definition =
                            HighlightingManager.Instance
                                .GetDefinition("JavaScript");
                    }
                }

                // =================================================
                // HTML
                // =================================================

                else if (normalized == "html")
                {
                    definition =
                        HighlightingManager.Instance
                            .GetDefinitionByExtension(".html");

                    if (definition == null)
                    {
                        definition =
                            HighlightingManager.Instance
                                .GetDefinition("HTML");
                    }
                }

                // =================================================
                // CSS
                // =================================================

                else if (normalized == "css")
                {
                    definition =
                        HighlightingManager.Instance
                            .GetDefinitionByExtension(".css");

                    if (definition == null)
                    {
                        definition =
                            HighlightingManager.Instance
                                .GetDefinition("CSS");
                    }
                }

                // =================================================
                // POWERSHELL
                // =================================================

                else if (normalized == "powershell" ||
                         normalized == "ps" ||
                         normalized == "ps1")
                {
                    definition =
                        HighlightingManager.Instance
                            .GetDefinitionByExtension(".ps1");

                    if (definition == null)
                    {
                        definition =
                            HighlightingManager.Instance
                                .GetDefinition("PowerShell");
                    }
                }

                // =================================================
                // VB.NET
                // =================================================

                else if (normalized == "vb" ||
                         normalized == "vbnet")
                {
                    definition =
                        HighlightingManager.Instance
                            .GetDefinitionByExtension(".vb");

                    if (definition == null)
                    {
                        definition =
                            HighlightingManager.Instance
                                .GetDefinition("VBNET");
                    }
                }

                // =================================================
                // APPLY
                // =================================================

                if (definition != null)
                {
                    editor.SyntaxHighlighting =
                        definition;

                    ApplyReadableSyntaxColors(
                        editor);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "AvalonEdit highlighting error: " +
                    ex.Message);
            }
        }

        private void ApplyReadableSyntaxColors(
    TextEditor editor)
        {
            if (editor == null)
                return;

            try
            {
                if (editor.SyntaxHighlighting == null)
                    return;

                // -----------------------------------------------------
                // DEFAULT EDITOR COLORS
                // -----------------------------------------------------

                editor.Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(
                            220,
                            220,
                            225));

                editor.Background =
                    Brushes.Transparent;


                // -----------------------------------------------------
                // GET NAMED COLORS
                // -----------------------------------------------------

                var namedColors =
                    editor.SyntaxHighlighting
                          .NamedHighlightingColors;

                if (namedColors == null)
                    return;


                // -----------------------------------------------------
                // KEYWORD
                // -----------------------------------------------------

                SetHighlightColor(
                    namedColors,
                    "Keyword",
                    Color.FromRgb(
                        255,
                        121,
                        198));


                // -----------------------------------------------------
                // KEYWORDS - ALTERNATE NAMES
                // -----------------------------------------------------

                SetHighlightColor(
                    namedColors,
                    "Keywords",
                    Color.FromRgb(
                        255,
                        121,
                        198));

                SetHighlightColor(
                    namedColors,
                    "KeywordColor",
                    Color.FromRgb(
                        255,
                        121,
                        198));


                // -----------------------------------------------------
                // TYPE
                // -----------------------------------------------------

                SetHighlightColor(
                    namedColors,
                    "Type",
                    Color.FromRgb(
                        102,
                        217,
                        239));

                SetHighlightColor(
                    namedColors,
                    "Types",
                    Color.FromRgb(
                        102,
                        217,
                        239));

                SetHighlightColor(
                    namedColors,
                    "TypeName",
                    Color.FromRgb(
                        102,
                        217,
                        239));


                // -----------------------------------------------------
                // STRING
                // -----------------------------------------------------

                SetHighlightColor(
                    namedColors,
                    "String",
                    Color.FromRgb(
                        166,
                        226,
                        46));

                SetHighlightColor(
                    namedColors,
                    "Strings",
                    Color.FromRgb(
                        166,
                        226,
                        46));


                // -----------------------------------------------------
                // COMMENT
                // -----------------------------------------------------

                SetHighlightColor(
                    namedColors,
                    "Comment",
                    Color.FromRgb(
                        117,
                        113,
                        94));

                SetHighlightColor(
                    namedColors,
                    "Comments",
                    Color.FromRgb(
                        117,
                        113,
                        94));


                // -----------------------------------------------------
                // NUMBER
                // -----------------------------------------------------

                SetHighlightColor(
                    namedColors,
                    "Number",
                    Color.FromRgb(
                        174,
                        129,
                        255));

                SetHighlightColor(
                    namedColors,
                    "Numbers",
                    Color.FromRgb(
                        174,
                        129,
                        255));


                // -----------------------------------------------------
                // METHOD / FUNCTION
                // -----------------------------------------------------

                SetHighlightColor(
                    namedColors,
                    "Method",
                    Color.FromRgb(
                        166,
                        226,
                        46));

                SetHighlightColor(
                    namedColors,
                    "Methods",
                    Color.FromRgb(
                        166,
                        226,
                        46));

                SetHighlightColor(
                    namedColors,
                    "Function",
                    Color.FromRgb(
                        166,
                        226,
                        46));

                SetHighlightColor(
                    namedColors,
                    "Functions",
                    Color.FromRgb(
                        166,
                        226,
                        46));


                // -----------------------------------------------------
                // PROPERTY
                // -----------------------------------------------------

                SetHighlightColor(
                    namedColors,
                    "Property",
                    Color.FromRgb(
                        102,
                        217,
                        239));

                SetHighlightColor(
                    namedColors,
                    "Properties",
                    Color.FromRgb(
                        102,
                        217,
                        239));


                // -----------------------------------------------------
                // CLASS
                // -----------------------------------------------------

                SetHighlightColor(
                    namedColors,
                    "Class",
                    Color.FromRgb(
                        253,
                        151,
                        31));

                SetHighlightColor(
                    namedColors,
                    "ClassName",
                    Color.FromRgb(
                        253,
                        151,
                        31));


                // -----------------------------------------------------
                // INTERFACE
                // -----------------------------------------------------

                SetHighlightColor(
                    namedColors,
                    "Interface",
                    Color.FromRgb(
                        102,
                        217,
                        239));


                // -----------------------------------------------------
                // CONSTANT
                // -----------------------------------------------------

                SetHighlightColor(
                    namedColors,
                    "Constant",
                    Color.FromRgb(
                        174,
                        129,
                        255));


                // -----------------------------------------------------
                // LOCAL VARIABLE
                // -----------------------------------------------------

                SetHighlightColor(
                    namedColors,
                    "LocalVariable",
                    Color.FromRgb(
                        248,
                        248,
                        242));


                // -----------------------------------------------------
                // OPERATOR
                // -----------------------------------------------------

                SetHighlightColor(
                    namedColors,
                    "Operator",
                    Color.FromRgb(
                        249,
                        38,
                        114));


                // -----------------------------------------------------
                // XML TAG
                // -----------------------------------------------------

                SetHighlightColor(
                    namedColors,
                    "Tag",
                    Color.FromRgb(
                        249,
                        38,
                        114));

                SetHighlightColor(
                    namedColors,
                    "XmlTag",
                    Color.FromRgb(
                        249,
                        38,
                        114));


                // -----------------------------------------------------
                // ATTRIBUTE
                // -----------------------------------------------------

                SetHighlightColor(
                    namedColors,
                    "Attribute",
                    Color.FromRgb(
                        166,
                        226,
                        46));

                SetHighlightColor(
                    namedColors,
                    "XmlAttribute",
                    Color.FromRgb(
                        166,
                        226,
                        46));


                // -----------------------------------------------------
                // ATTRIBUTE VALUE
                // -----------------------------------------------------

                SetHighlightColor(
                    namedColors,
                    "AttributeValue",
                    Color.FromRgb(
                        230,
                        219,
                        116));


                // -----------------------------------------------------
                // REGEX
                // -----------------------------------------------------

                SetHighlightColor(
                    namedColors,
                    "Regex",
                    Color.FromRgb(
                        230,
                        219,
                        116));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Syntax color customization error: " +
                    ex.Message);
            }
        }

        private void SetHighlightColor(
    System.Collections.Generic.IEnumerable<HighlightingColor> colors,
    string name,
    Color color)
        {
            if (colors == null)
                return;

            foreach (HighlightingColor item in colors)
            {
                if (item == null)
                    continue;

                if (string.Equals(
                        item.Name,
                        name,
                        StringComparison.OrdinalIgnoreCase))
                {
                    item.Foreground =
                        new SimpleHighlightingBrush(
                            color);
                }
            }
        }


        // =========================================================
        // APPLY READABLE SYNTAX COLORS
        //
        // We use a renderer transformer instead of modifying
        // AvalonEdit's internal HighlightingColor objects.
        //
        // This guarantees that colors are actually applied
        // to the visual text.
        // =========================================================

        private void ApplyReadableSyntaxColors(
            TextEditor editor,
            string language)
        {
            if (editor == null)
                return;


            if (string.IsNullOrWhiteSpace(language))
                return;


            try
            {
                editor.TextArea.TextView.LineTransformers.Clear();


                editor.TextArea.TextView.LineTransformers.Add(
                    new InterviewCodeColorizer(
                        language));


                editor.TextArea.TextView.Redraw();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "AvalonEdit colorizer error: " +
                    ex.Message);
            }
        }


        // =========================================================
        // LANGUAGE NORMALIZATION
        // =========================================================

        private string NormalizeLanguage(
            string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return "";


            string value =
                language.Trim()
                        .ToLowerInvariant();


            switch (value)
            {
                case "sql":
                case "tsql":
                case "mysql":
                case "postgres":
                case "postgresql":
                case "plsql":
                    return "sql";


                case "c#":
                case "cs":
                case "csharp":
                    return "csharp";


                case "js":
                case "javascript":
                case "jsx":
                    return "javascript";


                case "ts":
                case "typescript":
                case "tsx":
                    return "typescript";


                case "json":
                    return "json";


                case "xml":
                case "xaml":
                    return value;


                case "html":
                case "htm":
                    return "html";


                case "css":
                    return "css";


                case "powershell":
                case "ps":
                case "ps1":
                    return "powershell";


                case "vb":
                case "vbnet":
                case "visualbasic":
                    return "vb";


                default:
                    return value;
            }
        }


        // =========================================================
        // LANGUAGE IDENTIFIER
        // =========================================================

        private bool IsLanguageIdentifier(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;


            string normalized =
                value.Trim()
                        .ToLowerInvariant();


            switch (normalized)
            {
                case "sql":
                case "tsql":
                case "mysql":
                case "postgresql":
                case "postgres":
                case "plsql":

                case "csharp":
                case "cs":
                case "c#":

                case "json":

                case "xml":
                case "xaml":

                case "javascript":
                case "js":

                case "typescript":
                case "ts":

                case "html":
                case "htm":

                case "css":

                case "powershell":
                case "ps":
                case "ps1":

                case "vb":
                case "vbnet":
                case "visualbasic":

                    return true;


                default:
                    return false;
            }
        }


        // =========================================================
        // FALLBACK LANGUAGE DETECTION
        // =========================================================

        private string DetectLanguageFromCode(
            string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return "";


            string sample =
                code.Trim();


            // =====================================================
            // SQL
            // =====================================================

            if (Regex.IsMatch(
                    sample,
                    @"\b(SELECT|INSERT|UPDATE|DELETE|FROM|WHERE|JOIN|GROUP\s+BY|ORDER\s+BY|HAVING|CREATE\s+TABLE|ALTER\s+TABLE|DROP\s+TABLE|DECLARE|EXEC|EXECUTE)\b",
                    RegexOptions.IgnoreCase))
            {
                return "sql";
            }


            // =====================================================
            // C#
            // =====================================================

            if (Regex.IsMatch(
                    sample,
                    @"\b(using\s+System|namespace\s+\w+|public\s+(class|interface|enum)|private\s+\w+|Console\.WriteLine|async\s+Task|IEnumerable<)",
                    RegexOptions.IgnoreCase))
            {
                return "csharp";
            }


            // =====================================================
            // JSON
            // =====================================================

            if (Regex.IsMatch(
                    sample,
                    @"^\s*[\{\[]") &&
                Regex.IsMatch(
                    sample,
                    @"""[^""]+""\s*:",
                    RegexOptions.IgnoreCase))
            {
                return "json";
            }


            // =====================================================
            // XML / XAML
            // =====================================================

            if (Regex.IsMatch(
                    sample,
                    @"<([A-Za-z_][\w\.\-]*)(\s|>)") &&
                Regex.IsMatch(
                    sample,
                    @"</?[A-Za-z_][\w\.\-]*",
                    RegexOptions.IgnoreCase))
            {
                if (Regex.IsMatch(
                        sample,
                        @"\b(Grid|Window|StackPanel|TextBlock|Button|TextBox|Border)\b"))
                {
                    return "xaml";
                }

                return "xml";
            }


            // =====================================================
            // JAVASCRIPT / TYPESCRIPT
            // =====================================================

            if (Regex.IsMatch(
                    sample,
                    @"\b(const|let|var|function|console\.log|document\.|window\.|=>)\b",
                    RegexOptions.IgnoreCase))
            {
                return "javascript";
            }


            // =====================================================
            // HTML
            // =====================================================

            if (Regex.IsMatch(
                    sample,
                    @"</?(html|head|body|div|span|table|script|style|input|button|form)\b",
                    RegexOptions.IgnoreCase))
            {
                return "html";
            }


            // =====================================================
            // CSS
            // =====================================================

            if (Regex.IsMatch(
                    sample,
                    @"[.#]?[A-Za-z][\w\-]*\s*\{[^}]*:",
                    RegexOptions.IgnoreCase))
            {
                return "css";
            }


            // =====================================================
            // POWERSHELL
            // =====================================================

            if (Regex.IsMatch(
                    sample,
                    @"(\$\w+|\b(Get-|Set-|New-|Remove-|Write-Host|foreach|Where-Object)\b)",
                    RegexOptions.IgnoreCase))
            {
                return "powershell";
            }


            // =====================================================
            // VB.NET
            // =====================================================

            if (Regex.IsMatch(
                    sample,
                    @"\b(Module|End\s+Module|Dim|As\s+\w+|Sub|End\s+Sub|Function|End\s+Function)\b",
                    RegexOptions.IgnoreCase))
            {
                return "vb";
            }


            return "";
        }


        // =========================================================
        // UPDATE AI MESSAGE
        // =========================================================

        private void UpdateAIMessage(
            Border bubble,
            string message)
        {
            if (bubble == null)
                return;


            string finalMessage =
                message ?? "";


            if (bubble.Child is Grid mainGrid)
            {
                foreach (UIElement child
                         in mainGrid.Children)
                {
                    if (child is ScrollViewer scrollViewer)
                    {
                        if (scrollViewer.Content
                            is StackPanel panel)
                        {
                            RenderAIContent(
                                panel,
                                finalMessage);
                        }

                        break;
                    }
                }


                // =====================================================
                // UPDATE COPY BUTTON TEXT
                // =====================================================

                foreach (UIElement child
                         in mainGrid.Children)
                {
                    if (child is Button copyButton &&
                        copyButton.ToolTip?.ToString()
                            == "Copy answer")
                    {
                        copyButton.Tag =
                            finalMessage;

                        break;
                    }
                }
            }


            //ChatScrollViewer.ScrollToEnd();
        }

        // =========================================================
        // ENSURE CHAT BOTTOM SPACER
        // =========================================================

        private void EnsureChatBottomSpacer()
        {
            if (ChatPanel == null ||
                ChatScrollViewer == null)
                return;

            if (_chatBottomSpacer != null)
                return;

            _chatBottomSpacer =
                new Border
                {
                    Height =
                        Math.Max(
                            200,
                            ChatScrollViewer.ActualHeight),

                    Background =
                        Brushes.Transparent,

                    IsHitTestVisible =
                        false
                };

            ChatPanel.Children.Add(
                _chatBottomSpacer);
        }


        // =========================================================
        // SCROLL USER QUESTION TO TOP
        // =========================================================

        private void ScrollMessageToTop(
    Border bubble)
        {
            if (bubble == null ||
                ChatScrollViewer == null)
                return;

            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    try
                    {
                        ChatScrollViewer.UpdateLayout();
                        ChatPanel.UpdateLayout();
                        bubble.UpdateLayout();

                        GeneralTransform transform =
                            bubble.TransformToAncestor(
                                ChatScrollViewer);

                        Point point =
                            transform.Transform(
                                new Point(0, 0));

                        double targetOffset =
                            ChatScrollViewer.VerticalOffset +
                            point.Y;

                        ChatScrollViewer.ScrollToVerticalOffset(
                            Math.Max(
                                0,
                                targetOffset));
                    }
                    catch
                    {
                    }
                }),
                DispatcherPriority.Render);
        }


        // =========================================================
        // UPDATE AI MESSAGE ON UI
        // =========================================================

        private async Task UpdateAIMessageOnUI(
            Border bubble,
            string message)
        {
            await Dispatcher.InvokeAsync(
                () =>
                {
                    UpdateAIMessage(
                        bubble,
                        message);
                });
        }
    }


    // =================================================================
    // AVALONEDIT VISUAL SYNTAX COLORIZER
    // =================================================================

    internal sealed class InterviewCodeColorizer :
        DocumentColorizingTransformer
    {
        private readonly string _language;


        private static readonly Regex SqlRegex =
            new Regex(
                @"(?<comment>--.*$|/\*.*?\*/)|" +
                @"(?<string>'(?:''|[^'])*'|""(?:""""|[^""])*"")|" +
                @"(?<number>\b\d+(?:\.\d+)?\b)|" +
                @"(?<keyword>\b(?:SELECT|FROM|WHERE|JOIN|INNER|LEFT|RIGHT|FULL|OUTER|ON|AS|AND|OR|NOT|IN|IS|NULL|LIKE|BETWEEN|GROUP|BY|ORDER|HAVING|ASC|DESC|DISTINCT|TOP|LIMIT|OFFSET|UNION|ALL|INSERT|INTO|VALUES|UPDATE|SET|DELETE|CREATE|ALTER|DROP|TABLE|VIEW|INDEX|DATABASE|PRIMARY|KEY|FOREIGN|REFERENCES|CONSTRAINT|CASE|WHEN|THEN|ELSE|END|EXISTS|WITH|OVER|PARTITION|DECLARE|EXEC|EXECUTE|BEGIN|COMMIT|ROLLBACK|IF|WHILE)\b)|" +
                @"(?<function>\b(?:COUNT|SUM|AVG|MIN|MAX|COALESCE|ISNULL|CAST|CONVERT|ROW_NUMBER|RANK|DENSE_RANK|NTILE|LEN|LOWER|UPPER|SUBSTRING|GETDATE|DATEADD|DATEDIFF)\b)",
                RegexOptions.IgnoreCase |
                RegexOptions.Compiled);


        private static readonly Regex CSharpRegex =
            new Regex(
                @"(?<comment>//.*$|/\*.*?\*/)|" +
                @"(?<string>""(?:\\""|[^""])*""|'(?:\\'|[^'])*')|" +
                @"(?<number>\b\d+(?:\.\d+)?[fFdDmM]?\b)|" +
                @"(?<keyword>\b(?:abstract|as|async|await|base|bool|break|byte|case|catch|char|checked|class|const|continue|decimal|default|delegate|do|double|else|enum|event|explicit|extern|false|finally|fixed|float|for|foreach|goto|if|implicit|in|int|interface|internal|is|lock|long|namespace|new|null|object|operator|out|override|params|private|protected|public|readonly|ref|return|sbyte|sealed|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|unchecked|unsafe|ushort|using|virtual|void|volatile|while|var|dynamic|get|set|init|record|with|yield)\b)|" +
                @"(?<type>\b(?:Task|List|Dictionary|IEnumerable|String|Int32|Int64|Boolean|DateTime|Guid|HttpClient|Action|Func)\b)|" +
                @"(?<function>\b[A-Za-z_][A-Za-z0-9_]*(?=\s*\())",
                RegexOptions.Compiled);


        private static readonly Regex JavaScriptRegex =
            new Regex(
                @"(?<comment>//.*$|/\*.*?\*/)|" +
                @"(?<string>""(?:\\""|[^""])*""|'(?:\\'|[^'])*'|`(?:\\`|[^`])*`)|" +
                @"(?<number>\b\d+(?:\.\d+)?\b)|" +
                @"(?<keyword>\b(?:const|let|var|function|return|if|else|for|while|do|switch|case|break|continue|new|class|extends|import|export|from|async|await|try|catch|finally|throw|typeof|instanceof|in|of|this|true|false|null|undefined)\b)|" +
                @"(?<function>\b[A-Za-z_$][A-Za-z0-9_$]*(?=\s*\())",
                RegexOptions.Compiled);


        private static readonly Regex JsonRegex =
            new Regex(
                @"(?<property>""(?:\\""|[^""])*""(?=\s*:))|" +
                @"(?<string>""(?:\\""|[^""])*"")|" +
                @"(?<number>-?\b\d+(?:\.\d+)?\b)|" +
                @"(?<keyword>\b(?:true|false|null)\b)",
                RegexOptions.Compiled);


        private static readonly Regex XmlRegex =
            new Regex(
                @"(?<comment><!--.*?-->)|" +
                @"(?<tag></?[A-Za-z_][A-Za-z0-9_\.\-:]*)|" +
                @"(?<property>[A-Za-z_][A-Za-z0-9_\.\-:]*)(?=\s*=)|" +
                @"(?<string>""[^""]*""|'[^']*')",
                RegexOptions.Compiled);


        private static readonly Regex CssRegex =
            new Regex(
                @"(?<comment>/\*.*?\*/)|" +
                @"(?<string>""[^""]*""|'[^']*')|" +
                @"(?<number>\b\d+(?:\.\d+)?(?:px|em|rem|%|vh|vw|s|ms)?\b)|" +
                @"(?<property>[A-Za-z\-]+)(?=\s*:)|" +
                @"(?<keyword>\b(?:important|inherit|initial|unset|none|auto|block|inline|flex|grid|absolute|relative|fixed)\b)",
                RegexOptions.IgnoreCase |
                RegexOptions.Compiled);


        private static readonly Regex PowerShellRegex =
            new Regex(
                @"(?<comment>#.*$)|" +
                @"(?<string>""(?:`""|[^""])*""|'(?:`'|[^'])*')|" +
                @"(?<number>\b\d+(?:\.\d+)?\b)|" +
                @"(?<variable>\$\w+)|" +
                @"(?<keyword>\b(?:function|param|if|else|elseif|foreach|for|while|switch|return|break|continue|try|catch|finally|throw|class|enum|begin|process|end|filter|where|select|foreach|in)\b)",
                RegexOptions.IgnoreCase |
                RegexOptions.Compiled);


        private static readonly Regex VbRegex =
            new Regex(
                @"(?<comment>'.*$)|" +
                @"(?<string>""[^""]*"")|" +
                @"(?<number>\b\d+(?:\.\d+)?\b)|" +
                @"(?<keyword>\b(?:Dim|As|Integer|Long|String|Boolean|Double|Decimal|Date|Object|Class|Module|Sub|Function|End|If|Then|Else|ElseIf|For|Each|Next|While|Do|Loop|Select|Case|Return|Imports|Namespace|Public|Private|Protected|Friend|Shared|Static|New|Nothing|True|False|Try|Catch|Finally|Throw|Inherits|Implements|Interface|Structure|Enum|ByVal|ByRef|Optional|ParamArray)\b)",
                RegexOptions.IgnoreCase |
                RegexOptions.Compiled);


        public InterviewCodeColorizer(
            string language)
        {
            _language =
                Normalize(
                    language);
        }


        protected override void ColorizeLine(
            DocumentLine line)
        {
            if (line == null)
                return;


            string text =
                CurrentContext.Document.GetText(
                    line);


            if (string.IsNullOrEmpty(text))
                return;


            Regex regex =
                GetRegex();


            if (regex == null)
                return;


            MatchCollection matches =
                regex.Matches(text);


            foreach (Match match in matches)
            {
                if (!match.Success)
                    continue;


                Color color =
                    CodeTextColor;


                string groupName =
                    GetGroupName(
                        match);


                switch (groupName)
                {
                    case "comment":
                        color =
                            SyntaxCommentColor;
                        break;


                    case "string":
                        color =
                            SyntaxStringColor;
                        break;


                    case "number":
                        color =
                            SyntaxNumberColor;
                        break;


                    case "keyword":
                        color =
                            SyntaxKeywordColor;
                        break;


                    case "type":
                        color =
                            SyntaxTypeColor;
                        break;


                    case "function":
                        color =
                            SyntaxFunctionColor;
                        break;


                    case "property":
                        color =
                            SyntaxPropertyColor;
                        break;


                    case "variable":
                        color =
                            SyntaxOperatorColor;
                        break;


                    case "tag":
                        color =
                            SyntaxKeywordColor;
                        break;


                    default:
                        continue;
                }


                int start =
                    line.Offset +
                    match.Index;


                int end =
                    start +
                    match.Length;


                ChangeLinePart(
                    start,
                    end,
                    element =>
                    {
                        element.TextRunProperties.SetForegroundBrush(
                            new SolidColorBrush(
                                color));
                    });
            }
        }


        private Regex GetRegex()
        {
            switch (_language)
            {
                case "sql":
                    return SqlRegex;


                case "csharp":
                    return CSharpRegex;


                case "javascript":
                case "typescript":
                    return JavaScriptRegex;


                case "json":
                    return JsonRegex;


                case "xml":
                case "xaml":
                case "html":
                    return XmlRegex;


                case "css":
                    return CssRegex;


                case "powershell":
                    return PowerShellRegex;


                case "vb":
                    return VbRegex;


                default:
                    return null;
            }
        }


        private string GetGroupName(Match match)
        {
            if (match == null)
                return "";

            string[] groupNames =
                new[]
                {
                            "comment",
                            "string",
                            "number",
                            "keyword",
                            "type",
                            "function",
                            "property",
                            "variable",
                            "tag"
                };

            foreach (string name in groupNames)
            {
                Group group = match.Groups[name];

                if (group != null &&
                    group.Success)
                {
                    return name;
                }
            }

            return "";
        }

        private static string Normalize(
            string language)
        {
            if (string.IsNullOrWhiteSpace(
                    language))
            {
                return "";
            }


            switch (
                language.Trim()
                        .ToLowerInvariant())
            {
                case "sql":
                case "tsql":
                case "mysql":
                case "postgres":
                case "postgresql":
                case "plsql":
                    return "sql";


                case "c#":
                case "cs":
                case "csharp":
                    return "csharp";


                case "js":
                case "javascript":
                    return "javascript";


                case "ts":
                case "typescript":
                    return "typescript";


                case "json":
                    return "json";


                case "xml":
                    return "xml";


                case "xaml":
                    return "xaml";


                case "html":
                case "htm":
                    return "html";


                case "css":
                    return "css";


                case "powershell":
                case "ps":
                case "ps1":
                    return "powershell";


                case "vb":
                case "vbnet":
                    return "vb";


                default:
                    return language
                        .Trim()
                        .ToLowerInvariant();
            }
        }


        // =========================================================
        // COLORS
        // =========================================================

        private static readonly Color SyntaxKeywordColor =
            Color.FromRgb(255, 121, 198);

        private static readonly Color SyntaxStringColor =
            Color.FromRgb(166, 227, 161);

        private static readonly Color SyntaxCommentColor =
            Color.FromRgb(120, 130, 140);

        private static readonly Color SyntaxNumberColor =
            Color.FromRgb(189, 147, 249);

        private static readonly Color SyntaxTypeColor =
            Color.FromRgb(139, 233, 253);

        private static readonly Color SyntaxFunctionColor =
            Color.FromRgb(80, 250, 123);

        private static readonly Color SyntaxOperatorColor =
            Color.FromRgb(255, 184, 108);

        private static readonly Color SyntaxPropertyColor =
            Color.FromRgb(139, 233, 253);

        private static readonly Color CodeTextColor =
            Color.FromRgb(225, 225, 230);
    }


    // =================================================================
    // SMALL HELPER
    // =================================================================

    internal static class EnumerableExtensions
    {
        public static string[] ToArraySafe(
            this IEnumerable<string> source)
        {
            if (source == null)
                return new string[0];


            List<string> result =
                new List<string>();


            foreach (string item in source)
            {
                result.Add(item);
            }


            return result.ToArray();
        }
    }
}