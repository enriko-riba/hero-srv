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

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
