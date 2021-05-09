/*	MiniscriptInterpreter.cs

The only class in this file is Interpreter, which is your main interface 
to the Minisript system.  You give Interpreter some Minisript source 
code, and tell it where to send its output (via delegate functions called
TextOutputMethod).  Then you typically call RunUntilDone, which returns 
when either the script has stopped or the given timeout has passed.  

For details, see Chapters 1-3 of the Minisript Integration Guide.
*/

using System;
using System.Collections.Generic;
using Miniscript.errors;
using Miniscript.keywords;
using Miniscript.parser;
using Miniscript.tac;
using Miniscript.types;

namespace Miniscript.interpreter {

    /// <summary>
    /// TextOutputMethod: a delegate used to return text from the script
    /// (e.g. normal output, errors, etc.) to your C# code.
    /// </summary>
    /// <param name="output"></param>
    public delegate void TextOutputMethod(string output);

    /// <summary>
    /// Interpreter: an object that contains and runs one Minisript script.
    /// </summary>
    public class Interpreter {

        /// <summary>
        /// standardOutput: receives the output of the "print" intrinsic.
        /// </summary>
        public TextOutputMethod StandardOutput {
            get => _standardOutput;
            set {
                _standardOutput = value;
                if (_vm != null) _vm.StandardOutput = value;
            }
        }

        /// <summary>
        /// ImplicitOutput: receives the value of expressions entered when
        /// in REPL mode.  If you're not using the REPL() method, you can
        /// safely ignore this.
        /// </summary>
        public TextOutputMethod ImplicitOutput;

        /// <summary>
        /// ErrorOutput: receives error messages from the runtime.  (This happens
        /// via the ReportError method, which is virtual; so if you want to catch
        /// the actual exceptions rather than get the error messages as strings,
        /// you can subclass Interpreter and override that method.)
        /// </summary>
        public TextOutputMethod ErrorOutput;

        /// <summary>
        /// Done: returns true when we don't have a virtual machine, or we do have
        /// one and it is Done (has reached the end of its code).
        /// </summary>
        public bool Done => _vm == null || _vm.Done;

        /// <summary>
        /// vm: the virtual machine this interpreter is running.  Most applications will
        /// not need to use this, but it's provided for advanced users.
        /// </summary>
        private Machine _vm;

        private TextOutputMethod _standardOutput;
        private string _source;
        private Parser _parser;

        /// <summary>
        /// Constructor taking some Minisript source code, and the output delegates.
        /// </summary>
        public Interpreter(string source = null, TextOutputMethod standardOutput = null, TextOutputMethod errorOutput = null) {
            this._source = source;
            standardOutput ??= Console.WriteLine;
            errorOutput ??= Console.WriteLine;
            this.StandardOutput = standardOutput;
            this.ErrorOutput = errorOutput;
        }

        /// <summary>
        /// Constructor taking source code in the form of a list of strings.
        /// </summary>
        public Interpreter(List<string> source) : this(string.Join("\n", source.ToArray())) {
        }

        /// <summary>
        /// Constructor taking source code in the form of a string array.
        /// </summary>
        public Interpreter(string[] source) : this(string.Join("\n", source)) {
        }

        /// <summary>
        /// Stop the virtual machine, and jump to the end of the program code.
        /// Also reset the parser, in case it's stuck waiting for a block ender.
        /// </summary>
        public void Stop() {
            _vm?.Stop();
            _parser?.PartialReset();
        }

        /// <summary>
        /// Reset the interpreter with the given source code.
        /// </summary>
        /// <param name="source"></param>
        public void Reset(string source = "") {
            _source = source;
            _parser = null;
            _vm = null;
        }

        /// <summary>
        /// Compile our source code, if we haven't already Done so, so that we are
        /// either ready to run, or generate compiler errors (reported via ErrorOutput).
        /// </summary>
        public void Compile() {
            if (_vm != null) return; // already compiled

            _parser ??= new Parser();
            try {
                _parser.Parse(_source);
                _vm = _parser.CreateVM(StandardOutput);
                _vm.Interpreter = new WeakReference(this);
            }
            catch (MiniscriptException mse) {
                ReportError(mse);
            }
        }

        /// <summary>
        /// Reset the virtual machine to the beginning of the code.  Note that this
        /// does *not* reset global variables; it simply clears the stack and jumps
        /// to the beginning.  Useful in cases where you have a short script you
        /// want to run over and over, without recompiling every time.
        /// </summary>
        public void Restart() {
            _vm?.Reset();
        }

        /// <summary>
        /// Run the compiled code until we either reach the end, or we reach the
        /// specified time limit.  In the latter case, you can then call RunUntilDone
        /// again to continue execution right from where it left off.
        /// 
        /// Or, if returnEarly is true, we will also return if we reach an intrinsic
        /// method that returns a partial result, indicating that it needs to wait
        /// for something.  Again, call RunUntilDone again later to continue.
        /// 
        /// Note that this method first compiles the source code if it wasn't compiled
        /// already, and in that case, may generate compiler errors.  And of course
        /// it may generate runtime errors while running.  In either case, these are
        /// reported via ErrorOutput.
        /// </summary>
        /// <param name="timeLimit">maximum amout of time to run before returning, in seconds</param>
        /// <param name="returnEarly">if true, return as soon as we reach an intrinsic that returns a partial result</param>
        public void RunUntilDone(double timeLimit = 60, bool returnEarly = true) {
            try {
                if (_vm == null) {
                    Compile();
                    if (_vm == null) return; // (must have been some error)
                }

                var startTime = _vm.RunTime;
                _vm.Yielding = false;
                while (!_vm.Done && !_vm.Yielding) {
                    if (_vm.RunTime - startTime > timeLimit) return; // time's up for now!
                    _vm.Step(); // update the machine
                    if (returnEarly && _vm.GetTopContext().PartialResult != null) return; // waiting for something
                }
            }
            catch (MiniscriptException mse) {
                ReportError(mse);
                _vm.GetTopContext().JumpToEnd();
            }
        }

        /// <summary>
        /// Run one step of the virtual machine.  This method is not very useful
        /// except in special cases; usually you will use RunUntilDone (above) instead.
        /// </summary>
        public void Step() {
            try {
                Compile();
                _vm.Step();
            }
            catch (MiniscriptException mse) {
                ReportError(mse);
                _vm.GetTopContext().JumpToEnd();
            }
        }

        /// <summary>
        /// Read Eval Print Loop.  Run the given source until it either terminates,
        /// or hits the given time limit.  When it terminates, if we have new
        /// implicit output, print that to the ImplicitOutput stream.
        /// </summary>
        /// <param name="sourceLine">Source line.</param>
        /// <param name="timeLimit">Time limit.</param>
        public void REPL(string sourceLine, double timeLimit = 60) {
            _parser ??= new Parser();
            if (_vm == null) {
                _vm = _parser.CreateVM(StandardOutput);
                _vm.Interpreter = new WeakReference(this);
            }
            else if (_vm.Done && !_parser.NeedMoreInput()) {
                // Since the machine and parser are both Done, we don't really need the
                // previously-compiled code.  So let's clear it out, as a memory optimization.
                _vm.GetTopContext().ClearCodeAndTemps();
                _parser.PartialReset();
            }

            if (sourceLine == Consts.DUMP) {
                DumpTopContext();
                return;
            }

            var startTime = _vm.RunTime;
            var startImpResultCount = _vm.GlobalContext.ImplicitResultCounter;
            _vm.StoreImplicit = (ImplicitOutput != null);

            try {
                if (sourceLine != null) _parser.Parse(sourceLine, true);
                if (_parser.NeedMoreInput()) return;

                while (!_vm.Done && !_vm.Yielding) {
                    if (_vm.RunTime - startTime > timeLimit) return; // time's up for now!
                    _vm.Step();
                }

                if (ImplicitOutput == null || _vm.GlobalContext.ImplicitResultCounter <= startImpResultCount) return;
                var result = _vm.GlobalContext.GetVar(ValVar.ImplicitResult.Identifier);
                if (result != null) {
                    ImplicitOutput.Invoke(result.ToString(_vm));
                }
            }
            catch (MiniscriptException mse) {
                ReportError(mse);
                // Attempt to recover from an error by jumping to the end of the code.
                _vm.GetTopContext().JumpToEnd();
            }
        }

        /// <summary>
        /// Report whether the virtual machine is still running, that is,
        /// whether it has not yet reached the end of the program code.
        /// </summary>
        /// <returns></returns>
        public bool Running() {
            return _vm != null && !_vm.Done;
        }

        /// <summary>
        /// Return whether the parser needs more input, for example because we have
        /// run out of source code in the middle of an "if" block.  This is typically
        /// used with REPL for making an interactive console, so you can change the
        /// prompt when more input is expected.
        /// </summary>
        /// <returns></returns>
        public bool NeedMoreInput() {
            return _parser != null && _parser.NeedMoreInput();
        }

        /// <summary>
        /// Get a value from the global namespace of this interpreter.
        /// </summary>
        /// <param name="varName">name of global variable to get</param>
        /// <returns>Value of the named variable, or null if not found</returns>
        public Value GetGlobalValue(string varName) {
            var c = _vm?.GlobalContext;
            if (c == null) return null;

            try {
                return c.GetVar(varName);
            }
            catch (UndefinedIdentifierException) {
                return null;
            }
        }

        /// <summary>
        /// Set a value in the global namespace of this interpreter.
        /// </summary>
        /// <param name="varName">name of global variable to set</param>
        /// <param name="value">value to set</param>
        public void SetGlobalValue(string varName, Value value) {
            _vm?.GlobalContext.SetVar(varName, value);
        }

        /// <summary>
        /// Report a Minisript error to the user.  The default implementation 
        /// simply invokes ErrorOutput with the error description.  If you want
        /// to do something different, then make an Interpreter subclass, and
        /// override this method.
        /// </summary>
        /// <param name="mse">exception that was thrown</param>
        protected virtual void ReportError(MiniscriptException mse) {
            ErrorOutput.Invoke(mse.Description());
        }

        public void DumpTopContext() {
            _vm.DumpTopContext();
        }

    }

}