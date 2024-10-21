$(document).ready(function () {
    var connection = new signalR.HubConnectionBuilder().withUrl("/progressHub").build();

    connection.on("ReceiveProgress", function (progress) {
        $("#progressText").text(progress);
    });

    connection.start().catch(function (err) {
        return console.error(err.toString());
    });
});