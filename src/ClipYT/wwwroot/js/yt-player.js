﻿var player;
var playerReady = false;
var iframeWindow;

function onYouTubeIframeAPIReady() {
    player = new YT.Player('yt-player', {
        height: '100%',
        width: '100%',
        events: {
            'onReady': () => {
                playerReady = true;
                iframeWindow = player.getIframe().contentWindow;
            },
            'onStateChange': playVideoUntilEndTime,
            'onError': onPlayerError
        }
    });
}

function updateInputFromPlayer(inputElementId) {
    var currentTime = player.getCurrentTime();
    const element = document.getElementById(inputElementId);
    element.value = convertToTimestampFormat(currentTime);


    const e = new Event("change");
    element.dispatchEvent(e); // Manually trigger the 'change' event

    const input = $('#' + inputElementId);
    input.trigger('input'); // To make the clear button appear

    input.valid(); // Manually trigger client-side validation
}

function updatePlayerFromInput(event) {
    const element = $(event.target);
    var test = element.val();
    const elementTimeSeconds = convertToSeconds(element.val());
    if (!isNaN(elementTimeSeconds)) {
        player.seekTo(elementTimeSeconds);
    }
}

function convertToTimestampFormat(seconds) {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const remainingSeconds = Math.floor(seconds % 60);

    const formattedHours = hours < 10 ? `0${hours}` : hours;
    const formattedMinutes = minutes < 10 ? `0${minutes}` : minutes;
    const formattedSeconds = remainingSeconds < 10 ? `0${remainingSeconds}` : remainingSeconds;

    return `${formattedHours}:${formattedMinutes}:${formattedSeconds}`;
}

function convertToSeconds(timestamp) {
    const timeParts = timestamp.split(':');

    const hours = parseInt(timeParts[0], 10) || 0;
    const minutes = parseInt(timeParts[1], 10) || 0;
    const seconds = parseFloat(timeParts[2]) || 0;

    const totalSeconds = (hours * 3600) + (minutes * 60) + seconds;

    return totalSeconds;
}

async function updateVideoFrame(videoUrl) {
    await waitForPlayerToBeLoaded();
    var videoId = getIdFromYoutubeUrl(videoUrl);
    toggleYtVideoValidationError(true);
    player.cueVideoById(videoId);
}

function waitForPlayerToBeLoaded() {
    return new Promise(resolve => {
        const interval = setInterval(() => {
            if (playerReady === true) {
                clearInterval(interval);
                resolve();
            } else {
                console.log("Player not ready");
            }
        }, 100);
    });
}

function getIdFromYoutubeUrl(url) {
    var regExp = /^.*((youtu.be\/)|(v\/)|(\/u\/\w\/)|(embed\/)|(watch\?))\??v?=?([^#&?]*).*/;
    var match = url.match(regExp);

    return (match && match[7].length == 11) ? match[7] : false;
}


var pauseAtEndTime = false;
function startClipPreview() {
    const videoStartTime = $("#videoStartInput").val();
    const videoLength = $("#videoLengthInput").val();
    if (!videoStartTime || videoLength <= 0) {
        return;
    }
    const startTimeSeconds = convertToSeconds(videoStartTime)
    player.seekTo(startTimeSeconds);
    player.playVideo();
    pauseAtEndTime = true;
}

function playVideoUntilEndTime(event) {
    if (event.data == YT.PlayerState.PLAYING && pauseAtEndTime) {
        // Monitor the playback position to pause at the end timestamp
        var checkTime = setInterval(function () {
            var currentTimeSeconds = player.getCurrentTime();
            var endTimeString = $("#videoEndInput").val();
            if (endTimeString) {
                if (currentTimeSeconds >= convertToSeconds(endTimeString)) {
                    player.pauseVideo();
                    pauseAtEndTime = false;
                    clearInterval(checkTime);
                }
            }
        }, 100);
    }
}

function isTimestampPositionValidFromEvent(event) {
    const elementId = event.target.id;
    const element = $('#' + elementId);

    return isTimestampPositionValid(element.val());
}

function isTimestampPositionValid(timestamp) {
    if (!$('#yt-player').is(':visible')) {
        return true;
    }

    const timeStampSeconds = convertToSeconds(timestamp);
    videoLength = getCurrentVideoDuration();

    if (videoLength == 0) {
        return true;
    }

    return timeStampSeconds < videoLength;
}

function getCurrentVideoDuration() {
    return player.getDuration();
}

function onPlayerError(event) {
    console.log(`Error code ${event.data}`)
    if (event.data === 101 || event.data === 150) {
        const errorMessage = "This video is probably age restricted. If so, you cannot download it.";
        console.log(errorMessage);
        $("#yt-video-validation-message").html(`
        <span class="field-validation-error" data-valmsg-replace="true">
            <span id="yt-video-error">${errorMessage}</span>
        </span>
    `);
    } else {
        const errorMessage = "An error occurred: " + event.data;
        console.log(errorMessage);
    }

    toggleYtVideoValidationError(false);
}

function toggleYtVideoValidationError(isValid) {
    if (isValid) {
        //$("#submit-button").prop("disabled", false);
        $("#yt-video-validation-message").hide();
    } else {
        //$("#submit-button").prop("disabled", true);
        $("#yt-video-validation-message").show();
    }
}