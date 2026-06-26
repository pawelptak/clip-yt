const appData = document.getElementById('app-data');
const progressHubUrl = appData.getAttribute('data-progress-hub-url');

var connection = new signalR.HubConnectionBuilder()
    .withUrl(progressHubUrl)
    .build();

connection.on("ReceiveProgress", function (progress) {
    $("#progressText").text(progress);

    const overlay = document.getElementById("video-loading-overlay");
    if (overlay && overlay.style.display !== "none") {
        const textEl = overlay.querySelector(".loading-text");
        if (textEl) {
            textEl.textContent = progress;
        }
    }
});

connection.on("ReceivePreviewProgress", function (progress) {
    const overlay = document.getElementById("video-loading-overlay");
    if (overlay && overlay.style.display !== "none") {
        const textEl = overlay.querySelector(".loading-text");
        if (textEl) {
            textEl.textContent = progress;
        }
    }
});

async function connectToHub() {
    if (connection.state === signalR.HubConnectionState.Disconnected) {
        try {
            await connection.start();
        } catch (err) {
            console.error("Error while connecting to SignalR hub:", err);
        }
    }
}