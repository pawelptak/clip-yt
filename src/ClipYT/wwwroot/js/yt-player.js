var player;
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
            'onStateChange': playVideoUntilEndTime
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

function isTimestampPositionValid(event) {
    if (!$('#yt-player').is(':visible')) {
        return true;
    }

    const elementId = event.target.id;
    const timestampElement = $('#' + elementId);
    const timeStampSeconds = convertToSeconds(timestampElement.val());
    videoLength = player.getDuration();

    return timeStampSeconds < videoLength;
}