/*	MiniScriptErrors.cs

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

using Miniscript.keywords;
using Miniscript.types;

namespace Miniscript.errors {

    public static class Check {

        public static void Range(int i, int min, int max, string desc = Consts.INDEX) {
            if (i < min || i > max) throw new IndexException($"Index Error: {desc} ({i}) out of range ({min} to {max})");
        }

        public static void Type(Value val, System.Type requiredType, string desc = null) {
            if (requiredType.IsInstanceOfType(val)) return;

            var typeStr = val == null ? Consts.NULL : $"a {val.GetType()}";
            desc ??= $" ({desc})";

            throw new TypeException($"got {typeStr} where a {requiredType} was required{desc}");
        }

    }

}