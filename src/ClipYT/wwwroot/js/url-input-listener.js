$(document).ready(function () {
    const appData = document.getElementById('app-data');
    const ytRegex = appData.getAttribute('data-yt-regex');
    const tiktokRegex = appData.getAttribute('data-tiktok-regex');
    const twitterRegex = appData.getAttribute('data-twitter-regex');
    const clipytLogoUrl = appData.getAttribute('data-clipyt-logo');
    const cliptokLogoUrl = appData.getAttribute('data-cliptok-logo');


    // TODO: make it smarter, idk how
    const platforms = [
        {
            regex: ytRegex,
            setMode: setClipytMode,
            logoUrl: clipytLogoUrl,
            showPlayer: true,
        },
        {
            regex: tiktokRegex,
            setMode: setCliptokMode,
            logoUrl: cliptokLogoUrl,
            showPlayer: false,
        },
        {
            regex: twitterRegex,
            setMode: setTwitterMode,
            showPlayer: false,
        }
    ];

    $("#urlInput").on('input', function () {
        var inputUrl = $(this).val();

        for (let platform of platforms) {
            if (inputUrl.match(platform.regex)) {
                $("#video-details").show();

                if (platform.showPlayer == true) {
                    updateVideoFrame(inputUrl);
                } else {
                    $("#player-container").hide();
                }

                platform.setMode();
                if (platform.logoUrl) {
                    $("#logo-img").attr('src', platform.logoUrl);
                }
                break;
            }
        }
    });

    function setCliptokMode() {
        const body = document.body;
        if (!body.classList.contains('tiktok-mode')) {
            body.classList.remove('yt-mode');
            body.classList.add('tiktok-mode');
            $("#logo-img").attr('src', cliptokLogoUrl);
            $("#get-current-end-btn").hide();
            $("#get-current-start-btn").hide();
        }
    }

    function setClipytMode() {
        const body = document.body;
        if (!body.classList.contains('yt-mode')) {
            body.classList.remove('tiktok-mode');
            body.classList.add('yt-mode');
            $("#logo-img").attr('src', clipytLogoUrl);
            $("#get-current-end-btn").show();
            $("#get-current-start-btn").show();
        }
    }

    function setTwitterMode() {
            setClipytMode();
            $("#get-current-end-btn").hide();
            $("#get-current-start-btn").hide();
    }
});