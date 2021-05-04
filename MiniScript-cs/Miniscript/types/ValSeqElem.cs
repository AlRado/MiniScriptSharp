using Miniscript.sources.tac;

namespace Miniscript.sources.types {

    public class ValSeqElem : Value {

        public Value sequence;
        public Value index;
        public bool noInvoke; // reflects use of "@" (address-of) operator

        public ValSeqElem(Value sequence, Value index) {
            this.sequence = sequence;
            this.index = index;
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
                        var found = map.map.TryGetValue(idVal, out var result);
                        TempValString.Release(idVal);
                        if (found) {
                            valueFoundIn = map;
                            return result;
                        }

                        // Otherwise, if we have an __isa, try that next.
                        if (loopsLeft < 0) return null; // (unless we've hit the loop limit)
                        if (!map.map.TryGetValue(ValString.magicIsA, out sequence)) {
                            // ...and if we don't have an __isa, try the generic map type if allowed
                            if (!includeMapType) throw new KeyException(identifier);
                            sequence = context.vm.mapType ?? Intrinsic.MapType();
                            includeMapType = false;
                        }

                        break;
                    }
                    case ValList _:
                        sequence = context.vm.listType ?? Intrinsic.ListType();
                        includeMapType = false;
                        break;
                    case ValString _:
                        sequence = context.vm.stringType ?? Intrinsic.StringType();
                        includeMapType = false;
                        break;
                    case ValNumber _:
                        sequence = context.vm.numberType ?? Intrinsic.NumberType();
                        includeMapType = false;
                        break;
                    case ValFunction _:
                        sequence = context.vm.functionType ?? Intrinsic.FunctionType();
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
            var baseSeq = sequence;
            if (sequence == ValVar.self) {
                baseSeq = context.self;
            }

            valueFoundIn = null;
            var idxVal = index?.Val(context);
            if (idxVal is ValString s) return Resolve(baseSeq, s.value, context, out valueFoundIn);
            // Ok, we're searching for something that's not a string;
            // this can only be done in maps and lists (and lists, only with a numeric index).
            var baseVal = baseSeq.Val(context);
            switch (baseVal) {
                case ValMap map: {
                    var result = map.Lookup(idxVal, out valueFoundIn);
                    if (valueFoundIn == null) throw new KeyException(idxVal?.CodeForm(context.vm, 1));
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
            return $"{(noInvoke ? "@" : "")}{sequence}[{index}]";
        }

        public override int Hash(int recursionDepth = 16) {
            return sequence.Hash(recursionDepth - 1) ^ index.Hash(recursionDepth - 1);
        }

        public override double Equality(Value rhs, int recursionDepth = 16) {
            return rhs is ValSeqElem elem && elem.sequence == sequence
                                          && elem.index == index
                ? 1
                : 0;
        }

    }

}