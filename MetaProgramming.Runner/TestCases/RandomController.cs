using MetaProgramming.Controller;
using System;
using System.Collections.Generic;

namespace MetaProgramming.Runner.TestCases
{
    public class PostNumberArgs 
    {
        public int Number { get; set; }
    }

    public class PostNumbersArgs 
    {
        public List<int> Numbers { get; set; }
    }

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
        public void PostNumber(PostNumberArgs args)
        {
            Console.WriteLine("Got number " + args?.Number.ToString());
        }

        [HttpPost("numbers")]
        public void PostNumbers(PostNumbersArgs args)
        {
            foreach (var n in args.Numbers)
            {
                Console.WriteLine("Got number " + n);
            }
        }
    }
}
