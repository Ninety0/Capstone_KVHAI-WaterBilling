﻿using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace KVHAI.Hubs
{
    public class WaterReadingHub : Hub
    {
        private readonly HubConnectionRepository _connectionRepository;

        public WaterReadingHub(HubConnectionRepository hubConnectionRepository)
        {
            _connectionRepository = hubConnectionRepository;
        }

        public async Task SaveUserConnection(string username)
        {
            var connectionId = Context.ConnectionId;
            HubConnect hubConnection = new HubConnect
            {
                Resident_ID = connectionId,
                Username = username
            };

            await _connectionRepository.SaveHubConnection(hubConnection);
        }

        public override Task OnConnectedAsync()
        {
            Clients.Caller.SendAsync("OnConnected");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            Task.Run(async () => await _connectionRepository.RemoveHubConnection(connectionId));

            return base.OnDisconnectedAsync(exception);
        }
    }
}
