namespace JonahTEC.Audio.Transcription.Configurations
{
    public class WhisperConfiguration
    {
        public const string SECTION_NAME = "Whisper";

        public string ModelPath { get; set; } = string.Empty;

        public string ExecutablePath { get; set; } = string.Empty;

        public string Args { get; set; } = string.Empty;

        public string ExtraOutputArgs { get; set; } = string.Empty;

        public bool SkipIfJsonExists { get; set; } = true;

        public string OutputDirectory { get; set; } = string.Empty;

        public BackendConfiguration GetBackendConfiguration()
        {
            return new()
            {
                WhisperCppExe = ExecutablePath,
                WhisperCppModel = ModelPath,
                WhisperCppArgs = Args,
                WhisperCppExtraOutputArgs = ExtraOutputArgs,
                WhisperCppSkipIfTranscriptExists = SkipIfJsonExists,
                WhisperCppOutputDir = OutputDirectory
            };
        }
    }
}