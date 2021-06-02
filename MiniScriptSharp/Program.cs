using System;
using System.IO;
using MiniScriptSharp.Intrinsics;
using MiniScriptSharp.Tests;

namespace MiniScriptSharp {
	
	/// <summary>
	/// Unit and integration tests, quick test script from file and REPL in console
	/// </summary>
	internal static class Program {

		private const string TestSuiteFilePath = "../../tests/TestSuiteData.txt";
		private const string QuickTestFilePath = "../../tests/QuickTest.ms";
	
		public static void Main(string[] args) {
			HostInfo.Name = "Test harness";
		
			Console.WriteLine("Miniscript test harness.\n");

			Console.WriteLine("Running unit tests.\n");
			UnitTest.Run();

			Console.WriteLine("Running integration tests.\n");
			TestSuite.Run(TestSuiteFilePath);

			if (File.Exists(QuickTestFilePath)) {
				Console.WriteLine("Running quick test.\n");
				TestSuite.RunFile(QuickTestFilePath, true);
			} else {
				Console.WriteLine("Quick test not found, skipping...\n");
			}

			if (args.Length > 0) {
				var path = args[0];
				Console.WriteLine($"Running script from file: {path}\n");
				TestSuite.RunFile(path);
				return;
			}
		
			var interpreter = new Interpreter();
			interpreter.ImplicitOutput = interpreter.StandardOutput;

			Console.WriteLine("MiniScript REPL is ready to use:\n");
			while (true) {
				Console.Write(interpreter.NeedMoreInput() ? ">>> " : "> ");
			
				var inp = Console.ReadLine();
				if (inp == null) break;
			
				interpreter.REPL(inp);
			}
		}
	}

}
