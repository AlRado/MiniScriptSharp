using System;

namespace Miniscript {

    public class IndexException : RuntimeException {

        public IndexException() : base("Index Error (index out of range)") {
        }

        public IndexException(string message) : base(message) {
        }

        public IndexException(string message, Exception inner) : base(message, inner) {
        }

    }

}