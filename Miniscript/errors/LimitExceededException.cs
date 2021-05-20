﻿/*	
This file defines the exception hierarchy used by Miniscript.
The core of the tree is this:

	MiniscriptException
		LexerException -- any error while finding tokens from raw source
		CompilerException -- any error while compiling tokens into bytecode
		RuntimeException -- any error while actually executing code.

We have a number of fine-grained exception types within these,
but they will always derive from one of those three (and ultimately
from MiniscriptException).
*/

using System;

namespace Miniscript.errors {

    public class LimitExceededException : RuntimeException {

        public LimitExceededException() : base("Runtime Limit Exceeded") {}

        public LimitExceededException(string message) : base(message) {}

        public LimitExceededException(string message, Exception inner) : base(message, inner) {}

    }

}