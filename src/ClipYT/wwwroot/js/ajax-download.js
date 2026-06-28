$(document).ready(function () {
    const appData = document.getElementById('app-data');
    const downloadMethodUrl = appData.getAttribute('data-download-url');

    function restoreButtonState($button, $buttonText, $buttonIcon) {
        $button.prop('disabled', false);
        $buttonText.text('Download');
        $buttonIcon.removeClass('bi-hourglass-split hourglass-processing').addClass('bi-download');
        $("#progress-container").hide();
    }

    $('form').on('submit', async function (e) {
        e.preventDefault();

        if (!$(this).valid()) {
            return;
        }

        // Disable empty precise timestamp inputs so they don't override visible inputs
        const preciseStartInput = $('#preciseStartTimestamp');
        const preciseEndInput = $('#preciseEndTimestamp');
        const isStartEmpty = !preciseStartInput.val();
        const isEndEmpty = !preciseEndInput.val();

        if (isStartEmpty) preciseStartInput.prop('disabled', true);
        if (isEndEmpty) preciseEndInput.prop('disabled', true);

        await connectToHub();
        var $button = $('#submit-button');
        var $buttonIcon = $('#submit-button-icon');
        var $buttonText = $('#submit-button-text');

        // Disable button and change to processing state
        $button.prop('disabled', true);
        $buttonText.text('Processing...');
        $buttonIcon.removeClass('bi-download').addClass('bi-hourglass-split hourglass-processing');

        $("#progressText").text("Download is starting");
        $("#progress-container").css('display', 'flex');

        var formData = $(this).serialize() + '&signalRConnectionId=' + encodeURIComponent(connection.connectionId || '');

        // Re-enable inputs after serialization
        if (isStartEmpty) preciseStartInput.prop('disabled', false);
        if (isEndEmpty) preciseEndInput.prop('disabled', false);

        $.ajax({
            url: downloadMethodUrl,
            type: 'POST',
            data: formData,
            xhrFields: {
                responseType: 'blob'
            },
            success: function (data, status, xhr) {
                var disposition = xhr.getResponseHeader('Content-Disposition');
                var filename = '';

                var filenameUTF8 = disposition.match(/filename\*\=UTF-8''([^;]+)/);
                if (filenameUTF8) {
                    filename = decodeURIComponent(filenameUTF8[1]);
                } else {
                    var filenameNormal = disposition.match(/filename="([^"]+)"/);
                    if (filenameNormal) {
                        filename = filenameNormal[1];
                    }
                }

                var link = document.createElement('a');
                link.href = window.URL.createObjectURL(data);
                link.download = filename;
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);

                restoreButtonState($button, $buttonText, $buttonIcon);
            },
            error: function (xhr, status, error) {
                console.error('AJAX Request failed:', status, error);
                console.error('Response Text:', xhr.responseText);
                $('#url-validation-message').text('An error occurred. Please try again.').show();

                restoreButtonState($button, $buttonText, $buttonIcon);
            }
        });
    });
});