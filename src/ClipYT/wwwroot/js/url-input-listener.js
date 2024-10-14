$(document).ready(function () {
    const appData = document.getElementById('app-data');
    const ytRegex = appData.getAttribute('data-yt-regex');
    const tiktokRegex = appData.getAttribute('data-tiktok-regex');
    const twitterRegex = appData.getAttribute('data-twitter-regex');
    const instagramRegex = appData.getAttribute('data-instagram-regex');
    const clipytLogoUrl = appData.getAttribute('data-clipyt-logo');
    const cliptokLogoUrl = appData.getAttribute('data-cliptok-logo');
    const clipxLogoUrl = appData.getAttribute('data-clipx-logo');
    const clipstagramLogoUrl = appData.getAttribute('data-clipstagram-logo');

    const clipytAccentColor = getComputedStyle(document.documentElement).getPropertyValue('--accent-color').trim();
    const clipytAccentColorDark = getComputedStyle(document.documentElement).getPropertyValue('--accent-color-dark').trim();

    const youtubePlatformSource = new MediaPlatformSource(ytRegex, clipytLogoUrl, true, true, clipytAccentColor, clipytAccentColorDark);
    const tiktokPlatformSource = new MediaPlatformSource(tiktokRegex, cliptokLogoUrl, false, false, "#6020f3", "#351287");
    const twitterPlatformSource = new MediaPlatformSource(twitterRegex, clipxLogoUrl, true, false, "#1DA1F2", "#2f62b5");
    const instagramPlatformSource = new MediaPlatformSource(instagramRegex, clipstagramLogoUrl, true, false, "#a83299", "#8c2a7f");

    const platforms = [youtubePlatformSource, tiktokPlatformSource, twitterPlatformSource, instagramPlatformSource];

    var playerContainer = $('#player-container');
    $("#urlInput").on('input', function () {
        var inputUrl = $(this).val();

        for (let platform of platforms) {
            if (inputUrl.match(platform.regex)) {
                handlePlatformEmbed(platform, inputUrl);
                platform.setUiMode();

                break;
            }
            else {
                resetUi();

            }
        }
    });

    function handlePlatformEmbed(platform, inputUrl) {
        $(".twitter-tweet").remove();
        $(".instagram-media").remove();
        setPlayerContainerStyle(isDefault = true)
        $('#yt-player').hide();

        switch (platform.regex) {
            case ytRegex:
                $('#yt-player').show();
                playerContainer.css('display', '');
                updateVideoFrame(inputUrl);
                break;

            case twitterRegex:
                var twitterElement = createEmbeddedTwitterElement(transformTwitterUrlForEmbedding(inputUrl));
                playerContainer.append(twitterElement);
                setPlayerContainerStyle(isDefault = false);
                twttr.widgets.load();
                break;

            case instagramRegex:
                var instagramElement = createEmbeddedInstagramElement(inputUrl);
                playerContainer.append(instagramElement);
                setPlayerContainerStyle(isDefault = false);
                if (window.instgrm) {
                    window.instgrm.Embeds.process();
                }
                break;
        }
    }

    function setPlayerContainerStyle(isDefault) {
        var container = $('#player-container');
        container.removeClass('default-style flex-style');
        if (isDefault) {
            container.addClass('default-style');
        } else {
            container.addClass('flex-style');
        }
    }

    function resetUi() {
        $("#player-container").hide();
        $("#video-details").hide();

        // Clear all clip inputs
        $("#videoStartInput").val('');
        $("#videoEndInput").val('');
        $("#videoLengthInput").val('');
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

    function createEmbeddedTwitterElement(url) {
        const blockquote = $('<blockquote>', {
            class: 'twitter-tweet',
            'data-media-max-width': '640',
            align: 'center',
            dnt: 'false'
        });

        const videoAnchor = $('<a>', {
            href: url,
        });

        blockquote.append(videoAnchor);

        return blockquote;
    }

    function createEmbeddedInstagramElement(url) {
        const blockquote = $('<blockquote>', {
            class: 'instagram-media',
            'data-instgrm-permalink': url,
        });

        return blockquote;
    }
});

class MediaPlatformSource {
    constructor(regex, logoUrl, showPlayer, showClipButtons, accentColorCode, accentColorDarkCode) {
        this.regex = regex;
        this.logoUrl = logoUrl;
        this.showPlayer = showPlayer;
        this.showClipButtons = showClipButtons;
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

        if (this.showClipButtons) {
            $("#get-current-end-btn").show();
            $("#get-current-start-btn").show();
            $("#clip-preview-button").show();
        } else {
            $("#get-current-end-btn").hide();
            $("#get-current-start-btn").hide();
            $("#clip-preview-button").hide();
        }
    }
}