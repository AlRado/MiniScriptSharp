using System;
using System.Collections.Generic;
using Miniscript.errors;
using Miniscript.intrinsic;
using Miniscript.types;

namespace Miniscript.tac {

		public class Line {
			public enum Op {
				Noop = 0,
				AssignA,
				AssignImplicit,
				APlusB,
				AMinusB,
				ATimesB,
				ADividedByB,
				AModB,
				APowB,
				AEqualB,
				ANotEqualB,
				AGreaterThanB,
				AGreatOrEqualB,
				ALessThanB,
				ALessOrEqualB,
				AisaB,
				AAndB,
				AOrB,
				BindAssignA,
				CopyA,
				NotA,
				GotoA,
				GotoAifB,
				GotoAifTrulyB,
				GotoAifNotB,
				PushParam,
				CallFunctionA,
				CallIntrinsicA,
				ReturnA,
				ElemBofA,
				ElemBofIterA,
				LengthOfA
			}

			public Value lhs;
			public Op op;
			public Value rhsA;
			public Value rhsB;
			public SourceLoc location;

			public Line(Value lhs, Op op, Value rhsA=null, Value rhsB=null) {
				this.lhs = lhs;
				this.op = op;
				this.rhsA = rhsA;
				this.rhsB = rhsB;
			}
			
			public override int GetHashCode() {
				return lhs.GetHashCode() ^ op.GetHashCode() ^ rhsA.GetHashCode() ^ rhsB.GetHashCode() ^ location.GetHashCode();
			}
			
			public override bool Equals(object obj) {
				if (!(obj is Line)) return false;
				Line b = (Line)obj;
				return op == b.op && lhs == b.lhs && rhsA == b.rhsA && rhsB == b.rhsB && location == b.location;
			}
			
			public override string ToString() {
				var text = op switch {
					Op.AssignA => $"{lhs} := {rhsA}",
					Op.AssignImplicit => $"_ := {rhsA}",
					Op.APlusB => $"{lhs} := {rhsA} + {rhsB}",
					Op.AMinusB => $"{lhs} := {rhsA} - {rhsB}",
					Op.ATimesB => $"{lhs} := {rhsA} * {rhsB}",
					Op.ADividedByB => $"{lhs} := {rhsA} / {rhsB}",
					Op.AModB => $"{lhs} := {rhsA} % {rhsB}",
					Op.APowB => $"{lhs} := {rhsA} ^ {rhsB}",
					Op.AEqualB => $"{lhs} := {rhsA} == {rhsB}",
					Op.ANotEqualB => $"{lhs} := {rhsA} != {rhsB}",
					Op.AGreaterThanB => $"{lhs} := {rhsA} > {rhsB}",
					Op.AGreatOrEqualB => $"{lhs} := {rhsA} >= {rhsB}",
					Op.ALessThanB => $"{lhs} := {rhsA} < {rhsB}",
					Op.ALessOrEqualB => $"{lhs} := {rhsA} <= {rhsB}",
					Op.AAndB => $"{lhs} := {rhsA} and {rhsB}",
					Op.AOrB => $"{lhs} := {rhsA} or {rhsB}",
					Op.AisaB => $"{lhs} := {rhsA} isa {rhsB}",
					Op.BindAssignA => $"{rhsA} := {rhsB}; {rhsA}.outerVars=",
					Op.CopyA => $"{lhs} := copy of {rhsA}",
					Op.NotA => $"{lhs} := not {rhsA}",
					Op.GotoA => $"goto {rhsA}",
					Op.GotoAifB => $"goto {rhsA} if {rhsB}",
					Op.GotoAifTrulyB => $"goto {rhsA} if truly {rhsB}",
					Op.GotoAifNotB => $"goto {rhsA} if not {rhsB}",
					Op.PushParam => $"push param {rhsA}",
					Op.CallFunctionA => $"{lhs} := call {rhsA} with {rhsB} args",
					Op.CallIntrinsicA => $"intrinsic {Intrinsic.GetByID(rhsA.IntValue())}",
					Op.ReturnA => $"{lhs} := {rhsA}; return",
					Op.ElemBofA => $"{lhs} = {rhsA}[{rhsB}]",
					Op.ElemBofIterA => $"{lhs} = {rhsA} iter {rhsB}",
					Op.LengthOfA => $"{lhs} = len({rhsA})",
					_ => throw new RuntimeException("unknown opcode: " + op)
				};
				if (location != null) text = text + "\t// " + location;
				
				return text;
			}

			/// <summary>
			/// Evaluate this line and return the value that would be stored
			/// into the lhs.
			/// </summary>
			public Value Evaluate(Context context) {
				switch (op) {
					case Op.AssignA:
					case Op.ReturnA:
					case Op.AssignImplicit: {
						switch (rhsA) {
							// Assignment is a bit of a special case.  It's EXTREMELY common
							// in TAC, so needs to be efficient, but we have to watch out for
							// the case of a RHS that is a list or map.  This means it was a
							// literal in the source, and may contain references that need to
							// be evaluated now.
							case ValList _:
							case ValMap _:
								return rhsA.FullEval(context);
							case null:
								return null;
							default:
								return rhsA.Val(context);
						}
					}
					// This opcode is used for assigning a literal.  We actually have
					// to copy the literal, in the case of a mutable object like a
					// list or map, to ensure that if the same code executes again,
					// we get a new, unique object.
					case Op.CopyA when rhsA is ValList list:
						return list.EvalCopy(context);
					case Op.CopyA when rhsA is ValMap map:
						return map.EvalCopy(context);
					case Op.CopyA when rhsA == null:
						return null;
					case Op.CopyA:
						return rhsA.Val(context);
				}

				var opA = rhsA?.Val(context);
				var opB = rhsB?.Val(context);

				switch (op) {
					case Op.AisaB:
						return opA == null ? ValNumber.Truth(opB == null) : ValNumber.Truth(opA.IsA(opB, context.vm));
					case Op.ElemBofA when opB is ValString valString: {
						// You can now look for a string in almost anything...
						// and we have a convenient (and relatively fast) method for it:
						return ValSeqElem.Resolve(opA, valString.value, context, out _);
					}
					// check for special cases of comparison to null (works with any type)
					case Op.AEqualB when (opA == null || opB == null):
						return ValNumber.Truth(opA == opB);
					case Op.ANotEqualB when (opA == null || opB == null):
						return ValNumber.Truth(opA != opB);
				}

				// check for implicit coersion of other types to string; this happens
				// when either side is a string and the operator is addition.
				if ((opA is ValString || opB is ValString) && op == Op.APlusB) {
					if (opA == null) return opB;
					if (opB == null) return opA;
					var sA = opA.ToString(context.vm);
					var sB = opB.ToString(context.vm);
					if (sA.Length + sB.Length > ValString.maxSize) throw new LimitExceededException("string too large");
					return new ValString(sA + sB);
				}

				switch (opA) {
					case ValNumber number: {
						var fA = number.value;
						switch (op) {
							case Op.GotoA:
								context.lineNum = (int)fA;
								return null;
							case Op.GotoAifB:
								if (opB != null && opB.BoolValue()) context.lineNum = (int)fA;
								return null;
							case Op.GotoAifTrulyB:
							{
								// Unlike GotoAifB, which branches if B has any nonzero
								// value (including 0.5 or 0.001), this branches only if
								// B is TRULY true, i.e., its integer value is nonzero.
								// (Used for short-circuit evaluation of "or".)
								int i = 0;
								if (opB != null) i = opB.IntValue();
								if (i != 0) context.lineNum = (int)fA;
								return null;
							}
							case Op.GotoAifNotB:
								if (opB == null || !opB.BoolValue()) context.lineNum = (int)fA;
								return null;
							case Op.CallIntrinsicA:
								// NOTE: intrinsics do not go through NextFunctionContext.  Instead
								// they execute directly in the current context.  (But usually, the
								// current context is a wrapper function that was invoked via
								// Op.CallFunction, so it got a parameter context at that time.)
								Result result = Intrinsic.Execute((int)fA, context, context.partialResult);
								if (result.done) {
									context.partialResult = null;
									return result.result;
								}
								// OK, this intrinsic function is not yet done with its work.
								// We need to stay on this same line and call it again with 
								// the partial result, until it reports that its job is complete.
								context.partialResult = result;
								context.lineNum--;
								return null;
							case Op.NotA:
								return new ValNumber(1.0 - AbsClamp01(fA));
						}
						
						if (opB is ValNumber || opB == null) {
							var fB = ((ValNumber) opB)?.value ?? 0;
							switch (op) {
								case Op.APlusB:
									return new ValNumber(fA + fB);
								case Op.AMinusB:
									return new ValNumber(fA - fB);
								case Op.ATimesB:
									return new ValNumber(fA * fB);
								case Op.ADividedByB:
									return new ValNumber(fA / fB);
								case Op.AModB:
									return new ValNumber(fA % fB);
								case Op.APowB:
									return new ValNumber(Math.Pow(fA, fB));
								case Op.AEqualB:
									return ValNumber.Truth(fA == fB);
								case Op.ANotEqualB:
									return ValNumber.Truth(fA != fB);
								case Op.AGreaterThanB:
									return ValNumber.Truth(fA > fB);
								case Op.AGreatOrEqualB:
									return ValNumber.Truth(fA >= fB);
								case Op.ALessThanB:
									return ValNumber.Truth(fA < fB);
								case Op.ALessOrEqualB:
									return ValNumber.Truth(fA <= fB);
								case Op.AAndB:
									if (!(opB is ValNumber)) fB = opB != null && opB.BoolValue() ? 1 : 0;
									return new ValNumber(Clamp01(fA * fB));
								case Op.AOrB:
									if (!(opB is ValNumber)) fB = opB != null && opB.BoolValue() ? 1 : 0;
									return new ValNumber(Clamp01(fA + fB - fA * fB));
								default:
									break;
							}
						}
						// Handle equality testing between a number (opA) and a non-number (opB).
						// These are always considered unequal.
						if (op == Op.AEqualB) return ValNumber.zero;
						if (op == Op.ANotEqualB) return ValNumber.one;
						break;
					}
					case ValString valString: {
						var sA = valString.value;
						switch (op) {
							case Op.ATimesB:
							case Op.ADividedByB: {
								double factor = 0;
								if (op == Op.ATimesB) {
									Check.Type(opB, typeof(ValNumber), "string replication");
									factor = ((ValNumber)opB).value;
								} else {
									Check.Type(opB, typeof(ValNumber), "string division");
									factor = 1.0 / ((ValNumber)opB).value;								
								}
								var repeats = (int)factor;
								if (repeats < 0) return ValString.empty;
								if (repeats * sA.Length > ValString.maxSize) throw new LimitExceededException("string too large");
								var result = new System.Text.StringBuilder();
								for (int i = 0; i < repeats; i++) result.Append(sA);
								var extraChars = (int)(sA.Length * (factor - repeats));
								if (extraChars > 0) result.Append(sA.Substring(0, extraChars));
								return new ValString(result.ToString());
							}
							case Op.ElemBofA:
							case Op.ElemBofIterA: {
								var idx = opB.IntValue();
								Check.Range(idx, -sA.Length, sA.Length - 1, "string index");
								if (idx < 0) idx += sA.Length;
								return new ValString(sA.Substring(idx, 1));
							}
						}

						if (opB == null || opB is ValString) {
							var sB = opB?.ToString(context.vm);
							switch (op) {
								case Op.AMinusB: {
									if (opB == null) return valString;
									if (sA.EndsWith(sB)) sA = sA.Substring(0, sA.Length - sB.Length);
									return new ValString(sA);
								}
								case Op.NotA:
									return ValNumber.Truth(string.IsNullOrEmpty(sA));
								case Op.AEqualB:
									return ValNumber.Truth(string.Equals(sA, sB));
								case Op.ANotEqualB:
									return ValNumber.Truth(!string.Equals(sA, sB));
								case Op.AGreaterThanB:
									return ValNumber.Truth(string.Compare(sA, sB, StringComparison.Ordinal) > 0);
								case Op.AGreatOrEqualB:
									return ValNumber.Truth(string.Compare(sA, sB, StringComparison.Ordinal) >= 0);
								case Op.ALessThanB:
									var foo = string.Compare(sA, sB, StringComparison.Ordinal);
									return ValNumber.Truth(foo < 0);
								case Op.ALessOrEqualB:
									return ValNumber.Truth(string.Compare(sA, sB, StringComparison.Ordinal) <= 0);
								case Op.LengthOfA:
									return new ValNumber(sA.Length);
								default:
									break;
							}
						} else {
							switch (op) {
								// RHS is neither null nor a string.
								// We no longer automatically coerce in all these cases; about
								// all we can do is equal or unequal testing.
								// (Note that addition was handled way above here.)
								case Op.AEqualB:
									return ValNumber.zero;
								case Op.ANotEqualB:
									return ValNumber.one;
							}
						}

						break;
					}
					
					case ValList valList: {
						var list = valList.values;
						switch (op) {
							case Op.ElemBofA:
							case Op.ElemBofIterA: {
								// list indexing
								var idx = opB.IntValue();
								Check.Range(idx, -list.Count, list.Count - 1, "list index");
								if (idx < 0) idx += list.Count;
								return list[idx];
							}
							case Op.LengthOfA:
								return new ValNumber(list.Count);
							case Op.AEqualB:
								return ValNumber.Truth(valList.Equality(opB));
							case Op.ANotEqualB:
								return ValNumber.Truth(1.0 - valList.Equality(opB));
							case Op.APlusB: {
								// list concatenation
								Check.Type(opB, typeof(ValList), "list concatenation");
								var list2 = ((ValList)opB).values;
								if (list.Count + list2.Count > ValList.maxSize) throw new LimitExceededException("list too large");
								var result = new List<Value>(list.Count + list2.Count);
								foreach (var v in list) result.Add(context.ValueInContext(v));
								foreach (var v in list2) result.Add(context.ValueInContext(v));
								return new ValList(result);
							}
							case Op.ATimesB:
							case Op.ADividedByB: {
								// list replication (or division)
								double factor = 0;
								if (op == Op.ATimesB) {
									Check.Type(opB, typeof(ValNumber), "list replication");
									factor = ((ValNumber)opB).value;
								} else {
									Check.Type(opB, typeof(ValNumber), "list division");
									factor = 1.0 / ((ValNumber)opB).value;								
								}
								if (factor <= 0) return new ValList();
								
								var finalCount = (int)(list.Count * factor);
								if (finalCount > ValList.maxSize) throw new LimitExceededException("list too large");
								
								var result = new List<Value>(finalCount);
								for (int i = 0; i < finalCount; i++) {
									result.Add(context.ValueInContext(list[i % list.Count]));
								}
								return new ValList(result);
							}
							case Op.NotA:
								return ValNumber.Truth(!valList.BoolValue());
						}

						break;
					}
					
					case ValMap _ when op == Op.ElemBofA: {
						// map lookup
						// (note, cases where opB is a string are handled above, along with
						// all the other types; so we'll only get here for non-string cases)
						ValSeqElem se = new ValSeqElem(opA, opB);
						return se.Val(context);
						// (This ensures we walk the "__isa" chain in the standard way.)
					}
					case ValMap map when op == Op.ElemBofIterA:
						// With a map, ElemBofIterA is different from ElemBofA.  This one
						// returns a mini-map containing a key/value pair.
						return map.GetKeyValuePair(opB.IntValue());
					case ValMap map when op == Op.LengthOfA:
						return new ValNumber(map.Count);
					case ValMap map when op == Op.AEqualB:
						return ValNumber.Truth(map.Equality(opB));
					case ValMap map when op == Op.ANotEqualB:
						return ValNumber.Truth(1.0 - map.Equality(opB));
					case ValMap valMap when op == Op.APlusB: {
						// map combination
						var map = valMap.map;
						Check.Type(opB, typeof(ValMap), "map combination");
						var map2 = ((ValMap)opB).map;
						var result = new ValMap();
						foreach (var kv in map) result.map[kv.Key] = context.ValueInContext(kv.Value);
						foreach (var kv in map2) result.map[kv.Key] = context.ValueInContext(kv.Value);
						return result;
					}
					case ValMap _ when op == Op.NotA:
						return ValNumber.Truth(!opA.BoolValue());
					case ValMap _:
						break;
					case ValFunction function when opB is ValFunction: {
						var fA = function.function;
						var fB = ((ValFunction)opB).function;
						switch (op) {
							case Op.AEqualB:
								return ValNumber.Truth(fA == fB);
							case Op.ANotEqualB:
								return ValNumber.Truth(fA != fB);
						}

						break;
					}
					default:
						// opA is something else... perhaps null
						switch (op) {
							case Op.BindAssignA: {
								context.variables ??= new ValMap();
								var valFunc = (ValFunction)opA;
								return valFunc.BindAndCopy(context.variables);
							}
							case Op.NotA:
								return opA != null && opA.BoolValue() ? ValNumber.zero : ValNumber.one;
						}

						break;
				}

				if (op != Op.AAndB && op != Op.AOrB) return null;
				{
					// We already handled the case where opA was a number above;
					// this code handles the case where opA is something else.
					double fA = opA != null && opA.BoolValue() ? 1 : 0;
					double fB;
					if (opB is ValNumber number) fB = number.value;
					else fB = opB != null && opB.BoolValue() ? 1 : 0;
					double result;
					if (op == Op.AAndB) {
						result = fA * fB;
					} else {
						result = 1.0 - (1.0 - AbsClamp01(fA)) * (1.0 - AbsClamp01(fB));
					}
					return new ValNumber(result);
				}
			}

			private static double Clamp01(double d) {
				if (d < 0) return 0;
				if (d > 1) return 1;
				return d;
			}
			private static double AbsClamp01(double d) {
				if (d < 0) d = -d;
				if (d > 1) return 1;
				return d;
			}

		}
		

}