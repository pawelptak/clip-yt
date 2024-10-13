$(document).ready(function () {
    const appData = document.getElementById('app-data');
    const timeFormatRegex = appData.getAttribute('data-time-format-regex');

    function isValidTimeFormat(timeStr) {
        return timeStr.match(timeFormatRegex);
    }
    function setLengthInputDefaultValue(defaultValue) {
        const videoLengthInput = $("#videoLengthInput");
        videoLengthInput.val(defaultValue);
        videoLengthInput.trigger('input'); // To make the clear button appear
    }

    function updateVideoLengthInput() {
        const startTimeStr = $("#videoStartInput").val();
        var endTimeStr = $("#videoEndInput").val();

        if (!isValidTimeFormat(startTimeStr) || !isValidTimeFormat(endTimeStr)) {
            return;
        }

        const startTimeInSeconds = convertToSeconds(startTimeStr);
        const endTimeInSeconds = convertToSeconds(endTimeStr);

        const timeDifference = endTimeInSeconds - startTimeInSeconds;

        const videoLengthInput = $("#videoLengthInput");
        videoLengthInput.val(timeDifference);
        videoLengthInput.trigger('input'); // To make the clear button appear
        videoLengthInput.valid();
    }

    function updateVideoEndInput() {
        const videoLengthInput = $("#videoLengthInput");
        const startTimeStr = $("#videoStartInput").val();

        if (!isValidTimeFormat(startTimeStr) || isNaN(videoLengthInput.val())) {
            return;
        }

        if (!videoLengthInput.val()) {
            setLengthInputDefaultValue(10);
        }

        const startTimeInSeconds = convertToSeconds(startTimeStr);
        const videoLengthSeconds = parseInt(videoLengthInput.val(), 10);

        const endTimeInSeconds = startTimeInSeconds + videoLengthSeconds;
        var endTimeFormatted = convertToTimestampFormat(endTimeInSeconds);
        endTimeFormatted = clipToVideoLength(endTimeFormatted);

        const videoEndInput = $("#videoEndInput");
        videoEndInput.val(endTimeFormatted);
        updateVideoLengthInput(); // In case the end time has been clipped, update the length value
        videoEndInput.trigger('input'); // To make the clear button appear
        videoEndInput.valid();
        videoLengthInput.valid();
    }

    function clipToVideoLength(timeInputValue) {
        if (timeInputValue) {
            if (!isTimestampPositionValid(timeInputValue)) {
                return convertToTimestampFormat(getCurrentVideoDuration());
            }
        }

        return timeInputValue;
    }

    $("#videoEndInput").on("change", updateVideoLengthInput);
    $("#videoEndInput").on("change", function () {
        var currentValue = $(this).val();
        $(this).val(clipToVideoLength(currentValue));
    });
    $("#videoStartInput").on("change", updateVideoLengthInput);
    $("#videoStartInput").on("change", updateVideoEndInput);
    $("#videoLengthInput").on("change", updateVideoEndInput);

    //$("#videoStartInput").on("blur", updatePlayerFromInput); // Experimental feature. Won't delete.

    Inputmask("99:99:99", {
        insertMode: false,
        showMaskOnHover: false,
        clearIncomplete: true,
    }).mask($(".time-input"));
});