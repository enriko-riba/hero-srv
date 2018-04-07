namespace ws_hero.Server
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    public class ConnectionManager
    {
        private Func<ClientConnection, int> mapper;
        public ConnectionManager(Func<ClientConnection, int> mapper)
        {
            this.mapper = mapper;
        }

        /// <summary>
        /// key, connection value pair
        /// </summary>
        private Dictionary<int, ClientConnection> connList = new Dictionary<int, ClientConnection>();

        public ICollection<ClientConnection> Connections { get => connList.Values; }

        public IEnumerable<KeyValuePair<int, ClientConnection>> GetAll()
        {
            return connList.Where(c => true);
        }

        public IEnumerable<KeyValuePair<int, ClientConnection>> GetAll(Func<KeyValuePair<int, ClientConnection>, bool> predicate)
        {
            return connList.Where(predicate);
        }

        public ClientConnection Get(int id)
        {
            return connList[id];
        }

        public void Add(ClientConnection connection)
        {
            var id = this.mapper(connection);
            connList[id] = connection;
        }

        public void Remove(int id)
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

        public int FindByConnection(ClientConnection connection)
        {
            var item = this.connList.FirstOrDefault(kvp => kvp.Value == connection);
            return item.Key;
        }
    }
}