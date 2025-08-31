namespace JonahTEC.Audio.Transcription.Configurations
{
    public sealed class BackendConfiguration
    {
        public string WhisperCppExe { get; set; } = "whisper-cli.exe";

        public string WhisperCppModel { get; set; } = "models/ggml-small.bin";

        public string WhisperCppArgs { get; set; } = "-ml 1 -oj";

        public string WhisperCppExtraOutputArgs { get; set; } = "-otxt -ovtt";

        public bool WhisperCppSkipIfTranscriptExists { get; set; } = true;

        public string? WhisperCppOutputDir { get; set; } = null;

        public string TargetPhrase { get; set; } = "example phrase";

        public int MaxTokenDistance { get; set; } = 1;

        public bool AllowSubstring { get; set; } = true;

        public int WindowSize { get; set; } = 4;
    }
}