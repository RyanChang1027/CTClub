var App = App || {};

App.Shell = (function (window, document) {
    "use strict";

    function init() {
        initSpecificDropdowns();
        initGlobalDropdownHandlers();
        initPrintReceiptHandlers();
        initDeleteHandlers();
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

            // Close all other open dropdowns first
            closeAllDropdowns();

            // Toggle this one
            var isOpen = menu.classList.contains("is-open");
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
        // 1. Handle Row Action toggles
        document.addEventListener("click", function (e) {
            // Check if we clicked a row action toggle (the 3 dots in the table)
            var toggleBtn = e.target.closest("[data-action-toggle]");

            if (toggleBtn) {
                e.preventDefault();
                e.stopPropagation();

                var menu = toggleBtn.nextElementSibling;
                var isOpen = menu.classList.contains("is-open");

                closeAllDropdowns();

                if (!isOpen) {
                    menu.classList.add("is-open");
                }
                return;
            }

            // 2. Handle clicking outside to close
            // If the click target is NOT inside ANY of our dropdown containers, close them.
            var clickedInsideNav = e.target.closest('.nav-dropdown');
            var clickedInsideUser = e.target.closest('.user-dropdown');
            var clickedInsideRow = e.target.closest('.row-actions');

            if (!clickedInsideNav && !clickedInsideUser && !clickedInsideRow) {
                closeAllDropdowns();
            }
        });

        // 3. Close on Escape key
        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape") {
                closeAllDropdowns();
            }
        });
    }

    /* ============================================================
       3. Helper: Close all menus
       ============================================================ */
    function closeAllDropdowns() {
        var openMenus = document.querySelectorAll(".dropdown-menu.is-open, .row-actions-menu.is-open");

        for (var i = 0; i < openMenus.length; i++) {
            openMenus[i].classList.remove("is-open");

            // Reset aria-expanded on the trigger if it exists
            var trigger = openMenus[i].previousElementSibling;
            if (trigger && trigger.tagName === "BUTTON") {
                trigger.setAttribute("aria-expanded", "false");
            }
        }
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

    // Initialize when DOM is ready
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }

    return { init: init };

}(window, document));