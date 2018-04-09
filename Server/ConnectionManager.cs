namespace ws_hero.Server
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    public class ConnectionManager
    {
        public ConnectionManager()
        {
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
            connList[connection.PlayerId] = connection;
        }

        public void Remove(string id)
        {
            connList.Remove(id);
        }

        public void Remove(ClientConnection connection)
        {
            var id = FindIdByConnection(connection);
            connList.Remove(id);
        }

        public ClientConnection FindByToken(string token)
        {
            var item = this.connList.FirstOrDefault(kvp => kvp.Value.IdToken == token);
            return item.Value;
        }

        public string FindIdByConnection(ClientConnection connection)
        {
            var item = this.connList.FirstOrDefault(kvp => kvp.Value == connection);
            return item.Key;
        }
    }
}