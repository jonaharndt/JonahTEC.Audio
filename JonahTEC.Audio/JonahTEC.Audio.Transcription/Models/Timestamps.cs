using System.Text.Json.Serialization;

namespace JonahTEC.Audio.Transcription.Models
{
    public sealed class Timestamps
    {
        /// <summary>
        /// Start of the segment in "HH:MM:SS,mmm" format.
        /// </summary>
        [JsonPropertyName("from")]
        public string From { get; set; } = string.Empty;

        /// <summary>
        /// End of the segment in "HH:MM:SS,mmm" format.
        /// </summary>
        [JsonPropertyName("to")]
        public string To { get; set; } = string.Empty;
    }
}