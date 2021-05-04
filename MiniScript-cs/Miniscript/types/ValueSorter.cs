﻿using System.Collections.Generic;

namespace Miniscript.types {

    public class ValueSorter : IComparer<Value> {
        public static readonly ValueSorter instance = new ValueSorter();
        public int Compare(Value x, Value y) {
            return Value.Compare(x, y);
        }
    }



}