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

            while (loopProgram)
            {

                Console.WriteLine("Input ASM files found:");
                foreach (var file in assembler.AvailableInputFiles)
                {
                    Console.WriteLine(file.Name);
                }
                Console.WriteLine();

                // Show the content of the file.
                foreach (var inputFile in assembler.AvailableInputFiles)
                {
                    assembler.AssembleSingleFile(inputFile);
                }

                // End repeater
                Console.WriteLine();
                Console.WriteLine("Press any key to repeat...");
                Console.ReadKey();
            }
        }
    }
}
