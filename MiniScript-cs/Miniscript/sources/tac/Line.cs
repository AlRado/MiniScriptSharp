using System;
using System.Collections.Generic;
using Miniscript.sources.intrinsic;
using Miniscript.sources.types;

namespace Miniscript.sources.tac {

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
//			public string comment;
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
				string text;
				switch (op) {
				case Op.AssignA:
					text = string.Format("{0} := {1}", lhs, rhsA);
					break;
				case Op.AssignImplicit:
					text = string.Format("_ := {0}", rhsA);
					break;
				case Op.APlusB:
					text = string.Format("{0} := {1} + {2}", lhs, rhsA, rhsB);
					break;
				case Op.AMinusB:
					text = string.Format("{0} := {1} - {2}", lhs, rhsA, rhsB);
					break;
				case Op.ATimesB:
					text = string.Format("{0} := {1} * {2}", lhs, rhsA, rhsB);
					break;
				case Op.ADividedByB:
					text = string.Format("{0} := {1} / {2}", lhs, rhsA, rhsB);
					break;
				case Op.AModB:
					text = string.Format("{0} := {1} % {2}", lhs, rhsA, rhsB);
					break;
				case Op.APowB:
					text = string.Format("{0} := {1} ^ {2}", lhs, rhsA, rhsB);
					break;
				case Op.AEqualB:
					text = string.Format("{0} := {1} == {2}", lhs, rhsA, rhsB);
					break;
				case Op.ANotEqualB:
					text = string.Format("{0} := {1} != {2}", lhs, rhsA, rhsB);
					break;
				case Op.AGreaterThanB:
					text = string.Format("{0} := {1} > {2}", lhs, rhsA, rhsB);
					break;
				case Op.AGreatOrEqualB:
					text = string.Format("{0} := {1} >= {2}", lhs, rhsA, rhsB);
					break;
				case Op.ALessThanB:
					text = string.Format("{0} := {1} < {2}", lhs, rhsA, rhsB);
					break;
				case Op.ALessOrEqualB:
					text = string.Format("{0} := {1} <= {2}", lhs, rhsA, rhsB);
					break;
				case Op.AAndB:
					text = string.Format("{0} := {1} and {2}", lhs, rhsA, rhsB);
					break;
				case Op.AOrB:
					text = string.Format("{0} := {1} or {2}", lhs, rhsA, rhsB);
					break;
				case Op.AisaB:
					text = string.Format("{0} := {1} isa {2}", lhs, rhsA, rhsB);
					break;
				case Op.BindAssignA:
					text = string.Format("{0} := {1}; {0}.outerVars=", rhsA, rhsB);
					break;
				case Op.CopyA:
					text = string.Format("{0} := copy of {1}", lhs, rhsA);
					break;
				case Op.NotA:
					text = string.Format("{0} := not {1}", lhs, rhsA);
					break;
				case Op.GotoA:
					text = string.Format("goto {0}", rhsA);
					break;
				case Op.GotoAifB:
					text = string.Format("goto {0} if {1}", rhsA, rhsB);
					break;
				case Op.GotoAifTrulyB:
					text = string.Format("goto {0} if truly {1}", rhsA, rhsB);
					break;
				case Op.GotoAifNotB:
					text = string.Format("goto {0} if not {1}", rhsA, rhsB);
					break;
				case Op.PushParam:
					text = string.Format("push param {0}", rhsA);
					break;
				case Op.CallFunctionA:
					text = string.Format("{0} := call {1} with {2} args", lhs, rhsA, rhsB);
					break;
				case Op.CallIntrinsicA:
					text = string.Format("intrinsic {0}", Intrinsic.GetByID(rhsA.IntValue()));
					break;
				case Op.ReturnA:
					text = string.Format("{0} := {1}; return", lhs, rhsA);
					break;
				case Op.ElemBofA:
					text = string.Format("{0} = {1}[{2}]", lhs, rhsA, rhsB);
					break;
				case Op.ElemBofIterA:
					text = string.Format("{0} = {1} iter {2}", lhs, rhsA, rhsB);
					break;
				case Op.LengthOfA:
					text = string.Format("{0} = len({1})", lhs, rhsA);
					break;
				default:
					throw new RuntimeException("unknown opcode: " + op);
					
				}
				//if (comment != null) text = text + "\t// " + comment;
				if (location != null) text = text + "\t// " + location;
				return text;
			}

			/// <summary>
			/// Evaluate this line and return the value that would be stored
			/// into the lhs.
			/// </summary>
			public Value Evaluate(Context context) {
				if (op == Op.AssignA || op == Op.ReturnA || op == Op.AssignImplicit) {
					// Assignment is a bit of a special case.  It's EXTREMELY common
					// in TAC, so needs to be efficient, but we have to watch out for
					// the case of a RHS that is a list or map.  This means it was a
					// literal in the source, and may contain references that need to
					// be evaluated now.
					if (rhsA is ValList || rhsA is ValMap) {
						return rhsA.FullEval(context);
					} else if (rhsA == null) {
						return null;
					} else {
						return rhsA.Val(context);
					}
				}
				if (op == Op.CopyA) {
					// This opcode is used for assigning a literal.  We actually have
					// to copy the literal, in the case of a mutable object like a
					// list or map, to ensure that if the same code executes again,
					// we get a new, unique object.
					if (rhsA is ValList) {
						return ((ValList)rhsA).EvalCopy(context);
					} else if (rhsA is ValMap) {
						return ((ValMap)rhsA).EvalCopy(context);
					} else if (rhsA == null) {
						return null;
					} else {
						return rhsA.Val(context);
					}
				}

				Value opA = rhsA!=null ? rhsA.Val(context) : null;
				Value opB = rhsB!=null ? rhsB.Val(context) : null;

				if (op == Op.AisaB) {
					if (opA == null) return ValNumber.Truth(opB == null);
					return ValNumber.Truth(opA.IsA(opB, context.vm));
				}

				if (op == Op.ElemBofA && opB is ValString) {
					// You can now look for a string in almost anything...
					// and we have a convenient (and relatively fast) method for it:
					ValMap ignored;
					return ValSeqElem.Resolve(opA, ((ValString)opB).value, context, out ignored);
				}

				// check for special cases of comparison to null (works with any type)
				if (op == Op.AEqualB && (opA == null || opB == null)) {
					return ValNumber.Truth(opA == opB);
				}
				if (op == Op.ANotEqualB && (opA == null || opB == null)) {
					return ValNumber.Truth(opA != opB);
				}
				
				// check for implicit coersion of other types to string; this happens
				// when either side is a string and the operator is addition.
				if ((opA is ValString || opB is ValString) && op == Op.APlusB) {
					if (opA == null) return opB;
					if (opB == null) return opA;
					string sA = opA.ToString(context.vm);
					string sB = opB.ToString(context.vm);
					if (sA.Length + sB.Length > ValString.maxSize) throw new LimitExceededException("string too large");
					return new ValString(sA + sB);
				}

				if (opA is ValNumber) {
					double fA = ((ValNumber)opA).value;
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
						double fB = opB != null ? ((ValNumber)opB).value : 0;
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

				} else if (opA is ValString) {
					string sA = ((ValString)opA).value;
					if (op == Op.ATimesB || op == Op.ADividedByB) {
						double factor = 0;
						if (op == Op.ATimesB) {
							Check.Type(opB, typeof(ValNumber), "string replication");
							factor = ((ValNumber)opB).value;
						} else {
							Check.Type(opB, typeof(ValNumber), "string division");
							factor = 1.0 / ((ValNumber)opB).value;								
						}
						int repeats = (int)factor;
						if (repeats < 0) return ValString.empty;
						if (repeats * sA.Length > ValString.maxSize) throw new LimitExceededException("string too large");
						var result = new System.Text.StringBuilder();
						for (int i = 0; i < repeats; i++) result.Append(sA);
						int extraChars = (int)(sA.Length * (factor - repeats));
						if (extraChars > 0) result.Append(sA.Substring(0, extraChars));
						return new ValString(result.ToString());						
					}
					if (op == Op.ElemBofA || op == Op.ElemBofIterA) {
						int idx = opB.IntValue();
						Check.Range(idx, -sA.Length, sA.Length - 1, "string index");
						if (idx < 0) idx += sA.Length;
						return new ValString(sA.Substring(idx, 1));
					}
					if (opB == null || opB is ValString) {
						string sB = (opB == null ? null : opB.ToString(context.vm));
						switch (op) {
							case Op.AMinusB: {
									if (opB == null) return opA;
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
								int foo = string.Compare(sA, sB, StringComparison.Ordinal);
								return ValNumber.Truth(foo < 0);
							case Op.ALessOrEqualB:
								return ValNumber.Truth(string.Compare(sA, sB, StringComparison.Ordinal) <= 0);
							case Op.LengthOfA:
								return new ValNumber(sA.Length);
							default:
								break;
						}
					} else {
						// RHS is neither null nor a string.
						// We no longer automatically coerce in all these cases; about
						// all we can do is equal or unequal testing.
						// (Note that addition was handled way above here.)
						if (op == Op.AEqualB) return ValNumber.zero;
						if (op == Op.ANotEqualB) return ValNumber.one;						
					}
				} else if (opA is ValList) {
					List<Value> list = ((ValList)opA).values;
					if (op == Op.ElemBofA || op == Op.ElemBofIterA) {
						// list indexing
						int idx = opB.IntValue();
						Check.Range(idx, -list.Count, list.Count - 1, "list index");
						if (idx < 0) idx += list.Count;
						return list[idx];
					} else if (op == Op.LengthOfA) {
						return new ValNumber(list.Count);
					} else if (op == Op.AEqualB) {
						return ValNumber.Truth(((ValList)opA).Equality(opB));
					} else if (op == Op.ANotEqualB) {
						return ValNumber.Truth(1.0 - ((ValList)opA).Equality(opB));
					} else if (op == Op.APlusB) {
						// list concatenation
						Check.Type(opB, typeof(ValList), "list concatenation");
						List<Value> list2 = ((ValList)opB).values;
						if (list.Count + list2.Count > ValList.maxSize) throw new LimitExceededException("list too large");
						List<Value> result = new List<Value>(list.Count + list2.Count);
						foreach (Value v in list) result.Add(context.ValueInContext(v));
						foreach (Value v in list2) result.Add(context.ValueInContext(v));
						return new ValList(result);
					} else if (op == Op.ATimesB || op == Op.ADividedByB) {
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
						int finalCount = (int)(list.Count * factor);
						if (finalCount > ValList.maxSize) throw new LimitExceededException("list too large");
						List<Value> result = new List<Value>(finalCount);
						for (int i = 0; i < finalCount; i++) {
							result.Add(context.ValueInContext(list[i % list.Count]));
						}
						return new ValList(result);
					} else if (op == Op.NotA) {
						return ValNumber.Truth(!opA.BoolValue());
					}
				} else if (opA is ValMap) {
					if (op == Op.ElemBofA) {
						// map lookup
						// (note, cases where opB is a string are handled above, along with
						// all the other types; so we'll only get here for non-string cases)
						ValSeqElem se = new ValSeqElem(opA, opB);
						return se.Val(context);
						// (This ensures we walk the "__isa" chain in the standard way.)
					} else if (op == Op.ElemBofIterA) {
						// With a map, ElemBofIterA is different from ElemBofA.  This one
						// returns a mini-map containing a key/value pair.
						return ((ValMap)opA).GetKeyValuePair(opB.IntValue());
					} else if (op == Op.LengthOfA) {
						return new ValNumber(((ValMap)opA).Count);
					} else if (op == Op.AEqualB) {
						return ValNumber.Truth(((ValMap)opA).Equality(opB));
					} else if (op == Op.ANotEqualB) {
						return ValNumber.Truth(1.0 - ((ValMap)opA).Equality(opB));
					} else if (op == Op.APlusB) {
						// map combination
						Dictionary<Value, Value> map = ((ValMap)opA).map;
						Check.Type(opB, typeof(ValMap), "map combination");
						Dictionary<Value, Value> map2 = ((ValMap)opB).map;
						ValMap result = new ValMap();
						foreach (KeyValuePair<Value, Value> kv in map) result.map[kv.Key] = context.ValueInContext(kv.Value);
						foreach (KeyValuePair<Value, Value> kv in map2) result.map[kv.Key] = context.ValueInContext(kv.Value);
						return result;
					} else if (op == Op.NotA) {
						return ValNumber.Truth(!opA.BoolValue());
					}
				} else if (opA is ValFunction && opB is ValFunction) {
					Function fA = ((ValFunction)opA).function;
					Function fB = ((ValFunction)opB).function;
					switch (op) {
					case Op.AEqualB:
						return ValNumber.Truth(fA == fB);
					case Op.ANotEqualB:
						return ValNumber.Truth(fA != fB);
					}
				} else {
					// opA is something else... perhaps null
					switch (op) {
					case Op.BindAssignA:
						{
							if (context.variables == null) context.variables = new ValMap();
							ValFunction valFunc = (ValFunction)opA;
                            return valFunc.BindAndCopy(context.variables);
						}
					case Op.NotA:
						return opA != null && opA.BoolValue() ? ValNumber.zero : ValNumber.one;
					}
				}
				

				if (op == Op.AAndB || op == Op.AOrB) {
					// We already handled the case where opA was a number above;
					// this code handles the case where opA is something else.
					double fA = opA != null && opA.BoolValue() ? 1 : 0;
					double fB;
					if (opB is ValNumber) fB = ((ValNumber)opB).value;
					else fB = opB != null && opB.BoolValue() ? 1 : 0;
					double result;
					if (op == Op.AAndB) {
						result = fA * fB;
					} else {
						result = 1.0 - (1.0 - AbsClamp01(fA)) * (1.0 - AbsClamp01(fB));
					}
					return new ValNumber(result);
				}
				return null;
			}

			static double Clamp01(double d) {
				if (d < 0) return 0;
				if (d > 1) return 1;
				return d;
			}
			static double AbsClamp01(double d) {
				if (d < 0) d = -d;
				if (d > 1) return 1;
				return d;
			}

		}
		

}