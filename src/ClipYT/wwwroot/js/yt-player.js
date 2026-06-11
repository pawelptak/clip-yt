var player;
var playerReady = false;
var pauseAtEndTime = false;
var thumbnail;
var loadingOverlay;

document.addEventListener("DOMContentLoaded", initializePlayer);

function initializePlayer() {
    player = document.getElementById("yt-player");
    thumbnail = document.getElementById("video-thumbnail");
    loadingOverlay = document.getElementById("video-loading-overlay");

    if (!player) {
        return;
    }

    playerReady = true;
    player.addEventListener("timeupdate", playVideoUntilEndTime);
    player.addEventListener("error", onPlayerError);
    player.addEventListener("loadedmetadata", function () {
        toggleYtVideoValidationError(true);
    });
    player.addEventListener("canplay", function () {
        hideLoadingOverlay();
        showPlayer();
    });
}

function updateInputFromPlayer(inputElementId) {
    if (!playerReady) {
        return;
    }

    var currentTime = player.currentTime;
    const element = document.getElementById(inputElementId);
    element.value = convertToTimestampFormat(currentTime);

    const changedEvent = new Event("change");
    element.dispatchEvent(changedEvent);

    const input = $('#' + inputElementId);
    input.trigger('input');
    input.valid();
}

function updatePlayerFromInput(event) {
    if (!playerReady) {
        return;
    }

    const element = $(event.target);
    const elementTimeSeconds = convertToSeconds(element.val());
    if (!isNaN(elementTimeSeconds)) {
        player.currentTime = elementTimeSeconds;
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

async function updateVideoFrame(videoUrl, shouldLoadPreview = true) {
    await waitForPlayerToBeLoaded();
    toggleYtVideoValidationError(true);

    hidePlayer();
    hideThumbnail();
    hideLoadingOverlay();

    $("#player-container").show();
    showLoadingOverlay();

    try {
        const thumbnailPromise = getThumbnailUrl(videoUrl).then(thumbnailUrl => {
            if (thumbnailUrl) {
                return showThumbnail(thumbnailUrl).catch(error => {
                    console.log("Thumbnail failed to load:", error);
                    return null;
                });
            }
            return null;
        });

        let previewPromise;
        if (shouldLoadPreview) {
            previewPromise = getPreviewInfo(videoUrl).then(previewInfo => {
                loadPlayerSource(previewInfo.streamUrl, previewInfo.contentType);
                return previewInfo;
            });
        } else {
            // For platforms without preview (e.g., TikTok)
            // Just wait for thumbnail and hide loading when done
            previewPromise = thumbnailPromise.then(() => {
                hideLoadingOverlay();
                return null;
            });
        }

        await Promise.all([thumbnailPromise, previewPromise]);

    } catch (error) {
        hideLoadingOverlay();
        hideThumbnail();
        if (shouldLoadPreview) {
            showPlayer();
        }
        clearVideoFrame();
        console.log(error);
    }
}

function clearVideoFrame() {
    if (!playerReady) {
        return;
    }

    hideThumbnail();
    hideLoadingOverlay();
    pauseAtEndTime = false;
    player.pause();
    player.removeAttribute("src");
    player.load();
}

function waitForPlayerToBeLoaded() {
    return new Promise(function (resolve) {
        const interval = setInterval(function () {
            if (playerReady === true) {
                clearInterval(interval);
                resolve();
            }
        }, 100);
    });
}

async function getThumbnailUrl(videoUrl) {
    const appData = document.getElementById("app-data");
    const thumbnailUrlEndpoint = appData.getAttribute("data-thumbnail-url");

    try {
        const response = await fetch(`${thumbnailUrlEndpoint}?url=${encodeURIComponent(videoUrl)}`);
        const payload = await response.json();

        if (response.ok && payload.isSuccessful && payload.thumbnailUrl) {
            return payload.thumbnailUrl;
        }
    } catch (error) {
        console.log("Failed to load thumbnail:", error);
    }

    return null;
}

async function getPreviewInfo(videoUrl) {
    const appData = document.getElementById("app-data");
    const previewInfoUrl = appData.getAttribute("data-preview-info-url");
    const response = await fetch(`${previewInfoUrl}?url=${encodeURIComponent(videoUrl)}`);

    const payload = await response.json();
    if (!response.ok || !payload.isSuccessful || !payload.streamUrl) {
        throw new Error(payload.errorMessage || "Unable to resolve preview stream.");
    }

    return payload;
}

function loadPlayerSource(streamUrl, contentType) {
    pauseAtEndTime = false;
    player.pause();
    player.src = streamUrl;
    player.load();
}

function showThumbnail(thumbnailUrl) {
    if (!thumbnail) {
        return Promise.resolve();
    }

    return new Promise((resolve, reject) => {
        thumbnail.onload = () => {
            if (player && player.style.display === "block") {
                resolve();
                return;
            }

            thumbnail.style.display = "block";
            setTimeout(() => {
                thumbnail.classList.add("fade-in");
            }, 10);
            resolve();
        };
        thumbnail.onerror = () => {
            reject(new Error("Failed to load thumbnail"));
        };
        thumbnail.src = thumbnailUrl;
    });
}

function hideThumbnail() {
    if (!thumbnail) {
        return;
    }

    thumbnail.classList.remove("fade-in");
    thumbnail.style.display = "none";
    thumbnail.removeAttribute("src");
}

function showLoadingOverlay() {
    if (!loadingOverlay) {
        return;
    }

    loadingOverlay.style.display = "flex";
}

function hideLoadingOverlay() {
    if (!loadingOverlay) {
        return;
    }

    loadingOverlay.style.display = "none";
}

function showPlayer() {
    if (!player) {
        return;
    }

    hideThumbnail();
    hideLoadingOverlay();
    player.style.display = "block";
}

function hidePlayer() {
    if (!player) {
        return;
    }

    player.style.display = "none";
}

function startClipPreview() {
    if (!playerReady) {
        return;
    }

    const videoStartTime = $("#videoStartInput").val();
    const videoLength = $("#videoLengthInput").val();
    if (!videoStartTime || videoLength <= 0) {
        return;
    }

    const startTimeSeconds = convertToSeconds(videoStartTime);
    player.currentTime = startTimeSeconds;
    pauseAtEndTime = true;
    player.play().catch(function (error) {
        console.log(error);
    });
}

function playVideoUntilEndTime() {
    if (!pauseAtEndTime) {
        return;
    }

    var currentTimeSeconds = player.currentTime;
    var endTimeString = $("#videoEndInput").val();
    if (endTimeString && currentTimeSeconds >= convertToSeconds(endTimeString)) {
        player.pause();
        pauseAtEndTime = false;
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
    const videoLength = getCurrentVideoDuration();

    if (videoLength === 0) {
        return true;
    }

    return timeStampSeconds < videoLength;
}

function getCurrentVideoDuration() {
    if (!playerReady || isNaN(player.duration)) {
        return 0;
    }

    return player.duration;
}

function onPlayerError() {
    hideLoadingOverlay();
    hideThumbnail();
    showPlayer();
    const mediaError = player ? player.error : null;
    let errorMessage = "Unable to play this video preview.";

    if (mediaError && mediaError.code === MediaError.MEDIA_ERR_SRC_NOT_SUPPORTED) {
        errorMessage = "This video preview format is not supported by your browser.";
    }

    setPlayerErrorMessage(errorMessage);
    toggleYtVideoValidationError(false);
}

function setPlayerErrorMessage(errorMessage) {
    $("#yt-video-validation-message").html(`
        <span class="field-validation-error" data-valmsg-replace="true">
            <span id="yt-video-error"></span>
        </span>
    `);

    $("#yt-video-error").text(errorMessage);
}

function toggleYtVideoValidationError(isValid) {
    if (isValid) {
        $("#yt-video-validation-message").hide();
    } else {
        $("#yt-video-validation-message").show();
    }
}