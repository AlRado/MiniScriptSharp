using System.Collections.Generic;

namespace Miniscript.sources.types {

    public class ValueSorter : IComparer<Value> {
        public static ValueSorter instance = new ValueSorter();
        public int Compare(Value x, Value y) {
            return Value.Compare(x, y);
        }
    }



}