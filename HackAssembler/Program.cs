using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using HackAssembler.AssemblerLibrary;

namespace HackAssembler
{
    class Program
    {
        static void Main(string[] args)
        {
            // Setup directories to be used for input and output files.
            string inputDirectory = @"D:\Nand2Tetris\Assembler\HackAssembler\HackAssembler\FileDir\INPUT_ASM";
            string outputDirectory = @"D:\Nand2Tetris\Assembler\HackAssembler\HackAssembler\FileDir\OUTPUT_BINARY";

            // Initialize assembler object.
            Assembler assembler = new Assembler(inputDirectory, outputDirectory);

            // Start stopwatch to meassure performance.
            Stopwatch totalTimeStopwatch = new Stopwatch();
            Stopwatch singleFileStopwatch = new Stopwatch();

            Console.WriteLine("Input ASM files found:");
            // Go through all available files.
            foreach (var file in assembler.AvailableInputFiles)
            {
                Console.WriteLine(file.Name);
            }
            Console.WriteLine();

            
            totalTimeStopwatch.Start();

            // Show the content of the file.
            foreach (var inputFile in assembler.AvailableInputFiles)
            {
                singleFileStopwatch.Start();
                Console.WriteLine(inputFile.Name + " -- Starting assembling...");
                assembler.AssembleSingleFile(inputFile);
                Console.WriteLine(inputFile.Name + " -- Assembling finished...");

                singleFileStopwatch.Stop();
                Console.WriteLine(inputFile.Name + " -- Total time spend: " + singleFileStopwatch.Elapsed);
                Console.WriteLine();
            }

            totalTimeStopwatch.Stop();
            Console.WriteLine("Total time spend: " + totalTimeStopwatch.Elapsed);

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
