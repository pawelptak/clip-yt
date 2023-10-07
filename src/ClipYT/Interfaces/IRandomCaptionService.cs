namespace ClipYT.Interfaces
{
    public interface IRandomCaptionService
    {
        public IList<string> Captions { get; }
        string GetRandomCaption();
    }
}
