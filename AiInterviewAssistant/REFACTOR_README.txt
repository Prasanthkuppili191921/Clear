Segregated MainWindow files

All files are partial MainWindow classes. Existing private fields and methods remain shared across files.

1. MainWindow.xaml.cs - fields, constructor, Whisper/OCR initialization
2. MainWindow.ScreenCapture.cs - screen capture, OCR, MCQ extraction, online-test capture
3. MainWindow.Hotkeys.cs - hotkeys and overlay/window lifecycle
4. MainWindow.AI.cs - OpenRouter, chat messages, AI generation
5. MainWindow.UI.cs - textbox/buttons/UI events
6. MainWindow.Voice.cs - recording, silence detection, Whisper transcription

If the project uses explicit Compile Include entries, add the five new .cs files and remove the old monolithic .cs file from the project. Do not keep both copies of the same MainWindow methods.
