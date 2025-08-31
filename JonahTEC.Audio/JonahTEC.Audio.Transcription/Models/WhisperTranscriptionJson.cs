using System.Text.Json.Serialization;

namespace JonahTEC.Audio.Transcription.Models
{
    public sealed partial class WhisperTranscriptionJson
    {
        [JsonPropertyName("transcription")]
        public List<Item> Transcription { get; set; } = [];
    }
}