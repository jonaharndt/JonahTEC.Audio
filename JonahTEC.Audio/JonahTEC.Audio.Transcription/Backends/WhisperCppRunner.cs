using JonahTEC.Audio.Transcription.Configurations;
using JonahTEC.Audio.Transcription.Interfaces;
using JonahTEC.Audio.Transcription.Models;
using JonahTEC.Audio.Transcription.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace JonahTEC.Audio.Transcription.Backends
{
    /// <summary>
    /// Provides functionality to transcribe audio files using the Whisper.cpp backend and parse the resulting
    /// transcripts.
    /// </summary>
    /// <remarks>This class integrates with the Whisper.cpp CLI tool to perform audio transcription and uses a
    /// parsing service to extract structured data from the generated transcripts. It supports configurable backend
    /// settings and logging for diagnostics. The transcription process can be skipped if a transcript already exists,
    /// based on the configuration.</remarks>
    public sealed class WhisperCppRunner : ITranscriptionBackend
    {
        private readonly ParsingService _parsingService;
        private readonly BackendConfiguration _backendConfiguration;
        private readonly ILogger<WhisperCppRunner> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WhisperCppRunner"/> class.
        /// </summary>
        /// <param name="parsingService">The service responsible for parsing input data. This parameter cannot be null.</param>
        /// <param name="backendConfiguration">The configuration settings for the backend. This parameter cannot be null.</param>
        /// <param name="logger">The logger instance used for logging messages and diagnostics. This parameter cannot be null.</param>
        public WhisperCppRunner(ParsingService parsingService, BackendConfiguration backendConfiguration, ILogger<WhisperCppRunner> logger)
        {
            _parsingService = parsingService;
            _backendConfiguration = backendConfiguration;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<List<WordHit>> TranscribeThenFindAsync(string audioPath, CancellationToken ct)
        {
            string outBase = GetFilePath(audioPath);
            string jsonPath = outBase + ".json";

            bool needsTranscribing = true;

            if (_backendConfiguration.WhisperCppSkipIfTranscriptExists && File.Exists(jsonPath))
            {
                needsTranscribing = false;
            }

            if (needsTranscribing)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outBase)!);

                var args = $"-m \"{_backendConfiguration.WhisperCppModel}\" -f \"{audioPath}\" {_backendConfiguration.WhisperCppArgs} {_backendConfiguration.WhisperCppOutputDir} -of \"{outBase}\"";
                var psi = new ProcessStartInfo
                {
                    FileName = _backendConfiguration.WhisperCppExe,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start whisper CLI");

                // Drain to avoid deadlocks & capture stderr for diagnostics
                var _ = proc.StandardOutput.ReadToEndAsync(ct);
                var stderr = await proc.StandardError.ReadToEndAsync(ct);
                await proc.WaitForExitAsync(ct);

                if (!File.Exists(jsonPath))
                {
                    _logger.LogWarning("No transcript was created for {File}. Expected: {JsonPath}. Whisper stderr: {Stderr}", audioPath, jsonPath, stderr);

                    return [];
                }
            }

            if (File.Exists(jsonPath))
                return await _parsingService.ParseFromJsonAsync(jsonPath, audioPath, ct);

            return [];
        }

        private string GetFilePath(string audioPath)
        {
            var fileNoExt = Path.GetFileNameWithoutExtension(audioPath);
            var parent = _backendConfiguration.WhisperCppOutputDir ?? Path.GetDirectoryName(audioPath)!;
            return Path.Combine(parent, fileNoExt);
        }
    }
}