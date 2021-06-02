/*
 * The core of the exception hierarchy used by Miniscript:
 * 
 * 	MiniscriptException
 * 		LexerException -- any error while finding tokens from raw source
 * 		CompilerException -- any error while compiling tokens into bytecode
 * 		RuntimeException -- any error while actually executing code.
 * 
 * We have a number of fine-grained exception types within these,
 * but they will always derive from one of those three (and ultimately
 * from MiniscriptException).
 */

using System;

namespace MiniScriptSharp.Errors {

    public class CompilerException : MiniscriptException {

        public CompilerException() : base("Compiler Error") {}

        public CompilerException(string message) : base(message) {}

        public CompilerException(string context, int lineNum, string message) : base(context, lineNum, message) {}

        public CompilerException(string message, Exception inner) : base(message, inner) {}

    }

}