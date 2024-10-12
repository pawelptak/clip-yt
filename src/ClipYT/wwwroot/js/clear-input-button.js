$(document).ready(function () {
    $(document).on('input', '.clear-input-container input', function (e) {
        const input = $(this);
        if (input.val() && !input.hasClass("clear-input--touched")) {
            input.addClass("clear-input--touched");
        } else if (!input.val() && input.hasClass("clear-input--touched")) {
            input.removeClass("clear-input--touched");
        }
    });

    $(document).on('click', '.clear-input-button', function (e) {
        const input = $(this).closest('.clear-input-container').find('input');
        if (input.length > 0) {
            input.val('').focus().removeClass("clear-input--touched");
        }
    });

    $(document).on('mousedown', '.clear-input-button', function (e) {
        e.preventDefault(); // Prevents other inputs from updating when clicking the clear button
    });
});
