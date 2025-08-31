namespace JonahTEC.Audio.Transcription.Models
{
    public sealed class WordHit
    {
        public required string FilePath { get; init; }

        public required string Word { get; init; }

        public required double StartSec { get; init; }

        public required double EndSec { get; init; }

        public required string Context { get; init; }
    }
}