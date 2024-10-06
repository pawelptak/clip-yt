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

    const youtubePlatformSource = new MediaPlatformSource(ytRegex, clipytLogoUrl, true, true, clipytAccentColor, clipytAccentColorDark);
    const tiktokPlatformSource = new MediaPlatformSource(tiktokRegex, cliptokLogoUrl, false, false, "#6020f3", "#351287");
    const twitterPlatformSource = new MediaPlatformSource(twitterRegex, clipxLogoUrl, true, false, "#1DA1F2", "#2f62b5");

    const platforms = [youtubePlatformSource, tiktokPlatformSource, twitterPlatformSource];

    var playerContainer = $('#player-container');
    $("#urlInput").on('input', function () {
        var inputUrl = $(this).val();

        for (let platform of platforms) {
            if (inputUrl.match(platform.regex)) {
                $(".twitter-tweet").remove();

                if (platform.regex == ytRegex) {
                    $('#yt-player').show();
                    playerContainer.css('display', '');
                    updateVideoFrame(inputUrl);
                }

                if (platform.regex == twitterRegex) {
                    $('#yt-player').hide();
                    var twitterElement = createEmbeddedTwitterElement(transformTwitterUrlForEmbedding(inputUrl));
                    playerContainer.append(twitterElement);
                    playerContainer.css('display', 'contents');
                    twttr.widgets.load();
                } 
                platform.setUiMode()
                break;
            }
        }
    });
});

class MediaPlatformSource {
    constructor(regex, logoUrl, showPlayer, showTimestampButtons, accentColorCode, accentColorDarkCode) {
        this.regex = regex;
        this.logoUrl = logoUrl;
        this.showPlayer = showPlayer;
        this.showTimestampButtons = showTimestampButtons;
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
        } else {
            $("#player-container").hide();

        }

        if (this.showTimestampButtons) {
            $("#get-current-end-btn").show();
            $("#get-current-start-btn").show();
        } else {
            $("#get-current-end-btn").hide();
            $("#get-current-start-btn").hide();
        }
    }
}

function transformTwitterUrlForEmbedding(url) {
    // Extract the tweet ID
    const tweetIdMatch = url.match(/status\/(\d+)/);

    if (tweetIdMatch && tweetIdMatch[1]) {
        const tweetId = tweetIdMatch[1];

        return `https://twitter.com/i/status/${tweetId}`;
    } else {
        console.warn('Invalid Twitter URL. Could not extract tweet ID.');

        return url;
    }
}

function createEmbeddedTwitterElement(url, playerContainer) {
    var blockquote = $('<blockquote>', {
        class: 'twitter-tweet',
        'data-media-max-width': '640',
        align: 'center',
        dnt: 'false'
    });

    var videoAnchor = $('<a>', {
        href: url,
        text: 'Tweet link'
    });

    blockquote.append(videoAnchor);

    return blockquote;
}