using System.Text.Json.Serialization;

namespace JonahTEC.Audio.Transcription.Models
{
    public sealed class Item
    {
        [JsonPropertyName("timestamps")]
        public Timestamps? Timestamps { get; set; }

        [JsonPropertyName("offsets")]
        public Offsets? Offsets { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }
}