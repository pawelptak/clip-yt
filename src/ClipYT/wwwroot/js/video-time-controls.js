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
        const endTimeStr = $("#videoEndInput").val();

        if (!isValidTimeFormat(startTimeStr) || !isValidTimeFormat(endTimeStr)) {
            return;
        }

        const startTime = new Date(`1970-01-01T${startTimeStr}`);
        const endTime = new Date(`1970-01-01T${endTimeStr}`);
        const timeDifference = (endTime - startTime) / 1000;

        const videoLengthInput = $("#videoLengthInput");
        videoLengthInput.val(timeDifference);
        videoLengthInput.trigger('input'); // To make the clear button appear
        $('#videoLengthInput').valid();
    }

    function updateVideoEndInput() {
        const videoLengthInput = $("#videoLengthInput");
        if (!videoLengthInput.val()) {
            setLengthInputDefaultValue(10);
        }

        const startTimeStr = $("#videoStartInput").val();
        const videoLengthStr = videoLengthInput.val();

        if (!isValidTimeFormat(startTimeStr) || isNaN(videoLengthStr)) {
            return;
        }

        const startTime = new Date(`1970-01-01T${startTimeStr}`);
        const videoLengthSeconds = parseInt(videoLengthStr);
        const endTimeMilliseconds = startTime.getTime() + (videoLengthSeconds * 1000);
        const endTime = new Date(endTimeMilliseconds);
        const endTimeFormatted = `${String(endTime.getHours()).padStart(2, '0')}:${String(endTime.getMinutes()).padStart(2, '0')}:${String(endTime.getSeconds()).padStart(2, '0')}`;

        const videoEndInput = $("#videoEndInput");
        videoEndInput.val(endTimeFormatted);
        videoEndInput.trigger('input'); // To make the clear button appear
        videoLengthInput.valid();
    }

    $("#videoEndInput").on("change", updateVideoLengthInput);
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