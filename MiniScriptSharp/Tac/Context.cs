using System;
using System.Collections.Generic;
using MiniScriptSharp.Errors;
using MiniScriptSharp.Intrinsics;
using MiniScriptSharp.Types;
using static MiniScriptSharp.Constants.Consts;

namespace MiniScriptSharp.Tac {

		/// <summary>
		/// Context keeps track of the runtime environment, including local 
		/// variables.  Context objects form a linked list via a "parent" reference,
		/// with a new context formed on each function call (this is known as the
		/// call stack).
		/// </summary>
		public class Context {
			public List<Line> Code;			// TAC lines we're executing
			public int LineNum;				// next line to be executed
			public ValMap Variables;		// local variables for this call frame
			public ValMap OuterVars;        // variables of the context where this function was defined
			public Value Self;				// value of self in this context
			public Value ResultStorage;		// where to store the return value (in the calling context)
			public Machine Vm;				// virtual machine
			public Result PartialResult;	// work-in-progress of our current intrinsic
			public int ImplicitResultCounter;	// how many times we have stored an implicit result
			private Stack<Value> args;		// pushed arguments for upcoming calls
			private Context parent;			// parent (calling) context

			public bool Done => LineNum >= Code.Count;

			public Context Root {
				get {
					var c = this;
					while (c.parent != null) c = c.parent;
					return c;
				}
			}

			public Interpreter Interpreter => Vm?.Interpreter?.Target as Interpreter;

			private List<Value> temps;			// values of temporaries; temps[0] is always return value

			public Context(List<Line> code) {
				this.Code = code;
			}

			public void ClearCodeAndTemps() {
		 		Code.Clear();
				LineNum = 0;
				temps?.Clear();
			}

			/// <summary>
			/// Reset this context to the first line of code, clearing out any 
			/// temporary variables, and optionally clearing out all variables.
			/// </summary>
			/// <param name="clearVariables">if true, clear our local variables</param>
			public void Reset(bool clearVariables=true) {
				LineNum = 0;
				temps = null;
				if (clearVariables) Variables = new ValMap();
			}

			public void JumpToEnd() {
				LineNum = Code.Count;
			}

			public void SetTemp(int tempNum, Value value) {
				// OFI: let each context record how many temps it will need, so we
				// can pre-allocate this list with that many and avoid having to
				// grow it later.  Also OFI: do lifetime analysis on these temps
				// and reuse ones we don't need anymore.
				temps ??= new List<Value>();
				while (temps.Count <= tempNum) temps.Add(null);
				temps[tempNum] = value;
			}

			public Value GetTemp(int tempNum) {
				return temps?[tempNum];
			}

			public Value GetTemp(int tempNum, Value defaultValue) {
				if (temps != null && tempNum < temps.Count) return temps[tempNum];
				return defaultValue;
			}

			public void SetVar(string identifier, Value value) {
				switch (identifier) {
					case GLOBALS:
					case LOCALS:
						throw new RuntimeException($"can't assign to {identifier}");
					case SELF:
						Self = value;
						break;
				}

				Variables ??= new ValMap();
				if (Variables.AssignOverride == null || !Variables.AssignOverride(new ValString(identifier), value)) {
					Variables[identifier] = value;
				}
			}
			
			/// <summary>
			/// Get the value of a local variable ONLY -- does not check any other
			/// scopes, nor check for special built-in identifiers like "globals".
			/// Used mainly by host apps to easily look up an argument to an
			/// intrinsic function call by the parameter name.
			/// </summary>
			public Value GetLocal(string identifier, Value defaultValue = null) {
				if (Variables != null && Variables.TryGetValue(identifier, out var result)) {
					return result;
				}
				return defaultValue;
			}
			
			public int GetLocalInt(string identifier, int defaultValue = 0) {
				if (Variables != null && Variables.TryGetValue(identifier, out var result)) {
					return result?.IntValue() ?? defaultValue;
				}
				return defaultValue;
			}
			
			public double GetLocalDouble(string identifier, double defaultValue = 0) {
				if (Variables != null && Variables.TryGetValue(identifier, out var result)) {
					return result?.DoubleValue() ?? defaultValue;
				}
				return defaultValue;
			}

			public bool GetLocalBool(string identifier, bool defaultValue = false) {
				if (Variables != null && Variables.TryGetValue(identifier, out var result)) {
					return result?.BoolValue() ?? defaultValue;
				}
				return defaultValue;
			}

			public float GetLocalFloat(string identifier, float defaultValue = 0) {
				if (Variables != null && Variables.TryGetValue(identifier, out var result)) {
					return result?.FloatValue() ?? defaultValue;
				}
				return defaultValue;
			}

			public string GetLocalString(string identifier, string defaultValue = null) {
				if (Variables != null && Variables.TryGetValue(identifier, out var result)) {
					return result?.ToString() ?? defaultValue;
				}
				return defaultValue;
			}

			public SourceLoc GetSourceLoc() {
				if (LineNum < 0 || LineNum >= Code.Count) return null;
				
				return Code[LineNum].Location;
			}
			
			/// <summary>
			/// Get the value of a variable available in this context (including
			/// locals, globals, and intrinsics).  Raise an exception if no such
			/// identifier can be found.
			/// </summary>
			/// <param name="identifier">name of identifier to look up</param>
			/// <returns>value of that identifier</returns>
			public Value GetVar(string identifier) {
				switch (identifier) {
					// check for special built-in identifiers 'locals', 'globals', etc.
					case SELF:
						return Self;
					case LOCALS:
						return Variables ??= new ValMap();
					case GLOBALS:
						return Root.Variables ?? (Root.Variables = new ValMap());
					// return module variables, if we have them; else globals
					case OUTER when OuterVars != null:
						return OuterVars;
					case OUTER:
						return Root.Variables ?? (Root.Variables = new ValMap());
				}

				// check for a local variable
				if (Variables != null && Variables.TryGetValue(identifier, out var result)) {
					return result;
				}

				// check for a module variable
				if (OuterVars != null && OuterVars.TryGetValue(identifier, out result)) {
					return result;
				}

				// OK, we don't have a local or module variable with that name.
				// Check the global scope (if that's not us already).
				var globals = Root;
				if (parent != null) {
					if (globals.Variables != null && globals.Variables.TryGetValue(identifier, out result)) {
						return result;
					}
				}

				// Finally, check intrinsics.
				var intrinsic = Intrinsic.GetByName(identifier);
				if (intrinsic != null) return intrinsic.GetFunc();

				// No luck there either?  Undefined identifier.
				throw new UndefinedIdentifierException(identifier);
			}

			public void StoreValue(Value lhs, Value value) {
				switch (lhs) {
					case ValTemp temp:
						SetTemp(temp.TempNum, value);
						break;
					case ValVar @var:
						SetVar(@var.Identifier, value);
						break;
					case ValSeqElem seqElem: {
						var seq = seqElem.Sequence.Val(this);
						if (seq == null) throw new RuntimeException("can't set indexed element of null");
						if (!seq.CanSetElem()) {
							throw new RuntimeException("can't set an indexed element in this type");
						}
						var index = seqElem.Index;
						if (index is ValVar || index is ValSeqElem || 
						    index is ValTemp) index = index.Val(this);
						seq.SetElem(index, value);
						break;
					}
					default: {
						if (lhs != null) throw new RuntimeException("not an lvalue");
						break;
					}
				}
			}
			
			public Value ValueInContext(Value value) {
				return value?.Val(this);
			}

			/// <summary>
			/// Store a parameter argument in preparation for an upcoming call
			/// (which should be executed in the context returned by NextCallContext).
			/// </summary>
			/// <param name="arg">Argument.</param>
			public void PushParamArgument(Value arg) {
				args ??= new Stack<Value>();
				if (args.Count > 255) throw new RuntimeException("Argument limit exceeded");
				args.Push(arg);				
			}

			/// <summary>
			/// Get a context for the next call, which includes any parameter arguments
			/// that have been set.
			/// </summary>
			/// <returns>The call context.</returns>
			/// <param name="func">Function to call.</param>
			/// <param name="argCount">How many arguments to pop off the stack.</param>
			/// <param name="gotSelf">Whether this method was called with dot syntax.</param> 
			/// <param name="resultStorage">Value to stuff the result into when Done.</param>
			public Context NextCallContext(Function func, int argCount, bool gotSelf, Value resultStorage) {
				var result = new Context(func.Code) {
					Code = func.Code, ResultStorage = resultStorage, parent = this, Vm = Vm
				};

				// Stuff arguments, stored in our 'args' stack,
				// into local variables corresponding to parameter names.
				// As a special case, skip over the first parameter if it is named 'self'
				// and we were invoked with dot syntax.
				var selfParam = (gotSelf && func.Parameters.Count > 0 && func.Parameters[0].Name == SELF ? 1 : 0);
				for (int i = 0; i < argCount; i++) {
					// Careful -- when we pop them off, they're in reverse order.
					var argument = args.Pop();
					var paramNum = argCount - 1 - i + selfParam;
					if (paramNum >= func.Parameters.Count) {
						throw new TooManyArgumentsException();
					}
					var param = func.Parameters[paramNum].Name;
					if (param == SELF) result.Self = argument;
					else result.SetVar(param, argument);
				}
				// And fill in the rest with default values
				for (int paramNum = argCount+selfParam; paramNum < func.Parameters.Count; paramNum++) {
					result.SetVar(func.Parameters[paramNum].Name, func.Parameters[paramNum].DefaultValue);
				}

				return result;
			}

			/// <summary>
			/// This function prints the three-address code to the console, for debugging purposes.
			/// </summary>
			public void Dump() {
				Console.WriteLine("CODE:");
				TAC.Dump(Code, LineNum);

				Console.WriteLine("\nVARS:");
				if (Variables == null) {
					Console.WriteLine(" NONE");
				} else {
					foreach (Value v in Variables.Keys) {
						var id = v.ToString(Vm);
						Console.WriteLine($"{id}: {Variables[id].ToString(Vm)}");
					}
				}

				Console.WriteLine("\nTEMPS:");
				if (temps == null) {
					Console.WriteLine(" NONE");
				} else {
					for (int i = 0; i < temps.Count; i++) {
						Console.WriteLine($"_{i}: {temps[i]}");
					}
				}
			}

			public override string ToString() {
				return $"Context[{LineNum}/{Code.Count}]";
			}
		}
}