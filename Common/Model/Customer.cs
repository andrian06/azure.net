using Newtonsoft.Json;

namespace CustomerInfoCosmosDb.Common.Model
{
    public class Customer
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "firstname")]
        public string FirstName { get; set; }

        [JsonProperty(PropertyName = "lastname")]
        public string LastName { get; set; }

        [JsonProperty(PropertyName = "birthdayinepoch")]
        public long BirthdayInEpoch { get; set; }

        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }

    }
}
