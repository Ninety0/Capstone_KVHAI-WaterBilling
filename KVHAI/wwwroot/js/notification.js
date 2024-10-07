$(document).ready(function () {
    $(document).on('click', '#btn_notification', function (e) {
        $('.notification_body').fadeToggle();
        e.stopPropagation(); // Prevent this click from being detected by the document
    }); 

    $(document).on('click', '.notification_container', function (e) {
        $('.notification_body').fadeToggle();
        e.stopPropagation(); // Prevent this click from being detected by the document
    });

    $(document).on('click', function (e) {
        var notificationBody = $('.notification_body');

        // Check if notification body is visible and the click is outside of it
        if (notificationBody.is(':visible') && !notificationBody.is(e.target) && notificationBody.has(e.target).length === 0) {
            notificationBody.fadeOut();
        }
    });
});