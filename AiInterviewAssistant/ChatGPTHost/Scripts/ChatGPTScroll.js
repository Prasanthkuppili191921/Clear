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
                    window.getComputedStyle(current);

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
    // FIND CHATGPT ACTUAL SCROLL CONTAINER
    //
    // No hard-coded ChatGPT class/id.
    // Finds the largest visible vertical scroll container.
    // =========================================================

    function findChatScrollContainer() {

        try {

            const candidates =
                [...document.querySelectorAll('*')]
                    .filter(element => {

                        try {

                            const style =
                                window.getComputedStyle(
                                    element
                                );

                            const rect =
                                element.getBoundingClientRect();

                            return (
                                element.scrollHeight >
                                element.clientHeight &&

                                element.clientHeight > 100 &&

                                rect.height > 100 &&

                                rect.bottom > 0 &&
                                rect.top < window.innerHeight &&

                                (
                                    style.overflowY === 'auto' ||
                                    style.overflowY === 'scroll' ||
                                    style.overflowY === 'overlay'
                                )
                            );

                        }
                        catch (e) {

                            return false;
                        }
                    });


            if (!candidates.length)
                return null;


            // Prefer the container with the largest
            // actual scrollable area.

            candidates.sort(
                (a, b) => {

                    const aScrollable =
                        a.scrollHeight -
                        a.clientHeight;

                    const bScrollable =
                        b.scrollHeight -
                        b.clientHeight;

                    return bScrollable -
                        aScrollable;
                }
            );


            return candidates[0];

        }
        catch (e) {

            console.log(
                '[AI Interview] Chat scroll container detection error:',
                e
            );

            return null;
        }
    }


    // =========================================================
    // SCROLL LATEST USER QUESTION TO TOP
    // =========================================================

    function scrollLatestUserMessageToTop() {

        try {

            const viewport =
                findChatScrollContainer();

            if (!viewport)
                return false;


            const userMessages =
                document.querySelectorAll(
                    'li._wdUoQG_messageTurn[data-message-role="user"]'
                );

            if (!userMessages.length)
                return false;


            const latestUserMessage =
                userMessages[
                userMessages.length - 1
                ];

            if (!latestUserMessage)
                return false;


            // -------------------------------------------------
            // Remove old transform
            // -------------------------------------------------

            const conversationContent =
                latestUserMessage.closest(
                    '[class*="threadContent"]'
                );

            if (conversationContent) {

                conversationContent.style.removeProperty(
                    'transform'
                );

                conversationContent.style.removeProperty(
                    'translate'
                );
            }


            // -------------------------------------------------
            // Create spacer
            // -------------------------------------------------

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

                spacer.style.width =
                    '1px';

                spacer.style.pointerEvents =
                    'none';

                spacer.style.flexShrink =
                    '0';


                const content =
                    conversationContent ||
                    latestUserMessage.parentElement;


                if (content) {

                    content.appendChild(
                        spacer
                    );
                }
            }
            else {

                spacer.style.height =
                    viewport.clientHeight + 'px';
            }


            // -------------------------------------------------
            // Calculate question position
            // -------------------------------------------------

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


            // -------------------------------------------------
            // Scroll
            // -------------------------------------------------

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
    // WAIT FOR NEW USER MESSAGE
    // =========================================================

    function moveLatestQuestionToTop() {

        let attempts = 0;

        const maxAttempts = 30;


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
    // -1 = UP
    //  1 = DOWN
    // =========================================================

    function scrollChat(direction) {

        try {

            if (
                direction !== -1 &&
                direction !== 1
            ) {
                return false;
            }

            const viewport =
                findChatScrollContainer();

            if (!viewport)
                return false;

            const distance = 120;

            const start =
                viewport.scrollTop;

            const maxScroll =
                viewport.scrollHeight -
                viewport.clientHeight;

            const target =
                Math.max(
                    0,
                    Math.min(
                        maxScroll,
                        start + (direction * distance)
                    )
                );

            const duration = 350;

            const startTime =
                performance.now();


            function animate(currentTime) {

                const elapsed =
                    currentTime - startTime;

                const progress =
                    Math.min(
                        elapsed / duration,
                        1
                    );


                // Ease-in-out
                const eased =
                    progress < 0.5
                        ? 2 * progress * progress
                        : 1 -
                        Math.pow(
                            -2 * progress + 2,
                            2
                        ) / 2;


                viewport.scrollTop =
                    start +
                    (target - start) *
                    eased;


                if (progress < 1) {

                    requestAnimationFrame(
                        animate
                    );
                }
            }


            requestAnimationFrame(
                animate
            );

            return true;

        }
        catch (e) {

            console.log(
                '[AI Interview] Smooth scroll error:',
                e
            );

            return false;
        }
    }


    // =========================================================
    // ALT + UP / DOWN
    // =========================================================

    function handleChatGPTKeyboardScroll(event) {

        try {

            if (!event.altKey)
                return;


            if (event.key === 'ArrowUp') {

                event.preventDefault();
                event.stopPropagation();

                scrollChat(-1);

                return;
            }


            if (event.key === 'ArrowDown') {

                event.preventDefault();
                event.stopPropagation();

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
    // SINGLE KEYBOARD LISTENER
    // =========================================================

    window.addEventListener(
        'keydown',
        handleChatGPTKeyboardScroll,
        true
    );


    // =========================================================
    // EXPORT MODULES
    // =========================================================

    window.aiInterviewAssistantModules.findScrollableParent =
        findScrollableParent;

    window.aiInterviewAssistantModules.findChatScrollContainer =
        findChatScrollContainer;

    window.aiInterviewAssistantModules.scrollLatestUserMessageToTop =
        scrollLatestUserMessageToTop;

    window.aiInterviewAssistantModules.moveLatestQuestionToTop =
        moveLatestQuestionToTop;

    window.aiInterviewAssistantModules.scrollChat =
        scrollChat;


})();