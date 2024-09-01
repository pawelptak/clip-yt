$(document).ready(function () {
    $('#submit-button').click(function (e) {
        e.preventDefault();

        var $button = $(this);
        $button.addClass('rotating');

        $.ajax({
            url: '/Home/DownloadVideo',
            type: 'POST',
            data: $('form').serialize(),
            xhrFields: {
                responseType: 'blob'
            },
            success: function (data, status, xhr) {
                var disposition = xhr.getResponseHeader('Content-Disposition');
                var filename = disposition.match(/filename="(.+)"/)[1];

                var link = document.createElement('a');
                link.href = window.URL.createObjectURL(data);
                link.download = filename;
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);

                $button.removeClass('rotating');
            },
            error: function (xhr, status, error) {
                console.error('AJAX Request failed:', status, error);
                console.error('Response Text:', xhr.responseText);

                $button.removeClass('rotating');
            }
        });
    });
});