using System;

namespace Miniscript.errors {

    public class TypeException : RuntimeException {

        public TypeException() : base("Type Error (wrong type for whatever you're doing)") {}

        public TypeException(string message) : base(message) {}

        public TypeException(string message, Exception inner) : base(message, inner) {}

    }

}