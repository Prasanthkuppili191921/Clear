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
    // TOGGLE CHATGPT VOICE
    //
    // First click:
    //     Start Voice
    //
    // Second click:
    //     Stop Voice
    // =========================================================

    function toggleVoice() {

        let voiceButton =
            document.querySelector(
                'button[aria-label="Start Voice"]'
            );

        if (voiceButton) {
            console.log(
                '[AI Interview] Clicking ChatGPT Start Voice'
            );

            voiceButton.click();
            return true;
        }

        voiceButton =
            document.querySelector(
                'button[aria-label="Stop Voice"]'
            );

        if (voiceButton) {
            console.log(
                '[AI Interview] Clicking ChatGPT Stop Voice'
            );

            voiceButton.click();
            return true;
        }

        console.log(
            '[AI Interview] ChatGPT Voice button not found'
        );

        return false;
    }

    window.aiInterviewAssistantModules.findEditor =
        findEditor;

    window.aiInterviewAssistantModules.hideComposer =
        hideComposer;

    window.aiInterviewAssistantModules.setQuestion =
        setQuestion;

    window.aiInterviewAssistantModules.toggleVoice =
        toggleVoice;

})();