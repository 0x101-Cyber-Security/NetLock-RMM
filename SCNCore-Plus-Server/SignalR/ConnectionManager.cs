using System.Collections.Concurrent;
using System;

namespace SCNCore_Plus_Server.SignalR
{
    public class ConnectionManager
    {
        private static readonly Lazy<ConnectionManager> _instance = new Lazy<ConnectionManager>(() => new ConnectionManager());

        public static ConnectionManager Instance => _instance.Value;

        private readonly ConcurrentDictionary<string, string> _clientConnections = new ConcurrentDictionary<string, string>();

        public ConcurrentDictionary<string, string> ClientConnections => _clientConnections;
    }
}
