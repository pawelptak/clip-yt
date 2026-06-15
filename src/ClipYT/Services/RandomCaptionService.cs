using ClipYT.Interfaces;

namespace ClipYT.Services
{
    public class RandomCaptionService : IRandomCaptionService
    {
        private readonly Random _random;
        private readonly IList<string> _captions;

        public RandomCaptionService()
        {
            _random = new Random();
            _captions = BuildCaptions();
        }

        public string GetRandomCaption() => _captions[_random.Next(_captions.Count)];

        // Do not put captions with apostrophes because the used font does not support them :)
        private IList<string> BuildCaptions() => new List<string> {
            "Believe in yourself.",
            "Think about it.",
            "Never gonna give you up.",
            "Today was a good day.",
            "Have you heard of the High Elves?",
            "It just works.",
            "Broadcast yourself.",
            "I drive.",
            "We have to go back.",
            "Dame da ne.",
            "You picked the wrong house.",
            "Get some help.",
            "Wake up.",
            "Finish him.",
            "Mr. Salieri sends his regards.",
            "Why are you gay?",
            "Keep on keeping on.",
            "Kept you waiting, huh?",
            "AISEM TIBITI OOO"
        };
    }
}
