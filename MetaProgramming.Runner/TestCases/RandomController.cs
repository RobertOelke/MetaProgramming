using MetaProgramming.Controller;
using System;

namespace MetaProgramming.Runner.TestCases
{
    [HttpController("random")]
    public class RandomController
    {
        private readonly Random _random = new Random();

        [HttpGet("number")]
        public int GetRandomNumber()
        {
            return _random.Next(0, 100);
        }

        [HttpPost("number")]
        public void PostNumber(int number)
        {
        }
    }
}
