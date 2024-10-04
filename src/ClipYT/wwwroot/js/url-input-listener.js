$(document).ready(function () {
    const appData = document.getElementById('app-data');
    const ytRegex = appData.getAttribute('data-yt-regex');
    const tiktokRegex = appData.getAttribute('data-tiktok-regex');
    const twitterRegex = appData.getAttribute('data-twitter-regex');
    const clipytLogoUrl = appData.getAttribute('data-clipyt-logo');
    const cliptokLogoUrl = appData.getAttribute('data-cliptok-logo');
    const clipxLogoUrl = appData.getAttribute('data-clipx-logo');

    var clipytAccentColor = getComputedStyle(document.documentElement).getPropertyValue('--accent-color').trim();
    var clipytAccentColorDark = getComputedStyle(document.documentElement).getPropertyValue('--accent-color-dark').trim();

    const youtubePlatformSource = new MediaPlatformSource(ytRegex, clipytLogoUrl, true, clipytAccentColor, clipytAccentColorDark);
    const tiktokPlatformSource = new MediaPlatformSource(tiktokRegex, cliptokLogoUrl, false, "#6020f3", "#351287");
    const twitterPlatformSource = new MediaPlatformSource(twitterRegex, clipxLogoUrl, false, "#1DA1F2", "#2f62b5");

    const platforms = [youtubePlatformSource, tiktokPlatformSource, twitterPlatformSource];

    $("#urlInput").on('input', function () {
        var inputUrl = $(this).val();

        for (let platform of platforms) {
            if (inputUrl.match(platform.regex)) {
                if (platform.regex == ytRegex) {
                    updateVideoFrame(inputUrl);
                }
                platform.setUiMode()
                break;
            }
        }
    });
});

class MediaPlatformSource {
    constructor(regex, logoUrl, showPlayer, accentColorCode, accentColorDarkCode) {
        this.regex = regex;
        this.logoUrl = logoUrl;
        this.showPlayer = showPlayer;
        this.accentColorCode = accentColorCode;
        this.accentColorDarkCode = accentColorDarkCode;
    }

    setUiMode() {
        $("#video-details").show();
        document.documentElement.style.setProperty('--accent-color', this.accentColorCode);
        document.documentElement.style.setProperty('--accent-color-dark', this.accentColorDarkCode);
        $("#logo-img").attr('src', this.logoUrl);

        if (this.showPlayer) {
            $("#player-container").show();
            $("#get-current-end-btn").show();
            $("#get-current-start-btn").show();
        } else {
            $("#player-container").hide();
            $("#get-current-end-btn").hide();
            $("#get-current-start-btn").hide();
        }
    }
}