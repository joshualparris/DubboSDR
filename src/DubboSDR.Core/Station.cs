using System.Text.Json.Serialization;

namespace DubboSDR.Core
{
    public class Station
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("frequencyHz")]
        public uint FrequencyHz { get; set; }

        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "WFM";

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("showInKidsMode")]
        public bool ShowInKidsMode { get; set; }

        [JsonPropertyName("kidsLabel")]
        public string? KidsLabel { get; set; }

        [JsonPropertyName("kidsIcon")]
        public string? KidsIcon { get; set; }

        [JsonPropertyName("streamUrl")]
        public string? StreamUrl { get; set; }
    }
}
