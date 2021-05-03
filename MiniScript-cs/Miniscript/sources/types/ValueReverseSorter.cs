using System.Collections.Generic;

namespace Miniscript.sources.types {

    public class ValueReverseSorter : IComparer<Value> {
        public static ValueReverseSorter instance = new ValueReverseSorter();
        public int Compare(Value x, Value y) {
            return Value.Compare(y, x);
        }
    }


}