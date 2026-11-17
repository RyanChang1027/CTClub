var App = App || {};
App.Member = App.Member || {};

App.Member.scanner = (function (window, document) {
    "use strict";

    var scanner = null;
    var hasScanned = false;

    var result = document.getElementById("scan-result");
    var config = document.getElementById("scanner-config");

    var lookupUrl = config
        ? config.getAttribute("data-url-lookup")
        : null;

    var api = App.Base && App.Base.api;

    /*
     * Converts ASP.NET JSON dates into a readable date.
     */
    function formatDate(value) {
        if (!value) {
            return "-";
        }

        var timestamp = parseInt(
            value.replace("/Date(", "").replace(")/", ""),
            10
        );

        if (isNaN(timestamp)) {
            return value;
        }

        var date = new Date(timestamp);

        return date.toLocaleDateString("en-GB", {
            day: "2-digit",
            month: "2-digit",
            year: "numeric"
        });
    }

    /*
     * Displays a simple message in the result area.
     */
    function showMessage(message) {
        result.textContent = message;
    }

    /*
     * Returns the appropriate styling class for a membership status.
     */
    function getStatusClass(status) {
        var value = (status || "").toLowerCase();

        if (value.indexOf("pending") !== -1) {
            return "status-pill--pending";
        }

        if (value.indexOf("active") !== -1) {
            return "status-pill--active";
        }

        if (value.indexOf("expired") !== -1) {
            return "status-pill--expired";
        }

        if (value.indexOf("suspended") !== -1) {
            return "status-pill--suspended";
        }

        if (value.indexOf("cancelled") !== -1) {
            return "status-pill--cancelled";
        }

        return "";
    }

    /*
     * Displays the member information returned by the server.
     */
    function displayMember(member) {
        result.innerHTML = "";

        var heading = document.createElement("h3");
        heading.className = "headline-md scan-details__title";
        heading.textContent = "Membership Details";

        result.appendChild(heading);

        var details = document.createElement("div");
        details.className = "scan-details__grid";

        var fields = [
            { label: "Name", value: member.Name },
            { label: "Membership Number", value: member.MembershipNumber },
            { label: "Company", value: member.CompanyName, full: true },
            { label: "Department", value: member.Department },
            { label: "Admission Date", value: formatDate(member.AdmissionDate) },
            { label: "Membership Status", value: member.MembershipStatus, status: true },
            { label: "Current Term Status", value: member.CurrentTermStatus, status: true },
            { label: "Membership Year", value: member.MembershipYear }
        ];

        fields.forEach(function (field) {
            var item = document.createElement("div");
            item.className = "scan-detail";

            if (field.full) {
                item.className += " scan-detail--full";
            }

            var label = document.createElement("span");
            label.className = "scan-detail__label";
            label.textContent = field.label;

            var value = document.createElement("div");
            value.className = "scan-detail__value";

            var displayValue =
                field.value !== null &&
                    field.value !== undefined &&
                    field.value !== ""
                    ? field.value
                    : "-";

            if (field.status && displayValue !== "-") {
                var badge = document.createElement("span");
                badge.className = "status-pill " + getStatusClass(displayValue);
                badge.textContent = displayValue;
                value.appendChild(badge);
            } else {
                value.textContent = displayValue;
            }

            item.appendChild(label);
            item.appendChild(value);

            details.appendChild(item);
        });

        result.appendChild(details);
    }

    /*
     * Sends the scanned membership number to MemberController.
     */
    function lookupMembership(membershipNumber) {
        showMessage("Looking up membership details...");

        api.get(
            lookupUrl +
            "?membershipNumber=" +
            encodeURIComponent(membershipNumber)
        )
            .done(function (response) {
                if (!response || !response.Data) {
                    showMessage("No membership information was returned.");
                    return;
                }

                displayMember(response.Data);
            })
            .fail(function (error) {
                var message = error && error.Message
                    ? error.Message
                    : "Unable to retrieve membership details.";

                showMessage(message);
            });
    }

    /*
     * Handles a successfully decoded QR code.
     */
    function onScanSuccess(decodedText) {
        if (hasScanned) {
            return;
        }

        hasScanned = true;

        // Stop repeated scans of the same QR code.
        scanner.clear().catch(function (error) {
            console.error("Unable to stop the scanner:", error);
        });

        // Use the decoded QR content as the membership number.
        lookupMembership(decodedText);
    }

    /*
     * Initialises the QR scanner.
     */
    function init() {
        var reader = document.getElementById("qr-reader");

        if (!reader || !result) {
            return;
        }

        if (typeof window.Html5QrcodeScanner !== "function") {
            showMessage("The QR scanner library could not be loaded.");
            return;
        }

        if (!api || !lookupUrl) {
            showMessage("The scanner API configuration is missing.");
            return;
        }

        scanner = new window.Html5QrcodeScanner(
            "qr-reader",
            {
                fps: 10,
                qrbox: {
                    width: 250,
                    height: 250
                },
                rememberLastUsedCamera: true
            },
            false
        );

        scanner.render(onScanSuccess);
    }

    return {
        init: init
    };

}(window, document));

// Start the scanner when the script loads.
App.Member.scanner.init();