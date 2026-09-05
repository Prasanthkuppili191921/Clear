using System;

namespace AiInterviewAssistant
{
    public partial class MainWindow
    {
        // =========================================================
        // CHAT SCROLL
        //
        // ChatGPTView = true
        //     -> Scroll ChatGPT WebView
        //
        // ChatGPTView = false
        //     -> Existing WPF ChatScrollViewer
        // =========================================================

        private void ScrollChatWindow(
            int hotkeyId)
        {
            try
            {
                // =================================================
                // CHATGPT VIEW
                //
                // Controlled by App.config:
                //
                // <add key="ChatGPTView" value="true" />
                // =================================================

                if (_chatGPTView)
                {
                    if (
                        hotkeyId ==
                        HotKeysRegister.SCROLL_UP_HOTKEY_ID)
                    {
                        _ =
                            ChatGPTWebViewHost
                                .ScrollChatGPTAsync(-1);

                        return;
                    }


                    if (
                        hotkeyId ==
                        HotKeysRegister.SCROLL_DOWN_HOTKEY_ID)
                    {
                        _ =
                            ChatGPTWebViewHost
                                .ScrollChatGPTAsync(1);

                        return;
                    }


                    return;
                }


                // =================================================
                // EXISTING WPF CHAT
                //
                // DO NOT CHANGE THIS BEHAVIOR
                // =================================================

                if (ChatScrollViewer == null)
                    return;


                if (!ChatScrollViewer.IsLoaded)
                    return;


                const double SCROLL_DISTANCE = 70.0;


                double currentOffset =
                    ChatScrollViewer.VerticalOffset;


                double targetOffset;


                // =================================================
                // ALT + UP
                // =================================================

                if (
                    hotkeyId ==
                    HotKeysRegister.SCROLL_UP_HOTKEY_ID)
                {
                    targetOffset =
                        currentOffset -
                        SCROLL_DISTANCE;
                }


                // =================================================
                // ALT + DOWN
                // =================================================

                else if (
                    hotkeyId ==
                    HotKeysRegister.SCROLL_DOWN_HOTKEY_ID)
                {
                    targetOffset =
                        currentOffset +
                        SCROLL_DISTANCE;
                }


                else
                {
                    return;
                }


                // =================================================
                // TOP LIMIT
                // =================================================

                if (targetOffset < 0)
                    targetOffset = 0;


                // =================================================
                // BOTTOM LIMIT
                // =================================================

                if (
                    targetOffset >
                    ChatScrollViewer.ScrollableHeight)
                {
                    targetOffset =
                        ChatScrollViewer.ScrollableHeight;
                }


                // =================================================
                // NOTHING TO SCROLL
                // =================================================

                if (
                    Math.Abs(
                        targetOffset -
                        currentOffset) < 0.5)
                {
                    return;
                }


                // =================================================
                // EXISTING SMOOTH SCROLL
                // =================================================

                StartSmoothChatScroll(
                    targetOffset);
            }
            catch
            {
            }
        }
    }
}