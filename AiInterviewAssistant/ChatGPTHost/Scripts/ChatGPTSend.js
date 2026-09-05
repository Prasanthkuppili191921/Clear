(function () {

    window.aiInterviewAssistantModules =
        window.aiInterviewAssistantModules || {};

    // =========================================================
    // SEND QUESTION
    // =========================================================

    function sendQuestion() {

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


        // =====================================================
        // SEARCH VISIBLE BUTTONS
        // =====================================================

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


        if (!sendButton)
            return false;

        if (sendButton.disabled)
            return false;


        sendButton.click();


        const moveLatestQuestionToTop =
            window
                .aiInterviewAssistantModules
                .moveLatestQuestionToTop;

        if (moveLatestQuestionToTop)
            moveLatestQuestionToTop();


        return true;
    }


    window.aiInterviewAssistantModules.sendQuestion =
        sendQuestion;

})();