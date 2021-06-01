using System.Collections.Generic;

namespace MiniScriptSharp.Types {

    public class ValueSorter : IComparer<Value> {
        public static readonly ValueSorter Instance = new ValueSorter();
        
        public int Compare(Value x, Value y) {
            return Value.Compare(x, y);
        }
    }



}