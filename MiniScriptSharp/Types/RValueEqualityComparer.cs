using System.Collections.Generic;

namespace MiniScriptSharp.Types {

    public class RValueEqualityComparer : IEqualityComparer<Value> {

        public bool Equals(Value val1, Value val2) {
            return val1?.Equality(val2) > 0;
        }

        public int GetHashCode(Value val) {
            return val.Hash();
        }

        public static readonly RValueEqualityComparer Instance = new RValueEqualityComparer();

    }

}