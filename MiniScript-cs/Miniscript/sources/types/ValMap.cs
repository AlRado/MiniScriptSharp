using System;
using System.Collections.Generic;
using System.Linq;
using Miniscript.sources.tac;

namespace Miniscript.sources.types {

    /// <summary>
    /// ValMap represents a Minisript map, which under the hood is just a Dictionary
    /// of Value, Value pairs.
    /// </summary>
    public class ValMap : Value {

        public Dictionary<Value, Value> map;

        // Assignment override function: return true to cancel (override)
        // the assignment, or false to allow it to happen as normal.
        public delegate bool AssignOverrideFunc(Value key, Value value);

        public AssignOverrideFunc assignOverride;
        
        private static ValString keyStr = new ValString("key");
        private static ValString valStr = new ValString("value");

        public ValMap() {
            this.map = new Dictionary<Value, Value>(RValueEqualityComparer.instance);
        }

        public override bool BoolValue() {
            // A map is considered true if it is nonempty.
            return map != null && map.Count > 0;
        }

        /// <summary>
        /// Convenience method to check whether the map contains a given string key.
        /// </summary>
        /// <param name="identifier">string key to check for</param>
        /// <returns>true if the map contains that key; false otherwise</returns>
        public bool ContainsKey(string identifier) {
            var idVal = TempValString.Get(identifier);
            bool result = map.ContainsKey(idVal);
            TempValString.Release(idVal);
            return result;
        }

        /// <summary>
        /// Convenience method to check whether this map contains a given key
        /// (of arbitrary type).
        /// </summary>
        /// <param name="key">key to check for</param>
        /// <returns>true if the map contains that key; false otherwise</returns>
        public bool ContainsKey(Value key) {
            if (key == null) key = ValNull.instance;
            return map.ContainsKey(key);
        }

        /// <summary>
        /// Get the number of entries in this map.
        /// </summary>
        public int Count
        {
            get { return map.Count; }
        }

        /// <summary>
        /// Return the KeyCollection for this map.
        /// </summary>
        public Dictionary<Value, Value>.KeyCollection Keys
        {
            get { return map.Keys; }
        }


        /// <summary>
        /// Accessor to get/set on element of this map by a string key, walking
        /// the __isa chain as needed.  (Note that if you want to avoid that, then
        /// simply look up your value in .map directly.)
        /// </summary>
        /// <param name="identifier">string key to get/set</param>
        /// <returns>value associated with that key</returns>
        public Value this[string identifier]
        {
            get
            {
                var idVal = TempValString.Get(identifier);
                Value result = Lookup(idVal);
                TempValString.Release(idVal);
                return result;
            }
            set { map[new ValString(identifier)] = value; }
        }

        /// <summary>
        /// Look up the given identifier as quickly as possible, without
        /// walking the __isa chain or doing anything fancy.  (This is used
        /// when looking up local variables.)
        /// </summary>
        /// <param name="identifier">identifier to look up</param>
        /// <returns>true if found, false if not</returns>
        public bool TryGetValue(string identifier, out Value value) {
            var idVal = TempValString.Get(identifier);
            bool result = map.TryGetValue(idVal, out value);
            TempValString.Release(idVal);
            return result;
        }

        /// <summary>
        /// Look up a value in this dictionary, walking the __isa chain to find
        /// it in a parent object if necessary.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <returns>value associated with that key, or null if not found</returns>
        public Value Lookup(Value key) {
            if (key == null) key = ValNull.instance;
            Value result = null;
            ValMap obj = this;
            while (obj != null) {
                if (obj.map.TryGetValue(key, out result)) return result;
                Value parent;
                if (!obj.map.TryGetValue(ValString.magicIsA, out parent)) break;
                obj = parent as ValMap;
            }

            return null;
        }

        /// <summary>
        /// Look up a value in this dictionary, walking the __isa chain to find
        /// it in a parent object if necessary; return both the value found and
        /// (via the output parameter) the map it was found in.
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <returns>value associated with that key, or null if not found</returns>
        public Value Lookup(Value key, out ValMap valueFoundIn) {
            if (key == null) key = ValNull.instance;
            Value result = null;
            ValMap obj = this;
            while (obj != null) {
                if (obj.map.TryGetValue(key, out result)) {
                    valueFoundIn = obj;
                    return result;
                }

                Value parent;
                if (!obj.map.TryGetValue(ValString.magicIsA, out parent)) break;
                obj = parent as ValMap;
            }

            valueFoundIn = null;
            return null;
        }

        public override Value FullEval(Context context) {
            // Evaluate each of our elements, and if any of those is
            // a variable or temp, then resolve those now.
            foreach (Value k in map.Keys.ToArray()) {
                // TODO: something more efficient here.
                Value key = k; // stupid C#!
                Value value = map[key];
                if (key is ValTemp || key is ValVar) {
                    map.Remove(key);
                    key = key.Val(context);
                    map[key] = value;
                }

                if (value is ValTemp || value is ValVar) {
                    map[key] = value.Val(context);
                }
            }

            return this;
        }

        public ValMap EvalCopy(Context context) {
            // Create a copy of this map, evaluating its members as we go.
            // This is used when a map literal appears in the source, to
            // ensure that each time that code executes, we get a new, distinct
            // mutable object, rather than the same object multiple times.
            var result = new ValMap();
            foreach (Value k in map.Keys) {
                Value key = k; // stupid C#!
                Value value = map[key];
                if (key is ValTemp || key is ValVar) key = key.Val(context);
                if (value is ValTemp || value is ValVar) value = value.Val(context);
                result.map[key] = value;
            }

            return result;
        }

        public override string CodeForm(Machine vm, int recursionLimit = -1) {
            if (recursionLimit == 0) return "{...}";
            if (recursionLimit > 0 && recursionLimit < 3 && vm != null) {
                string shortName = vm.FindShortName(this);
                if (shortName != null) return shortName;
            }

            var strs = new string[map.Count];
            int i = 0;
            foreach (KeyValuePair<Value, Value> kv in map) {
                int nextRecurLimit = recursionLimit - 1;
                if (kv.Key == ValString.magicIsA) nextRecurLimit = 1;
                strs[i++] = string.Format("{0}: {1}", kv.Key.CodeForm(vm, nextRecurLimit),
                    kv.Value == null ? "null" : kv.Value.CodeForm(vm, nextRecurLimit));
            }

            return "{" + String.Join(", ", strs) + "}";
        }

        public override string ToString(Machine vm) {
            return CodeForm(vm, 3);
        }

        public override bool IsA(Value type, Machine vm) {
            // If the given type is the magic 'map' type, then we're definitely
            // one of those.  Otherwise, we have to walk the __isa chain.
            if (type == vm.mapType) return true;
            Value p = null;
            map.TryGetValue(ValString.magicIsA, out p);
            while (p != null) {
                if (p == type) return true;
                if (!(p is ValMap)) return false;
                ((ValMap) p).map.TryGetValue(ValString.magicIsA, out p);
            }

            return false;
        }

        public override int Hash(int recursionDepth = 16) {
            //return map.GetHashCode();
            int result = map.Count.GetHashCode();
            if (recursionDepth < 0) return result; // (important to recurse an odd number of times, due to bit flipping)
            foreach (KeyValuePair<Value, Value> kv in map) {
                result ^= kv.Key.Hash(recursionDepth - 1);
                if (kv.Value != null) result ^= kv.Value.Hash(recursionDepth - 1);
            }

            return result;
        }

        public override double Equality(Value rhs, int recursionDepth = 16) {
            if (!(rhs is ValMap)) return 0;
            Dictionary<Value, Value> rhm = ((ValMap) rhs).map;
            if (rhm == map) return 1; // (same map)
            int count = map.Count;
            if (count != rhm.Count) return 0;
            if (recursionDepth < 1) return 0.5; // in too deep
            double result = 1;
            foreach (KeyValuePair<Value, Value> kv in map) {
                if (!rhm.ContainsKey(kv.Key)) return 0;
                var rhvalue = rhm[kv.Key];
                if (kv.Value == null) {
                    if (rhvalue != null) return 0;
                    continue;
                }

                result *= kv.Value.Equality(rhvalue, recursionDepth - 1);
                if (result <= 0) break;
            }

            return result;
        }

        public override bool CanSetElem() {
            return true;
        }

        /// <summary>
        /// Set the value associated with the given key (index).  This is where
        /// we take the opportunity to look for an assignment override function,
        /// and if found, give that a chance to handle it instead.
        /// </summary>
        public override void SetElem(Value index, Value value) {
            if (index == null) index = ValNull.instance;
            if (assignOverride == null || !assignOverride(index, value)) {
                map[index] = value;
            }
        }

        /// <summary>
        /// Get the indicated key/value pair as another map containing "key" and "value".
        /// (This is used when iterating over a map with "for".)
        /// </summary>
        /// <param name="index">0-based index of key/value pair to get.</param>
        /// <returns>new map containing "key" and "value" with the requested key/value pair</returns>
        public ValMap GetKeyValuePair(int index) {
            Dictionary<Value, Value>.KeyCollection keys = map.Keys;
            if (index < 0 || index >= keys.Count) {
                throw new IndexException("index " + index + " out of range for map");
            }

            Value key = keys.ElementAt<Value>(index); // (TODO: consider more efficient methods here)
            var result = new ValMap();
            result.map[keyStr] = (key is ValNull ? null : key);
            result.map[valStr] = map[key];
            return result;
        }

    }

}