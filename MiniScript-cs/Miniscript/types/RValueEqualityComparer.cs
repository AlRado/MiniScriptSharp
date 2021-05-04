﻿using System.Collections.Generic;

namespace Miniscript.types {

    public class RValueEqualityComparer : IEqualityComparer<Value> {

        public bool Equals(Value val1, Value val2) {
            return val1?.Equality(val2) > 0;
        }

        public int GetHashCode(Value val) {
            return val.Hash();
        }

        static RValueEqualityComparer _instance = null;

        public static RValueEqualityComparer instance => _instance ??= new RValueEqualityComparer();

    }

}