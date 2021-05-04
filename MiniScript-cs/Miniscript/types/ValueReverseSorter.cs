using System.Collections.Generic;

namespace Miniscript.types {

    public class ValueReverseSorter : IComparer<Value> {
        public static readonly ValueReverseSorter instance = new ValueReverseSorter();
        public int Compare(Value x, Value y) {
            return Value.Compare(y, x);
        }
    }


}