$(document).ready(function () {
    const appData = document.getElementById('app-data');
    const ytRegex = appData.getAttribute('data-yt-regex');
    const tiktokRegex = appData.getAttribute('data-tiktok-regex');
    const clipytLogoUrl = appData.getAttribute('data-clipyt-logo');
    const cliptokLogoUrl = appData.getAttribute('data-cliptok-logo');

    $("#urlInput").on('input', function () {
        var inputUrl = $(this).val();

        if (inputUrl.match(ytRegex)) {
            updateVideoFrame($(this).val());
            $("#video-details").show();
            setClipytMode();
        }
        if (inputUrl.match(tiktokRegex)) {
            $("#player-container").hide();
            $("#video-details").show();
            setCliptokMode();
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
});
