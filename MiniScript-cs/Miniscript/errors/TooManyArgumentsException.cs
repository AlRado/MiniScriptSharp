using System;

namespace Miniscript.errors {

    public class TooManyArgumentsException : RuntimeException {

        public TooManyArgumentsException() : base("Too Many Arguments") {}

        public TooManyArgumentsException(string message) : base(message) {}

        public TooManyArgumentsException(string message, Exception inner) : base(message, inner) {}

    }

}