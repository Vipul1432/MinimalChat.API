using Microsoft.AspNetCore.SignalR;

namespace MinimalChat.API.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            try
            {
                // Your message handling logic
                await Clients.All.SendAsync("ReceiveMessage", user, message);
            }
            catch (Exception ex)
            {
                // Handle exceptions and log errors
                Console.WriteLine($"Error in SendMessage: {ex.Message}");
            }
        }
    }
}
