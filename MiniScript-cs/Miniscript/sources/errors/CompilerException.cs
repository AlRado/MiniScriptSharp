using System;

namespace Miniscript {

    public class CompilerException : MiniscriptException {

        public CompilerException() : base("Syntax Error") {}

        public CompilerException(string message) : base(message) {}

        public CompilerException(string context, int lineNum, string message) : base(context, lineNum, message) {}

        public CompilerException(string message, Exception inner) : base(message, inner) {}

    }

}