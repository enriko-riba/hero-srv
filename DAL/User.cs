namespace ws_hero.DAL
{
    using Microsoft.Azure.Documents;
    using Newtonsoft.Json;
    using System;

    public class User<T>
    {
        [JsonIgnore]
        public string Id { get => Email; }

        [JsonProperty(PropertyName = "id")]
        public string Email { get; set; }

        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string DisplayName { get; set; }
        public string PictureURL { get; set; }

        public bool? IsActive { get; set; }

        public T GameData { get; set; }

        [JsonConverter(typeof(UnixDateTimeConverter))]
        [JsonProperty(PropertyName = "_ts")]
        public DateTime LastActivity { get; set; }

        /// <summary>
        /// Last time server synced the player.
        /// For server side use only, not serialized.
        /// </summary>
        [JsonIgnore]
        public DateTime LastSync { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Last time the player is saved to DB.
        /// For server side use only, not serialized.
        /// </summary>
        [JsonIgnore]
        public DateTime LastSave { get; set; } = DateTime.MinValue;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
