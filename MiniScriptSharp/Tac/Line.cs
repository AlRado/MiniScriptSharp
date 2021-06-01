using System;
using System.Collections.Generic;
using System.Linq;
using MiniScriptSharp.Errors;
using MiniScriptSharp.Intrinsics;
using MiniScriptSharp.Types;

namespace MiniScriptSharp.Tac {

		public class Line {
			public Value Lhs;
			public Op Op;
			public Value RhsA;
			public Value RhsB;
			
			public SourceLoc Location;

			public Line(Value lhs, Op op, Value rhsA=null, Value rhsB=null) {
				this.Lhs = lhs;
				this.Op = op;
				this.RhsA = rhsA;
				this.RhsB = rhsB;
			}
			
			public override int GetHashCode() {
				return Lhs.GetHashCode() ^ Op.GetHashCode() ^ RhsA.GetHashCode() ^ RhsB.GetHashCode() ^ Location.GetHashCode();
			}
			
			public override bool Equals(object obj) {
				if (!(obj is Line)) return false;
				
				var b = (Line)obj;
				return Op == b.Op && Lhs == b.Lhs && RhsA == b.RhsA && RhsB == b.RhsB && Location == b.Location;
			}
			
			public override string ToString() {
				var text = Op switch {
					Op.AssignA => $"{Lhs} := {RhsA}",
					Op.AssignImplicit => $"_ := {RhsA}",
					Op.APlusB => $"{Lhs} := {RhsA} + {RhsB}",
					Op.AMinusB => $"{Lhs} := {RhsA} - {RhsB}",
					Op.ATimesB => $"{Lhs} := {RhsA} * {RhsB}",
					Op.ADividedByB => $"{Lhs} := {RhsA} / {RhsB}",
					Op.AModB => $"{Lhs} := {RhsA} % {RhsB}",
					Op.APowB => $"{Lhs} := {RhsA} ^ {RhsB}",
					Op.AEqualB => $"{Lhs} := {RhsA} == {RhsB}",
					Op.ANotEqualB => $"{Lhs} := {RhsA} != {RhsB}",
					Op.AGreaterThanB => $"{Lhs} := {RhsA} > {RhsB}",
					Op.AGreatOrEqualB => $"{Lhs} := {RhsA} >= {RhsB}",
					Op.ALessThanB => $"{Lhs} := {RhsA} < {RhsB}",
					Op.ALessOrEqualB => $"{Lhs} := {RhsA} <= {RhsB}",
					Op.AAndB => $"{Lhs} := {RhsA} and {RhsB}",
					Op.AOrB => $"{Lhs} := {RhsA} or {RhsB}",
					Op.AisaB => $"{Lhs} := {RhsA} isa {RhsB}",
					Op.BindAssignA => $"{RhsA} := {RhsB}; {RhsA}.outerVars=",
					Op.CopyA => $"{Lhs} := copy of {RhsA}",
					Op.NotA => $"{Lhs} := not {RhsA}",
					Op.GotoA => $"goto {RhsA}",
					Op.GotoAifB => $"goto {RhsA} if {RhsB}",
					Op.GotoAifTrulyB => $"goto {RhsA} if truly {RhsB}",
					Op.GotoAifNotB => $"goto {RhsA} if not {RhsB}",
					Op.PushParam => $"push param {RhsA}",
					Op.CallFunctionA => $"{Lhs} := call {RhsA} with {RhsB} args",
					Op.CallIntrinsicA => $"intrinsic {Intrinsic.GetByID(RhsA.IntValue())}",
					Op.ReturnA => $"{Lhs} := {RhsA}; return",
					Op.ElemBofA => $"{Lhs} = {RhsA}[{RhsB}]",
					Op.ElemBofIterA => $"{Lhs} = {RhsA} iter {RhsB}",
					Op.LengthOfA => $"{Lhs} = len({RhsA})",
					_ => throw new RuntimeException("unknown opcode: " + Op)
				};
				if (Location != null) text = text + "\t// " + Location;
				
				return text;
			}

			/// <summary>
			/// Evaluate this line and return the value that would be stored
			/// into the lhs.
			/// </summary>
			public Value Evaluate(Context context) {
				switch (Op) {
					case Op.AssignA:
					case Op.ReturnA:
					case Op.AssignImplicit: {
						switch (RhsA) {
							// Assignment is a bit of a special case.  It's EXTREMELY common
							// in TAC, so needs to be efficient, but we have to watch out for
							// the case of a RHS that is a list or map.  This means it was a
							// literal in the source, and may contain references that need to
							// be evaluated now.
							case ValList _:
							case ValMap _:
								return RhsA.FullEval(context);
							case null:
								return null;
							default:
								return RhsA.Val(context);
						}
					}
					// This opcode is used for assigning a literal.  We actually have
					// to copy the literal, in the case of a mutable object like a
					// list or map, to ensure that if the same code executes again,
					// we get a new, unique object.
					case Op.CopyA when RhsA is ValList list:
						return list.EvalCopy(context);
					case Op.CopyA when RhsA is ValMap map:
						return map.EvalCopy(context);
					case Op.CopyA when RhsA == null:
						return null;
					case Op.CopyA:
						return RhsA.Val(context);
				}

				var opA = RhsA?.Val(context);
				var opB = RhsB?.Val(context);

				switch (Op) {
					case Op.AisaB:
						return opA == null ? ValNumber.Truth(opB == null) : ValNumber.Truth(opA.IsA(opB, context.Vm));
					case Op.ElemBofA when opB is ValString valString: {
						// You can now look for a string in almost anything...
						// and we have a convenient (and relatively fast) method for it:
						return ValSeqElem.Resolve(opA, valString.Value, context, out _);
					}
					// check for special cases of comparison to null (works with any type)
					case Op.AEqualB when (opA == null || opB == null):
						return ValNumber.Truth(opA == opB);
					case Op.ANotEqualB when (opA == null || opB == null):
						return ValNumber.Truth(opA != opB);
				}

				// check for implicit coersion of other types to string; this happens
				// when either side is a string and the operator is addition.
				if ((opA is ValString || opB is ValString) && Op == Op.APlusB) {
					if (opA == null) return opB;
					if (opB == null) return opA;
					var sA = opA.ToString(context.Vm);
					var sB = opB.ToString(context.Vm);
					if (sA.Length + sB.Length > ValString.MaxSize) throw new LimitExceededException("string too large");
					return new ValString(sA + sB);
				}

				switch (opA) {
					case ValNumber number: {
						var fA = number.Value;
						switch (Op) {
							case Op.GotoA:
								context.LineNum = (int)fA;
								return null;
							case Op.GotoAifB:
								if (opB != null && opB.BoolValue()) context.LineNum = (int)fA;
								return null;
							case Op.GotoAifTrulyB:
							{
								// Unlike GotoAifB, which branches if B has any nonzero
								// value (including 0.5 or 0.001), this branches only if
								// B is TRULY true, i.e., its integer value is nonzero.
								// (Used for short-circuit evaluation of "or".)
								int i = 0;
								if (opB != null) i = opB.IntValue();
								if (i != 0) context.LineNum = (int)fA;
								return null;
							}
							case Op.GotoAifNotB:
								if (opB == null || !opB.BoolValue()) context.LineNum = (int)fA;
								return null;
							case Op.CallIntrinsicA:
								// NOTE: intrinsics do not go through NextFunctionContext.  Instead
								// they execute directly in the current context.  (But usually, the
								// current context is a wrapper function that was invoked via
								// Op.CallFunction, so it got a parameter context at that time.)
								var result = Intrinsic.Execute((int)fA, context, context.PartialResult);
								if (result.Done) {
									context.PartialResult = null;
									return result.ResultValue;
								}
								// OK, this intrinsic function is not yet Done with its work.
								// We need to stay on this same line and call it again with 
								// the partial result, until it reports that its job is complete.
								context.PartialResult = result;
								context.LineNum--;
								return null;
							case Op.NotA:
								return new ValNumber(1.0 - AbsClamp01(fA));
						}
						
						if (opB is ValNumber || opB == null) {
							var fB = ((ValNumber) opB)?.Value ?? 0;
							switch (Op) {
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
						if (Op == Op.AEqualB) return ValNumber.Zero;
						if (Op == Op.ANotEqualB) return ValNumber.One;
						break;
					}
					case ValString valString: {
						var sA = valString.Value;
						switch (Op) {
							case Op.ATimesB:
							case Op.ADividedByB: {
								double factor = 0;
								if (Op == Op.ATimesB) {
									Check.Type(opB, typeof(ValNumber), "string replication");
									factor = ((ValNumber)opB).Value;
								} else {
									Check.Type(opB, typeof(ValNumber), "string division");
									factor = 1.0 / ((ValNumber)opB).Value;								
								}
								var repeats = (int)factor;
								if (repeats < 0) return ValString.Empty;
								if (repeats * sA.Length > ValString.MaxSize) throw new LimitExceededException("string too large");
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
							var sB = opB?.ToString(context.Vm);
							switch (Op) {
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
							switch (Op) {
								// RHS is neither null nor a string.
								// We no longer automatically coerce in all these cases; about
								// all we can do is equal or unequal testing.
								// (Note that addition was handled way above here.)
								case Op.AEqualB:
									return ValNumber.Zero;
								case Op.ANotEqualB:
									return ValNumber.One;
							}
						}

						break;
					}
					
					case ValList valList: {
						var list = valList.Values;
						switch (Op) {
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
								var list2 = ((ValList)opB).Values;
								if (list.Count + list2.Count > ValList.MaxSize) throw new LimitExceededException("list too large");
								var result = new List<Value>(list.Count + list2.Count);
								result.AddRange(list.Select(context.ValueInContext));
								result.AddRange(list2.Select(context.ValueInContext));
								return new ValList(result);
							}
							case Op.ATimesB:
							case Op.ADividedByB: {
								// list replication (or division)
								double factor = 0;
								if (Op == Op.ATimesB) {
									Check.Type(opB, typeof(ValNumber), "list replication");
									factor = ((ValNumber)opB).Value;
								} else {
									Check.Type(opB, typeof(ValNumber), "list division");
									factor = 1.0 / ((ValNumber)opB).Value;								
								}
								if (factor <= 0) return new ValList();
								
								var finalCount = (int)(list.Count * factor);
								if (finalCount > ValList.MaxSize) throw new LimitExceededException("list too large");
								
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
					
					case ValMap _ when Op == Op.ElemBofA: {
						// map lookup
						// (note, cases where opB is a string are handled above, along with
						// all the other types; so we'll only get here for non-string cases)
						var se = new ValSeqElem(opA, opB);
						return se.Val(context);
						// (This ensures we walk the "__isa" chain in the standard way.)
					}
					case ValMap map when Op == Op.ElemBofIterA:
						// With a map, ElemBofIterA is different from ElemBofA.  This one
						// returns a mini-map containing a key/value pair.
						return map.GetKeyValuePair(opB.IntValue());
					case ValMap map when Op == Op.LengthOfA:
						return new ValNumber(map.Count);
					case ValMap map when Op == Op.AEqualB:
						return ValNumber.Truth(map.Equality(opB));
					case ValMap map when Op == Op.ANotEqualB:
						return ValNumber.Truth(1.0 - map.Equality(opB));
					case ValMap valMap when Op == Op.APlusB: {
						// map combination
						var map = valMap.Map;
						Check.Type(opB, typeof(ValMap), "map combination");
						var map2 = ((ValMap)opB).Map;
						var result = new ValMap();
						foreach (var kv in map) result.Map[kv.Key] = context.ValueInContext(kv.Value);
						foreach (var kv in map2) result.Map[kv.Key] = context.ValueInContext(kv.Value);
						return result;
					}
					case ValMap _ when Op == Op.NotA:
						return ValNumber.Truth(!opA.BoolValue());
					case ValMap _:
						break;
					case ValFunction function when opB is ValFunction valFunction: {
						var fA = function.Function;
						var fB = valFunction.Function;
						switch (Op) {
							case Op.AEqualB:
								return ValNumber.Truth(fA == fB);
							case Op.ANotEqualB:
								return ValNumber.Truth(fA != fB);
						}

						break;
					}
					default:
						// opA is something else... perhaps null
						switch (Op) {
							case Op.BindAssignA: {
								context.Variables ??= new ValMap();
								var valFunc = (ValFunction)opA;
								return valFunc.BindAndCopy(context.Variables);
							}
							case Op.NotA:
								return opA != null && opA.BoolValue() ? ValNumber.Zero : ValNumber.One;
						}

						break;
				}

				if (Op != Op.AAndB && Op != Op.AOrB) return null;
				{
					// We already handled the case where opA was a number above;
					// this code handles the case where opA is something else.
					double fA = opA != null && opA.BoolValue() ? 1 : 0;
					double fB;
					if (opB is ValNumber number) fB = number.Value;
					else fB = opB != null && opB.BoolValue() ? 1 : 0;
					double result;
					if (Op == Op.AAndB) {
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