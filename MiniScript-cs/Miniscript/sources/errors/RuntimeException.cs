using System;

namespace Miniscript {

    public class RuntimeException : MiniscriptException {

        public RuntimeException() : base("Runtime Error") {}

        public RuntimeException(string message) : base(message) {}

        public RuntimeException(string message, Exception inner) : base(message, inner) {}

    }

}