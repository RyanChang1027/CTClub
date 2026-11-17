var App = App || {};
App.Review = App.Review || {};

App.Review.page = (function (window, document, $, baseApi, notifications) {
    "use strict";

    /* ============================================================
       1. Configuration
       ============================================================ */
    var config = window.CTClubReview || {};

    var urls = {
        verify: config.verifyUrl,
        rejectDeptHead: config.rejectDeptHeadUrl,
        returnDeptHead: config.returnDeptHeadUrl,
        approve: config.approveUrl,
        rejectCommittee: config.rejectCommitteeUrl,
        pending: config.pendingUrl,
        verified: config.verifiedUrl
    };

    /* ============================================================
       2. Initialization & Binding
       ============================================================ */
    function init() {
        var memberIdField = document.getElementById("review-member-id");
        if (!memberIdField) { return; }

        var memberId = Number(memberIdField.value || 0);
        if (memberId <= 0) { return; }

        bind("verifyApplication", function () { verify(memberId); });
        bind("rejectApplicationDeptHead", function () { rejectDeptHead(memberId); });
        bind("returnApplicationDeptHead", function () { returnDeptHead(memberId); });
        bind("approveApplication", function () { approve(memberId); });
        bind("rejectApplicationCommittee", function () { rejectCommittee(memberId); });

        // NEW: Bind Cancel Membership button
        var cancelBtn = document.getElementById("btn-cancel-membership");
        if (cancelBtn) {
            cancelBtn.addEventListener("click", function () {
                cancelMembership(memberId);
            });
        }
    }

    function bind(id, handler) {
        var el = document.getElementById(id);
        if (el) { el.addEventListener("click", handler); }
    }

    /* ============================================================
       3. API Actions (Department Head)
       ============================================================ */
    function verify(memberId) {
        post(
            urls.verify,
            { id: memberId },
            document.getElementById("verifyApplication"),
            "Verifying...",
            "Application verified.",
            urls.pending);
    }

    function returnDeptHead(memberId) {
        askReason(
            "Return for Correction",
            "Please enter the reason for returning this application for correction.",
            function (reason) {
                post(
                    urls.returnDeptHead,
                    { id: memberId, reason: reason },
                    document.getElementById("returnApplicationDeptHead"),
                    "Returning...",
                    "Application returned for correction.",
                    urls.pending);
            });
    }

    function rejectDeptHead(memberId) {
        askReason(
            "Reject Application",
            "Please enter the reason for rejection.",
            function (reason) {
                post(
                    urls.rejectDeptHead,
                    { id: memberId, reason: reason },
                    document.getElementById("rejectApplicationDeptHead"),
                    "Rejecting...",
                    "Application rejected.",
                    urls.pending);
            });
    }

    /* ============================================================
       4. API Actions (Committee)
       ============================================================ */
    function approve(memberId) {
        //post(
        //    urls.approve,
        //    { id: memberId },
        //    document.getElementById("approveApplication"),
        //    "Approving...",
        //    "Application approved.",
        //    urls.verified);

        post(
            urls.approve,
            { id: memberId },
            document.getElementById("approveApplication"),
            "Approving...",
            "Application approved.",
            null // <-- Change this to null so it doesn't auto-redirect immediately
        );
    }

    function rejectCommittee(memberId) {
        askReason(
            "Reject Application",
            "Please enter the reason for rejection.",
            function (reason) {
                post(
                    urls.rejectCommittee,
                    { id: memberId, reason: reason },
                    document.getElementById("rejectApplicationCommittee"),
                    "Rejecting...",
                    "Application rejected.",
                    urls.verified);
            });
    }

    function cancelMembership(memberId) {
        if (!window.Swal) {
            var reason = window.prompt("Enter reason for cancelling this membership:");
            if (reason && reason.trim()) {
                post("/Review/CancelMembership", { id: memberId, reason: reason.trim() }, null, "Cancelling...", "Membership cancelled.", "/Review/Directory");
            }
            return;
        }

        window.Swal.fire({
            title: "Cancel Membership?",
            text: "This action is permanent. It will expire their current terms and remove their active status.",
            icon: "warning",
            input: "textarea",
            inputPlaceholder: "Reason for cancellation (required)...",
            inputValidator: function (value) {
                return (!value || !value.trim()) ? "A reason is required." : null;
            },
            showCancelButton: true,
            confirmButtonColor: "#b42318",
            confirmButtonText: "Yes, cancel membership"
        }).then(function (result) {
            if (result.isConfirmed && result.value) {
                post("/Review/CancelMembership", { id: memberId, reason: result.value.trim() }, document.getElementById("btn-cancel-membership"), "Cancelling...", "Membership cancelled.", "/Review/Directory");
            }
        });
    }

    /* ============================================================
       5. Core POST Logic (Form Encoded + Anti-Forgery Token)
       ============================================================ */
    //function getToken() {
    //    var el = document.querySelector('input[name="__RequestVerificationToken"]');
    //    return el ? el.value : "";
    //}

    //function post(url, data, buttonId, busyText, successMessage, redirectUrl) {
    //    if (!url) {
    //        notifications.error("The review endpoint is not configured.", "Configuration Error");
    //        return;
    //    }

    //    var token = getToken();
    //    if (!token) {
    //        notifications.error("The verification token is missing from the page.", "Configuration Error");
    //        return;
    //    }

    //    var button = document.getElementById(buttonId);
    //    setBusy(button, busyText);

    //    // MUST use $.ajax with form data to bind correctly to MVC parameters
    //    $.ajax({
    //        url: url,
    //        method: "POST",
    //        dataType: "json",
    //        data: data, // Form encoded (not JSON)
    //        headers: {
    //            "Accept": "application/json",
    //            "RequestVerificationToken": token
    //        }
    //    })
    //    .done(function (response) {
    //        if (!response || response.Success !== true) {
    //            notifications.error((response && response.Message) || "The request could not be completed.", "Something Went Wrong");
    //            return;
    //        }

    //        notifications.success((response && response.Message) || successMessage, "Success");

    //        if (redirectUrl) {
    //            window.location.href = redirectUrl;
    //        }
    //    })
    //    .fail(function (response) {
    //        console.error("[review] POST failed", url, response.status);
    //        console.error("[review] body", response.responseText);
    //        notifications.error(getFailureMessage(response), "Something Went Wrong");
    //    })
    //    .always(function () {
    //        restore(button);
    //    });
    //}

    /* ============================================================
       5. Core POST Logic (No Anti-Forgery Token)
       ============================================================ */
    function post(url, data, button, busyText, successMessage, redirectUrl) {
        if (!url) {
            notifications.error(
                "The review endpoint is not configured.",
                "Configuration Error");
            return;
        }

        setBusy(button, busyText);

        baseApi.postJson(url, data)
            .done(function (response) {
                var toast = notifications.success(
                    (response && response.Message) || successMessage,
                    "Success");

                if (redirectUrl) {
                    if (toast && typeof toast.then === "function") {
                        toast.then(function () { window.location.href = redirectUrl; });
                    } else {
                        window.location.href = redirectUrl;
                    }
                } else {
                    // If no redirect URL (like for Approve), just refresh the page on click OK
                    if (toast && typeof toast.then === "function") {
                        toast.then(function () { window.location.reload(); });
                    }
                }
            })
            .fail(function (response) {
                notifications.error(
                    getFailureMessage(response),
                    "Something Went Wrong");
            })
            .always(function () {
                restore(button);
            });
    }

    /* ============================================================
       6. UI Helpers
       ============================================================ */
    function askReason(title, text, onConfirm) {
        if (!window.Swal) {
            var reason = window.prompt(text);
            if (reason && reason.trim()) {
                onConfirm(reason.trim());
            }
            return;
        }

        window.Swal.fire({
            title: title,
            input: "textarea",
            inputPlaceholder: text,
            inputValidator: function (value) {
                return (!value || !value.trim())
                    ? "A reason is required."
                    : null;
            },
            showCancelButton: true,
            confirmButtonText: "Confirm"
        }).then(function (result) {
            if (result.isConfirmed && result.value) {
                onConfirm(result.value.trim());
            }
        });
    }

    function setBusy(button, text) {
        if (!button) { return; }
        button.disabled = true;
        button.setAttribute(
            "data-original-text",
            button.textContent);
        button.textContent = text;
    }

    function restore(button) {
        if (!button) { return; }
        button.disabled = false;
        var original =
            button.getAttribute("data-original-text");
        if (original) {
            button.textContent = original;
            button.removeAttribute("data-original-text");
        }
    }

    function getFailureMessage(response) {
        if (response && response.Message) { return response.Message; }
        if (response && response.responseJSON && response.responseJSON.Message) {
            return response.responseJSON.Message;
        }
        if (response && response.status) {
            return "HTTP " + response.status + " — the server rejected the request.";
        }
        return "The request could not be completed.";
    }

        function cancelMembership(memberId) {
        if (!window.Swal) {
            var reason = window.prompt("Enter reason for cancelling this membership:");
            if (reason && reason.trim()) {
                post("/Review/CancelMembership", { id: memberId, reason: reason.trim() }, null, "Cancelling...", "Membership cancelled.", "/Review/Directory");
            }
            return;
        }

        window.Swal.fire({
            title: "Cancel Membership?",
            text: "This action is permanent. It will expire their current terms and remove their active status.",
            icon: "warning",
            input: "textarea",
            inputPlaceholder: "Reason for cancellation (required)...",
            inputValidator: function (value) {
                return (!value || !value.trim()) ? "A reason is required." : null;
            },
            showCancelButton: true,
            confirmButtonColor: "#b42318",
            confirmButtonText: "Yes, cancel membership"
        }).then(function (result) {
            if (result.isConfirmed && result.value) {
                post("/Review/CancelMembership", { id: memberId, reason: result.value.trim() }, document.getElementById("btn-cancel-membership"), "Cancelling...", "Membership cancelled.", "/Review/Directory");
            }
        });
    }

    // Initialize
    $(init);

    return { init: init };
}(window, document, jQuery, App.Base.api, App.UI.notifications));