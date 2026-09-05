(function () {

    window.aiInterviewAssistant =
        window.aiInterviewAssistant || {};

    window.aiInterviewAssistantModules =
        window.aiInterviewAssistantModules || {};


    const modules =
        window.aiInterviewAssistantModules;


    // =========================================================
    // EXPOSE FUNCTIONS TO C#
    // =========================================================

    window.aiInterviewAssistant.setQuestion =
        modules.setQuestion;

    window.aiInterviewAssistant.sendQuestion =
        modules.sendQuestion;

    window.aiInterviewAssistant.startNewChat =
        modules.startNewChat;

    window.aiInterviewAssistant.scrollChat =
        modules.scrollChat;

    window.aiInterviewAssistant.toggleVoice =
        modules.toggleVoice;

    // =========================================================
    // APPLY CLEANUP
    // =========================================================

    function applyCleanup() {

        if (modules.applyCleanup)
            modules.applyCleanup();
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

                            timer =
                                null;

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


    function waitForBody() {

        if (document.body) {

            observe();

            return;
        }

        setTimeout(
            waitForBody,
            100
        );
    }


    waitForBody();

})();