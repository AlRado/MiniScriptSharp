using MiniScriptSharp.Errors;
using MiniScriptSharp.Intrinsics;
using MiniScriptSharp.Tac;

namespace MiniScriptSharp.Types {

    public class ValSeqElem : Value {

        public Value Sequence;
        public Value Index;
        public bool NoInvoke; // reflects use of "@" (address-of) operator

        public ValSeqElem(Value sequence, Value index) {
            Sequence = sequence;
            Index = index;
        }

        /// <summary>
        /// Look up the given identifier in the given sequence, walking the type chain
        /// until we either find it, or fail.
        /// </summary>
        /// <param name="sequence">Sequence (object) to look in.</param>
        /// <param name="identifier">Identifier to look for.</param>
        /// <param name="context">Context.</param>
        public static Value Resolve(Value sequence, string identifier, Context context, out ValMap valueFoundIn) {
            var includeMapType = true;
            valueFoundIn = null;
            var loopsLeft = 1000; // (max __isa chain depth)
            while (sequence != null) {
                if (sequence is ValTemp || sequence is ValVar) sequence = sequence.Val(context);
                switch (sequence) {
                    case ValMap map: {
                        // If the map contains this identifier, return its value.
                        var idVal = TempValString.Get(identifier);
                        var found = map.Map.TryGetValue(idVal, out var result);
                        TempValString.Release(idVal);
                        if (found) {
                            valueFoundIn = map;
                            return result;
                        }

                        // Otherwise, if we have an __isa, try that next.
                        if (loopsLeft < 0) return null; // (unless we've hit the loop limit)
                        if (!map.Map.TryGetValue(ValString.MagicIsA, out sequence)) {
                            // ...and if we don't have an __isa, try the generic map type if allowed
                            if (!includeMapType) throw new KeyException(identifier);
                            sequence = context.Vm.MapType ?? Intrinsic.MapType;
                            includeMapType = false;
                        }

                        break;
                    }
                    case ValList _:
                        sequence = context.Vm.ListType ?? Intrinsic.ListType;
                        includeMapType = false;
                        break;
                    case ValString _:
                        sequence = context.Vm.StringType ?? Intrinsic.StringType;
                        includeMapType = false;
                        break;
                    case ValNumber _:
                        sequence = context.Vm.NumberType ?? Intrinsic.NumberType;
                        includeMapType = false;
                        break;
                    case ValFunction _:
                        sequence = context.Vm.FunctionType ?? Intrinsic.FunctionType;
                        includeMapType = false;
                        break;
                    default:
                        throw new TypeException("Type Error (while attempting to look up " + identifier + ")");
                }

                loopsLeft--;
            }

            return null;
        }

        public override Value Val(Context context) {
            return Val(context, out _);
        }

        public override Value Val(Context context, out ValMap valueFoundIn) {
            var baseSeq = Sequence;
            if (Sequence == ValVar.Self) {
                baseSeq = context.Self;
            }

            valueFoundIn = null;
            var idxVal = Index?.Val(context);
            if (idxVal is ValString s) return Resolve(baseSeq, s.Value, context, out valueFoundIn);
            // Ok, we're searching for something that's not a string;
            // this can only be Done in maps and lists (and lists, only with a numeric index).
            var baseVal = baseSeq.Val(context);
            switch (baseVal) {
                case ValMap map: {
                    var result = map.Lookup(idxVal, out valueFoundIn);
                    if (valueFoundIn == null) throw new KeyException(idxVal?.CodeForm(context.Vm, 1));
                    return result;
                }
                case ValList list when idxVal is ValNumber:
                    return list.GetElem(idxVal);
                case ValString valString when idxVal is ValNumber:
                    return valString.GetElem(idxVal);
                default:
                    throw new TypeException("Type Exception: can't index into this type");
            }
        }

        public override string ToString(Machine vm) {
            return $"{(NoInvoke ? "@" : "")}{Sequence}[{Index}]";
        }

        public override int Hash(int recursionDepth = 16) {
            return Sequence.Hash(recursionDepth - 1) ^ Index.Hash(recursionDepth - 1);
        }

        public override double Equality(Value rhs, int recursionDepth = 16) {
            return rhs is ValSeqElem elem && elem.Sequence == Sequence
                                          && elem.Index == Index
                ? 1
                : 0;
        }

    }

}