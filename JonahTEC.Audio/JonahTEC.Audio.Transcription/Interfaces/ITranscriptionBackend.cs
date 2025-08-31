using JonahTEC.Audio.Transcription.Models;

namespace JonahTEC.Audio.Transcription.Interfaces
{
    public interface ITranscriptionBackend
    {
        /// <summary>
        /// Transcribes the audio from the specified audio file and searches for specific words within the transcription.
        /// </summary>
        /// <remarks>This method performs both transcription and word search in a single operation. It is
        /// suitable for scenarios where both tasks need to be performed sequentially.</remarks>
        /// <param name="audioPath">The file path to the audio file to be transcribed. The file must exist and be accessible.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="WordHit"/>
        /// objects representing the words found in the transcription. The list will be empty if no words are found.</returns>
        public Task<List<WordHit>> TranscribeThenFindAsync(string audioPath, CancellationToken ct);
    }
}