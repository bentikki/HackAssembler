using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HackAssembler.AssemblerLibrary
{
    public class Assembler
    {
        private readonly int BYTE_SIZE = 16;

        // Initialize C-Instruction Lookup tables.
        private Dictionary<string, string> compLookupTableA0;

        private Dictionary<string, string> compLookupTableA1;
        private Dictionary<string, string> destLookupTable;
        private Dictionary<string, string> jumpLookupTable;
        private Dictionary<string, string> predefinedLookupTable;

        // Current outputfile
        private FileInfo InputFile;

        // Current output lines
        private List<string> outputLines;
        private long outputLineNumber;

        // Startup file folders.
        private DirectoryInfo inputDirectory;
        private DirectoryInfo outputDirectory;

        public DirectoryInfo InputDirectory { get => inputDirectory; }
        public DirectoryInfo OutputDirectory { get => outputDirectory; }

        public string CMappingBitValueState { get; private set; }
        public FileInfo[] AvailableInputFiles { get; private set; }

        public Assembler()
        {
            this.CreateStartupDirectories();
            this.CreateLookUpTables();
            this.AvailableInputFiles = this.GetAvailableInputFiles();
        }

        /// <summary>
        /// Returns a list of available files in the input folder.
        /// </summary>
        /// <returns>string[] of files to assemble.</returns>
        public FileInfo[] GetAvailableInputFiles()
        {
            // Get input ASM files to be translated.
            FileInfo[] inputASMfiles = this.InputDirectory.GetFiles("*.asm");
            List<FileInfo> listOfAvailableFiles = new List<FileInfo>();

            foreach (var file in inputASMfiles)
            {
                listOfAvailableFiles.Add(file);
            }

            return listOfAvailableFiles.ToArray();
        }

        /// <summary>
        /// Assemble a single .hack file from .asm.
        /// </summary>
        /// <param name="inputFile">FileInfo object of file to assemble.</param>
        public void AssembleSingleFile(FileInfo inputFile)
        {
            this.InputFile = inputFile;

            // Set output array to contain the lines needed.
            string[] outputLinesRaw = this.GetStrippedInputLinesFromFile(this.InputFile.FullName);

            // Assemble the lines using the lookup tables. Output translated commands in binary.
            string[] rawLinesWithSymbolsTranslated = this.TranslateLineSymbols(outputLinesRaw);

            // Assemble the lines using the lookup tables. Output translated commands in binary.
            string[] translatedLines = this.AssembleLines(rawLinesWithSymbolsTranslated);

            // Write the lines to the output file.
            this.WriteLinesToOutputFile(translatedLines);
        }

        /// <summary>
        /// Returns the lines from the inputFile in a stripped version.
        /// This removed comments, new lines and spaces that might appear in the file.
        /// </summary>
        /// <param name="inputFileFullPath">Full path to file</param>
        /// <returns>Stripped version of all lines in provided input file.</returns>
        private string[] GetStrippedInputLinesFromFile(string inputFileFullPath)
        {
            // Set output array to contain the lines needed.
            List<string> outputLinesRaw = new List<string>();
            int counter = 0;

            try
            {
                // Read the file and display it line by line. 
                using (StreamReader file = new StreamReader(inputFileFullPath))
                {
                    string line;

                    while ((line = file.ReadLine()) != null)
                    {
                        try
                        {
                            int lineNumber = counter + 1;
                            string outputLineToAdd = line;

                            // Loop through the lines 1 by 1.
                            // Decide what to do with the input of the line.

                            // Check if line is empty
                            bool lineIsEmpty = false;

                            if (line.Length < 1)
                                lineIsEmpty = true;

                            // Check if line is only a comment.
                            bool lineIsCommentOnly = false;

                            if (line.Contains("//"))
                            {
                                // Split line by comment.
                                string[] lineCommentArray = line.Split("//");

                                // Set which 
                                if (lineCommentArray[0].ToString().Trim().Length < 1
                                    || lineCommentArray[0].ToString().Trim() == "/")
                                {
                                    lineIsCommentOnly = true;
                                }
                                else
                                {
                                    outputLineToAdd = lineCommentArray[0].ToString();
                                }
                            }


                            if (lineIsEmpty)
                            {
                                throw new Exception($"Line number:{lineNumber} is empty.");

                            }
                            if (lineIsCommentOnly)
                            {
                                throw new Exception($"Line number:{lineNumber} is only a comment.");
                            }

                            // Trim the line to remove unwanted spaces.
                            outputLineToAdd = outputLineToAdd.Trim();

                            // Add string line to outputLinesRaw list, to be returned.
                            outputLinesRaw.Add(outputLineToAdd);
                        }
                        catch (Exception e)
                        {
                            // The line should not be added.
                        }

                        counter++;
                    }

                    file.Close();
                }
            }
            catch (Exception e)
            {
                // In case of an error, throw the line number to the user.
                throw new Exception("An error occured while stripping input line:" + counter+1, e);
            }

            return outputLinesRaw.ToArray();
        }

        private string[] TranslateLineSymbols(string[] linesBeforeSymbols)
        {
            List<string> linesWithoutSymbols = new List<string>();
            List<string> linesWithTranslatedSymbols = new List<string>();
            Dictionary<string, long> labelReferenceLookup = new Dictionary<string, long>();
            Dictionary<string, long> labelReferenceLookupAfterAdjust = new Dictionary<string, long>();
            Dictionary<string, long> addedVariablesLookup = new Dictionary<string, long>();
            Dictionary<string, long> addedVariablesLookupAfterAdjust = new Dictionary<string, long>();
            long outputLineNumber = 0;
            long labelLineNumber = 0;
            long customVariablesCounter = 16;

            try
            {
                foreach (string outputRawLine in linesBeforeSymbols)
                {
                    string outputRawLineToAdd = outputRawLine;
                    bool addLineToCheck = true;

                    // Check if the line contains a label.
                    if (outputRawLineToAdd.Contains("(") && outputRawLineToAdd.Contains(")"))
                    {
                        // The line contains a label.
                        // Remove the brackets from the string.
                        outputRawLineToAdd = outputRawLineToAdd.Replace("(", "");
                        outputRawLineToAdd = outputRawLineToAdd.Replace(")", "");

                        // Add this to the label reference table.
                        labelReferenceLookup.Add(outputRawLineToAdd, labelLineNumber);
                        addLineToCheck = false;
                    }
                    else
                    {
                        labelLineNumber++;
                    }

                    if (addLineToCheck)
                        linesWithoutSymbols.Add(outputRawLineToAdd);

                    
                }
            }
            catch (Exception e)
            {
                throw new Exception("An error occured while translating labels. Line:" + outputLineNumber, e);
            }

            //int lookupAdjust = labelReferenceLookup.Count;
            //foreach (var reference in labelReferenceLookup)
            //{
            //    labelReferenceLookupAfterAdjust.Add(reference.Key, reference.Value - lookupAdjust);
            //}

            //labelReferenceLookup = labelReferenceLookupAfterAdjust;


            try
            {
                foreach (string outputRawLine in linesWithoutSymbols)
                {
                    string outputRawLineToAdd = outputRawLine;

                    // Check if line is an A-Instruction.
                    // This is done to catch labels., and add them to the lookup table.
                    if (this.LineIsAInstruction(outputRawLine))
                    {
                        string aInstructionValue = outputRawLine.Substring(1);

                        if (addedVariablesLookup.ContainsKey(aInstructionValue))
                        {
                            outputRawLineToAdd = "@" + addedVariablesLookup[aInstructionValue];
                        }
                        else
                        {
                            // Try to translate the A instruction bits. 
                            // This will throw an exception if the value is unkown,
                            // which will show that its a custom value.
                            try
                            {
                                this.TranslateAinstructionBits(aInstructionValue);
                            }
                            catch (ArgumentException)
                            {

                                // Handle the argument exception only - this warns that the value could not be found in lookup.
                                if (aInstructionValue.All(c => Char.IsLetterOrDigit(c) || c == '_' || c == '.' || c == '$' ))
                                {
                                    long counterToAdd;
                                    // Check if it exists in label table first TODO
                                    if (labelReferenceLookup.ContainsKey(aInstructionValue))
                                    {
                                        counterToAdd = labelReferenceLookup[aInstructionValue];
                                    }
                                    else
                                    {
                                        // If it passes the check, add it back into the lines, with a custom number.
                                        counterToAdd = customVariablesCounter;

                                        // Add the variable to the lookup table.
                                        addedVariablesLookup.Add(aInstructionValue, counterToAdd);
                                        
                                        customVariablesCounter++;
                                    }

                                    outputRawLineToAdd = "@" + counterToAdd;
                                }
                            }
                        }


                        

                    }


                    linesWithTranslatedSymbols.Add(outputRawLineToAdd);

                    outputLineNumber++;
                }
            }
            catch (Exception e)
            {
                throw new Exception("An error occured while translating symbols. Line:" + outputLineNumber, e);
            }

            return linesWithTranslatedSymbols.ToArray();
        }

        /// <summary>
        /// Assembles the lines, converting assemply to 16 bit via lookup tables.
        /// </summary>
        /// <param name="rawLines">The raw lines, without spaces and comments.</param>
        /// <returns>String array with assembled lines in bit.</returns>
        private string[] AssembleLines(string[] rawLines)
        {
            // Create string list to contain translated lines.
            List<string> outputLines = new List<string>();
            long outputLineNumber = 1;

            try
            {
                foreach (string outputRaw in rawLines)
                {
                    outputLines.Add(this.AssembleSingleLine(outputRaw, outputLineNumber));
                    outputLineNumber++;
                }
            }
            catch (Exception e)
            {
                throw new Exception("An error occured while translating code to assembly. Line:" + outputLineNumber, e);
            }
            
            return outputLines.ToArray();
        }

        private string AssembleSingleLine(string outputRaw, long outputLineNumber)
        {
            string outputLineBin = outputRaw;
            // Translate the assemply to binary.
            try
            {
                // Check if the line is an A-instruction
                if (this.LineIsAInstruction(outputLineBin))
                {
                    string aInstructionValue = outputLineBin.Substring(1);
                    outputLineBin = this.TranslateAinstructionBits(aInstructionValue);
                }
                else
                {
                    // The line is not an A-Instruction.
                    // Must therefore be a C-Instruction.
                    string cInstructionBinaryFull = string.Empty;

                    // Set default value.
                    string cLeadingBits = "111";
                    string cMappingBit = "0";
                    string cCompBits = "000000";
                    string cDestBits = "000";
                    string cJumpBits = "000";

                    // Check if the C-Instruction contains DEST
                    if (outputLineBin.Contains("="))
                    {
                        string[] destArray = outputLineBin.Split("=");
                        cDestBits = this.TranslateToDestBits(destArray[0]);
                        outputLineBin = destArray[1];
                    }

                    // Check if the C-Instruction contains JMP
                    if (outputLineBin.Contains(";"))
                    {
                        string[] destArray = outputLineBin.Split(";");
                        cJumpBits = this.TranslateToJumpBits(destArray[1]);
                        outputLineBin = destArray[0];
                    }

                    // Run the remaining through COMP bits
                    cCompBits = this.TranslateToCompBits(outputLineBin);
                    cMappingBit = this.CMappingBitValueState;

                    cInstructionBinaryFull = cLeadingBits + cMappingBit + cCompBits + cDestBits + cJumpBits;

                    outputLineBin = cInstructionBinaryFull;
                }
            }
            catch (Exception e)
            {

                throw e;
            }
            
            return outputLineBin;
        }

        /// <summary>
        /// Writes the provided lines to the current output file.
        /// </summary>
        /// <param name="outputLines">List of lines to be written.</param>
        public void WriteLinesToOutputFile(string[] outputLines)
        {
            try
            {
                // Write to output file.
                using (StreamWriter fileWriter = new StreamWriter(Path.Combine(this.OutputDirectory.FullName, Path.GetFileNameWithoutExtension(this.InputFile.FullName) + ".hack")))
                {
                    foreach (string outputLine in outputLines)
                    {
                        fileWriter.WriteLine(outputLine);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("An error occured while writing to Output file.", e);
            }
        }

        /// <summary>
        /// Checks if line is an A-Instruction.
        /// Returns true if it is, false if not.
        /// </summary>
        /// <param name="lineToCheck">Line to check, in string format.</param>
        /// <returns>Bool true if it is an A-Instruction, false if not</returns>
        private bool LineIsAInstruction(string lineToCheck)
        {
            bool lineIsAInstruction = false;
            
            if(lineToCheck.Substring(0, 1) == "@")
            {
                lineIsAInstruction = true;
            }

            return lineIsAInstruction;
        }

        private string TranslateAinstructionBits(string assemplyValue)
        {
            string binaryTranslation = string.Empty;
            if(Int16.TryParse(assemplyValue, out short intValueResult))
            {
                binaryTranslation = Convert.ToString(intValueResult, 2).PadLeft(BYTE_SIZE, '0');
            }
            else
            {
                string lookupResult = this.GetBitsFromLookup("PREDEFINED", assemplyValue, this.predefinedLookupTable);
                binaryTranslation = Convert.ToString(Convert.ToInt16(lookupResult), 2).PadLeft(BYTE_SIZE, '0');
            }
            return binaryTranslation;
        }

        private string TranslateToDestBits(string assemplyKey)
        {
            return this.GetBitsFromLookup("DEST", assemplyKey, this.destLookupTable);
        }

        private string TranslateToJumpBits(string assemplyKey)
        {
            return this.GetBitsFromLookup("JUMP", assemplyKey, this.jumpLookupTable);
        }

        private string TranslateToCompBits(string assemplyKey)
        {
            string resultFromCompLookup = string.Empty;

            try
            {
                resultFromCompLookup = this.GetBitsFromLookup("COMPA0", assemplyKey, this.compLookupTableA0);
                this.CMappingBitValueState = "0";
            }
            catch (Exception) { }

            try
            {
                resultFromCompLookup = this.GetBitsFromLookup("COMPA1", assemplyKey, this.compLookupTableA1);
                this.CMappingBitValueState = "1";
            }
            catch (Exception) { }

            if (resultFromCompLookup == string.Empty)
                throw new ArgumentException($"COMP lookup table does not contain a definition for: {assemplyKey}");

            return resultFromCompLookup;
        }

        private string GetBitsFromLookup(string tableName, string key, Dictionary<string, string> dictionary)
        {
            // Check if the supplied value is conained in the lookuptable.
            if (!dictionary.ContainsKey(key))
                throw new ArgumentException($"{tableName} lookup table does not contain a definition for: {key}");

            return dictionary[key];
        }

        /// <summary>
        /// Creates the start directories.
        /// </summary>
        private void CreateStartupDirectories()
        {
            // Create file folders.
            string inputDirectoryPath = @"D:\Nand2Tetris\Assembler\HackAssembler\HackAssembler\FileDir\INPUT_ASM";
            this.inputDirectory = Directory.CreateDirectory(inputDirectoryPath);

            string outputDirectoryPath = @"D:\Nand2Tetris\Assembler\HackAssembler\HackAssembler\FileDir\OUTPUT_BINARY";
            this.outputDirectory = Directory.CreateDirectory(outputDirectoryPath);
        }

        /// <summary>
        /// Populates the c-instruction lookup tables.
        /// </summary>
        private void CreateLookUpTables()
        {
            this.compLookupTableA0 = this.GetCompLookupTableA0();
            this.compLookupTableA1 = this.GetCompLookupTableA1();
            this.destLookupTable = this.GetDestLookupTable();
            this.jumpLookupTable = this.GetJumpLookupTable();
            this.predefinedLookupTable = this.GetPredefinedTable();
        }

        private Dictionary<string, string> GetCompLookupTableA0()
        {
            Dictionary<string, string> lookupDict = new Dictionary<string, string>();
            lookupDict.Add("0", "101010");
            lookupDict.Add("1", "111111");
            lookupDict.Add("-1", "111010");
            lookupDict.Add("D", "001100");
            lookupDict.Add("A", "110000");
            lookupDict.Add("!D", "001101");
            lookupDict.Add("!A", "110001");
            lookupDict.Add("-D", "001111");
            lookupDict.Add("-A", "110011");
            lookupDict.Add("D+1", "011111");
            lookupDict.Add("A+1", "110111");
            lookupDict.Add("D-1", "001110");
            lookupDict.Add("A-1", "110010");
            lookupDict.Add("D+A", "000010");
            lookupDict.Add("D-A", "010011");
            lookupDict.Add("A-D", "000111");
            lookupDict.Add("D&A", "000000");
            lookupDict.Add("D|A", "010101");

            return lookupDict;
        }

        private Dictionary<string, string> GetCompLookupTableA1()
        {
            Dictionary<string, string> lookupDict = new Dictionary<string, string>();
            lookupDict.Add("M", "110000");
            lookupDict.Add("!M", "110001");
            lookupDict.Add("-M", "110011");
            lookupDict.Add("M+1", "110111");
            lookupDict.Add("M-1", "110010");
            lookupDict.Add("D+M", "000010");
            lookupDict.Add("D-M", "010011");
            lookupDict.Add("M-D", "000111");
            lookupDict.Add("D&M", "000000");
            lookupDict.Add("D|M", "010101");

            return lookupDict;
        }

        private Dictionary<string, string> GetDestLookupTable()
        {
            Dictionary<string, string> lookupDict = new Dictionary<string, string>();
            lookupDict.Add("", "000");
            lookupDict.Add("M", "001");
            lookupDict.Add("D", "010");
            lookupDict.Add("MD", "011");
            lookupDict.Add("A", "100");
            lookupDict.Add("AM", "101");
            lookupDict.Add("AD", "110");
            lookupDict.Add("AMD", "111");

            return lookupDict;
        }

        private Dictionary<string, string> GetJumpLookupTable()
        {
            Dictionary<string, string> lookupDict = new Dictionary<string, string>();
            lookupDict.Add("", "000");
            lookupDict.Add("JGT", "001");
            lookupDict.Add("JEQ", "010");
            lookupDict.Add("JGE", "011");
            lookupDict.Add("JLT", "100");
            lookupDict.Add("JNE", "101");
            lookupDict.Add("JLE", "110");
            lookupDict.Add("JMP", "111");

            return lookupDict;
        }

        private Dictionary<string, string> GetPredefinedTable()
        {
            Dictionary<string, string> lookupDict = new Dictionary<string, string>();

            for (int i = 0; i < 16; i++)
            {
                lookupDict.Add("R" + i, i.ToString());
            }

            lookupDict.Add("SCREEN", "16384");
            lookupDict.Add("KBD", "24576");
            lookupDict.Add("SP", "0");
            lookupDict.Add("LCL", "1");
            lookupDict.Add("ARG", "2");
            lookupDict.Add("THIS", "3");
            lookupDict.Add("THAT", "4");

            return lookupDict;
        }
    }
}