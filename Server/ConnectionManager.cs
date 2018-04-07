namespace ws_hero.Server
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    public class ConnectionManager
    {
        private Func<ClientConnection, string> mapper;
        public ConnectionManager(Func<ClientConnection, string> mapper)
        {
            this.mapper = mapper;
        }

        /// <summary>
        /// key, connection value pair
        /// </summary>
        private Dictionary<string, ClientConnection> connList = new Dictionary<string, ClientConnection>();

        public ICollection<ClientConnection> Connections { get => connList.Values; }

        public IEnumerable<KeyValuePair<string, ClientConnection>> GetAll()
        {
            return connList.Where(c => true);
        }

        public IEnumerable<KeyValuePair<string, ClientConnection>> GetAll(Func<KeyValuePair<string, ClientConnection>, bool> predicate)
        {
            return connList.Where(predicate);
        }

        public ClientConnection Get(string id)
        {
            return connList[id];
        }

        public void Add(ClientConnection connection)
        {
            var id = this.mapper(connection);
            connList[id] = connection;
        }

        public void Remove(string id)
        {
            connList.Remove(id);
        }

        public void Remove(ClientConnection connection)
        {
            var id = FindByConnection(connection);
            connList.Remove(id);
        }

        public ClientConnection FindByToken(string token)
        {
            var item = this.connList.FirstOrDefault(kvp => kvp.Value.IdToken == token);
            return item.Value;
        }

        public string FindByConnection(ClientConnection connection)
        {
            var item = this.connList.FirstOrDefault(kvp => kvp.Value == connection);
            return item.Key;
        }
    }
}