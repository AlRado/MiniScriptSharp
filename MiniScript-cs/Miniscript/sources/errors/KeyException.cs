using System;

namespace Miniscript {

    public class KeyException : RuntimeException {

        public KeyException(string key) : base("Key Not Found: '" + key + "' not found in map") {
        }

        public KeyException(string message, Exception inner) : base(message, inner) {
        }

    }

}