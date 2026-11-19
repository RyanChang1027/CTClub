var App = App || {};
App.Base = App.Base || {};

App.Base.api = (function ($) {
    "use strict";

    /*
     * Sends an AJAX request to the MVC API.
     *
     * The API returns:
     *
     * {
     *     Success: true or false,
     *     Message: "...",
     *     Data: { ... }
     * }
     *
     * Unsuccessful API responses are routed to
     * the jQuery fail() callback.
     */
    function request(
        url,
        method,
        data
    ) {
        var options = {
            url: url,
            method: method,
            dataType: "json",
            headers: {
                "Accept": "application/json"
            }
        };

        if (data !== undefined) {
            options.data =
                JSON.stringify(data);

            options.contentType =
                "application/json; charset=utf-8";
        }

        return $.ajax(options)
            .then(
                function (response) {
                    if (!response ||
                        response.Success !== true) {
                        return $.Deferred()
                            .reject(response)
                            .promise();
                    }

                    return response;
                },
                function (xhr) {
                    return $.Deferred()
                        .reject(xhr)
                        .promise();
                });
    }

    return {
        get: function (url) {
            return request(
                url,
                "GET");
        },

        postJson: function (
            url,
            data
        ) {
            return request(
                url,
                "POST",
                data);
        },

        putJson: function (
            url,
            data
        ) {
            return request(
                url,
                "PUT",
                data);
        },

        delete: function (url) {
            return request(
                url,
                "DELETE");
        }
    };
}(jQuery));
