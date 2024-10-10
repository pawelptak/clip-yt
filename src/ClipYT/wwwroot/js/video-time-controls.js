$(document).ready(function () {
    const appData = document.getElementById('app-data');
    const timeFormatRegex = appData.getAttribute('data-time-format-regex');

    function isValidTimeFormat(timeStr) {
        return timeStr.match(timeFormatRegex);
    }
    function setLengthInputDefaultValue(defaultValue) {
        const videoLengthElement = document.getElementById("videoLengthInput");
        videoLengthElement.value = defaultValue;
    }

    function updateVideoLengthInput() {
        const startTimeStr = document.getElementById("videoStartInput").value;
        const endTimeStr = document.getElementById("videoEndInput").value;

        if (!isValidTimeFormat(startTimeStr) || !isValidTimeFormat(endTimeStr)) {
            return;
        }

        const startTime = new Date(`1970-01-01T${startTimeStr}`);
        const endTime = new Date(`1970-01-01T${endTimeStr}`);
        const timeDifference = (endTime - startTime) / 1000;

        document.getElementById("videoLengthInput").value = timeDifference;
        $('#videoLengthInput').valid();
    }

    function updateVideoEndInput() {
        const videoLengthElement = document.getElementById("videoLengthInput")
        if (videoLengthElement.value.length === 0) {
            setLengthInputDefaultValue(10);
        }

        const startTimeStr = document.getElementById("videoStartInput").value;
        const videoLengthStr = videoLengthElement.value;

        if (!isValidTimeFormat(startTimeStr) || isNaN(videoLengthStr)) {
            return;
        }

        const startTime = new Date(`1970-01-01T${startTimeStr}`);
        const videoLengthSeconds = parseInt(videoLengthStr);
        const endTimeMilliseconds = startTime.getTime() + (videoLengthSeconds * 1000);
        const endTime = new Date(endTimeMilliseconds);
        const endTimeFormatted = `${String(endTime.getHours()).padStart(2, '0')}:${String(endTime.getMinutes()).padStart(2, '0')}:${String(endTime.getSeconds()).padStart(2, '0')}`;

        document.getElementById("videoEndInput").value = endTimeFormatted;
        $('#videoLengthInput').valid();
    }

    document.getElementById("videoEndInput").addEventListener("change", updateVideoLengthInput);
    document.getElementById("videoStartInput").addEventListener("change", updateVideoLengthInput);
    document.getElementById("videoStartInput").addEventListener("change", updateVideoEndInput);
    document.getElementById("videoLengthInput").addEventListener("change", updateVideoEndInput);
});