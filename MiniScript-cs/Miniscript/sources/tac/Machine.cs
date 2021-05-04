using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Miniscript.sources.types;

namespace Miniscript.sources.tac {

		/// <summary>
		/// Machine implements a complete Minisript virtual machine.  It 
		/// keeps the context stack, keeps track of run time, and provides 
		/// methods to step, stop, or reset the program.		
		/// </summary>
		public class Machine {
			public WeakReference interpreter;		// interpreter hosting this machine
			public TextOutputMethod standardOutput;	// where print() results should go
			public bool storeImplicit = false;		// whether to store implicit values (e.g. for REPL)
			public bool yielding = false;			// set to true by yield intrinsic
			public ValMap functionType;
			public ValMap listType;
			public ValMap mapType;
			public ValMap numberType;
			public ValMap stringType;
			public ValMap versionMap;
			
			// contains global variables
			public Context globalContext => _globalContext;

			public bool done => (stack.Count <= 1 && stack.Peek().done);

			public double runTime => stopwatch?.Elapsed.TotalSeconds ?? 0;

			private Context _globalContext;
			private Stack<Context> stack;
			private Stopwatch stopwatch;

			public Machine(Context globalContext, TextOutputMethod standardOutput) {
				_globalContext = globalContext;
				_globalContext.vm = this;
				this.standardOutput = standardOutput ?? Console.WriteLine;
				stack = new Stack<Context>();
				stack.Push(_globalContext);
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
				while (context.done) {
					if (stack.Count == 1) return;	// all done (can't pop the global context)
					PopContext();
					context = stack.Peek();
				}

				var line = context.code[context.lineNum++];
				try {
					DoOneLine(line, context);
				} catch (MiniscriptException mse) {
					mse.location ??= line.location;
					if (mse.location != null) throw mse;
					foreach (Context c in stack) {
						if (c.lineNum >= c.code.Count) continue;
						mse.location = c.code[c.lineNum].location;
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
				var nextContext = stack.Peek().NextCallContext(func.function, argCount, self != null, null);
				if (self != null) nextContext.self = self;
				nextContext.resultStorage = resultStorage;
				stack.Push(nextContext);				
			}
			
			private void DoOneLine(Line line, Context context) {
				switch (line.op) {
    
//				Console.WriteLine("EXECUTING line " + (context.lineNum-1) + ": " + line);
					case Line.Op.PushParam: {
						var val = context.ValueInContext(line.rhsA);
						context.PushParamArgument(val);
						break;
					}
					case Line.Op.CallFunctionA: {
						// Resolve rhsA.  If it's a function, invoke it; otherwise,
						// just store it directly (but pop the call context).
						ValMap valueFoundIn;
						var funcVal = line.rhsA.Val(context, out valueFoundIn);	// resolves the whole dot chain, if any
						if (funcVal is ValFunction func) {
							Value self = null;
							// bind "super" to the parent of the map the function was found in
							var super = valueFoundIn?.Lookup(ValString.magicIsA);
							if (line.rhsA is ValSeqElem elem) {
								// bind "self" to the object used to invoke the call, except
								// when invoking via "super"
								var seq = elem.sequence;
								if (seq is ValVar @var && @var.identifier == "super") self = context.self;
								else self = context.ValueInContext(seq);
							}

							var argCount = line.rhsB.IntValue();
							var nextContext = context.NextCallContext(func.function, argCount, self != null, line.lhs);
							nextContext.outerVars = func.outerVars;
							if (valueFoundIn != null) nextContext.SetVar("super", super);
							if (self != null) nextContext.self = self;	// (set only if bound above)
							stack.Push(nextContext);
						} else {
							// The user is attempting to call something that's not a function.
							// We'll allow that, but any number of parameters is too many.  [#35]
							// (No need to pop them, as the exception will pop the whole call stack anyway.)
							var argCount = line.rhsB.IntValue();
							if (argCount > 0) throw new TooManyArgumentsException();
							context.StoreValue(line.lhs, funcVal);
						}

						break;
					}
					case Line.Op.ReturnA: {
						var val = line.Evaluate(context);
						context.StoreValue(line.lhs, val);
						PopContext();
						break;
					}
					case Line.Op.AssignImplicit: {
						var val = line.Evaluate(context);
						if (storeImplicit) {
							context.StoreValue(ValVar.implicitResult, val);
							context.implicitResultCounter++;
						}

						break;
					}
					default: {
						var val = line.Evaluate(context);
						context.StoreValue(line.lhs, val);
						break;
					}
				}
			}

			private void PopContext() {
				// Our top context is done; pop it off, and copy the return value in temp 0.
				if (stack.Count == 1) return;	// down to just the global stack (which we keep)
				var context = stack.Pop();
				var result = context.GetTemp(0, null);
				var storage = context.resultStorage;
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
				if (globalContext?.variables == null) return null;
				foreach (var kv in globalContext.variables.map) {
					if (kv.Value == val && kv.Key != val) return kv.Key.ToString(this);
				}

				Intrinsic.shortNames.TryGetValue(val, out var result);
				return result;
			}
			
			public List<SourceLoc> GetStack() {
				return stack.Select(context => context.GetSourceLoc()).ToList();
			}
		}


}