using MiniScriptSharp.Constants;
using MiniScriptSharp.Types;

namespace MiniScriptSharp.Errors {

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