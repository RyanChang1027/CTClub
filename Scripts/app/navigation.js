(function (window, document) {
    "use strict";

    function initializeNavigation() {
        var nav = document.querySelector(".main-nav");

        if (!nav) {
            return;
        }

        if (nav.getAttribute("data-initialized") === "true") {
            return;
        }

        nav.setAttribute("data-initialized", "true");

        var toggle =
            nav.querySelector(".nav-toggle");

        var links =
            nav.querySelector(".nav-links");

        if (!toggle || !links) {
            return;
        }

        toggle.addEventListener("click", function () {
            var isOpen =
                links.classList.toggle("is-open");

            toggle.setAttribute(
                "aria-expanded",
                isOpen ? "true" : "false");

            updateToggleIcon(toggle, isOpen);
        });

        var navLinks =
            links.querySelectorAll("a");

        for (var i = 0; i < navLinks.length; i++) {
            navLinks[i].addEventListener(
                "click",
                function () {
                    closeNavigation(toggle, links);
                });
        }

        window.addEventListener(
            "resize",
            function () {
                if (window.innerWidth > 1050) {
                    closeNavigation(toggle, links);
                }
            });
    }

    function closeNavigation(toggle, links) {
        links.classList.remove("is-open");

        toggle.setAttribute(
            "aria-expanded",
            "false");

        updateToggleIcon(toggle, false);
    }

    function updateToggleIcon(toggle, isOpen) {
        var icon =
            toggle.querySelector(".nav-toggle__icon");

        if (icon) {
            icon.textContent = isOpen ? "×" : "☰";
        }
    }

    document.addEventListener(
        "DOMContentLoaded",
        initializeNavigation);

})(window, document);
