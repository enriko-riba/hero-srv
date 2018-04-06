namespace my_hero.Server
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    public class ConnectionManager<T> where T : class
    {
        private Func<T, string> mapper;
        public ConnectionManager(Func<T, string> mapper)
        {
            this.mapper = mapper;
        }

        /// <summary>
        /// key, connection value pair
        /// </summary>
        private Dictionary<string, T> connList = new Dictionary<string, T>();

        public ICollection<T> Connections { get => connList.Values; }

        public IEnumerable<KeyValuePair<string, T>> GetAll()
        {
            return connList.Where(c => true);
        }

        public IEnumerable<KeyValuePair<string, T>> GetAll(Func<KeyValuePair<string, T>, bool> predicate)
        {
            return connList.Where(predicate);
        }

        public T Get(string id)
        {
            return connList[id];
        }

        public void Add(T connection)
        {
            var id = this.mapper(connection);
            connList[id] = connection;
        }

        public void Remove(string id)
        {
            connList.Remove(id);
        }

        public void Remove(T connection)
        {
            var id = FindId(connection);
            connList.Remove(id);
        }

        public string FindId(T connection)
        {
            var item = this.connList.FirstOrDefault(kvp => kvp.Value as T == connection);
            return item.Key;
        }
    }
}