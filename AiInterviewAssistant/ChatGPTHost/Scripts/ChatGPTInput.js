(function () {

    window.aiInterviewAssistantModules =
        window.aiInterviewAssistantModules || {};

    // =========================================================
    // FIND CHATGPT INPUT
    // =========================================================

    function findEditor() {

        let editor =
            document.querySelector(
                '#prompt-textarea'
            );

        if (editor)
            return editor;

        editor =
            document.querySelector(
                'textarea'
            );

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

        const editor =
            findEditor();

        if (!editor)
            return;

        let composer = null;

        composer =
            editor.closest('form');

        if (!composer) {

            composer =
                editor.closest(
                    '[data-testid="composer"]'
                );
        }

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

        if (composer) {

            console.log('[AI Interview] COMPOSER TARGET:', composer);
            console.log('[AI Interview] TAG:', composer.tagName);
            console.log('[AI Interview] ID:', composer.id);
            console.log('[AI Interview] CLASS:', composer.className);
            console.log('[AI Interview] TESTID:', composer.getAttribute('data-testid'));
            console.log('[AI Interview] OUTER:', composer.outerHTML.slice(0, 2000));

            composer.classList.add(
                'ai-interview-hidden-composer'
            );
        }
    }


    // =========================================================
    // SET QUESTION INTO CHATGPT INPUT
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

        window.aiInterviewAssistant.lastQuestion =
            text;


        // =====================================================
        // TEXTAREA
        // =====================================================

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


        // =====================================================
        // CONTENTEDITABLE
        // =====================================================

        if (
            editor.isContentEditable ||
            editor.getAttribute(
                'contenteditable'
            ) === 'true'
        ) {

            editor.focus();

            editor.innerHTML =
                '';

            const textNode =
                document.createTextNode(
                    text
                );

            editor.appendChild(
                textNode
            );

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
    // START NEW CHAT
    // =========================================================

    function startNewChat() {

        const selectors = [
            'a[data-testid="create-new-chat-button"]',
            'a[href="https://chatgpt.com/"]'
        ];

        for (const selector of selectors) {

            const newChatButton =
                document.querySelector(selector);

            if (!newChatButton)
                continue;

            console.log(
                '[AI Interview] Clicking ChatGPT New Chat:',
                selector
            );

            newChatButton.click();
            return true;
        }

        console.log(
            '[AI Interview] ChatGPT New Chat button not found'
        );

        return false;
    }

    // =========================================================
    // START CHATGPT VOICE IF NOT ALREADY ACTIVE
    //
    // AUTO VOICE ONLY
    //
    // If voice is already active:
    //     DO NOTHING
    //
    // If voice is not active:
    //     START VOICE
    // =========================================================

    async function startVoiceIfNotActive() {

        console.log(
            '[AI Interview Assistant] startVoiceIfNotActive START'
        );

        // ---------------------------------------------------------
        // ACTIVE VOICE SELECTORS
        // ---------------------------------------------------------

        const activeSelectors = [
            'button[aria-label="Stop dictation"]',
            'button[aria-label="Submit dictation"]',
            'button[aria-label="Cancel dictation"]'
        ];

        // ---------------------------------------------------------
        // START VOICE SELECTORS
        // ---------------------------------------------------------

        const startSelectors = [
            'button[aria-label="Start dictation"]',
            'button[aria-label="Dictate button"]'
        ];

        // ---------------------------------------------------------
        // STEP 1
        // Check whether voice is already active
        // ---------------------------------------------------------

        for (const selector of activeSelectors) {

            const activeButton =
                document.querySelector(selector);

            if (activeButton) {

                console.log(
                    '[AI Interview Assistant] Voice already ACTIVE'
                );

                return true;
            }
        }

        // ---------------------------------------------------------
        // STEP 2
        // Find START button
        // ---------------------------------------------------------

        let startButton = null;

        for (const selector of startSelectors) {

            const button =
                document.querySelector(selector);

            if (!button)
                continue;

            if (button.disabled)
                continue;

            if (
                button.getAttribute('aria-disabled') === 'true'
            )
                continue;

            startButton = button;
            break;
        }

        // ---------------------------------------------------------
        // STEP 3
        // Start button not found
        // ---------------------------------------------------------

        if (!startButton) {

            console.log(
                '[AI Interview Assistant] AUTO START: ' +
                'Start dictation button not found'
            );

            return false;
        }

        // ---------------------------------------------------------
        // STEP 4
        // Click START
        // ---------------------------------------------------------

        try {

            startButton.focus();

            await new Promise(resolve =>
                setTimeout(resolve, 50)
            );

            console.log(
                '[AI Interview Assistant] AUTO START: ' +
                'Starting ChatGPT dictation'
            );

            startButton.click();

        }
        catch (error) {

            console.error(
                '[AI Interview Assistant] AUTO START: ' +
                'Failed to start dictation',
                error
            );

            return false;
        }

        // ---------------------------------------------------------
        // STEP 5
        // Verify recording state
        // ---------------------------------------------------------

        for (let i = 0; i < 20; i++) {

            for (const selector of activeSelectors) {

                if (
                    document.querySelector(selector)
                ) {

                    console.log(
                        '[AI Interview Assistant] AUTO START: ' +
                        'ChatGPT dictation ACTIVE'
                    );

                    return true;
                }
            }

            await new Promise(resolve =>
                setTimeout(resolve, 100)
            );
        }

        // ---------------------------------------------------------
        // Click happened but state could not be verified
        // ---------------------------------------------------------

        console.log(
            '[AI Interview Assistant] AUTO START: ' +
            'Dictation state could not be verified'
        );

        return true;
    }

    // =========================================================
    // TOGGLE CHATGPT VOICE DICTATION
    // =========================================================

    async function toggleVoice() {

        console.log(
            '[AI Interview Assistant] toggleVoice START'
        );

        // ---------------------------------------------------------
        // Find the current ChatGPT voice button
        // ---------------------------------------------------------

        const startSelectors = [
            'button[aria-label="Start dictation"]',
            'button[aria-label="Dictate button"]'
        ];

        const activeSelectors = [
            'button[aria-label="Stop dictation"]',
            'button[aria-label="Submit dictation"]',
            'button[aria-label="Cancel dictation"]'
        ];

        // ---------------------------------------------------------
        // STEP 1
        // Check whether ChatGPT is already recording
        // ---------------------------------------------------------

        let activeButton = null;

        for (const selector of activeSelectors) {

            const button = document.querySelector(selector);

            if (button) {
                activeButton = button;
                break;
            }
        }

        // ---------------------------------------------------------
        // If recording is already active, stop it
        // ---------------------------------------------------------

        if (activeButton) {

            console.log(
                '[AI Interview Assistant] Voice already active. ' +
                'Stopping dictation.'
            );

            try {
                activeButton.click();

                console.log(
                    '[AI Interview Assistant] Active voice button clicked'
                );

                return true;
            }
            catch (error) {

                console.error(
                    '[AI Interview Assistant] Failed to stop dictation',
                    error
                );

                return false;
            }
        }

        // ---------------------------------------------------------
        // STEP 2
        // Find ONLY the actual START dictation button
        // ---------------------------------------------------------

        let startButton = null;

        for (const selector of startSelectors) {

            const button = document.querySelector(selector);

            if (!button)
                continue;

            // Ignore disabled buttons
            if (button.disabled)
                continue;

            if (
                button.getAttribute('aria-disabled') === 'true'
            )
                continue;

            startButton = button;
            break;
        }

        // ---------------------------------------------------------
        // No start button
        // ---------------------------------------------------------

        if (!startButton) {

            console.log(
                '[AI Interview Assistant] Start dictation button not found'
            );

            return false;
        }

        // ---------------------------------------------------------
        // STEP 3
        // Focus the button before clicking
        // This makes programmatic click behave closer to
        // normal ChatGPT UI interaction.
        // ---------------------------------------------------------

        try {

            startButton.focus();

            await new Promise(resolve =>
                setTimeout(resolve, 50)
            );

            console.log(
                '[AI Interview Assistant] Starting ChatGPT dictation'
            );

            startButton.click();

        }
        catch (error) {

            console.error(
                '[AI Interview Assistant] Failed to start dictation',
                error
            );

            return false;
        }

        // ---------------------------------------------------------
        // STEP 4
        // Verify that ChatGPT actually entered recording state
        // ---------------------------------------------------------

        for (let i = 0; i < 20; i++) {

            let recording = false;

            for (const selector of activeSelectors) {

                if (document.querySelector(selector)) {

                    recording = true;
                    break;
                }
            }

            if (recording) {

                console.log(
                    '[AI Interview Assistant] ChatGPT dictation ACTIVE'
                );

                return true;
            }

            await new Promise(resolve =>
                setTimeout(resolve, 100)
            );
        }

        // ---------------------------------------------------------
        // Button was clicked but active state was not detected
        // ---------------------------------------------------------

        console.log(
            '[AI Interview Assistant] Dictation start state ' +
            'could not be verified'
        );

        return true;
    }

    // =========================================================
    // STOP DICTATION + WAIT FOR FINAL TEXT + SEND
    // =========================================================

    async function stopDictationAndSend() {

        console.log(
            '[AI Interview Assistant] stopDictationAndSend START'
        );

        // ---------------------------------------------------------
        // STEP 1: Find current dictation stop button
        // ---------------------------------------------------------

        const stopSelectors = [
            'button[aria-label="Stop dictation"]',
            'button[aria-label="Submit dictation"]'
        ];

        let stopButton = null;

        for (const selector of stopSelectors) {

            const button = document.querySelector(selector);

            if (button) {
                stopButton = button;
                break;
            }
        }

        // ---------------------------------------------------------
        // STEP 2: Stop current dictation
        // ---------------------------------------------------------

        if (stopButton) {

            console.log(
                '[AI Interview Assistant] Clicking dictation stop button'
            );

            try {
                stopButton.click();
            }
            catch (error) {

                console.error(
                    '[AI Interview Assistant] Stop button click failed',
                    error
                );

                return false;
            }

        }
        else {

            console.log(
                '[AI Interview Assistant] Stop button not found'
            );
        }

        // ---------------------------------------------------------
        // STEP 3: Wait until dictation stop button disappears
        // ---------------------------------------------------------

        let stopButtonGone = false;

        for (let i = 0; i < 40; i++) {

            let stillPresent = false;

            for (const selector of stopSelectors) {

                if (document.querySelector(selector)) {
                    stillPresent = true;
                    break;
                }
            }

            if (!stillPresent) {

                stopButtonGone = true;

                console.log(
                    '[AI Interview Assistant] Dictation stopped'
                );

                break;
            }

            await new Promise(resolve =>
                setTimeout(resolve, 100)
            );
        }

        if (!stopButtonGone) {

            console.log(
                '[AI Interview Assistant] Stop button did not disappear'
            );
        }

        // ---------------------------------------------------------
        // STEP 4: Get editor
        // ---------------------------------------------------------

        let finalText = '';
        let lastText = '';
        let stableCount = 0;

        // ---------------------------------------------------------
        // STEP 5: Wait for final dictation text
        // ---------------------------------------------------------

        for (let i = 0; i < 60; i++) {

            const editor = findEditor();

            if (editor) {

                let text = '';

                if (editor.tagName === 'TEXTAREA') {
                    text = editor.value || '';
                }
                else {
                    text =
                        editor.innerText ||
                        editor.textContent ||
                        '';
                }

                text = text.trim();

                if (text.length > 0) {

                    console.log(
                        '[AI Interview Assistant] Dictation text:',
                        text
                    );

                    if (text === lastText) {

                        stableCount++;

                    }
                    else {

                        lastText = text;
                        stableCount = 0;
                    }

                    finalText = text;

                    // -------------------------------------------------
                    // Text remained unchanged for 3 consecutive checks
                    // -------------------------------------------------

                    if (stableCount >= 2) {

                        console.log(
                            '[AI Interview Assistant] Final dictation text:',
                            finalText
                        );

                        break;
                    }
                }
            }

            await new Promise(resolve =>
                setTimeout(resolve, 150)
            );
        }

        // ---------------------------------------------------------
        // STEP 6: Make sure we actually have text
        // ---------------------------------------------------------

        if (!finalText) {

            console.log(
                '[AI Interview Assistant] No final dictation text found'
            );

            return false;
        }

        // ---------------------------------------------------------
        // STEP 7: Wait for Send button to become available
        // ---------------------------------------------------------

        let sendButton = null;

        for (let i = 0; i < 30; i++) {

            sendButton =
                document.querySelector(
                    'button[data-testid="send-button"]'
                );

            if (!sendButton) {

                const buttons =
                    document.querySelectorAll('button');

                for (const button of buttons) {

                    const aria =
                        button.getAttribute('aria-label') || '';

                    const title =
                        button.getAttribute('title') || '';

                    const text =
                        button.innerText ||
                        button.textContent ||
                        '';

                    const combined =
                        `${aria} ${title} ${text}`.toLowerCase();

                    if (
                        combined.includes('send prompt') ||
                        combined.includes('send message')
                    ) {

                        sendButton = button;
                        break;
                    }
                }
            }

            // -----------------------------------------------------
            // Send button found and enabled
            // -----------------------------------------------------

            if (
                sendButton &&
                !sendButton.disabled &&
                sendButton.getAttribute('aria-disabled') !== 'true'
            ) {

                console.log(
                    '[AI Interview Assistant] Send button ready'
                );

                break;
            }

            sendButton = null;

            await new Promise(resolve =>
                setTimeout(resolve, 150)
            );
        }

        // ---------------------------------------------------------
        // STEP 8: Send
        // ---------------------------------------------------------

        if (!sendButton) {

            console.log(
                '[AI Interview Assistant] Send button not ready'
            );

            return false;
        }

        // ---------------------------------------------------------
        // IMPORTANT:
        // Use existing sendQuestion() so the normal ChatGPTView
        // send flow remains unchanged.
        // ---------------------------------------------------------

        if (
            window.aiInterviewAssistant &&
            typeof window.aiInterviewAssistant.sendQuestion ===
            'function'
        ) {

            console.log(
                '[AI Interview Assistant] Calling sendQuestion()'
            );

            const result =
                window.aiInterviewAssistant.sendQuestion();

            console.log(
                '[AI Interview Assistant] sendQuestion result:',
                result
            );

            return result !== false;
        }

        console.log(
            '[AI Interview Assistant] sendQuestion() not available'
        );

        return false;
    }

    window.aiInterviewAssistantModules.toggleVoice = toggleVoice;

    window.aiInterviewAssistantModules.findEditor =
        findEditor;

    window.aiInterviewAssistantModules.hideComposer =
        hideComposer;

    window.aiInterviewAssistantModules.setQuestion =
        setQuestion;

    window.aiInterviewAssistantModules.startNewChat =
        startNewChat;

    window.aiInterviewAssistantModules.toggleVoice =
        toggleVoice;

    window.aiInterviewAssistantModules.stopDictationAndSend =
        stopDictationAndSend;

    window.aiInterviewAssistantModules.startVoiceIfNotActive =
        startVoiceIfNotActive;


})();