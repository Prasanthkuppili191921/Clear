(function () {

    const styleId = 'ai-interview-chatgpt-clean-style';


    // =========================================================
    // ADD CSS
    // =========================================================

    function injectStyle() {

        if (!document.head)
            return;

        let style = document.getElementById(styleId);

        if (!style) {

            style = document.createElement('style');
            style.id = styleId;

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

                .ai-interview-hidden-composer {

                    opacity: 0 !important;
                    visibility: hidden !important;

                    pointer-events: none !important;

                    height: 0 !important;
                    min-height: 0 !important;
                    max-height: 0 !important;

                    margin: 0 !important;
                    padding: 0 !important;

                    overflow: hidden !important;
                }


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


    // =========================================================
    // FIND CHATGPT INPUT
    // =========================================================

    function findEditor() {

        let editor =
            document.querySelector('#prompt-textarea');

        if (editor)
            return editor;


        editor =
            document.querySelector('textarea');

        if (editor)
            return editor;


        editor =
            document.querySelector(
                '[contenteditable="true"]'
            );

        if (editor)
            return editor;


        return null;
    }


    // =========================================================
    // FIND COMPOSER
    // =========================================================

    function hideComposer() {

        const editor = findEditor();

        if (!editor)
            return;


        let composer = null;


        // -----------------------------------------------------
        // 1. Composer FORM
        // -----------------------------------------------------

        composer =
            editor.closest('form');


        // -----------------------------------------------------
        // 2. Known composer container
        // -----------------------------------------------------

        if (!composer) {

            composer =
                editor.closest(
                    '[data-testid="composer"]'
                );
        }


        // -----------------------------------------------------
        // 3. Carefully walk up the DOM
        // -----------------------------------------------------

        if (!composer) {

            let current =
                editor.parentElement;


            for (
                let i = 0;
                i < 12 && current;
                i++
            ) {

                const rect =
                    current.getBoundingClientRect();


                const buttons =
                    current.querySelectorAll(
                        'button'
                    );


                const hasButtons =
                    buttons &&
                    buttons.length > 0;


                if (
                    hasButtons &&
                    rect.width > 300 &&
                    rect.height < 300
                ) {

                    composer =
                        current;

                    break;
                }


                current =
                    current.parentElement;
            }
        }


        // -----------------------------------------------------
        // Hide only composer
        // -----------------------------------------------------

        if (composer) {

            composer.classList.add(
                'ai-interview-hidden-composer'
            );
        }
    }


    // =========================================================
    // HIDE FEATURE MENU ITEMS
    // =========================================================

    function hideFeatureMenus() {

        const elements =
            document.querySelectorAll(
                'button, [role="menuitem"], [role="option"]'
            );


        elements.forEach(function (element) {

            const text =
                (
                    element.innerText ||
                    element.textContent ||
                    ''
                )
                    .trim()
                    .toLowerCase();


            const aria =
                (
                    element.getAttribute(
                        'aria-label'
                    ) ||
                    ''
                )
                    .trim()
                    .toLowerCase();


            const title =
                (
                    element.getAttribute(
                        'title'
                    ) ||
                    ''
                )
                    .trim()
                    .toLowerCase();


            const value =
                text +
                ' ' +
                aria +
                ' ' +
                title;


            if (
                value.includes(
                    'create an image'
                ) ||
                value.includes(
                    'create image'
                ) ||
                value.includes(
                    'create a sticker'
                ) ||
                value.includes(
                    'write or edit'
                ) ||
                value.includes(
                    'search the web'
                )
            ) {

                element.classList.add(
                    'ai-interview-hidden-feature'
                );
            }

        });
    }


    // =========================================================
    // HIDE CHATGPT EMPTY / WELCOME SCREEN
    // =========================================================

    function hideWelcomeScreen() {

        try {

            const elements =
                document.querySelectorAll(
                    "h1, h2, h3, [role='heading']"
                );


            elements.forEach(
                function (element) {

                    const text =
                        (
                            element.innerText ||
                            element.textContent ||
                            ""
                        )
                            .replace(
                                /\s+/g,
                                " "
                            )
                            .trim()
                            .replace(
                                /^#\s*/,
                                ""
                            );


                    if (
                        text ===
                        "Where should we begin?" ||
                        text ===
                        "What are you working on?" ||
                        text ===
                        "What can I do for you?"
                    ) {

                        element.style.display =
                            "none";


                        const parent =
                            element.parentElement;


                        if (parent) {

                            parent.style.display =
                                "none";
                        }
                    }
                }
            );

        }
        catch (e) {
            // Ignore cleanup errors.
        }
    }


    // =========================================================
    // HIDE CHATGPT DISCLAIMER
    // =========================================================

    function hideChatGPTDisclaimer() {

        try {

            const disclaimerTexts = [
                'ChatGPT can make mistakes. Check important info.',
                'ChatGPT is AI and can make mistakes.'
            ];


            const elements =
                document.querySelectorAll('*');


            elements.forEach(function (element) {

                const text =
                    (
                        element.innerText ||
                        element.textContent ||
                        ''
                    )
                        .replace(/\s+/g, ' ')
                        .trim();


                if (!text)
                    return;


                let matched = false;


                for (
                    const disclaimerText of disclaimerTexts
                ) {

                    if (
                        text.includes(disclaimerText)
                    ) {

                        matched = true;
                        break;
                    }
                }


                if (!matched)
                    return;


                // -------------------------------------------------
                // Only hide small elements.
                // This prevents accidentally hiding the
                // complete answer/message container.
                // -------------------------------------------------

                const rect =
                    element.getBoundingClientRect();


                if (
                    rect.width > 0 &&
                    rect.height > 0 &&
                    rect.height < 150 &&
                    text.length < 200
                ) {

                    element.style.setProperty(
                        'display',
                        'none',
                        'important'
                    );
                }

            });

        }
        catch (e) {
            // Ignore cleanup errors.
        }
    }

    // =========================================================
    // HIDE CHATGPT NATIVE COMPOSER
    // =========================================================

    function hideChatGPTNativeComposer() {

        try {

            const elements =
                document.querySelectorAll('div, form');


            for (const element of elements) {

                const text =
                    (
                        element.innerText ||
                        element.textContent ||
                        ''
                    )
                        .replace(/\s+/g, ' ')
                        .trim();


                // ---------------------------------------------
                // ChatGPT native composer contains "Ask anything"
                // ---------------------------------------------

                if (
                    text === 'Ask anything'
                ) {

                    let target =
                        element;


                    // -----------------------------------------
                    // Walk upward to find the complete composer
                    // -----------------------------------------

                    for (
                        let i = 0;
                        i < 6 && target.parentElement;
                        i++
                    ) {

                        const parent =
                            target.parentElement;


                        const rect =
                            parent.getBoundingClientRect();


                        if (
                            rect.width >= 500 &&
                            rect.height >= 80 &&
                            rect.height <= 250
                        ) {

                            target =
                                parent;

                            break;
                        }


                        target =
                            parent;
                    }


                    // -----------------------------------------
                    // Hide complete native composer
                    // -----------------------------------------

                    target.style.setProperty(
                        'display',
                        'none',
                        'important'
                    );

                    return;
                }
            }

        }
        catch (e) {
            // Ignore cleanup errors.
        }
    }


    // =========================================================
    // HIDE TERMS / PRIVACY NOTICE
    // =========================================================

    function hideTermsNotice() {

        const elements =
            document.querySelectorAll(
                'div, p, span'
            );


        for (const element of elements) {

            const text =
                (
                    element.innerText ||
                    ''
                ).trim();


            if (
                text.includes(
                    'ChatGPT is AI. By using it, you agree'
                ) &&
                text.includes(
                    'Terms & Privacy Policy'
                )
            ) {

                const rect =
                    element.getBoundingClientRect();


                if (
                    rect.height < 150 &&
                    rect.width > 250
                ) {

                    element.classList.add(
                        'ai-interview-hidden-terms'
                    );
                }
            }
        }
    }


    // =========================================================
    // SET QUESTION INTO CHATGPT INPUT
    //
    // IMPORTANT:
    // This ONLY puts text into the textbox.
    // It DOES NOT press Send.
    // =========================================================

    function setQuestion(question) {

        if (
            question === null ||
            question === undefined
        ) {
            return false;
        }


        const editor =
            findEditor();


        if (!editor)
            return false;


        const text =
            String(question);


        // Store latest question so that the newly-created
        // user message can be identified if needed.

        window.aiInterviewAssistant.lastQuestion =
            text;


        // -----------------------------------------------------
        // TEXTAREA
        // -----------------------------------------------------

        if (
            editor.tagName &&
            editor.tagName.toLowerCase() ===
            'textarea'
        ) {

            const descriptor =
                Object.getOwnPropertyDescriptor(
                    HTMLTextAreaElement.prototype,
                    'value'
                );


            if (
                descriptor &&
                descriptor.set
            ) {

                descriptor.set.call(
                    editor,
                    text
                );

            }
            else {

                editor.value =
                    text;
            }


            editor.dispatchEvent(
                new Event(
                    'input',
                    {
                        bubbles: true
                    }
                )
            );


            editor.dispatchEvent(
                new Event(
                    'change',
                    {
                        bubbles: true
                    }
                )
            );


            return true;
        }


        // -----------------------------------------------------
        // CONTENTEDITABLE
        // -----------------------------------------------------

        if (
            editor.isContentEditable ||
            editor.getAttribute(
                'contenteditable'
            ) === 'true'
        ) {

            editor.focus();


            // Clear existing content

            editor.innerHTML =
                '';


            // Insert text safely

            const textNode =
                document.createTextNode(
                    text
                );


            editor.appendChild(
                textNode
            );


            // Notify React / ChatGPT

            editor.dispatchEvent(
                new InputEvent(
                    'input',
                    {
                        bubbles: true,
                        inputType: 'insertText',
                        data: text
                    }
                )
            );


            editor.dispatchEvent(
                new Event(
                    'change',
                    {
                        bubbles: true
                    }
                )
            );


            return true;
        }


        return false;
    }


    // =========================================================
    // FIND SCROLLABLE PARENT
    // =========================================================

    function findScrollableParent(element) {

        let current =
            element.parentElement;


        while (current) {

            try {

                const style =
                    window.getComputedStyle(
                        current
                    );


                const overflowY =
                    style.overflowY;


                if (
                    (
                        overflowY === 'auto' ||
                        overflowY === 'scroll' ||
                        overflowY === 'overlay'
                    ) &&
                    current.scrollHeight >
                    current.clientHeight
                ) {

                    return current;
                }

            }
            catch (e) {
            }


            current =
                current.parentElement;
        }


        return null;
    }

    // =========================================================
    // SCROLL CHATGPT CONVERSATION
    //
    // direction:
    //   -1 = UP
    //    1 = DOWN
    // =========================================================

    function scrollChat(direction) {

        try {

            if (direction !== -1 && direction !== 1)
                return;


            // -----------------------------------------------------
            // ChatGPT's conversation viewport
            // -----------------------------------------------------

            let viewport =
                document.querySelector(
                    '.wm-app-threadViewport'
                );


            // -----------------------------------------------------
            // If selector changed, locate the largest visible
            // scrollable element.
            // -----------------------------------------------------

            if (
                !viewport ||
                viewport.scrollHeight <=
                viewport.clientHeight + 2
            ) {

                const elements =
                    document.querySelectorAll(
                        'main div, [role="main"] div'
                    );


                let best = null;
                let bestArea = 0;


                for (const element of elements) {

                    const rect =
                        element.getBoundingClientRect();


                    if (
                        rect.width <= 0 ||
                        rect.height <= 0
                    )
                        continue;


                    if (
                        element.scrollHeight <=
                        element.clientHeight + 2
                    )
                        continue;


                    const style =
                        window.getComputedStyle(element);


                    if (
                        style.overflowY !== 'auto' &&
                        style.overflowY !== 'scroll' &&
                        style.overflowY !== 'overlay'
                    )
                        continue;


                    const area =
                        rect.width * rect.height;


                    if (area > bestArea) {

                        bestArea = area;
                        best = element;
                    }
                }


                if (best)
                    viewport = best;
            }


            if (!viewport)
                return;


            // -----------------------------------------------------
            // Scroll amount
            // -----------------------------------------------------

            const amount =
                Math.max(
                    300,
                    Math.floor(
                        viewport.clientHeight * 0.80
                    )
                );


            // -----------------------------------------------------
            // Calculate target position
            // -----------------------------------------------------

            const maxScroll =
                Math.max(
                    0,
                    viewport.scrollHeight -
                    viewport.clientHeight
                );


            let target =
                viewport.scrollTop +
                (direction * amount);


            target =
                Math.max(
                    0,
                    Math.min(
                        target,
                        maxScroll
                    )
                );


            // -----------------------------------------------------
            // Move ChatGPT's actual scrollbar
            // -----------------------------------------------------

            viewport.scrollTo({
                top: target,
                behavior: 'smooth'
            });

        }
        catch (e) {
        }
    }
    // =========================================================
    // SCROLL LATEST USER QUESTION TO TOP
    //
    // TEMPORARILY DISABLED.
    //
    // ChatGPT native scrolling is used so that all previous
    // Q&A remain available and nothing is moved/removed by
    // our script.
    // =========================================================

    function scrollLatestUserMessageToTop() {
        return false;
    }


    function scrollLatestUserMessageToTop_old() {

        try {

            // =====================================================
            // FIND CHATGPT VIEWPORT
            // =====================================================

            const viewport =
                document.querySelector(
                    '.wm-app-threadViewport'
                );


            if (!viewport)
                return false;


            // =====================================================
            // FIND LATEST USER QUESTION
            // =====================================================

            const userMessages =
                document.querySelectorAll(
                    'li._wdUoQG_messageTurn[data-message-role="user"]'
                );


            if (
                !userMessages ||
                userMessages.length === 0
            ) {
                return false;
            }


            const latestUserMessage =
                userMessages[
                userMessages.length - 1
                ];


            if (!latestUserMessage)
                return false;


            // =====================================================
            // FIND THE COMPLETE CONVERSATION CONTENT
            //
            // IMPORTANT:
            // Never select a transformed message/answer element.
            // Q + A + previous Q&A must remain one continuous
            // conversation.
            // =====================================================

            let conversationContent =
                viewport.querySelector(
                    '.wm-app-threadContent'
                );


            // Fallback

            if (!conversationContent) {

                conversationContent =
                    latestUserMessage.closest(
                        '.wm-app-thread'
                    );
            }


            if (!conversationContent)
                return false;


            // =====================================================
            // CURRENT POSITION
            // =====================================================

            const viewportRect =
                viewport.getBoundingClientRect();


            const questionRect =
                latestUserMessage.getBoundingClientRect();


            const offset =
                questionRect.top -
                viewportRect.top;


            // =====================================================
            // QUESTION ALREADY AT TOP
            // =====================================================

            if (
                Math.abs(offset) <= 2
            ) {
                return true;
            }


            // =====================================================
            // READ EXISTING TRANSFORM
            //
            // IMPORTANT:
            // Preserve X / scale / other transform values.
            // Only change Y.
            // =====================================================

            const style =
                window.getComputedStyle(
                    conversationContent
                );


            let matrix =
                null;


            try {

                if (
                    style.transform &&
                    style.transform !== 'none'
                ) {

                    matrix =
                        new DOMMatrix(
                            style.transform
                        );
                }

            }
            catch (e) {

                matrix = null;
            }


            // =====================================================
            // CURRENT Y
            // =====================================================

            const currentY =
                matrix
                    ? matrix.m42
                    : 0;


            // =====================================================
            // MOVE COMPLETE CONVERSATION
            //
            // This moves:
            //
            // Previous Q&A
            //      +
            // Latest Question
            //      +
            // Latest Answer
            //
            // together.
            //
            // Therefore previous Q&A are NOT lost/deleted.
            // They simply move above the visible viewport.
            // =====================================================

            const newY =
                currentY -
                offset;


            // =====================================================
            // PRESERVE EXISTING TRANSFORM
            // =====================================================

            if (matrix) {

                conversationContent.style.setProperty(
                    'transform',
                    'matrix(' +
                    matrix.a + ',' +
                    matrix.b + ',' +
                    matrix.c + ',' +
                    matrix.d + ',' +
                    matrix.e + ',' +
                    newY +
                    ')',
                    'important'
                );

            }
            else {

                conversationContent.style.setProperty(
                    'transform',
                    'translateY(' +
                    newY +
                    'px)',
                    'important'
                );
            }


            return true;

        }
        catch (e) {

            console.log(
                '[AI Interview] Question scroll error:',
                e
            );


            return false;
        }
    }


    // =========================================================
    // WAIT FOR NEW USER MESSAGE AND MOVE IT TO TOP
    // =========================================================

    function moveLatestQuestionToTop_old() {

        let attempts =
            0;


        const maxAttempts =
            30;


        function attempt() {

            attempts++;


            const moved =
                scrollLatestUserMessageToTop();


            if (
                moved ||
                attempts >= maxAttempts
            ) {

                return;
            }


            setTimeout(
                attempt,
                100
            );
        }


        attempt();
    }


    // =========================================================
    // SEND QUESTION
    //
    // IMPORTANT:
    // This function ONLY clicks ChatGPT's Send button.
    // =========================================================

    function sendQuestion() {

        // -----------------------------------------------------
        // 1. Try known Send button selectors
        // -----------------------------------------------------

        let sendButton =
            document.querySelector(
                'button[data-testid="send-button"]'
            );


        if (!sendButton) {

            sendButton =
                document.querySelector(
                    'button[aria-label*="Send prompt" i]'
                );
        }


        if (!sendButton) {

            sendButton =
                document.querySelector(
                    'button[aria-label*="Send message" i]'
                );
        }


        // -----------------------------------------------------
        // 2. Search visible buttons
        // -----------------------------------------------------

        if (!sendButton) {

            const buttons =
                document.querySelectorAll(
                    'button'
                );


            for (
                const button of buttons
            ) {

                const aria =
                    (
                        button.getAttribute(
                            'aria-label'
                        ) ||
                        ''
                    )
                        .trim()
                        .toLowerCase();


                const title =
                    (
                        button.getAttribute(
                            'title'
                        ) ||
                        ''
                    )
                        .trim()
                        .toLowerCase();


                const text =
                    (
                        button.innerText ||
                        button.textContent ||
                        ''
                    )
                        .trim()
                        .toLowerCase();


                const value =
                    aria +
                    ' ' +
                    title +
                    ' ' +
                    text;


                if (
                    value.includes(
                        'send prompt'
                    ) ||
                    value.includes(
                        'send message'
                    )
                ) {

                    sendButton =
                        button;

                    break;
                }
            }
        }


        // -----------------------------------------------------
        // 3. Validate button
        // -----------------------------------------------------

        if (!sendButton)
            return false;


        if (sendButton.disabled)
            return false;


        // -----------------------------------------------------
        // 4. Click Send
        // -----------------------------------------------------

        sendButton.click();


        // -----------------------------------------------------
        // 5. ChatGPT handles its own native scrolling.
        // -----------------------------------------------------

        return true;
    }


    // =========================================================
    // EXPOSE FUNCTIONS TO C#
    // =========================================================

    window.aiInterviewAssistant =
        window.aiInterviewAssistant || {};


    window.aiInterviewAssistant.setQuestion =
        setQuestion;


    window.aiInterviewAssistant.sendQuestion =
        sendQuestion;


    window.aiInterviewAssistant.scrollChat =
        scrollChat;


    // =========================================================
    // APPLY CLEANUP
    // =========================================================

    function applyCleanup() {

        try {
            injectStyle();
        }
        catch (e) {
        }

        try {
            hideChatGPTDisclaimer();
        }
        catch (e) {
        }


        // =====================================================
        // HIDE CHATGPT COMPOSER BY DEFAULT
        //
        // Composer is controlled from C# through the existing
        // Message Mode functionality.
        // =====================================================

        try {
            hideComposer();
        }
        catch (e) {
        }


        try {
            hideFeatureMenus();
        }
        catch (e) {
        }


        try {
            hideWelcomeScreen();
        }
        catch (e) {
        }


        try {
            hideTermsNotice();
        }
        catch (e) {
        }

        try {
            hideChatGPTNativeComposer();
        }
        catch (e) {
        }
    }


    // =========================================================
    // INITIAL LOAD
    // =========================================================

    function start() {

        applyCleanup();


        setTimeout(
            applyCleanup,
            300
        );


        setTimeout(
            applyCleanup,
            1000
        );


        setTimeout(
            applyCleanup,
            2000
        );


        setTimeout(
            applyCleanup,
            4000
        );
    }


    if (
        document.readyState ===
        'loading'
    ) {

        document.addEventListener(
            'DOMContentLoaded',
            start
        );

    }
    else {

        start();
    }


    // =========================================================
    // MONITOR CHATGPT SPA DOM CHANGES
    // =========================================================

    let timer =
        null;


    const observer =
        new MutationObserver(
            function () {

                if (timer)
                    return;


                timer =
                    setTimeout(
                        function () {

                            timer = null;

                            applyCleanup();

                        },
                        100
                    );
            }
        );


    function observe() {

        if (!document.body)
            return;


        observer.observe(
            document.body,
            {
                childList: true,
                subtree: true
            }
        );
    }


    if (document.body) {

        observe();

    }
    else {

        const bodyObserver =
            new MutationObserver(
                function () {

                    if (document.body) {

                        bodyObserver.disconnect();

                        observe();
                    }

                }
            );


        bodyObserver.observe(
            document.documentElement,
            {
                childList: true,
                subtree: true
            }
        );
    }

})();