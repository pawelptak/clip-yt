$(document).ready(function () {
    const appData = document.getElementById('app-data');
    const downloadMethodUrl = appData.getAttribute('data-download-url');

    $('form').on('submit', function (e) {
        e.preventDefault();

        if (!$(this).valid()) {
            return;
        }

        connectToHub();
        var $button = $('#submit-button');
        $button.addClass('rotating');

        $("#progressText").text("Download is starting");
        $("#progress-container").css('display', 'flex');

        $.ajax({
            url: downloadMethodUrl,
            type: 'POST',
            data: $(this).serialize(),
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

                $button.removeClass('rotating');
                $("#progress-container").hide();
            },
            error: function (xhr, status, error) {
                console.error('AJAX Request failed:', status, error);
                console.error('Response Text:', xhr.responseText);
                $('#url-validation-message').text('An error occurred. Please try again.').show();

                $button.removeClass('rotating');
                $("#progress-container").hide();
            }
        });
    });
});