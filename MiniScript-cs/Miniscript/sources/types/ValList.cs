using System.Collections.Generic;
using Miniscript.sources.tac;

namespace Miniscript.sources.types {

    /// <summary>
    /// ValList represents a Minisript list (which, under the hood, is
    /// just a wrapper for a List of Values).
    /// </summary>
    public class ValList : Value {

        public static long maxSize = 0xFFFFFF; // about 16 MB

        public List<Value> values;

        public ValList(List<Value> values = null) {
            this.values = values ?? new List<Value>();
        }

        public override Value FullEval(Context context) {
            // Evaluate each of our list elements, and if any of those is
            // a variable or temp, then resolve those now.
            // CAUTION: do not mutate our original list!  We may need
            // it in its original form on future iterations.
            ValList result = null;
            for (var i = 0; i < values.Count; i++) {
                var copied = false;
                if (values[i] is ValTemp || values[i] is ValVar) {
                    var newVal = values[i].Val(context);
                    if (newVal != values[i]) {
                        // OK, something changed, so we're going to need a new copy of the list.
                        if (result == null) {
                            result = new ValList();
                            for (var j = 0; j < i; j++) result.values.Add(values[j]);
                        }

                        result.values.Add(newVal);
                        copied = true;
                    }
                }

                if (!copied && result != null) {
                    // No change; but we have new results to return, so copy it as-is
                    result.values.Add(values[i]);
                }
            }

            return result ?? this;
        }

        public ValList EvalCopy(Context context) {
            // Create a copy of this list, evaluating its members as we go.
            // This is used when a list literal appears in the source, to
            // ensure that each time that code executes, we get a new, distinct
            // mutable object, rather than the same object multiple times.
            var result = new ValList();
            for (var i = 0; i < values.Count; i++) {
                result.values.Add(values[i] == null ? null : values[i].Val(context));
            }

            return result;
        }

        public override string CodeForm(Machine vm, int recursionLimit = -1) {
            if (recursionLimit == 0) return "[...]";
            if (recursionLimit > 0 && recursionLimit < 3 && vm != null) {
                var shortName = vm.FindShortName(this);
                if (shortName != null) return shortName;
            }

            var strs = new string[values.Count];
            for (var i = 0; i < values.Count; i++) {
                if (values[i] == null) strs[i] = "null";
                else strs[i] = values[i].CodeForm(vm, recursionLimit - 1);
            }

            return "[" + string.Join(", ", strs) + "]";
        }

        public override string ToString(Machine vm) {
            return CodeForm(vm, 3);
        }

        public override bool BoolValue() {
            // A list is considered true if it is nonempty.
            return values != null && values.Count > 0;
        }

        public override bool IsA(Value type, Machine vm) {
            return type == vm.listType;
        }

        public override int Hash(int recursionDepth = 16) {
            //return values.GetHashCode();
            var result = values.Count.GetHashCode();
            if (recursionDepth < 1) return result;
            foreach (var t in values) {
                result ^= t.Hash(recursionDepth - 1);
            }

            return result;
        }

        public override double Equality(Value rhs, int recursionDepth = 16) {
            if (!(rhs is ValList list)) return 0;
            var rhl = list.values;
            if (rhl == values) return 1; // (same list)
            var count = values.Count;
            if (count != rhl.Count) return 0;
            if (recursionDepth < 1) return 0.5; // in too deep
            double result = 1;
            for (var i = 0; i < count; i++) {
                result *= values[i].Equality(rhl[i], recursionDepth - 1);
                if (result <= 0) break;
            }

            return result;
        }

        public override bool CanSetElem() {
            return true;
        }

        public override void SetElem(Value index, Value value) {
            var i = index.IntValue();
            if (i < 0) i += values.Count;
            if (i < 0 || i >= values.Count) {
                throw new IndexException("Index Error (list index " + index + " out of range)");
            }

            values[i] = value;
        }

        public Value GetElem(Value index) {
            var i = index.IntValue();
            if (i < 0) i += values.Count;
            if (i < 0 || i >= values.Count) {
                throw new IndexException("Index Error (list index " + index + " out of range)");
            }

            return values[i];
        }

    }

}