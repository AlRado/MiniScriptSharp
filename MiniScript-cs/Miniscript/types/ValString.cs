using Miniscript.sources.tac;

namespace Miniscript.sources.types {

    /// <summary>
    /// ValString represents a string (text) value.
    /// </summary>
    public class ValString : Value {
        
        // Magic identifier for the is-a entry in the class system:
        public static readonly ValString magicIsA = new ValString("__isa");

        /// <summary>
        /// Handy accessor for an empty ValString.
        /// IMPORTANT: do not alter the value of the object returned!
        /// </summary>
        public static readonly ValString empty = new ValString("");

        public static long maxSize = 0xFFFFFF; // about 16M elements
        
        public string value;

        public ValString(string value) {
            this.value = value ?? empty.value;
        }

        public override string ToString(Machine vm) {
            return value;
        }

        public override string CodeForm(Machine vm, int recursionLimit = -1) {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        public override bool BoolValue() {
            // Any nonempty string is considered true.
            return !string.IsNullOrEmpty(value);
        }

        public override bool IsA(Value type, Machine vm) {
            return type == vm.stringType;
        }

        public override int Hash(int recursionDepth = 16) {
            return value.GetHashCode();
        }

        public override double Equality(Value rhs, int recursionDepth = 16) {
            // String equality is treated the same as in C#.
            return rhs is ValString valString && valString.value == value ? 1 : 0;
        }

        public Value GetElem(Value index) {
            var i = index.IntValue();
            if (i < 0) i += value.Length;
            if (i < 0 || i >= value.Length) {
                throw new IndexException("Index Error (string index " + index + " out of range)");
            }

            return new ValString(value.Substring(i, 1));
        }

    }

}