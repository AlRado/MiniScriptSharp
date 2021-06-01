using System.Collections.Generic;
using System.Linq;
using MiniScriptSharp.Errors;
using MiniScriptSharp.Tac;
using static MiniScriptSharp.Constants.Consts;

namespace MiniScriptSharp.Types {

    /// <summary>
    /// ValMap represents a MiniScript map, which under the hood is just a Dictionary
    /// of Value, Value pairs.
    /// </summary>
    public class ValMap : Value {

        public Dictionary<Value, Value> Map;

        // Assignment override function: return true to cancel (override)
        // the assignment, or false to allow it to happen as normal.
        public delegate bool AssignOverrideFunc(Value key, Value value);

        public AssignOverrideFunc AssignOverride;
        
        private static readonly ValString KeyStr = new ValString(KEY);
        private static readonly ValString ValStr = new ValString(VALUE);

        public ValMap() {
            Map = new Dictionary<Value, Value>(RValueEqualityComparer.Instance);
        }

        public override bool BoolValue() {
            // A map is considered true if it is nonempty.
            return Map != null && Map.Count > 0;
        }

        /// <summary>
        /// Convenience method to check whether the map contains a given string key.
        /// </summary>
        /// <param name="identifier">string key to check for</param>
        /// <returns>true if the map contains that key; false otherwise</returns>
        public bool ContainsKey(string identifier) {
            var idVal = TempValString.Get(identifier);
            var result = Map.ContainsKey(idVal);
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
            key ??= ValNull.Instance;
            return Map.ContainsKey(key);
        }

        /// <summary>
        /// Get the number of entries in this map.
        /// </summary>
        public int Count => Map.Count;

        /// <summary>
        /// Return the KeyCollection for this map.
        /// </summary>
        public Dictionary<Value, Value>.KeyCollection Keys => Map.Keys;


        /// <summary>
        /// Accessor to get/set on element of this map by a string key, walking
        /// the __isa chain as needed.  (Note that if you want to avoid that, then
        /// simply look up your value in .map directly.)
        /// </summary>
        /// <param name="identifier">string key to get/set</param>
        /// <returns>value associated with that key</returns>
        public Value this[string identifier] {
            get {
                var idVal = TempValString.Get(identifier);
                var result = Lookup(idVal);
                TempValString.Release(idVal);
                return result;
            }
            set => Map[new ValString(identifier)] = value;
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
            var result = Map.TryGetValue(idVal, out value);
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
            key ??= ValNull.Instance;
            var obj = this;
            while (obj != null) {
                if (obj.Map.TryGetValue(key, out var result)) return result;
                if (!obj.Map.TryGetValue(ValString.MagicIsA, out var parent)) break;
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
            key ??= ValNull.Instance;
            var obj = this;
            while (obj != null) {
                if (obj.Map.TryGetValue(key, out var result)) {
                    valueFoundIn = obj;
                    return result;
                }

                if (!obj.Map.TryGetValue(ValString.MagicIsA, out var parent)) break;
                obj = parent as ValMap;
            }

            valueFoundIn = null;
            return null;
        }

        public override Value FullEval(Context context) {
            // Evaluate each of our elements, and if any of those is
            // a variable or temp, then resolve those now.
            foreach (var k in Map.Keys.ToArray()) {
                // TODO: something more efficient here.
                var key = k; // stupid C#!
                var value = Map[key];
                if (key is ValTemp || key is ValVar) {
                    Map.Remove(key);
                    key = key.Val(context);
                    Map[key] = value;
                }

                if (value is ValTemp || value is ValVar) {
                    Map[key] = value.Val(context);
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
            foreach (var k in Map.Keys) {
                var key = k; // stupid C#!
                var value = Map[key];
                if (key is ValTemp || key is ValVar) key = key.Val(context);
                if (value is ValTemp || value is ValVar) value = value.Val(context);
                result.Map[key] = value;
            }

            return result;
        }

        public override string CodeForm(Machine vm, int recursionLimit = -1) {
            if (recursionLimit == 0) return "{...}";
            if (recursionLimit > 0 && recursionLimit < 3 && vm != null) {
                var shortName = vm.FindShortName(this);
                if (shortName != null) return shortName;
            }

            var strs = new string[Map.Count];
            var i = 0;
            foreach (var kv in Map) {
                var nextRecurLimit = recursionLimit - 1;
                if (kv.Key == ValString.MagicIsA) nextRecurLimit = 1;
                strs[i++] =
                    $"{kv.Key.CodeForm(vm, nextRecurLimit)}: {(kv.Value == null ? NULL : kv.Value.CodeForm(vm, nextRecurLimit))}";
            }

            return "{" + string.Join(", ", strs) + "}";
        }

        public override string ToString(Machine vm) {
            return CodeForm(vm, 3);
        }

        public override bool IsA(Value type, Machine vm) {
            // If the given type is the magic 'map' type, then we're definitely
            // one of those.  Otherwise, we have to walk the __isa chain.
            if (type == vm.MapType) return true;
            Map.TryGetValue(ValString.MagicIsA, out var p);
            while (p != null) {
                if (p == type) return true;
                if (!(p is ValMap valMap)) return false;
                valMap.Map.TryGetValue(ValString.MagicIsA, out p);
            }

            return false;
        }

        public override int Hash(int recursionDepth = 16) {
            //return map.GetHashCode();
            var result = Map.Count.GetHashCode();
            if (recursionDepth < 0) return result; // (important to recurse an odd number of times, due to bit flipping)
            foreach (var kv in Map) {
                result ^= kv.Key.Hash(recursionDepth - 1);
                if (kv.Value != null) result ^= kv.Value.Hash(recursionDepth - 1);
            }

            return result;
        }

        public override double Equality(Value rhs, int recursionDepth = 16) {
            if (!(rhs is ValMap valMap)) return 0;
            var rhm = valMap.Map;
            if (rhm == Map) return 1; // (same map)
            var count = Map.Count;
            if (count != rhm.Count) return 0;
            if (recursionDepth < 1) return 0.5; // in too deep
            double result = 1;
            foreach (var kv in Map) {
                if (!rhm.ContainsKey(kv.Key)) return 0;
                var rhValue = rhm[kv.Key];
                if (kv.Value == null) {
                    if (rhValue != null) return 0;
                    continue;
                }

                result *= kv.Value.Equality(rhValue, recursionDepth - 1);
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
            index ??= ValNull.Instance;
            if (AssignOverride == null || !AssignOverride(index, value)) {
                Map[index] = value;
            }
        }

        /// <summary>
        /// Get the indicated key/value pair as another map containing "key" and "value".
        /// (This is used when iterating over a map with "for".)
        /// </summary>
        /// <param name="index">0-based index of key/value pair to get.</param>
        /// <returns>new map containing "key" and "value" with the requested key/value pair</returns>
        public ValMap GetKeyValuePair(int index) {
            var keys = Map.Keys;
            if (index < 0 || index >= keys.Count) {
                throw new IndexException($"index {index} out of range for map");
            }

            var key = keys.ElementAt<Value>(index); // (TODO: consider more efficient methods here)
            var result = new ValMap {Map = {[KeyStr] = (key is ValNull ? null : key), [ValStr] = Map[key]}};
            return result;
        }

    }

}