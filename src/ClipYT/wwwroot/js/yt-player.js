var player;
var playerReady = false;
var pauseAtEndTime = false;
var thumbnail;
var loadingOverlay;
var loadingTextEl;
var videoTitleOverlay;
var videoTitleText;

document.addEventListener("DOMContentLoaded", initializePlayer);

function initializePlayer() {
    player = document.getElementById("yt-player");
    thumbnail = document.getElementById("video-thumbnail");
    loadingOverlay = document.getElementById("video-loading-overlay");
    loadingTextEl = loadingOverlay ? loadingOverlay.querySelector(".loading-text") : null;
    videoTitleOverlay = document.getElementById("video-title-overlay");
    videoTitleText = document.getElementById("video-title-text");

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
        $("#video-details").show();
        toggleSubmitButton(true);
    });
}

function updateInputFromPlayer(inputElementId) {
    if (!playerReady) {
        return;
    }

    var currentTime = player.currentTime;
    const element = document.getElementById(inputElementId);

    // Set precise timestamp (with milliseconds) in hidden input
    const preciseTimestamp = convertToTimestampFormat(currentTime);
    const hiddenInputId = inputElementId === 'videoStartInput' ? 'preciseStartTimestamp' : 'preciseEndTimestamp';
    const hiddenInput = document.getElementById(hiddenInputId);
    if (hiddenInput) {
        hiddenInput.value = preciseTimestamp;
    }

    // Set display value (without milliseconds) in visible input
    element.value = convertToTimestampFormatWithoutMilliseconds(currentTime);

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
    const milliseconds = Math.floor((seconds % 1) * 1000);

    const formattedHours = hours < 10 ? `0${hours}` : hours;
    const formattedMinutes = minutes < 10 ? `0${minutes}` : minutes;
    const formattedSeconds = remainingSeconds < 10 ? `0${remainingSeconds}` : remainingSeconds;
    const formattedMilliseconds = milliseconds.toString().padStart(3, '0');

    return `${formattedHours}:${formattedMinutes}:${formattedSeconds}.${formattedMilliseconds}`;
}

function convertToTimestampFormatWithoutMilliseconds(seconds) {
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

    const secondsPart = timeParts[2] || '0';
    const [seconds, milliseconds] = secondsPart.split('.');

    const secondsValue = parseFloat(seconds) || 0;
    const millisecondsValue = milliseconds ? parseFloat(`0.${milliseconds}`) : 0;

    const totalSeconds = (hours * 3600) + (minutes * 60) + secondsValue + millisecondsValue;

    return totalSeconds;
}

function getPreciseTimeFromInput(inputElementId) {
    // Try to get precise value from hidden input first
    const hiddenInputId = inputElementId === 'videoStartInput' ? 'preciseStartTimestamp' : 'preciseEndTimestamp';
    const hiddenInput = document.getElementById(hiddenInputId);

    if (hiddenInput && hiddenInput.value) {
        return convertToSeconds(hiddenInput.value);
    }

    // Fallback to visible input value
    const element = document.getElementById(inputElementId);
    if (!element) {
        return 0;
    }

    return convertToSeconds(element.value);
}

async function updateVideoFrame(videoUrl, shouldLoadPreview = true) {
    await waitForPlayerToBeLoaded();
    if (typeof connectToHub === 'function') {
        connectToHub();
    }
    toggleYtVideoValidationError(true);

    hidePlayer();
    hideThumbnail();
    hideLoadingOverlay();
    hideVideoTitle();

    $("#video-details").hide();
    toggleSubmitButton(false);

    $("#player-container").show();
    showLoadingOverlay();

    try {
        // Thumbnail i title pobierają się w tle - nie blokujemy na nich
        getThumbnailUrl(videoUrl)
            .then(thumbnailUrl => {
                if (thumbnailUrl) {
                    return showThumbnail(thumbnailUrl);
                }
                return null;
            })
            .catch(error => {
                console.log("Thumbnail failed to load:", error);
            });

        getVideoTitle(videoUrl)
            .then(title => {
                if (title) {
                    showVideoTitle(title);
                }
            })
            .catch(error => {
                console.log("Title failed to load:", error);
            });

        // Czekamy TYLKO na preview - to najważniejsze
        if (shouldLoadPreview) {
            const previewInfo = await getPreviewInfo(videoUrl);
            loadPlayerSource(previewInfo.streamUrl, previewInfo.contentType);
        } else {
            // Dla platform bez preview - od razu pokazujemy UI
            hideLoadingOverlay();
            $("#video-details").show();
            toggleSubmitButton(true);
        }

    } catch (error) {
        hideLoadingOverlay();
        hideThumbnail();
        hideVideoTitle();
        if (shouldLoadPreview) {
            showPlayer();
            const errorMessage = error.message || "Unable to load video preview.";
            setPlayerErrorMessage(errorMessage);
            toggleYtVideoValidationError(false);
            $("#video-details").show();
        } else {
            $("#video-details").hide();
        }
        toggleSubmitButton(false);
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
    hideVideoTitle();
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

async function getVideoTitle(videoUrl) {
    const appData = document.getElementById("app-data");
    const videoTitleEndpoint = appData.getAttribute("data-video-title-url");

    try {
        const response = await fetch(`${videoTitleEndpoint}?url=${encodeURIComponent(videoUrl)}`);
        const payload = await response.json();

        if (response.ok && payload.isSuccessful && payload.title) {
            return payload.title;
        }
    } catch (error) {
        console.log("Failed to load video title:", error);
    }

    return null;
}

async function getPreviewInfo(videoUrl, retryCount = 0, maxRetries = 2) {
    await connectToHub();
    const appData = document.getElementById("app-data");
    const previewInfoUrl = appData.getAttribute("data-preview-info-url");
    const connectionId = connection.connectionId || '';

    try {
        const response = await fetch(`${previewInfoUrl}?url=${encodeURIComponent(videoUrl)}&connectionId=${encodeURIComponent(connectionId)}`);

        if (!response.ok) {
            const payload = await response.json().catch(() => ({ errorMessage: null }));
            const errorMessage = payload.errorMessage || `Server returned error: ${response.status}`;

            if (response.status === 400) {
                throw new Error(errorMessage);
            }

            if (retryCount < maxRetries && (response.status === 500 || response.status === 503)) {
                console.log(`Preview failed (${response.status}), retrying... (${retryCount + 1}/${maxRetries})`);
                await sleep(1000 * (retryCount + 1));
                return getPreviewInfo(videoUrl, retryCount + 1, maxRetries);
            }

            throw new Error(errorMessage);
        }

        const payload = await response.json();
        if (!payload.isSuccessful || !payload.streamUrl) {
            throw new Error(payload.errorMessage || "Unable to resolve preview stream.");
        }

        return payload;
    } catch (error) {
        if (retryCount < maxRetries && error.name === 'TypeError') {
            console.log(`Network error, retrying... (${retryCount + 1}/${maxRetries})`);
            await sleep(1000 * (retryCount + 1));
            return getPreviewInfo(videoUrl, retryCount + 1, maxRetries);
        }
        throw error;
    }
}

function sleep(milliseconds) {
    return new Promise(resolve => setTimeout(resolve, milliseconds));
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

function showVideoTitle(title) {
    if (!videoTitleOverlay || !videoTitleText) {
        return;
    }

    videoTitleText.textContent = title;
    videoTitleOverlay.style.display = "block";
    setTimeout(() => {
        videoTitleOverlay.classList.add("fade-in");
    }, 10);
}

function hideVideoTitle() {
    if (!videoTitleOverlay || !videoTitleText) {
        return;
    }

    videoTitleOverlay.classList.remove("fade-in");
    videoTitleOverlay.style.display = "none";
    videoTitleText.textContent = "";
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
    if (loadingTextEl) {
        loadingTextEl.textContent = "Loading preview...";
    }
}

function showPlayer() {
    if (!player) {
        return;
    }

    hideThumbnail();
    hideLoadingOverlay();
    player.style.display = "block";
}

function toggleSubmitButton(enable) {
    const submitButton = document.getElementById("submit-button");
    if (!submitButton) {
        return;
    }

    if (enable) {
        submitButton.style.display = "flex";
    } else {
        submitButton.style.display = "none";
    }
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

    const startTimeSeconds = getPreciseTimeFromInput('videoStartInput');
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
    if (endTimeString) {
        var endTimeSeconds = getPreciseTimeFromInput('videoEndInput');
        if (currentTimeSeconds >= endTimeSeconds - 0.05) {
            player.currentTime = endTimeSeconds;
            player.pause();
            pauseAtEndTime = false;
        }
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