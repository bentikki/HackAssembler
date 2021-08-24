using System;
using System.Collections.Generic;
using System.IO;
using HackAssembler.AssemblerLibrary;

namespace HackAssembler
{
    class Program
    {
        static void Main(string[] args)
        {
            // Initialize assembler object.
            Assembler assembler = new Assembler();

            bool loopProgram = true;

            Console.WriteLine("Input ASM files found:");
            // Go through all available files.
            foreach (var file in assembler.AvailableInputFiles)
            {
                Console.WriteLine(file.Name);
            }
            Console.WriteLine();

            // Show the content of the file.
            foreach (var inputFile in assembler.AvailableInputFiles)
            {
                Console.WriteLine(inputFile.Name + " -- Starting assembling...");
                assembler.AssembleSingleFile(inputFile);
                Console.WriteLine(inputFile.Name + " -- Assembling finished...");
                Console.WriteLine();
            }

            // End repeater
            Console.WriteLine();
            Console.WriteLine("Done...");
            Console.ReadKey();
            Console.WriteLine("Exiting...");
            Console.ReadKey();

        }

        public static async void DisplayMessage(string message)
        {
            Console.WriteLine(message);
        }

    }
}
