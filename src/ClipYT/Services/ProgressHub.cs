using Microsoft.AspNetCore.SignalR;

namespace ClipYT.Services
{
    public class ProgressHub : Hub
    {
        public async Task SendProgressUpdate(string progress)
        {
            await Clients.All.SendAsync("ReceiveProgress", progress);
        }
    }
}
