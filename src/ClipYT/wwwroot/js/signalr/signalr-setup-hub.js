$(document).ready(function () {
    const appData = document.getElementById('app-data');
    const progressHubUrl = appData.getAttribute('data-progress-hub-url');
    var connection = new signalR.HubConnectionBuilder().withUrl(progressHubUrl).build();

    connection.on("ReceiveProgress", function (progress) {
        $("#progressText").text(progress);
    });

    connection.start().catch(function (err) {
        return console.error(err.toString());
    });
});