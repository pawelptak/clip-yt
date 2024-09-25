using ClipYT.Enums;
using ClipYT.Models;

namespace ClipYT.Interfaces
{
    public interface ITrackSeparationService
    {
        ProcessingResult SeparateTracks(byte[] audioBytes, int stemCount, string outputFileName, List<AudioTrackType> selectedAudioTracks); 
    }
}
