using System;
using System.Collections.Generic;
using System.IO;

namespace MiniScriptSharp.Tests {

    public class TestSuite {

        public static void Run(string path) {
            var file = new StreamReader(path);
            List<string> sourceLines = null;
            List<string> expectedOutput = null;
            int testLineNum = 0;
            int outputLineNum = 0;

            string line = file.ReadLine();
            int lineNum = 1;
            while (line != null) {
                if (line.StartsWith("====")) {
                    if (sourceLines != null) Test(sourceLines, testLineNum, expectedOutput, outputLineNum);
                    sourceLines = null;
                    expectedOutput = null;
                }
                else if (line.StartsWith("----")) {
                    expectedOutput = new List<string>();
                    outputLineNum = lineNum + 1;
                }
                else if (expectedOutput != null) {
                    expectedOutput.Add(line);
                }
                else {
                    if (sourceLines == null) {
                        sourceLines = new List<string>();
                        testLineNum = lineNum;
                    }

                    sourceLines.Add(line);
                }

                line = file.ReadLine();
                lineNum++;
            }

            if (sourceLines != null) Test(sourceLines, testLineNum, expectedOutput, outputLineNum);
        }
        
        public static void RunFile(string path, bool dumpTAC=false) {
            var file = new StreamReader(path);

            var sourceLines = new List<string>();
            while (!file.EndOfStream) sourceLines.Add(file.ReadLine());

            var miniscript = new Interpreter(sourceLines);
            miniscript.StandardOutput = miniscript.ImplicitOutput = Console.WriteLine;
            miniscript.Compile();

            if (dumpTAC) miniscript.DumpTopContext();

            while (!miniscript.Done) miniscript.RunUntilDone();
        }

        private static void Test(List<string> sourceLines, int sourceLineNum, List<string> expectedOutput,
            int outputLineNum) {
            expectedOutput ??= new List<string>();

//		Console.WriteLine("TEST (LINE {0}):", sourceLineNum);
//		Console.WriteLine(string.Join("\n", sourceLines));
//		Console.WriteLine("EXPECTING (LINE {0}):", outputLineNum);
//		Console.WriteLine(string.Join("\n", expectedOutput));

            Interpreter miniscript = new Interpreter(sourceLines);
            List<string> actualOutput = new List<string>();
            miniscript.StandardOutput = (string s) => actualOutput.Add(s);
            miniscript.ErrorOutput = miniscript.StandardOutput;
            miniscript.ImplicitOutput = miniscript.StandardOutput;
            miniscript.RunUntilDone(60, false);

//		Console.WriteLine("ACTUAL OUTPUT:");
//		Console.WriteLine(string.Join("\n", actualOutput));

            var minLen = expectedOutput.Count < actualOutput.Count ? expectedOutput.Count : actualOutput.Count;
            for (int i = 0; i < minLen; i++) {
                if (actualOutput[i] != expectedOutput[i]) {
                    Console.WriteLine(
                        $"TEST FAILED AT LINE {outputLineNum + i}\n  EXPECTED: {expectedOutput[i]}\n    ACTUAL: {actualOutput[i]}");
                }
            }

            if (expectedOutput.Count > actualOutput.Count) {
                Console.WriteLine($"TEST FAILED: MISSING OUTPUT AT LINE {outputLineNum + actualOutput.Count}");
                for (int i = actualOutput.Count; i < expectedOutput.Count; i++) {
                    Console.WriteLine("  MISSING: " + expectedOutput[i]);
                }
            }
            else if (actualOutput.Count > expectedOutput.Count) {
                Console.WriteLine($"TEST FAILED: EXTRA OUTPUT AT LINE {outputLineNum + expectedOutput.Count}");
                for (int i = expectedOutput.Count; i < actualOutput.Count; i++) {
                    Console.WriteLine("  EXTRA: " + actualOutput[i]);
                }
            }
            
        }

    }

}