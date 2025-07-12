using Exploder.Tests;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Exploder Application Test Suite");
            Console.WriteLine("==============================\n");
            
            TestRunner.RunAllTests();
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
} 