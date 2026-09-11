(function () {

    window.aiInterviewAssistantModules =
        window.aiInterviewAssistantModules || {};

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
    // SCROLL LATEST USER QUESTION TO TOP
    // =========================================================

    function scrollLatestUserMessageToTop() {

        try {

            const viewport =
                document.querySelector(
                    '.wm-app-threadViewport'
                );

            if (!viewport)
                return false;

            const userMessages =
                document.querySelectorAll(
                    'li._wdUoQG_messageTurn[data-message-role="user"]'
                );

            if (!userMessages.length)
                return false;

            const latestUserMessage =
                userMessages[userMessages.length - 1];

            if (!latestUserMessage)
                return false;


            // Remove any old transform that was previously
            // applied to the conversation content.
            const conversationContent =
                viewport.querySelector(
                    '.wm-app-threadContent'
                );

            if (conversationContent) {

                conversationContent.style.removeProperty(
                    'transform'
                );

                conversationContent.style.removeProperty(
                    'translate'
                );
            }


            // -----------------------------------------------------
            // Make enough room after the latest question so that
            // the question can actually reach the top.
            // -----------------------------------------------------

            let spacer =
                viewport.querySelector(
                    '[data-ai-interview-scroll-spacer]'
                );

            if (!spacer) {

                spacer =
                    document.createElement('div');

                spacer.setAttribute(
                    'data-ai-interview-scroll-spacer',
                    'true'
                );

                spacer.style.height =
                    viewport.clientHeight + 'px';

                spacer.style.width = '1px';

                spacer.style.pointerEvents =
                    'none';

                spacer.style.flexShrink =
                    '0';

                const content =
                    conversationContent ||
                    latestUserMessage.parentElement;

                if (content) {
                    content.appendChild(spacer);
                }
            }
            else {

                spacer.style.height =
                    viewport.clientHeight + 'px';
            }


            // -----------------------------------------------------
            // Calculate exact position of latest question
            // -----------------------------------------------------

            const viewportRect =
                viewport.getBoundingClientRect();

            const questionRect =
                latestUserMessage.getBoundingClientRect();

            const offset =
                questionRect.top -
                viewportRect.top;


            const targetScrollTop =
                viewport.scrollTop +
                offset;


            // -----------------------------------------------------
            // Move actual ChatGPT scrollbar
            // -----------------------------------------------------

            viewport.scrollTo({
                top: Math.max(
                    0,
                    targetScrollTop
                ),
                behavior: 'smooth'
            });

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

    function moveLatestQuestionToTop() {

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
    // MANUAL CHAT SCROLL
    //
    // direction:
    //   -1 = UP
    //    1 = DOWN
    //
    // IMPORTANT:
    // - Moves the complete conversation content.
    // - Previous Q&A are never removed.
    // - Question + answer remain together.
    // =========================================================

    function scrollChat(direction) {
        try {
            if (direction !== -1 && direction !== 1)
                return false;

            const viewport =
                document.querySelector('.wm-app-threadViewport');

            if (!viewport)
                return false;

            const SCROLL_DISTANCE = 80;

            viewport.scrollBy({
                top: direction * SCROLL_DISTANCE,
                behavior: 'smooth'
            });

            return true;
        }
        catch (e) {
            console.log(
                '[AI Interview] Manual smooth scroll error:',
                e
            );

            return false;
        }
    }


    // =========================================================
    // CHATGPT KEYBOARD SCROLL
    // ALT + UP    = SCROLL UP
    // ALT + DOWN  = SCROLL DOWN
    // =========================================================

    function handleChatGPTKeyboardScroll(event) {

        try {

            if (!event.altKey)
                return;

            if (event.key === 'ArrowUp') {

                event.preventDefault();
                event.stopImmediatePropagation();

                scrollChat(-1);

                return;
            }

            if (event.key === 'ArrowDown') {

                event.preventDefault();
                event.stopImmediatePropagation();

                scrollChat(1);

                return;
            }

        }
        catch (e) {

            console.log(
                '[AI Interview] Keyboard scroll error:',
                e
            );
        }
    }


    // =========================================================
    // WINDOW LEVEL KEYBOARD CAPTURE
    // =========================================================

    window.addEventListener(
        'keydown',
        handleChatGPTKeyboardScroll,
        true
    );


    // =========================================================
    // DOCUMENT LEVEL KEYBOARD CAPTURE
    // =========================================================

    document.addEventListener(
        'keydown',
        handleChatGPTKeyboardScroll,
        true
    );


    // =========================================================
    // EXPORT MODULES
    // =========================================================

    window.aiInterviewAssistantModules.findScrollableParent =
        findScrollableParent;

    window.aiInterviewAssistantModules.scrollLatestUserMessageToTop =
        scrollLatestUserMessageToTop;

    window.aiInterviewAssistantModules.moveLatestQuestionToTop =
        moveLatestQuestionToTop;

    window.aiInterviewAssistantModules.scrollChat =
        scrollChat;

    

})();