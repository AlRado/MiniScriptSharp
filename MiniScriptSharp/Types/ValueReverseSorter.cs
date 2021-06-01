using System.Collections.Generic;

namespace MiniScriptSharp.Types {

    public class ValueReverseSorter : IComparer<Value> {
        public static readonly ValueReverseSorter Instance = new ValueReverseSorter();
        
        public int Compare(Value x, Value y) {
            return Value.Compare(y, x);
        }
    }


}