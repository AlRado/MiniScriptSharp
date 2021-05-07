using System;
using System.Collections.Generic;
using Miniscript.errors;
using Miniscript.interpreter;
using Miniscript.intrinsic;
using Miniscript.types;

namespace Miniscript.tac {

		/// <summary>
		/// Context keeps track of the runtime environment, including local 
		/// variables.  Context objects form a linked list via a "parent" reference,
		/// with a new context formed on each function call (this is known as the
		/// call stack).
		/// </summary>
		public class Context {
			public List<Line> code;			// TAC lines we're executing
			public int lineNum;				// next line to be executed
			public ValMap variables;		// local variables for this call frame
			public ValMap outerVars;        // variables of the context where this function was defined
			public Value self;				// value of self in this context
			public Stack<Value> args;		// pushed arguments for upcoming calls
			public Context parent;			// parent (calling) context
			public Value resultStorage;		// where to store the return value (in the calling context)
			public Machine vm;				// virtual machine
			public Result partialResult;	// work-in-progress of our current intrinsic
			public int implicitResultCounter;	// how many times we have stored an implicit result

			public bool done => lineNum >= code.Count;

			public Context root {
				get {
					var c = this;
					while (c.parent != null) c = c.parent;
					return c;
				}
			}

			public Interpreter interpreter => vm?.interpreter?.Target as Interpreter;

			private List<Value> temps;			// values of temporaries; temps[0] is always return value

			public Context(List<Line> code) {
				this.code = code;
			}

			public void ClearCodeAndTemps() {
		 		code.Clear();
				lineNum = 0;
				temps?.Clear();
			}

			/// <summary>
			/// Reset this context to the first line of code, clearing out any 
			/// temporary variables, and optionally clearing out all variables.
			/// </summary>
			/// <param name="clearVariables">if true, clear our local variables</param>
			public void Reset(bool clearVariables=true) {
				lineNum = 0;
				temps = null;
				if (clearVariables) variables = new ValMap();
			}

			public void JumpToEnd() {
				lineNum = code.Count;
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
					case "globals":
					case "locals":
						throw new RuntimeException($"can't assign to {identifier}");
					case "self":
						self = value;
						break;
				}

				variables ??= new ValMap();
				if (variables.assignOverride == null || !variables.assignOverride(new ValString(identifier), value)) {
					variables[identifier] = value;
				}
			}
			
			/// <summary>
			/// Get the value of a local variable ONLY -- does not check any other
			/// scopes, nor check for special built-in identifiers like "globals".
			/// Used mainly by host apps to easily look up an argument to an
			/// intrinsic function call by the parameter name.
			/// </summary>
			public Value GetLocal(string identifier, Value defaultValue=null) {
				if (variables != null && variables.TryGetValue(identifier, out var result)) {
					return result;
				}
				return defaultValue;
			}
			
			public int GetLocalInt(string identifier, int defaultValue = 0) {
				if (variables != null && variables.TryGetValue(identifier, out var result)) {
					return result?.IntValue() ?? 0;
				}
				return defaultValue;
			}

			public bool GetLocalBool(string identifier, bool defaultValue = false) {
				if (variables != null && variables.TryGetValue(identifier, out var result)) {
					return result != null && result.BoolValue();
				}
				return defaultValue;
			}

			public float GetLocalFloat(string identifier, float defaultValue = 0) {
				if (variables == null || !variables.TryGetValue(identifier, out var result)) return defaultValue;
				
				if (result == null) return 0;	// variable found, but its value was null!
				
				return result.FloatValue();
			}

			public string GetLocalString(string identifier, string defaultValue = null) {
				if (variables != null && variables.TryGetValue(identifier, out var result)) {
					return result?.ToString();
				}
				return defaultValue;
			}

			public SourceLoc GetSourceLoc() {
				if (lineNum < 0 || lineNum >= code.Count) return null;
				
				return code[lineNum].location;
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
					case "self":
						return self;
					case "locals":
						return variables ??= new ValMap();
					case "globals":
						return root.variables ?? (root.variables = new ValMap());
					// return module variables, if we have them; else globals
					case "outer" when outerVars != null:
						return outerVars;
					case "outer":
						return root.variables ?? (root.variables = new ValMap());
				}

				// check for a local variable
				if (variables != null && variables.TryGetValue(identifier, out var result)) {
					return result;
				}

				// check for a module variable
				if (outerVars != null && outerVars.TryGetValue(identifier, out result)) {
					return result;
				}

				// OK, we don't have a local or module variable with that name.
				// Check the global scope (if that's not us already).
				var globals = root;
				if (parent != null) {
					if (globals.variables != null && globals.variables.TryGetValue(identifier, out result)) {
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
						SetTemp(temp.tempNum, value);
						break;
					case ValVar @var:
						SetVar(@var.identifier, value);
						break;
					case ValSeqElem seqElem: {
						var seq = seqElem.sequence.Val(this);
						if (seq == null) throw new RuntimeException("can't set indexed element of null");
						if (!seq.CanSetElem()) {
							throw new RuntimeException("can't set an indexed element in this type");
						}
						var index = seqElem.index;
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
				var result = new Context(func.code) {
					code = func.code, resultStorage = resultStorage, parent = this, vm = vm
				};

				// Stuff arguments, stored in our 'args' stack,
				// into local variables corresponding to parameter names.
				// As a special case, skip over the first parameter if it is named 'self'
				// and we were invoked with dot syntax.
				var selfParam = (gotSelf && func.parameters.Count > 0 && func.parameters[0].name == "self" ? 1 : 0);
				for (int i = 0; i < argCount; i++) {
					// Careful -- when we pop them off, they're in reverse order.
					var argument = args.Pop();
					var paramNum = argCount - 1 - i + selfParam;
					if (paramNum >= func.parameters.Count) {
						throw new TooManyArgumentsException();
					}
					var param = func.parameters[paramNum].name;
					if (param == "self") result.self = argument;
					else result.SetVar(param, argument);
				}
				// And fill in the rest with default values
				for (int paramNum = argCount+selfParam; paramNum < func.parameters.Count; paramNum++) {
					result.SetVar(func.parameters[paramNum].name, func.parameters[paramNum].defaultValue);
				}

				return result;
			}

			/// <summary>
			/// This function prints the three-address code to the console, for debugging purposes.
			/// </summary>
			public void Dump() {
				Console.WriteLine("CODE:");
				TAC.Dump(code, lineNum);

				Console.WriteLine("\nVARS:");
				if (variables == null) {
					Console.WriteLine(" NONE");
				} else {
					foreach (Value v in variables.Keys) {
						var id = v.ToString(vm);
						Console.WriteLine($"{id}: {variables[id].ToString(vm)}");
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
				return $"Context[{lineNum}/{code.Count}]";
			}
		}
		

}