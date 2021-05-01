using System;

namespace Miniscript {

    public class LimitExceededException : RuntimeException {

        public LimitExceededException() : base("Runtime Limit Exceeded") {
        }

        public LimitExceededException(string message) : base(message) {
        }

        public LimitExceededException(string message, Exception inner) : base(message, inner) {
        }

    }

}