var App = App || {};
App.Member = App.Member || {};

App.Member.form = (function (window, document, $, api, notifications) {
    "use strict";

    /* ============================================================
       1. Configuration & State
       ============================================================ */
    var corporateReadonlyFields = [
        "Name", "NRIC", "Gender", "Race", "MaritalStatus", "Nationality",
        "DateOfBirth", "PlaceOfBirth", "CompanyName", "CompanyAddress",
        "Department", "Occupation", "Tel", "Email", "HomeAddress"
    ];

    var applicantEditableFields = [
        "HeadOfDepartment", "Hobbies", "Floor"
    ];

    var template = document.getElementById("membership-template");
    var app = document.getElementById("app");

    var pageConfig = document.getElementById("membership-page-config");
    var myApplicationsUrl = pageConfig ? pageConfig.getAttribute("data-url-my-applications") : null;

    var jsonBaseUrl = "/Content/app/data/";
    var companiesData = []; // Holds the parsed companies.json

    /* ============================================================
       2. Initialization & Setup
       ============================================================ */
    function init() {
        if (!template || !app) { return; }

        // 1. Inject HTML
        app.innerHTML = template.innerHTML;

        // 2. Bind DOM events
        bindEvents();
        loadInitialNric();

        // 3. Load JSON, then hydrate any saved data
        loadJsonData().then(function () {
            hydrateInitialMember();

            // NEW: Lock NRIC for normal applications (non-manual entry)
            var isManualEntry = myApplicationsUrl && myApplicationsUrl.toLowerCase().indexOf("verified") > -1;

            if (!isManualEntry) {
                var nricField = document.getElementById("nric");
                if (nricField) {
                    // Visually lock the field
                    nricField.readOnly = true;
                    nricField.classList.add("staff-readonly");
                }
            }
        });

        var isEditMode = pageConfig && pageConfig.getAttribute("data-is-edit-mode") === "True";
        
        if (isEditMode) {
            // Change the submit button text
            var submitBtn = document.querySelector('[type="submit"]');
            if (submitBtn) submitBtn.textContent = "Save Changes";

            // Hide Save Draft and Clear buttons
            var draftBtn = document.querySelector('[data-action="draft"]');
            if (draftBtn) draftBtn.style.display = "none";
            
            var clearBtn = document.querySelector('[data-action="clear"]');
            if (clearBtn) clearBtn.style.display = "none";

            var retrieveButton = document.getElementById("retrieveStaffDetails");
            if (retrieveButton) retrieveButton.style.display = "none";
        }
    }

    function bindEvents() {
        var form = document.getElementById("membership-form");
        if (!form) { return; }

        form.addEventListener("submit", submitApplication);
        form.addEventListener("input", clearFieldError);
        form.addEventListener("change", clearFieldError);

        var retrieveButton = document.getElementById("retrieveStaffDetails");
        if (retrieveButton) retrieveButton.addEventListener("click", retrieveStaffDetails);

        var draftButton = document.querySelector('[data-action="draft"]');
        if (draftButton) draftButton.addEventListener("click", saveDraft);

        var clearButton = document.querySelector('[data-action="clear"]');
        if (clearButton) clearButton.addEventListener("click", clearForm);

        // Company Change Cascade
        var companySelect = document.getElementById("company-name");
        if (companySelect) {
            companySelect.addEventListener("change", handleCompanyChange);
        }
    }

    function loadInitialNric() {
        if (!pageConfig) { return; }

        var initialNric = pageConfig.getAttribute("data-initial-nric");
        if (!initialNric) { return; }

        var form = document.getElementById("membership-form");
        if (!form || !form.elements.NRIC) { return; }

        form.elements.NRIC.value = initialNric;
        clearFieldError({ target: form.elements.NRIC });
    }

    /* ============================================================
       3. API Actions
       ============================================================ */
    function retrieveStaffDetails() {
        var form = document.getElementById("membership-form");
        if (!form || !form.elements.NRIC) { return; }

        var nric = form.elements.NRIC.value.trim();

        if (!nric) {
            showFieldError(form.elements.NRIC, "Please enter an NRIC.");
            return;
        }
        if (!isValidNric(nric)) {
            showFieldError(form.elements.NRIC, "NRIC must follow 000000-00-0000 format.");
            return;
        }

        var button = document.getElementById("retrieveStaffDetails");
        setButtonBusy(button, "Retrieving...");

        api.getStaffDetails(nric)
            .done(function (response) {
                if (!response || response.Success !== true || !response.Data) {
                    showNotice("No staff details were returned.", "error");
                    return;
                }

                populateFromCorporateRecord(response.Data);
                applyCorporateReadonlyState();
                showNotice(response.Message || "Staff details retrieved.", "success");
            })
            .fail(function (response) {
                showApiError(response);
            })
            .always(function () {
                restoreButton(button, "Retrieve Staff Details");
            });
    }

    function saveDraft(event) {
        if (event) { event.preventDefault(); }

        var button = document.querySelector('[data-action="draft"]');
        if (button && button.disabled) { return; }

        // NRIC is mandatory for saving a draft
        var form = document.getElementById("membership-form");
        var nricField = form ? form.elements.NRIC : null;

        if (!nricField || !nricField.value.trim()) {
            showFieldError(nricField, "NRIC is required to save a draft.");
            focusFirstError();
            return;
        }

        var model = modelFromForm();
        if (button) { setButtonBusy(button, "Saving Draft..."); }

        api.saveDraft(model)
            .done(function (response) {
                if (response && response.Data && response.Data.MemberId) {
                    setMemberId(response.Data.MemberId);
                }

                var toast = showNotice(response.Message || "Your draft has been saved successfully.", "success");
                handleRedirect(toast, myApplicationsUrl);
            })
            .fail(function (response) {
                showApiError(response);
            })
            .always(function () {
                if (button) { restoreButton(button, "Save Draft"); }
            });
    }

    function submitApplication(event) {
        event.preventDefault();

        var model = modelFromForm();
        if (!validateFinalApplication()) { return; }

        var submitButton = document.querySelector('[type="submit"]');
        if (submitButton && submitButton.disabled) { return; }
        if (submitButton) { setButtonBusy(submitButton, "Submitting..."); }

        api.submit(model)
            .done(function (response) {
                var toast = showNotice(response.Message || "Your membership application has been submitted successfully.", "success");
                handleRedirect(toast, myApplicationsUrl);
            })
            .fail(function (response) {
                showApiError(response);
            })
            .always(function () {
                if (submitButton) { restoreButton(submitButton, "Submit Application"); }
            });
    }

    /* ============================================================
       4. JSON Loading & Cascading
       ============================================================ */
    function loadJsonData() {
        return $.when(
            $.getJSON(jsonBaseUrl + "genders.json", function (data) {
                var select = document.getElementById("gender");
                if (!select) return;
                data.forEach(function (item) { select.add(new Option(item.name, item.code)); });
            }),

            $.getJSON(jsonBaseUrl + "marital-statuses.json", function (data) {
                var select = document.getElementById("marital-status");
                if (!select) return;
                data.forEach(function (item) { select.add(new Option(item.name, item.code)); });
            }),

            $.getJSON(jsonBaseUrl + "races.json", function (data) {
                var select = document.getElementById("race");
                if (!select) return;
                data.forEach(function (item) { select.add(new Option(item.name, item.code)); });
            }),

            $.getJSON(jsonBaseUrl + "nationalities.json", function (data) {
                var select = document.getElementById("nationality");
                if (!select) return;
                data.forEach(function (item) { select.add(new Option(item.name, item.code)); });
            }),

            //$.getJSON(jsonBaseUrl + "hods.json", function (data) {
            //    var select = document.getElementById("head-of-department");
            //    if (!select) return;
            //    data.forEach(function (item) { select.add(new Option(item.name, item.name)); });
            //}),

            $.getJSON(jsonBaseUrl + "companies.json", function (data) {
                companiesData = data;
                var select = document.getElementById("company-name");
                if (!select) return;
                data.forEach(function (company) { select.add(new Option(company.name, company.name)); });
            })
        );
    }

    function handleCompanyChange() {
        var companyName = this.value;
        var addressField = document.getElementById("company-address");
        var deptSelect = document.getElementById("department");

        // Reset Department
        deptSelect.innerHTML = "";

        if (!companyName) {
            addressField.value = "";
            deptSelect.add(new Option("Select Company First", ""));
            deptSelect.disabled = true;
            return;
        }

        var company = companiesData.find(function (c) { return c.name === companyName; });

        if (company) {
            addressField.value = company.address || "";
            deptSelect.add(new Option("Select Department", ""));

            if (company.departments && company.departments.length > 0) {
                company.departments.forEach(function (dept) {
                    deptSelect.add(new Option(dept.name, dept.name));
                });
                deptSelect.disabled = false;
            } else {
                deptSelect.add(new Option("No departments listed", ""));
                deptSelect.disabled = true;
            }
        }
    }

    /* ============================================================
       5. Form Population (Retrieve & Hydrate)
       ============================================================ */
    function populateFromCorporateRecord(employee) {
        var form = document.getElementById("membership-form");
        if (!form || !employee) { return; }

        setTextValue(form.elements.Name, employee.Name);
        setTextValue(form.elements.NRIC, employee.NRIC);
        setSelectValue(form.elements.Gender, employee.Gender);
        setSelectValue(form.elements.Race, employee.Race);
        setSelectValue(form.elements.MaritalStatus, employee.MaritalStatus);
        setSelectValue(form.elements.Nationality, employee.Nationality);
        setDateValue(form.elements.DateOfBirth, employee.DateOfBirth);
        setTextValue(form.elements.PlaceOfBirth, employee.PlaceOfBirth);
        setSelectValue(form.elements.CompanyName, employee.CompanyName);

        // Trigger cascade for Company/Department
        if (form.elements.CompanyName.value) {
            $(form.elements.CompanyName).trigger("change");

            setTimeout(function () {
                setTextValue(form.elements.CompanyAddress, employee.CompanyAddress);
                setSelectValue(form.elements.Department, employee.Department);
                setTextValue(form.elements.Occupation, employee.Occupation);
            }, 50);
        } else {
            setTextValue(form.elements.CompanyAddress, employee.CompanyAddress);
            setSelectValue(form.elements.Department, employee.Department);
            setTextValue(form.elements.Occupation, employee.Occupation);
        }

        setTextValue(form.elements.Tel, employee.Tel);
        setTextValue(form.elements.Email, employee.Email);
        setTextValue(form.elements.HomeAddress, employee.HomeAddress);
    }

    function hydrateInitialMember() {
        if (!pageConfig) { return; }

        var raw = pageConfig.getAttribute("data-initial-member");
        if (!raw) { return; }

        var member;
        try { member = JSON.parse(raw); } catch (e) { return; }
        if (!member) { return; }

        fillForm(member);
        if (member.MemberId) { setMemberId(member.MemberId); }

        if (member.ApplicationStatus === "Returned for Correction") {
            showCorrectionBanner(member.CorrectionReason);
        } else if (member.ApplicationStatus === "Draft") {
            showNotice("Draft loaded.", "info");
        }
    }

    function fillForm(member) {
        var form = document.getElementById("membership-form");
        if (!form || !member) { return; }

        Object.keys(member).forEach(function (key) {
            var field = form.elements[key];
            if (!field) { return; }

            if (field.type === "checkbox") {
                field.checked = !!member[key];
            } else if (key === "DateOfBirth") {
                setDateValue(field, member[key]);
            } else if (field.tagName === "SELECT") {
                setSelectValue(field, member[key]);

                // If this is the company select, manually trigger the change cascade
                if (key === "CompanyName") {
                    $(field).trigger("change");
                }
            } else {
                setTextValue(field, member[key]);
            }
        });

        applyCorporateReadonlyState();
    }

    /* ============================================================
       6. Readonly State Management
       ============================================================ */
    function applyCorporateReadonlyState() {
        var form = document.getElementById("membership-form");
        if (!form) { return; }

        // Lock the non-editable fields
        var isEditMode = pageConfig && pageConfig.getAttribute("data-is-edit-mode") === "True";
        if (isEditMode) {
            // Lock the Personal fields so they can't be changed
            var personalFields = ["Name", "NRIC", "Gender", "Race", "Nationality", "DateOfBirth", "PlaceOfBirth", "Declaration1", "Declaration2"];
            personalFields.forEach(function (fieldName) {
                var field = document.getElementById(fieldName.toLowerCase());
                if (!field) {
                    // Handle specific IDs like date-of-birth
                    field = document.getElementsByName(fieldName)[0];
                }
                if (field) {
                    field.readOnly = true;
                    if (field.tagName === 'SELECT') field.disabled = true;
                    if (field.type === "checkbox") field.disabled = true;
                    field.classList.add("staff-readonly");
                }
            });
        } else {
            corporateReadonlyFields.forEach(function (fieldName) {
                var field = form.elements[fieldName];
                if (!field) { return; }
                if (field.tagName === "SELECT") field.disabled = true;
                else field.readOnly = true;
                field.classList.add("staff-readonly");
            });

            applicantEditableFields.forEach(function (fieldName) {
                var field = form.elements[fieldName];
                if (!field) { return; }
                field.readOnly = false;
                field.disabled = false;
                field.classList.remove("staff-readonly");
            });
        }
    }

    function clearCorporateReadonlyState() {
        var form = document.getElementById("membership-form");
        if (!form) { return; }

        var allFields = corporateReadonlyFields.concat(applicantEditableFields);
        allFields.forEach(function (fieldName) {
            var field = form.elements[fieldName];
            if (!field) { return; }
            field.readOnly = false;
            field.disabled = false;
            field.classList.remove("staff-readonly");
        });
    }

    /* ============================================================
       7. Validation & Form Data
       ============================================================ */
    function modelFromForm() {
        var form = document.getElementById("membership-form");
        var model = {};
        if (!form) { return model; }

        var fields = form.querySelectorAll("[name]");
        Array.prototype.forEach.call(fields, function (field) {
            if (field.type === "checkbox") {
                model[field.name] = field.checked;
            } else if (field.name === "IsEditMode") {
                model[field.name] = field.value === "true"; // Convert string to boolean
            } else {
                model[field.name] = field.value || null;
            }
        });

        model.MemberId = Number(form.elements.MemberId.value || 0);
        return model;
    }

    function validateFinalApplication() {
        var form = document.getElementById("membership-form");
        if (!form) { return false; }

        clearAllFieldErrors();
        var valid = true;

        var requiredFields = [
            { name: "Name", message: "Please enter your full name." },
            { name: "NRIC", message: "Please enter your NRIC." },
            { name: "Gender", message: "Please select your gender." },
            { name: "Race", message: "Please select your race." },
            { name: "MaritalStatus", message: "Please select your marital status." },
            { name: "Nationality", message: "Please enter your nationality." },
            { name: "DateOfBirth", message: "Please enter your date of birth." },
            { name: "PlaceOfBirth", message: "Please enter your place of birth." },
            { name: "CompanyName", message: "Please enter your company name." },
            { name: "CompanyAddress", message: "Please enter your company address." },
            { name: "Department", message: "Please enter your department." },
            { name: "Occupation", message: "Please enter your occupation." },
            { name: "HeadOfDepartment", message: "Please enter your Head of Department." },
            { name: "Floor", message: "Please enter your floor." },
            { name: "Tel", message: "Please enter your telephone number." },
            { name: "Email", message: "Please enter your email address." },
            { name: "HomeAddress", message: "Please enter your home address." },
            { name: "Hobbies", message: "Please enter your hobbies." }
        ];

        requiredFields.forEach(function (item) {
            var field = form.elements[item.name];
            if (!field || !String(field.value || "").trim()) {
                showFieldError(field, item.message);
                valid = false;
            }
        });

        if (form.elements.NRIC && form.elements.NRIC.value.trim() && !isValidNric(form.elements.NRIC.value.trim())) {
            showFieldError(form.elements.NRIC, "NRIC must follow 000000-00-0000 format.");
            valid = false;
        }

        if (form.elements.Email && form.elements.Email.value.trim() && !isValidEmail(form.elements.Email.value.trim())) {
            showFieldError(form.elements.Email, "Please enter a valid email address.");
            valid = false;
        }

        var telField = form.elements.Tel;
        if (telField && telField.value.trim() && !isValidMalaysianPhone(telField.value.trim())) {
            showFieldError(telField, "Please enter a valid Malaysian phone number.");
            valid = false;
        }

        if (!form.elements.Declaration1 || !form.elements.Declaration1.checked) {
            showFieldError(form.elements.Declaration1, "You must agree to the first declaration.");
            valid = false;
        }

        if (!form.elements.Declaration2 || !form.elements.Declaration2.checked) {
            showFieldError(form.elements.Declaration2, "You must agree to the second declaration.");
            valid = false;
        }

        if (!valid) { focusFirstError(); }
        return valid;
    }

    function isValidNric(nric) {
        return /^\d{6}-\d{2}-\d{2}-\d{4}$/.test(nric) || /^\d{6}-\d{2}-\d{4}$/.test(nric);
    }

    function isValidEmail(email) {
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
    }

    function isValidMalaysianPhone(phone) {
        if (!phone) return true;
        var cleanPhone = phone.replace(/\D/g, '');
        return /^[06]/.test(cleanPhone) && cleanPhone.length >= 9 && cleanPhone.length <= 12;
    }

    /* ============================================================
       8. DOM Helpers & Formatting
       ============================================================ */
    function setMemberId(memberId) {
        var form = document.getElementById("membership-form");
        if (form && form.elements.MemberId) {
            form.elements.MemberId.value = Number(memberId || 0);
        }
    }

    function clearForm(event) {
        if (event) { event.preventDefault(); }
        var form = document.getElementById("membership-form");
        if (!form) { return; }

        form.reset();
        setMemberId(0);
        clearCorporateReadonlyState();
        clearAllFieldErrors();

        // NEW: Lock NRIC for normal applications (non-manual entry)
        var isManualEntry = myApplicationsUrl && myApplicationsUrl.toLowerCase().indexOf("verified") > -1;

        if (!isManualEntry) {
            var nricField = form.elements.NRIC;
            if (nricField) {
                // Re-apply the logged in user's NRIC
                if (pageConfig) {
                    var initialNric = pageConfig.getAttribute("data-initial-nric");
                    if (initialNric) nricField.value = initialNric;
                }

                // Visually lock the field
                nricField.readOnly = true;
                nricField.classList.add("staff-readonly");
            }
        }

        showNotice("Form cleared.", "info");
    }

    function setTextValue(field, value) {
        if (!field) { return; }
        field.value = (value === null || value === undefined) ? "" : String(value);
    }

    function setSelectValue(select, value) {
        if (!select || value === null || value === undefined || value === "") {
            if (select) select.value = "";
            return;
        }

        var normalizedValue = String(value).trim().toUpperCase();
        var matchingOption = null;

        for (var i = 0; i < select.options.length; i++) {
            var option = select.options[i];
            if (option.value.toUpperCase() === normalizedValue || option.text.toUpperCase() === normalizedValue) {
                matchingOption = option;
                break;
            }
        }

        if (!matchingOption && normalizedValue !== "") {
            matchingOption = new Option(value, value);
            select.add(matchingOption);
        }

        select.value = matchingOption ? matchingOption.value : "";
    }

    function setDateValue(field, value) {
        if (!field) { return; }
        if (value === null || value === undefined || value === "") {
            field.value = "";
            return;
        }

        var dateText = String(value).trim();
        var isoDate = /^(\d{4})-(\d{2})-(\d{2})/.exec(dateText);
        if (isoDate) {
            field.value = isoDate[1] + "-" + isoDate[2] + "-" + isoDate[3];
            return;
        }

        var slashDate = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(dateText);
        if (slashDate) {
            field.value = slashDate[3] + "-" + slashDate[2] + "-" + slashDate[1];
            return;
        }
        field.value = "";
    }

    function showCorrectionBanner(reason) {
        var form = document.getElementById("membership-form");
        if (!form) { return; }

        if (!reason) { reason = "Please review your application and resubmit."; }

        var banner = document.createElement("section");
        banner.className = "card card--info-banner";
        banner.setAttribute("role", "status");

        var title = document.createElement("h2");
        title.className = "headline-md section-title";
        title.textContent = "Returned for Correction";

        var message = document.createElement("p");
        message.className = "body-md";
        message.textContent = reason;

        banner.appendChild(title);
        banner.appendChild(message);
        form.insertBefore(banner, form.firstChild);
    }

    /* ============================================================
       9. Validation UI Helpers
       ============================================================ */
    function clearFieldError(event) {
        var field = event.target;
        if (!field || !field.classList.contains("input-error")) { return; }

        field.classList.remove("input-error");
        var error = field.parentNode.querySelector(".error");
        if (error) { error.textContent = ""; }
    }

    function clearAllFieldErrors() {
        var form = document.getElementById("membership-form");
        if (!form) { return; }

        var fields = form.querySelectorAll(".input-error");
        Array.prototype.forEach.call(fields, function (f) { f.classList.remove("input-error"); });

        var errors = form.querySelectorAll(".error");
        Array.prototype.forEach.call(errors, function (e) { e.textContent = ""; });
    }

    function showFieldError(field, message) {
        if (!field) { return; }
        field.classList.add("input-error");

        var error = field.parentNode.querySelector(".error");
        if (!error) {
            error = document.createElement("small");
            error.className = "error";
            field.parentNode.appendChild(error);
        }
        error.textContent = message;
    }

    function focusFirstError() {
        var form = document.getElementById("membership-form");
        if (!form) { return; }

        var firstError = form.querySelector(".input-error");
        if (!firstError) { return; }

        if (typeof firstError.focus === "function") { firstError.focus(); }
        if (typeof firstError.scrollIntoView === "function") {
            firstError.scrollIntoView({ behavior: "smooth", block: "center" });
        }
    }

    /* ============================================================
       10. Button & Redirect Helpers
       ============================================================ */
    function setButtonBusy(button, text) {
        if (!button) { return; }
        button.disabled = true;
        button.setAttribute("data-original-text", button.textContent);
        button.textContent = text;
    }

    function restoreButton(button, fallbackText) {
        if (!button) { return; }
        button.disabled = false;
        var originalText = button.getAttribute("data-original-text");
        button.textContent = originalText || fallbackText;
        button.removeAttribute("data-original-text");
    }

    function handleRedirect(toast, url) {
        if (!url) { return; }
        if (toast && typeof toast.then === "function") {
            toast.then(function () { window.location.href = url; });
        } else {
            window.location.href = url;
        }
    }

    /* ============================================================
       11. Notifications
       ============================================================ */
    function showNotice(message, type) {
        if (!message || !notifications) { return null; }
        if (type === "success") { return notifications.success(message, "Success"); }
        if (type === "error") { return notifications.error(message, "Something Went Wrong"); }
        if (type === "warning") { return notifications.warning(message, "Please Check"); }
        return notifications.info(message, "Information");
    }

    function getFailureMessage(response) {
        if (response && response.Message) { return response.Message; }
        if (response && response.responseJSON && response.responseJSON.Message) {
            return response.responseJSON.Message;
        }
        return "The request could not be completed.";
    }

    function showApiError(response) {
        var message = getFailureMessage(response);
        var normalizedMessage = message.toLowerCase();
        if (normalizedMessage.indexOf("this nric has already been registered") >= 0) {
            showNotice(message, "warning");
            return;
        }
        showNotice(message, "error");
    }

    // Initialize on ready
    $(init);

    return { init: init };
}(window, document, jQuery, App.Member.api, App.UI.notifications));