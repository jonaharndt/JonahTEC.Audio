using System.Text.Json.Serialization;

namespace JonahTEC.Audio.Transcription.Models
{
    public sealed class Offsets
    {
        [JsonPropertyName("from")]
        public int From { get; set; }

        [JsonPropertyName("to")]
        public int To { get; set; }
    }
}