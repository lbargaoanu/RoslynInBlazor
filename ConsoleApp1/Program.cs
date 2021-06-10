using Microsoft.CodeAnalysis;
using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(Compiler.Compiler.Compile(new[] { MetadataReference.CreateFromFile(typeof(int).Assembly.Location) }));
        }
    }
}
