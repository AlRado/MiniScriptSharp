/*
 * UnitTest.cs
 *
 * This file contains a number of unit tests for various parts of the MiniScript
 * architecture.  It's used by the MiniScript developers to ensure we don't
 * break something when we make changes.
 * 
 * You can safely ignore this, but if you really want to run the tests yourself,
 * just call MiniScriptSharp.Tests.UnitTest.Run().
 */

using System;
using MiniScriptSharp.Lexis;
using MiniScriptSharp.Parse;

namespace MiniScriptSharp.Tests {

    public static class UnitTest {

        private static void ReportError(string err) {
            // Set a breakpoint here if you want to drop into the debugger
            // on any unit test failure
            Console.WriteLine(err);
        }

        public static void ErrorIf(bool condition, string err) {
            if (condition) ReportError(err);
        }

        public static void ErrorIfNull(object obj) {
            if (obj == null) ReportError("Unexpected null");
        }

        public static void ErrorIfNotNull(object obj) {
            if (obj != null) ReportError("Expected null, but got non-null");
        }

        public static void ErrorIfNotEqual(string actual, string expected) {
             if (actual != expected) ReportError($"Expected {expected}, got {actual}");
        }

        public static void ErrorIfNotEqual(float actual, float expected) {
             if (actual != expected) ReportError($"Expected {expected}, got {actual}");
        }

        public static void Run() {
            Lexer.RunUnitTests();
            Parser.RunUnitTests();
        }

    }

}