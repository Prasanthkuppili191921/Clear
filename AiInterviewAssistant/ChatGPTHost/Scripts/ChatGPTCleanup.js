(function () {

    window.aiInterviewAssistantModules =
        window.aiInterviewAssistantModules || {};


    // =========================================================
    // HIDE FEATURE MENU ITEMS
    // =========================================================

    function hideFeatureMenus() {

        const elements =
            document.querySelectorAll(
                'button, [role="menuitem"], [role="option"]'
            );

        elements.forEach(
            function (element) {

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

            }
        );
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

        for (
            const element of elements
        ) {

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
    // APPLY CLEANUP
    // =========================================================

    function applyCleanup() {

        const modules =
            window.aiInterviewAssistantModules;


        try {

            if (modules.injectStyle)
                modules.injectStyle();

        }
        catch (e) {
        }


        try {

            if (modules.hideComposer)
                modules.hideComposer();

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
    }


    window.aiInterviewAssistantModules.hideFeatureMenus =
        hideFeatureMenus;

    window.aiInterviewAssistantModules.hideWelcomeScreen =
        hideWelcomeScreen;

    window.aiInterviewAssistantModules.hideTermsNotice =
        hideTermsNotice;

    window.aiInterviewAssistantModules.applyCleanup =
        applyCleanup;

})();