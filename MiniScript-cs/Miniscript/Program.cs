using System;
using System.IO;

// Unit and integration tests, quick test script from file and REPL in console
namespace Miniscript {

	internal static class Program {

		private const string TestSuiteFilePath = "../../sources/tests/TestSuiteData.txt";
		private const string QuickTestFilePath = "../../../QuickTest.mscp";
	
		public static void Main(string[] args) {
			HostInfo.name = "Test harness";
		
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
			interpreter.implicitOutput = interpreter.standardOutput;

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
