var App = App || {};
App.UI = App.UI || {};

App.UI.notifications = (function (
    window
) {
    "use strict";

    function show(
        icon,
        title,
        message,
        confirmButtonText
    ) {
        if (!window.Swal) {
            window.alert(
                message || title);

            return;
        }

        return window.Swal.fire({
            icon: icon,
            title: title,
            text: message,
            confirmButtonText:
                confirmButtonText || "Okay"
        });
    }

    function success(
        message,
        title
    ) {
        return show(
            "success",
            title || "Success",
            message,
            "Got it!");
    }

    function info(
        message,
        title
    ) {
        return show(
            "info",
            title || "Information",
            message,
            "Got it!");
    }

    function warning(
        message,
        title
    ) {
        return show(
            "warning",
            title || "Please Check",
            message,
            "Got it!");
    }

    function error(
        message,
        title
    ) {
        return show(
            "error",
            title || "Something Went Wrong",
            message,
            "Okay");
    }

    return {
        show: show,
        success: success,
        info: info,
        warning: warning,
        error: error
    };
}(window));
