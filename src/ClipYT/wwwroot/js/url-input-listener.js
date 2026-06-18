$(document).ready(function () {
    const appData = document.getElementById('app-data');
    const ytRegex = appData.getAttribute('data-yt-regex');
    const tiktokRegex = appData.getAttribute('data-tiktok-regex');
    const twitterRegex = appData.getAttribute('data-twitter-regex');
    const instagramRegex = appData.getAttribute('data-instagram-regex');
    const facebookRegex = appData.getAttribute('data-facebook-regex');
    const clipytLogoUrl = appData.getAttribute('data-clipyt-logo');
    const cliptokLogoUrl = appData.getAttribute('data-cliptok-logo');
    const clipxLogoUrl = appData.getAttribute('data-clipx-logo');
    const clipstagramLogoUrl = appData.getAttribute('data-clipstagram-logo');
    const clipfbLogoUrl = appData.getAttribute('data-clipfb-logo');

    const clipytAccentColor = getComputedStyle(document.documentElement).getPropertyValue('--accent-color').trim();
    const clipytAccentColorDark = getComputedStyle(document.documentElement).getPropertyValue('--accent-color-dark').trim();
    const clipytAccentColorHighlight = getComputedStyle(document.documentElement).getPropertyValue('--accent-color-highlight').trim();

    const youtubePlatformSource = new MediaPlatformSource(ytRegex, clipytLogoUrl, true, true, true, clipytAccentColor, clipytAccentColorDark, clipytAccentColorHighlight);
    const tiktokPlatformSource = new MediaPlatformSource(tiktokRegex, cliptokLogoUrl, true, true, false, "#6020f3", "#351287", "#871248");
    const twitterPlatformSource = new MediaPlatformSource(twitterRegex, clipxLogoUrl, true, true, true, "#1DA1F2", "#2f62b5", "#01a55c");
    const instagramPlatformSource = new MediaPlatformSource(instagramRegex, clipstagramLogoUrl, true, true, false, "#a83299", "#8c2a7f", "#017fa5");
    const facebookPlatformSource = new MediaPlatformSource(facebookRegex, clipfbLogoUrl, true, true, false, "#ff3796", "#b80060", "#00c784");

    const platforms = [youtubePlatformSource, tiktokPlatformSource, twitterPlatformSource, instagramPlatformSource, facebookPlatformSource];

    $("#urlInput").on('input', function () {
        var inputUrl = $(this).val();

        for (let platform of platforms) {
            if (inputUrl.match(platform.regex)) {
                handlePlatformPreview(platform, inputUrl);
                platform.setUiMode();

                break;
            }
            else {
                resetUi();
            }
        }
    });

    function handlePlatformPreview(platform, inputUrl) {
        setPlayerContainerStyle();
        updateVideoFrame(inputUrl, platform.showPlayer);
    }

    function setPlayerContainerStyle() {
        var container = $('#player-container');
        container.removeClass('default-style');
        container.addClass('default-style');
    }

    function resetUi() {
        $("#player-container").attr("style", "display: none !important");
        $("#video-details").hide();
        toggleYtVideoValidationError(true);
        clearVideoFrame();

        // Clear all clip inputs and hidden precise timestamps
        $("#videoStartInput").val('');
        $("#videoEndInput").val('');
        $("#videoLengthInput").val('');
        $("#preciseStartTimestamp").val('');
        $("#preciseEndTimestamp").val('');
    }

    $("input[name='Format']").on('change', function () {
        var inputUrl = $("#urlInput").val();

        if (!inputUrl || !inputUrl.match(ytRegex)) {
            return;
        }

        var selectedFormat = $("input[name='Format']:checked").val();
        if (selectedFormat === "MP3") {
            toggleQualitySelectorAvailability(false);
        } else {
            toggleQualitySelectorAvailability(true);
        }
    });
});

class MediaPlatformSource {
    /**
     * @param {string} regex - URL regex pattern for platform detection
     * @param {string} logoUrl - Platform logo URL
     * @param {boolean} showPlayer - Whether to load video preview (thumbnail always loads)
     * @param {boolean} showClipButtons - Whether to show clip editing buttons
     * @param {boolean} enableQualitySelector - Whether to enable quality selector
     * @param {string} accentColorCode - Primary accent color
     * @param {string} accentColorDarkCode - Dark accent color
     * @param {string} accentColorHighlight - Highlight accent color
     */
    constructor(regex, logoUrl, showPlayer, showClipButtons, enableQualitySelector, accentColorCode, accentColorDarkCode, accentColorHighlight) {
        this.regex = regex;
        this.logoUrl = logoUrl;
        this.showPlayer = showPlayer;
        this.showClipButtons = showClipButtons;
        this.showQualitySelector = enableQualitySelector;
        this.accentColorCode = accentColorCode;
        this.accentColorDarkCode = accentColorDarkCode;
        this.accentColorHighlight = accentColorHighlight;
    }

    setUiMode() {
        $("#video-details").show();
        document.documentElement.style.setProperty('--accent-color', this.accentColorCode);
        document.documentElement.style.setProperty('--accent-color-dark', this.accentColorDarkCode);
        document.documentElement.style.setProperty('--accent-color-highlight', this.accentColorHighlight);
        $("#logo-img").attr('src', this.logoUrl);


        if (this.showClipButtons) {
            $("#get-current-end-btn").show();
            $("#get-current-start-btn").show();
            $("#clip-preview-button").show();
        } else {
            $("#get-current-end-btn").hide();
            $("#get-current-start-btn").hide();
            $("#clip-preview-button").hide();
        }

        toggleQualitySelectorAvailability(this.showQualitySelector);
    }
}