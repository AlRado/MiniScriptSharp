using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MiniScriptSharp.Errors;
using MiniScriptSharp.Intrinsics;
using MiniScriptSharp.Types;
using Consts = MiniScriptSharp.Constants.Consts;

namespace MiniScriptSharp.Tac {

		/// <summary>
		/// Machine implements a complete MiniScript virtual machine.  It 
		/// keeps the context stack, keeps track of run time, and provides 
		/// methods to step, stop, or reset the program.		
		/// </summary>
		public class Machine {
			public WeakReference Interpreter;		// interpreter hosting this machine
			public TextOutputMethod StandardOutput;	// where print() results should go
			public bool StoreImplicit;		// whether to store implicit values (e.g. for REPL)
			public bool Yielding;			// set to true by yield intrinsic
			public ValMap FunctionType;
			public ValMap ListType;
			public ValMap MapType;
			public ValMap NumberType;
			public ValMap StringType;
			public ValMap VersionMap;
			
			// contains global variables
			public Context GlobalContext { get; }

			public bool Done => stack.Count <= 1 && stack.Peek().Done;

			public double RunTime => stopwatch?.Elapsed.TotalSeconds ?? 0;

			private Stack<Context> stack;
			private Stopwatch stopwatch;

			public Machine(Context globalContext, TextOutputMethod standardOutput) {
				GlobalContext = globalContext;
				GlobalContext.Vm = this;
				StandardOutput = standardOutput ?? Console.WriteLine;
				stack = new Stack<Context>();
				stack.Push(GlobalContext);
			}

			public void Stop() {
				while (stack.Count > 1) stack.Pop();
				stack.Peek().JumpToEnd();
			}
			
			public void Reset() {
				while (stack.Count > 1) stack.Pop();
				stack.Peek().Reset(false);
			}

			public void Step() {
				if (stack.Count == 0) return;		// not even a global context
				if (stopwatch == null) {
					stopwatch = new System.Diagnostics.Stopwatch();
					stopwatch.Start();
				}
				var context = stack.Peek();
				while (context.Done) {
					if (stack.Count == 1) return;	// all Done (can't pop the global context)
					PopContext();
					context = stack.Peek();
				}

				var line = context.Code[context.LineNum++];
				try {
					DoOneLine(line, context);
				} catch (MiniscriptException mse) {
					mse.location ??= line.Location;
					if (mse.location != null) throw mse;
					
					foreach (var c in stack) {
						if (c.LineNum >= c.Code.Count) continue;
						mse.location = c.Code[c.LineNum].Location;
						if (mse.location != null) break;
					}
					throw mse;
				}
			}
			
			/// <summary>
			/// Directly invoke a ValFunction by manually pushing it onto the call stack.
			/// This might be useful, for example, in invoking handlers that have somehow
			/// been registered with the host app via intrinsics.
			/// </summary>
			/// <param name="func">Miniscript function to invoke</param>
			/// <param name="resultStorage">where to store result of the call, in the calling context</param>
			public void ManuallyPushCall(ValFunction func, Value resultStorage=null) {
				var argCount = 0;
				Value self = null;	// "self" is always null for a manually pushed call
				var nextContext = stack.Peek().NextCallContext(func.Function, argCount, self != null, null);
				if (self != null) nextContext.Self = self;
				nextContext.ResultStorage = resultStorage;
				stack.Push(nextContext);				
			}
			
			private void DoOneLine(Line line, Context context) {
				switch (line.Op) {
    
//				Console.WriteLine("EXECUTING line " + (context.lineNum-1) + ": " + line);
					case Op.PushParam: {
						var val = context.ValueInContext(line.RhsA);
						context.PushParamArgument(val);
						break;
					}
					case Op.CallFunctionA: {
						// Resolve rhsA.  If it's a function, invoke it; otherwise,
						// just store it directly (but pop the call context).
						ValMap valueFoundIn;
						var funcVal = line.RhsA.Val(context, out valueFoundIn);	// resolves the whole dot chain, if any
						if (funcVal is ValFunction func) {
							Value self = null;
							// bind "super" to the parent of the map the function was found in
							var super = valueFoundIn?.Lookup(ValString.MagicIsA);
							if (line.RhsA is ValSeqElem elem) {
								// bind "self" to the object used to invoke the call, except
								// when invoking via "super"
								var seq = elem.Sequence;
								if (seq is ValVar @var && @var.Identifier == Consts.SUPER) self = context.Self;
								else self = context.ValueInContext(seq);
							}

							var argCount = line.RhsB.IntValue();
							var nextContext = context.NextCallContext(func.Function, argCount, self != null, line.Lhs);
							nextContext.OuterVars = func.OuterVars;
							if (valueFoundIn != null) nextContext.SetVar(Consts.SUPER, super);
							if (self != null) nextContext.Self = self;	// (set only if bound above)
							stack.Push(nextContext);
						} else {
							// The user is attempting to call something that's not a function.
							// We'll allow that, but any number of parameters is too many.  [#35]
							// (No need to pop them, as the exception will pop the whole call stack anyway.)
							var argCount = line.RhsB.IntValue();
							if (argCount > 0) throw new TooManyArgumentsException();
							context.StoreValue(line.Lhs, funcVal);
						}

						break;
					}
					case Op.ReturnA: {
						var val = line.Evaluate(context);
						context.StoreValue(line.Lhs, val);
						PopContext();
						break;
					}
					case Op.AssignImplicit: {
						var val = line.Evaluate(context);
						if (StoreImplicit) {
							context.StoreValue(ValVar.ImplicitResult, val);
							context.ImplicitResultCounter++;
						}

						break;
					}
					default: {
						var val = line.Evaluate(context);
						context.StoreValue(line.Lhs, val);
						break;
					}
				}
			}

			private void PopContext() {
				// Our top context is Done; pop it off, and copy the return value in temp 0.
				if (stack.Count == 1) return;	// down to just the global stack (which we keep)
				
				var context = stack.Pop();
				var result = context.GetTemp(0, null);
				var storage = context.ResultStorage;
				context = stack.Peek();
				context.StoreValue(storage, result);
			}

			public Context GetTopContext() {
				return stack.Peek();
			}

			public void DumpTopContext() {
				stack.Peek().Dump();
			}
			
			public string FindShortName(Value val) {
				if (GlobalContext?.Variables == null) return null;
				foreach (var kv in GlobalContext.Variables.Map) {
					if (kv.Value == val && kv.Key != val) return kv.Key.ToString(this);
				}

				Intrinsic.ShortNames.TryGetValue(val, out var result);
				return result;
			}
			
			public List<SourceLoc> GetStack() {
				return stack.Select(context => context.GetSourceLoc()).ToList();
			}
		}


}