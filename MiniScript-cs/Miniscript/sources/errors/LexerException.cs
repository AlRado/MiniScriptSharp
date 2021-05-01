using System;

namespace Miniscript {

    public class LexerException : MiniscriptException {

        public LexerException() : base("Lexer Error") {
        }

        public LexerException(string message) : base(message) {
        }

        public LexerException(string message, Exception inner) : base(message, inner) {
        }

    }

}