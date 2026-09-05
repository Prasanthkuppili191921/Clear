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


            let conversationContent =
                viewport.querySelector(
                    '.wm-app-threadContent'
                );


            if (!conversationContent) {

                conversationContent =
                    latestUserMessage.closest(
                        '.wm-app-thread'
                    );
            }

            if (!conversationContent)
                return false;


            const viewportRect =
                viewport.getBoundingClientRect();

            const questionRect =
                latestUserMessage.getBoundingClientRect();

            const offset =
                questionRect.top -
                viewportRect.top;


            if (
                Math.abs(offset) <= 2
            ) {
                return true;
            }


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


            const currentY =
                matrix
                    ? matrix.m42
                    : 0;


            const newY =
                currentY -
                offset;


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
                document.querySelector(
                    '[data-scroll-root]'
                );

            if (!viewport)
                return false;

            const SCROLL_DISTANCE = 80;

            const maxScrollTop =
                viewport.scrollHeight -
                viewport.clientHeight;

            const targetScrollTop =
                Math.max(
                    0,
                    Math.min(
                        viewport.scrollTop +
                        (direction * SCROLL_DISTANCE),
                        maxScrollTop
                    )
                );

            viewport.scrollTo({
                top: targetScrollTop,
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

    window.aiInterviewAssistantModules.findScrollableParent =
        findScrollableParent;

    window.aiInterviewAssistantModules.scrollLatestUserMessageToTop =
        scrollLatestUserMessageToTop;

    window.aiInterviewAssistantModules.moveLatestQuestionToTop =
        moveLatestQuestionToTop;

    window.aiInterviewAssistantModules.scrollChat =
        scrollChat;

})();