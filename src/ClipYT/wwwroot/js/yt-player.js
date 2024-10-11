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
            }
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

function convertToTimestampFormat(seconds) {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const remainingSeconds = Math.floor(seconds % 60);

    const formattedHours = hours < 10 ? `0${hours}` : hours;
    const formattedMinutes = minutes < 10 ? `0${minutes}` : minutes;
    const formattedSeconds = remainingSeconds < 10 ? `0${remainingSeconds}` : remainingSeconds;

    return `${formattedHours}:${formattedMinutes}:${formattedSeconds}`;
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