(function () {

    window.aiInterviewAssistantModules =
        window.aiInterviewAssistantModules || {};

    const styleId =
        'ai-interview-chatgpt-clean-style';

    // =========================================================
    // ADD CSS
    // =========================================================

    function injectStyle() {

        if (!document.head)
            return;

        let style =
            document.getElementById(styleId);

        if (!style) {

            style =
                document.createElement('style');

            style.id =
                styleId;

            style.textContent = `

                /* =============================================
                   TRANSPARENT PAGE BACKGROUND
                   ============================================= */

                html,
                body,
                #__next,
                main,
                main > div,
                main > div > div,
                [role='main'],
                [data-testid='conversation-turns'],
                [class*='bg-token-main-surface'],
                [class*='bg-token-main-surface-primary'],
                [class*='bg-token-main-surface-secondary'],
                [class*='bg-token-sidebar'],
                [class*='bg-token-bg'] {

                    background: transparent !important;
                    background-color: transparent !important;
                    background-image: none !important;
                }


                /* =============================================
                   REMOVE CHATGPT SURFACE BACKGROUNDS
                   ============================================= */

                main,
                main section,
                main article,
                main > div,
                main > div > div {

                    background: transparent !important;
                    background-color: transparent !important;
                    background-image: none !important;

                    mask-image: none !important;
                    -webkit-mask-image: none !important;

                    box-shadow: none !important;
                }


                /* =============================================
                   FORCE MAIN CHAT SURFACES TRANSPARENT
                   ============================================= */

                main *,
                [role='main'],
                [role='main'] * {

                    background-color: transparent !important;
                    background-image: none !important;

                    mask-image: none !important;
                    -webkit-mask-image: none !important;

                    box-shadow: none !important;
                }


                /* =============================================
                   CHATGPT DARK SURFACE CLASSES
                   ============================================= */

                [class*='bg-black'],
                [class*='bg-gray-'],
                [class*='bg-token-main-surface'],
                [class*='bg-token-main-surface-primary'],
                [class*='bg-token-main-surface-secondary'] {

                    background: transparent !important;
                    background-color: transparent !important;
                    background-image: none !important;
                }


                /* =============================================
                   HIDE SIDEBAR
                   ============================================= */

                aside {
                    display: none !important;
                }


                /* =============================================
                   HIDE HEADER
                   ============================================= */

                header {
                    display: none !important;
                }


                /* =============================================
                   HIDE NAVIGATION
                   ============================================= */

                nav {
                    display: none !important;
                }


                /* =============================================
                   HIDE MODEL SELECTOR
                   ============================================= */

                [data-testid='model-switcher'] {
                    display: none !important;
                }


                /* =============================================
                   HIDDEN COMPOSER
                   ============================================= */

                //.ai-interview-hidden-composer {

                //    opacity: 0 !important;
                //    visibility: hidden !important;

                //    pointer-events: none !important;

                //    height: 0 !important;
                //    min-height: 0 !important;
                //    max-height: 0 !important;

                //    margin: 0 !important;
                //    padding: 0 !important;

                //    overflow: hidden !important;
                //}


                /* =============================================
                   HIDE COMPOSER FOOTER
                   ============================================= */

                [data-testid='composer-footer'] {
                    display: none !important;
                }


                /* =============================================
                   HIDE FEATURE MENU ITEMS
                   ============================================= */

                .ai-interview-hidden-feature {
                    display: none !important;
                }


                /* =============================================
                   HIDE CREATE IMAGE
                   ============================================= */

                button[aria-label*='Create an image' i],
                button[aria-label*='Create image' i] {

                    display: none !important;
                }


                /* =============================================
                   HIDE CREATE STICKER
                   ============================================= */

                button[aria-label*='Create a sticker' i],
                button[aria-label*='Sticker' i] {

                    display: none !important;
                }


                /* =============================================
                   HIDE WRITE OR EDIT
                   ============================================= */

                button[aria-label*='Write or edit' i] {

                    display: none !important;
                }


                /* =============================================
                   HIDE SEARCH THE WEB
                   ============================================= */

                button[aria-label*='Search the web' i],
                button[aria-label*='Search web' i] {

                    display: none !important;
                }


                /* =============================================
                   HIDE WELCOME SCREEN
                   ============================================= */

                .ai-interview-hidden-welcome {
                    display: none !important;
                }


                /* =============================================
                   HIDE TERMS / PRIVACY NOTICE
                   ============================================= */

                .ai-interview-hidden-terms {
                    display: none !important;
                }

                /* HIDE CHATGPT DISCLAIMER */
                [data-testid='thread-disclaimer'] {
                    display: none !important;
                }


                /* =============================================
                   REMOVE CHATGPT SCROLL FADE / OVERLAY LAYERS
                   ============================================= */

                main::before,
                main::after,
                main *::before,
                main *::after,
                [role='main']::before,
                [role='main']::after,
                [role='main'] *::before,
                [role='main'] *::after {

                    background: transparent !important;
                    background-color: transparent !important;
                    background-image: none !important;

                    box-shadow: none !important;
                }

                /* =============================================
                   CHATGPT SCROLLBAR
                   ============================================= */

                [data-scroll-root] {
                    scrollbar-width: auto !important;
                }

                [data-scroll-root]::-webkit-scrollbar {
                    width: 10px !important;
                }

                [data-scroll-root]::-webkit-scrollbar-track {
                    background: transparent !important;
                }

                [data-scroll-root]::-webkit-scrollbar-thumb {
                    background: rgba(128, 128, 128, 0.55) !important;
                    border-radius: 8px !important;
                    border: 3px solid transparent !important;
                    background-clip: padding-box !important;
                }

                [data-scroll-root]::-webkit-scrollbar-thumb:hover {
                    background: rgba(128, 128, 128, 0.8) !important;
                    border: 2px solid transparent !important;
                    background-clip: padding-box !important;
                }

                /* =============================================
                   REMOVE HORIZONTAL OVERFLOW
                   ============================================= */

                html,
                body {

                    overflow-x: hidden !important;
                }

            `;

            document.head.appendChild(style);
        }
    }


    window.aiInterviewAssistantModules.injectStyle =
        injectStyle;

})();