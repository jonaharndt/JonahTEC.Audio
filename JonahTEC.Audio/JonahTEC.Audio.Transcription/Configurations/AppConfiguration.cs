namespace JonahTEC.Audio.Transcription.Configurations
{
    public class AppConfiguration
    {
        public const string SECTION_NAME = "App";

        public string ScanRoot { get; set; } = string.Empty;

        public string SearchPhrase { get; set; } = string.Empty;

        public string OutputCsv { get; set; } = string.Empty;

        public int MaxParallel { get; set; } = Environment.ProcessorCount;

        public string? CopyDirectory { get; set; } = null;
    }
}