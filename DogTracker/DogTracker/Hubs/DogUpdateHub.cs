using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace DogTracker.Web.Hubs // Adjust namespace if needed
{
    // This Hub receives connections and manages groups.
    // It also defines methods the SERVER can call on CLIENTS ("ReceiveUpdateNotification").
    public class DogUpdateHub : Hub
    {
        private readonly ILogger<DogUpdateHub> _logger;

        public DogUpdateHub(ILogger<DogUpdateHub> logger)
        {
            _logger = logger;
        }

        // Method for CLIENTS to call to join a specific dog's update group
        public async Task JoinDogGroup(int dogId)
        {
            if (dogId <= 0) return; // Ignore invalid IDs

            string groupName = $"dog-{dogId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} joined group {GroupName}", Context.ConnectionId, groupName);

            // Optional: Send a confirmation back to the specific caller
            // await Clients.Caller.SendAsync("JoinedGroupConfirmation", groupName);
        }

        // Method for CLIENTS to call if they navigate away (optional, cleanup is mostly automatic)
        public async Task LeaveDogGroup(int dogId)
        {
            if (dogId <= 0) return;

            string groupName = $"dog-{dogId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} left group {GroupName}", Context.ConnectionId, groupName);
        }

        // --- Methods called by the SERVER to notify clients ---
        // We don't define them here, but the IHubContext uses the client method names
        // e.g. Clients.Group(groupName).SendAsync("ReceiveUpdateNotification", dogId, dataType);

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}. Error: {Exception}", Context.ConnectionId, exception?.Message);
            // Groups are cleaned up automatically by SignalR on disconnect
            return base.OnDisconnectedAsync(exception);
        }
    }
}