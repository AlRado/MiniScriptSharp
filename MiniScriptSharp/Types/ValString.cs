using System;
using MiniScriptSharp.Constants;
using MiniScriptSharp.Errors;
using MiniScriptSharp.Tac;

namespace MiniScriptSharp.Types {

    /// <summary>
    /// ValString represents a string (text) value.
    /// </summary>
    public class ValString : Value {
        
        // Magic identifier for the is-a entry in the class system:
        public static readonly ValString MagicIsA = new ValString(Consts.IS_A);

        /// <summary>
        /// Handy accessor for an empty ValString.
        /// IMPORTANT: do not alter the value of the object returned!
        /// </summary>
        public static readonly ValString Empty = new ValString(string.Empty);

        public const long MaxSize = 0xFFFFFF; // about 16M elements
        
        public string Value;

        public ValString(string value) {
            Value = value ?? Empty.Value;
        }

        public override string ToString(Machine vm) {
            return Value;
        }

        public override string CodeForm(Machine vm, int recursionLimit = -1) {
            return "\"" + Value.Replace("\"", "\"\"") + "\"";
        }

        public override bool BoolValue() {
            // Any nonempty string is considered true.
            return !string.IsNullOrEmpty(Value);
        }

        public override bool IsA(Value type, Machine vm) {
            return type == vm.StringType;
        }

        public override int Hash(int recursionDepth = 16) {
            return Value.GetHashCode();
        }

        public override double Equality(Value rhs, int recursionDepth = 16) {
            // String equality is treated the same as in C#.
            return rhs is ValString valString && valString.Value == Value ? 1 : 0;
        }

        public Value GetElem(Value index) {
            var i = index.IntValue();
            if (i < 0) i += Value.Length;
            if (i < 0 || i >= Value.Length) {
                throw new IndexException($"Index Error (string index {index} out of range)");
            }

            return new ValString(Value.Substring(i, 1));
        }

    }

}