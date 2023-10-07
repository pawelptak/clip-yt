﻿using ClipYT.Interfaces;

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
            "We have to go back.",
            "Dame da ne.",
            "You picked the wrong house.",
            "Get some help.",
            "Wake up.",
            "Finish him.",
            "Mr. Salieri sends his regards."
        };

        public string GetRandomCaption() => Captions[_random.Next(Captions.Count)];
    }
}
