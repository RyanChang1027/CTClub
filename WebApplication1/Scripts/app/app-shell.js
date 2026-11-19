var App = App || {};

App.Shell = (function (window, document) {
    "use strict";

    function init() {
        initSpecificDropdowns();
        initGlobalDropdownHandlers();
        initPrintReceiptHandlers();
        initDeleteHandlers();
        initMobileNavigation();
    }

    /* ============================================================
       1. Specific Dropdowns (Nav & User Profile)
       ============================================================ */
    function initSpecificDropdowns() {
        // User Profile Dropdown
        initDropdown("user-profile-trigger", "user-dropdown-menu");

        // Top Nav "More" Dropdown
        initDropdown("nav-more-trigger", "nav-more-menu");
    }

    function initDropdown(triggerId, menuId) {
        var trigger = document.getElementById(triggerId);
        var menu = document.getElementById(menuId);

        if (!trigger || !menu) return;

        trigger.addEventListener("click", function (e) {
            e.stopPropagation();

            // Check whether this dropdown is currently open
            var isOpen = menu.classList.contains("is-open");

            // Close all other open dropdowns
            closeAllDropdowns();

            // Only reopen if it was previously closed
            if (!isOpen) {
                menu.classList.add("is-open");
                trigger.setAttribute("aria-expanded", "true");
            }
        });
    }

    /* ============================================================
       2. Dynamic Dropdowns (Directory Table Row Actions)
       ============================================================ */
    function initGlobalDropdownHandlers() {
        document.addEventListener("click", function (e) {
            var toggleBtn = e.target.closest("[data-action-toggle]");

            if (toggleBtn) {
                e.preventDefault();
                e.stopPropagation();

                var menu = toggleBtn.nextElementSibling;

                if (!menu || !menu.classList.contains("row-actions-menu")) {
                    return;
                }

                var isOpen = menu.classList.contains("is-open");

                closeAllDropdowns();

                if (!isOpen) {
                    openRowActionsMenu(toggleBtn, menu);
                }

                return;
            }

            var clickedInsideNav = e.target.closest(".nav-dropdown");
            var clickedInsideUser = e.target.closest(".user-dropdown");
            var clickedInsideRowMenu = e.target.closest(".row-actions-menu");

            if (!clickedInsideNav && !clickedInsideUser && !clickedInsideRowMenu) {
                closeAllDropdowns();
            }
        });

        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape") {
                closeAllDropdowns();
            }
        });

        // Close the menu if the page or a scrollable container moves.
        window.addEventListener("scroll", closeAllDropdowns, true);
        window.addEventListener("resize", closeAllDropdowns);
    }

    /* ============================================================
       3. Helper: Close all menus
       ============================================================ */
    function closeAllDropdowns() {
        var openMenus = document.querySelectorAll(
            ".dropdown-menu.is-open, .row-actions-menu.is-open"
        );

        for (var i = 0; i < openMenus.length; i++) {
            var menu = openMenus[i];

            menu.classList.remove("is-open");

            // Restore row menus to their original table position.
            if (menu.classList.contains("row-actions-menu") && menu._originalParent) {
                if (menu._originalNextSibling &&
                    menu._originalNextSibling.parentNode === menu._originalParent) {
                    menu._originalParent.insertBefore(menu, menu._originalNextSibling);
                } else {
                    menu._originalParent.appendChild(menu);
                }

                menu._originalParent = null;
                menu._originalNextSibling = null;

                menu.style.position = "";
                menu.style.top = "";
                menu.style.left = "";
                menu.style.right = "";
                menu.style.bottom = "";
                menu.style.zIndex = "";
            }

            var trigger = menu.previousElementSibling;

            if (trigger && trigger.tagName === "BUTTON") {
                trigger.setAttribute("aria-expanded", "false");
            }
        }
    }

    function openRowActionsMenu(toggleBtn, menu) {
        // Remember where the menu originally belongs.
        menu._originalParent = menu.parentNode;
        menu._originalNextSibling = menu.nextSibling;

        // Move it outside the table's scrollable container.
        document.body.appendChild(menu);

        menu.classList.add("is-open");

        // Position the menu relative to the viewport.
        menu.style.position = "fixed";
        menu.style.top = "0px";
        menu.style.left = "0px";
        menu.style.right = "auto";
        menu.style.bottom = "auto";
        menu.style.zIndex = "10000";

        var buttonRect = toggleBtn.getBoundingClientRect();
        var menuRect = menu.getBoundingClientRect();

        var spaceBelow = window.innerHeight - buttonRect.bottom;
        var spaceAbove = buttonRect.top;

        // Open upwards when there isn't enough room below.
        var openAbove = spaceBelow < menuRect.height + 8 &&
            spaceAbove > spaceBelow;

        var top = openAbove
            ? buttonRect.top - menuRect.height - 4
            : buttonRect.bottom + 4;

        // Keep the menu inside the horizontal viewport.
        var left = Math.min(
            buttonRect.right - menuRect.width,
            window.innerWidth - menuRect.width - 8
        );

        left = Math.max(8, left);

        menu.style.top = top + "px";
        menu.style.left = left + "px";
    }

    /* ============================================================
      4. Print Receipt Logic (Global)
      ============================================================ */
    function initPrintReceiptHandlers() {
        document.addEventListener("click", function (e) {
            var printBtn = e.target.closest(".btn-print-receipt");
            if (!printBtn) return;

            e.preventDefault();
            e.stopPropagation();

            // Close the row actions menu
            closeAllDropdowns();

            var data = printBtn.dataset;

            // Get logo URL (Assuming you have a global config, or fallback)
            var logoUrl = "/Content/app/images/krctk-logo.png";

            printReceipt(data, logoUrl);
        });
    }

    function printReceipt(data, logoUrl) {
        var iframe = document.createElement('iframe');
        iframe.style.display = 'none';
        document.body.appendChild(iframe);

        var doc = iframe.contentWindow.document;
        doc.open();
        doc.write(`
            <html>
            <head>
                <title>Receipt - ${data.receipt}</title>
                <style>
                    body { font-family: Arial, sans-serif; padding: 40px; color: #000; }
                    .header { display: flex; flex-direction: column; align-items: center; text-align: center; margin-bottom: 30px; }
                    .logo { max-height: 80px; max-width: 100%; margin-bottom: 15px; object-fit: contain; }
                    .header h1 { margin: 0; font-size: 24px; text-transform: uppercase; letter-spacing: 1px; }
                    .header p { margin: 5px 0; color: #555; font-size: 14px; }
                    .receipt-box { border: 1px solid #ccc; padding: 20px; max-width: 400px; margin: 0 auto; border-radius: 8px; }
                    .row { display: flex; justify-content: space-between; margin-bottom: 12px; font-size: 14px; }
                    .row strong { display: inline-block; width: 140px; color: #333; }
                    .total { border-top: 2px dashed #ccc; padding-top: 12px; margin-top: 20px; font-size: 18px; font-weight: bold; }
                    .footer { text-align: center; margin-top: 40px; font-size: 12px; color: #777; line-height: 1.5; }
                </style>
            </head>
            <body>
                <div class="header">
                    ${logoUrl ? `<img src="${logoUrl}" class="logo" alt="Club Logo" />` : ''}
                    <h1>Crown Towers Club</h1>
                    <p>Official Receipt</p>
                </div>
                <div class="receipt-box">
                    <div class="row"><strong>Receipt No:</strong> <span>${data.receipt}</span></div>
                    <div class="row"><strong>Date:</strong> <span>${data.date}</span></div>
                    <div class="row"><strong>Received From:</strong> <span>${data.name}</span></div>
                    <div class="row"><strong>Membership No:</strong> <span>${data.memberNo}</span></div>
                    <div class="row"><strong>Payment For:</strong> <span>Annual Fee (${data.year})</span></div>
                    <div class ="row"><strong>Payment Method: </strong> <span>${data.method}</span></div>
                    <div class ="row"><strong>Remark: </strong> <span>${data.remarks}</span></div>
                    <div class="row total"><strong>Amount Paid:</strong> <span>${data.amount}</span></div>
                </div>
                <div class="footer">
                    Thank you for your payment.<br/>
                    This is a computer-generated receipt. No signature is required.
                </div>
                <script>
                    window.onload = function() {
                        setTimeout(function() {
                            window.print();
                            setTimeout(function() { window.frameElement.remove(); }, 1000);
                        }, 200);
                    }
                </script>
            </body>
            </html>
        `);
        doc.close();
    }

    /* ============================================================
      5. Delete Draft & Rejected
      ============================================================ */
    function initDeleteHandlers() {
        document.addEventListener("click", function (e) {
            var deleteBtn = e.target.closest(".btn-delete-record");
            if (!deleteBtn) return;

            e.preventDefault();
            e.stopPropagation();

            // Close the dropdown menu
            closeAllDropdowns();

            var memberId = deleteBtn.dataset.id;
            var memberName = deleteBtn.dataset.name;

            if (!window.Swal) {
                if (confirm(`Are you sure you want to delete the application for ${memberName}? This cannot be undone.`)) {
                    processDelete(memberId);
                }
                return;
            }

            window.Swal.fire({
                title: "Are you sure?",
                html: `You are about to delete the application for <b>${memberName}</b>.<br/>This action cannot be undone.`,
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: "#b42318",
                cancelButtonColor: "#64748b",
                confirmButtonText: "Yes, delete it"
            }).then((result) => {
                if (result.isConfirmed) {
                    processDelete(memberId);
                }
            });
        });
    }

    function processDelete(memberId) {
        $.ajax({
            url: "/Review/DeleteRecord",
            method: "POST",
            dataType: "json",
            data: { id: memberId }
        })
        .done(function (response) {
            if (!response || response.Success !== true) {
                if (window.Swal) window.Swal.fire("Error", response.Message, "error");
                return;
            }

            if (window.Swal) {
                window.Swal.fire("Deleted!", response.Message, "success").then(function () {
                    window.location.reload();
                });
            } else {
                window.location.reload();
            }
        })
        .fail(function () {
            if (window.Swal) window.Swal.fire("Error", "A server error occurred.", "error");
        });
    }

    function initMobileNavigation() {
        var toggle = document.getElementById("mobile-nav-toggle");
        var sidebar = document.getElementById("app-sidebar");
        var backdrop = document.getElementById("sidebar-backdrop");

        if (!toggle || !sidebar || !backdrop) {
            return;
        }

        function openMenu() {
            sidebar.classList.add("is-open");
            backdrop.classList.add("is-visible");
            document.body.classList.add("mobile-nav-open");

            toggle.setAttribute("aria-expanded", "true");
            toggle.setAttribute("aria-label", "Close navigation menu");

            sidebar.focus();
        }

        function closeMenu(returnFocus) {
            sidebar.classList.remove("is-open");
            backdrop.classList.remove("is-visible");
            document.body.classList.remove("mobile-nav-open");

            toggle.setAttribute("aria-expanded", "false");
            toggle.setAttribute("aria-label", "Open navigation menu");

            if (returnFocus) {
                toggle.focus();
            }
        }

        toggle.addEventListener("click", function () {
            if (sidebar.classList.contains("is-open")) {
                closeMenu(true);
            } else {
                openMenu();
            }
        });

        backdrop.addEventListener("click", function () {
            closeMenu(true);
        });

        // Close the drawer after selecting a navigation link.
        sidebar.addEventListener("click", function (event) {
            if (event.target.closest("a")) {
                closeMenu(false);
            }
        });

        // Close the drawer when pressing Escape.
        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape" &&
                sidebar.classList.contains("is-open")) {
                closeMenu(true);
            }
        });

        // Close the drawer when returning to desktop width.
        window.addEventListener("resize", function () {
            if (window.innerWidth > 767) {
                closeMenu(false);
            }
        });
    }

    // Initialize when DOM is ready
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }

    return { init: init };

}(window, document));