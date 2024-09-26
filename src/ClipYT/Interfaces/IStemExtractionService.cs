using ClipYT.Enums;
using ClipYT.Models;

namespace ClipYT.Interfaces
{
    public interface IStemExtractionService
    {
        ProcessingResult ExtractStems(byte[] audioBytes, int stemCount, string outputFileName, List<StemType> selectedStems); 
    }
}
