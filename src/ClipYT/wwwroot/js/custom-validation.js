$.validator.addMethod("timestamp", function (value, element) {
    const event = { target: element };
    const isValid = isTimestampPositionValid(event);
    return isValid;
});

$.validator.unobtrusive.adapters.addBool("timestamp");