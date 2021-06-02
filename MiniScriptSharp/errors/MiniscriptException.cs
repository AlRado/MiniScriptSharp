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

    public class MiniscriptException: Exception {
        
        public SourceLoc location;
		
        public MiniscriptException(string message) : base(message) {}

        public MiniscriptException(string context, int lineNum, string message) : base(message) {
            location = new SourceLoc(context, lineNum);
        }

        public MiniscriptException(string message, Exception inner) : base(message, inner) {}

        /// <summary>
        /// Get a standard description of this error, including type and location.
        /// </summary>
        public string Description() {
            var desc = this switch {
                LexerException _ => "Lexer Error: ",
                CompilerException _ => "Compiler Error: ",
                RuntimeException _ => "Runtime Error: ",
                _ => "Error: "
            };
            desc += Message;
            if (location != null) desc += " " + location;
            
            return desc;		
        }

    }

}