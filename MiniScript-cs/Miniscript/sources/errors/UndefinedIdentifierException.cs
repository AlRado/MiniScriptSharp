using System;

namespace Miniscript {

    public class UndefinedIdentifierException : RuntimeException {

        public UndefinedIdentifierException(string ident) : base(
            $"Undefined Identifier: '{ident}' is unknown in this context") {
        }

        public UndefinedIdentifierException(string message, Exception inner) : base(message, inner) {
        }

    }

}