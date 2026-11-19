var App = App || {};
App.Member = App.Member || {};

App.Member.api = (function (
    $,
    baseApi
) {
    "use strict";

    var config =
        $("#api-config");

    if (!config.length) {
        throw new Error(
            "The #api-config element was not found.");
    }

    if (!baseApi) {
        throw new Error(
            "App.Base.api is unavailable. " +
            "Load app.base.js before member.api.js.");
    }

    var urls = {
        saveDraft:
            config.data("url-save-draft"),

        submit:
            config.data("url-submit"),

        staffDetails:
            config.data("url-staff-details")
    };

    if (!urls.saveDraft) {
        throw new Error(
            "The SaveDraft API URL is missing.");
    }

    if (!urls.submit) {
        throw new Error(
            "The Submit API URL is missing.");
    }

    if (!urls.staffDetails) {
        throw new Error(
            "The GetStaffDetails API URL is missing.");
    }

    return {
        /*
         * Creates a new draft or updates an existing draft.
         */
        saveDraft: function (model) {
            return baseApi.postJson(
                urls.saveDraft,
                model);
        },

        /*
         * Creates a new application or submits an
         * existing saved draft.
         *
         * The method name is submit.
         * Do not call api.subit().
         */
        submit: function (model) {
            return baseApi.postJson(
                urls.submit,
                model);
        },

        /*
         * Retrieves staff information.
         */
        getStaffDetails: function (nric) {
            return baseApi.get(
                urls.staffDetails +
                "?NRIC=" +
                encodeURIComponent(nric));
        }
    };
}(
    jQuery,
    App.Base.api
));
