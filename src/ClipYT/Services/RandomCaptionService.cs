using ClipYT.Interfaces;

namespace ClipYT.Services
{
    public class RandomCaptionService : IRandomCaptionService
    {
        private readonly Random _random;
        public RandomCaptionService()
        {
            _random = new Random();
        }
        public IList<string> Captions => new List<string> {
            "Believe in yourself.",
            "Think about it.",
            "Never gonna give you up.",
            "Today was a good day.",
            "Have you heard of the High Elves?",
            "It just works.",
            "Broadcast yourself.",
            "I drive.",
            "Dame da ne."
        };

        public string GetRandomCaption() => Captions[_random.Next(Captions.Count)];
    }
}
