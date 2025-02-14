using Newtonsoft.Json;

namespace com.rydersir.sonargg.Domains
{
    public class RedirectionDevice
    {
        public RedirectionDevice()
        {
        }

        public RedirectionDevice(SteelSeriesAPI.Sonar.Models.RedirectionDevice sonarRedirectionDevice)
        {
            Id = sonarRedirectionDevice.Id;
            Name = sonarRedirectionDevice.Name;
        }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

    }
}
