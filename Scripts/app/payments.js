var App = App || {};

App.Payments = (function (window, document, $, notifications) {
    "use strict";

    // Configuration passed from the Razor View
    var config = window.CTClubPayments || {};

    function init() {
        bindRecordPaymentButtons();
        bindRenewalGenerator();
        bindExpiryCheck();
        bindYearFilter();
    }

    /* ============================================================
       1. Record Payment (SweetAlert -> AJAX POST)
       ============================================================ */
    function bindRecordPaymentButtons() {
        var buttons = document.querySelectorAll(".btn-record-payment");
        for (var i = 0; i < buttons.length; i++) {
            buttons[i].addEventListener("click", handleRecordClick);
        }
    }

    function handleRecordClick(e) {
        var btn = e.currentTarget;
        var termId = btn.getAttribute("data-term-id");
        var amount = btn.getAttribute("data-amount");
        var name = btn.getAttribute("data-name");

        if (!window.Swal) {
            var method = prompt(`Record payment for ${name}.\nEnter method (Cash, Bank Transfer, Cheque):`, "Cash");
            if (method) {
                processPayment(termId, amount, method, "", btn);
            }
            return;
        }

        window.Swal.fire({
            title: "Record Payment",
            html: `Record payment of <b>RM ${amount}</b> for <b>${name}</b>?<br/><br/>
                   <select id="swal-payment-method" class="form-select" style="margin-bottom: 12px; width: 100%; padding: 8px;">
                       <option value="Cash">Cash</option>
                       <option value="Bank Transfer">Bank Transfer</option>
                       <option value="Cheque">Cheque</option>
                   </select>
                   <input id="swal-remarks" class="form-input" style="width: 100%; padding: 8px;" placeholder="Remarks (optional)" />`,
            showCancelButton: true,
            confirmButtonText: "Confirm Payment",
            preConfirm: () => {
                var method = document.getElementById("swal-payment-method").value;
                var remarks = document.getElementById("swal-remarks").value;
                return { method: method, remarks: remarks };
            }
        }).then((result) => {
            if (result.isConfirmed) {
                processPayment(termId, amount, result.value.method, result.value.remarks, btn);
            }
        });
    }

    function processPayment(termId, amount, method, remarks, btn) {
        if (btn) {
            btn.disabled = true;
            btn.textContent = "Processing...";
        }

        $.ajax({
            url: config.processUrl,
            method: "POST",
            dataType: "json",
            data: {
                membershipTermId: termId,
                amount: amount,
                paymentMethod: method,
                remarks: remarks
            }
        })
        .done(function (response) {
            if (!response || response.Success !== true) {
                notifications.error(response.Message || "Failed to record payment.", "Error");
                return;
            }

            var toast = notifications.success(response.Message, "Success");
            if (toast && typeof toast.then === "function") {
                toast.then(function () { window.location.reload(); });
            } else {
                window.location.reload();
            }
        })
        .fail(function () {
            notifications.error("A server error occurred.", "Error");
        })
        .always(function () {
            if (btn) {
                btn.disabled = false;
                btn.textContent = "Record Payment";
            }
        });
    }

    /* ============================================================
       2. Generate Renewals (Button -> SweetAlert -> AJAX POST)
       ============================================================ */
    function bindRenewalGenerator() {
        var btn = document.getElementById("btn-generate-renewals");
        if (btn) {
            btn.addEventListener("click", handleRenewalSubmit);
        }
    }

    function handleRenewalSubmit(e) {
        e.preventDefault();

        var year = parseInt(document.getElementById("renewal-year").value, 10);
        var fee = parseFloat(document.getElementById("renewal-fee").value);
        var btn = document.getElementById("btn-generate-renewals");

        if (!year || !fee) {
            notifications.error("Please enter a valid year and fee.", "Validation Error");
            return;
        }

        if (!window.Swal) {
            if (!confirm(`Generate renewals for ${year} with a fee of RM ${fee.toFixed(2)}?`)) return;
            processRenewals(year, fee, btn);
            return;
        }

        window.Swal.fire({
            title: "Generate Renewals?",
            html: `This will create pending payment terms for the year <b>${year}</b>.<br/><br/>Fee: <b>RM ${fee.toFixed(2)}</b>`,
            icon: "question",
            showCancelButton: true,
            confirmButtonText: "Yes, generate them"
        }).then((result) => {
            if (result.isConfirmed) {
                processRenewals(year, fee, btn);
            }
        });
    }

    function processRenewals(year, fee, btn) {
        if (btn) {
            btn.disabled = true;
            btn.textContent = "Generating...";
        }

        $.ajax({
            url: config.generateRenewalsUrl,
            method: "POST",
            dataType: "json",
            data: { year: year, feeAmount: fee }
        })
        .done(function (response) {
            if (!response || response.Success !== true) {
                notifications.error(response.Message || "Failed to generate renewals.", "Error");
                return;
            }

            var toast = notifications.success(response.Message, "Success");
            if (toast && typeof toast.then === "function") {
                toast.then(function () { window.location.reload(); });
            } else {
                window.location.reload();
            }
        })
        .fail(function () {
            notifications.error("A server error occurred.", "Error");
        })
        .always(function () {
            if (btn) {
                btn.disabled = false;
                btn.textContent = "Generate";
            }
        });
    }

    /* ============================================================
       3. Run Overdue Expiry Check (Button -> AJAX POST)
       ============================================================ */
    function bindExpiryCheck() {
        var btn = document.getElementById("btn-run-expiry-check");
        if (btn) {
            btn.addEventListener("click", handleExpiryCheck);
        }
    }

    function handleExpiryCheck() {
        if (!window.Swal) {
            if (!confirm("This will expire overdue terms and suspend members. Proceed?")) return;
            processExpiryCheck();
            return;
        }

        window.Swal.fire({
            title: "Run Overdue Check?",
            text: "This will mark all pending payments past their due date as 'Expired' and set the members to 'Suspended'.",
            icon: "warning",
            showCancelButton: true,
            confirmButtonText: "Yes, run check"
        }).then((result) => {
            if (result.isConfirmed) {
                processExpiryCheck();
            }
        });
    }

    function processExpiryCheck() {
        var btn = document.getElementById("btn-run-expiry-check");
        if (btn) {
            btn.disabled = true;
            btn.textContent = "Checking...";
        }

        $.ajax({
            url: config.expiryCheckUrl,
            method: "POST",
            dataType: "json"
        })
        .done(function (response) {
            if (!response || response.Success !== true) {
                notifications.error(response.Message || "Failed to run check.", "Error");
                return;
            }

            var toast = notifications.success(response.Message, "Success");
            if (toast && typeof toast.then === "function") {
                toast.then(function () { window.location.reload(); });
            } else {
                window.location.reload();
            }
        })
        .fail(function () {
            notifications.error("A server error occurred.", "Error");
        })
        .always(function () {
            if (btn) {
                btn.disabled = false;
                btn.textContent = "Run Overdue Check";
            }
        });
    }

    /* ============================================================
       4. Year Filter (Dropdown -> Redirect)
       ============================================================ */
    function bindYearFilter() {
        var yearFilter = document.getElementById("dashboard-year-filter");
        if (yearFilter) {
            yearFilter.addEventListener("change", function () {
                var selectedYear = this.value;
                // Build the URL: /Review/PaymentDashboard?tab=Pending&year=2027
                var url = config.dashboardBaseUrl + "?tab=" + config.currentTab + "&year=" + selectedYear;
                window.location.href = url;
            });
        }
    }

    // Initialize on ready
    $(init);

    return { init: init };
}(window, document, jQuery, App.UI.notifications));