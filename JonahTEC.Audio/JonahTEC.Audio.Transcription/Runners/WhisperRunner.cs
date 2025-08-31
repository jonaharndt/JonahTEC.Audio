using JonahTEC.Audio.Transcription.Configurations;
using JonahTEC.Audio.Transcription.Interfaces;
using JonahTEC.Audio.Transcription.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text;

namespace JonahTEC.Audio.Transcription.Runners
{
    /// <summary>
    /// Processes audio files in a specified directory, transcribes their content, and identifies occurrences of
    /// specific words.
    /// </summary>
    /// <remarks>This class scans a directory for audio files, transcribes their content using the provided
    /// transcription backend,  and searches for specific words or phrases. The results are written to a CSV file, and
    /// optionally, matching files  can be copied to a specified directory. The operation supports parallel processing
    /// and can be canceled via a  <see cref="CancellationToken"/>.</remarks>
    public class WhisperRunner
    {
        private readonly AppConfiguration _appConfiguration;
        private readonly ILogger<WhisperRunner> _logger;
        private readonly ITranscriptionBackend _transcriptionBackend;

        /// <summary>
        /// Initializes a new instance of the <see cref="WhisperRunner"/> class.
        /// </summary>
        /// <param name="appConfiguration">The application configuration settings used to initialize the runner. This parameter cannot be <see
        /// langword="null"/>.</param>
        /// <param name="logger">The logger instance used for logging diagnostic and operational messages. This parameter cannot be <see
        /// langword="null"/>.</param>
        /// <param name="transcriptionBackend">The transcription backend responsible for processing audio transcriptions. This parameter cannot be <see
        /// langword="null"/>.</param>
        public WhisperRunner(AppConfiguration appConfiguration,
                             ILogger<WhisperRunner> logger,
                             ITranscriptionBackend transcriptionBackend)
        {
            _appConfiguration = appConfiguration;
            _logger = logger;
            _transcriptionBackend = transcriptionBackend;
        }

        /// <summary>
        /// Processes audio files in the specified directory and its subdirectories, transcribes their content, searches
        /// for specific words, and returns a list of word hits.
        /// </summary>
        /// <remarks>This method scans the directory specified in the application configuration for audio
        /// files with supported formats (e.g., .mp3, .wav, .m4a, etc.). It processes the files in parallel, transcribes
        /// their content, and searches for specific words using the configured transcription backend. If any hits are
        /// found, they are added to the result set.  If a copy directory is configured, files containing hits are
        /// copied to that directory. Additionally, the results are written to a CSV file in the output directory
        /// specified in the application configuration.  The method supports cancellation via the <see
        /// cref="CancellationToken"/> parameter.</remarks>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is a list of <see cref="WordHit"/>
        /// objects, each representing a word hit found in the processed audio files.</returns>
        public async Task<List<WordHit>> RunAsync(CancellationToken cancellationToken = default)
        {
            var hits = new ConcurrentBag<WordHit>();
            string[] audioFiles = [.. Directory.GetFiles(_appConfiguration.ScanRoot, "*.*", SearchOption.AllDirectories)
                                               .Where(f => f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)
                                                           || f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)
                                                           || f.EndsWith(".m4a", StringComparison.OrdinalIgnoreCase)
                                                           || f.EndsWith(".flac", StringComparison.OrdinalIgnoreCase)
                                                           || f.EndsWith(".aac", StringComparison.OrdinalIgnoreCase)
                                                           || f.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase)
                                                           || f.EndsWith(".wma", StringComparison.OrdinalIgnoreCase))];

            await Parallel.ForEachAsync(audioFiles, new ParallelOptions { MaxDegreeOfParallelism = _appConfiguration.MaxParallel, CancellationToken = cancellationToken }, async (file, ct) =>
            {
                try
                {
                    _logger.LogInformation("Processing file: {File}", file);
                    var found = await _transcriptionBackend.TranscribeThenFindAsync(file, ct);
                    foreach (var hit in found)
                    {
                        hits.Add(hit);
                    }

                    if (found.Count > 0 && !string.IsNullOrEmpty(_appConfiguration.CopyDirectory))
                    {
                        File.Copy(file, Path.Combine(_appConfiguration.CopyDirectory, Path.GetFileName(file)), true);
                    }

                    _logger.LogInformation("Found {Count} hits in file: {File}", found.Count, file);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Operation canceled for file: {File}", file);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing file: {File}", file);
                    throw;
                }

            });

            Directory.CreateDirectory(Path.GetDirectoryName(_appConfiguration.OutputCsv)!);
            using (var sw = new StreamWriter(Path.Combine(_appConfiguration.OutputCsv, $"run-{DateTime.UtcNow.Ticks}.csv"), false, new UTF8Encoding(true)))
            {
                sw.WriteLine("file,start,end,word,context");
                foreach (var h in hits.OrderBy(h => h.FilePath).ThenBy(h => h.StartSec))
                {
                    var safeCtx = h.Context.Replace("\"", "\"\"");
                    sw.WriteLine($"\"{h.FilePath}\",{h.StartSec:0.000},{h.EndSec:0.000},\"{h.Word}\",\"{safeCtx}\"");
                }
            }

            return [.. hits];
        }
    }
}