using ClipYT.Models;

namespace ClipYT.Interfaces
{
    public interface ITrackSeparationService
    {
        ProcessingResult SeparateTracks(byte[] audioBytes, int stemCount, string outputFileName); 
    }
}
