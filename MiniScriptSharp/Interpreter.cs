/*
 * Interpreter.cs
 *
 * The only class in this file is Interpreter, which is your main interface 
 * to the MiniScript system.  You give Interpreter some MiniScript source 
 * code, and tell it where to send its output (via delegate functions called
 * TextOutputMethod).  Then you typically call RunUntilDone, which returns 
 * when either the script has stopped or the given timeout has passed.  
 * 
 * For details, see Chapters 1-3 of the MiniScript Integration Guide.
 */

using System;
using System.Collections.Generic;
using MiniScriptSharp.Constants;
using MiniScriptSharp.Errors;
using MiniScriptSharp.Parse;
using MiniScriptSharp.Tac;
using MiniScriptSharp.Types;

namespace MiniScriptSharp {

    /// <summary>
    /// TextOutputMethod: a delegate used to return text from the script
    /// (e.g. normal output, errors, etc.) to your C# code.
    /// </summary>
    /// <param name="output"></param>
    public delegate void TextOutputMethod(string output);

    /// <summary>
    /// Interpreter: an object that contains and runs one MiniScript script.
    /// </summary>
    public class Interpreter {

        /// <summary>
        /// standardOutput: receives the output of the "print" intrinsic.
        /// </summary>
        public TextOutputMethod StandardOutput {
            get => standardOutput;
            set {
                standardOutput = value;
                if (vm != null) vm.StandardOutput = value;
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
        public bool Done => vm == null || vm.Done;

        /// <summary>
        /// vm: the virtual machine this interpreter is running.  Most applications will
        /// not need to use this, but it's provided for advanced users.
        /// </summary>
        private Machine vm;

        private TextOutputMethod standardOutput;
        private string source;
        private Parser parser;

        /// <summary>
        /// Constructor taking some MiniScript source code, and the output delegates.
        /// </summary>
        public Interpreter(string source = null, TextOutputMethod standardOutput = null, TextOutputMethod errorOutput = null) {
            this.source = source;
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
            vm?.Stop();
            parser?.PartialReset();
        }

        /// <summary>
        /// Reset the interpreter with the given source code.
        /// </summary>
        /// <param name="source"></param>
        public void Reset(string source = "") {
            this.source = source;
            parser = null;
            vm = null;
        }

        /// <summary>
        /// Compile our source code, if we haven't already Done so, so that we are
        /// either ready to run, or generate compiler errors (reported via ErrorOutput).
        /// </summary>
        public void Compile() {
            if (vm != null) return; // already compiled

            parser ??= new Parser();
            try {
                parser.Parse(source);
                vm = parser.CreateVM(StandardOutput);
                vm.Interpreter = new WeakReference(this);
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
            vm?.Reset();
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
                if (vm == null) {
                    Compile();
                    if (vm == null) return; // (must have been some error)
                }

                var startTime = vm.RunTime;
                vm.Yielding = false;
                while (!vm.Done && !vm.Yielding) {
                    if (vm.RunTime - startTime > timeLimit) return; // time's up for now!
                    vm.Step(); // update the machine
                    if (returnEarly && vm.GetTopContext().PartialResult != null) return; // waiting for something
                }
            }
            catch (MiniscriptException mse) {
                ReportError(mse);
                vm.GetTopContext().JumpToEnd();
            }
        }

        /// <summary>
        /// Run one step of the virtual machine.  This method is not very useful
        /// except in special cases; usually you will use RunUntilDone (above) instead.
        /// </summary>
        public void Step() {
            try {
                Compile();
                vm.Step();
            }
            catch (MiniscriptException mse) {
                ReportError(mse);
                vm.GetTopContext().JumpToEnd();
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
            parser ??= new Parser();
            if (vm == null) {
                vm = parser.CreateVM(StandardOutput);
                vm.Interpreter = new WeakReference(this);
            }
            else if (vm.Done && !parser.NeedMoreInput()) {
                // Since the machine and parser are both Done, we don't really need the
                // previously-compiled code.  So let's clear it out, as a memory optimization.
                vm.GetTopContext().ClearCodeAndTemps();
                parser.PartialReset();
            }

            if (sourceLine == Consts.DUMP) {
                DumpTopContext();
                return;
            }

            var startTime = vm.RunTime;
            var startImpResultCount = vm.GlobalContext.ImplicitResultCounter;
            vm.StoreImplicit = (ImplicitOutput != null);

            try {
                if (sourceLine != null) parser.Parse(sourceLine, true);
                if (parser.NeedMoreInput()) return;

                while (!vm.Done && !vm.Yielding) {
                    if (vm.RunTime - startTime > timeLimit) return; // time's up for now!
                    vm.Step();
                }

                if (ImplicitOutput == null || vm.GlobalContext.ImplicitResultCounter <= startImpResultCount) return;
                var result = vm.GlobalContext.GetVar(ValVar.ImplicitResult.Identifier);
                if (result != null) {
                    ImplicitOutput.Invoke(result.ToString(vm));
                }
            }
            catch (MiniscriptException mse) {
                ReportError(mse);
                // Attempt to recover from an error by jumping to the end of the code.
                vm.GetTopContext().JumpToEnd();
            }
        }

        /// <summary>
        /// Report whether the virtual machine is still running, that is,
        /// whether it has not yet reached the end of the program code.
        /// </summary>
        /// <returns></returns>
        public bool Running() {
            return vm != null && !vm.Done;
        }

        /// <summary>
        /// Return whether the parser needs more input, for example because we have
        /// run out of source code in the middle of an "if" block.  This is typically
        /// used with REPL for making an interactive console, so you can change the
        /// prompt when more input is expected.
        /// </summary>
        /// <returns></returns>
        public bool NeedMoreInput() {
            return parser != null && parser.NeedMoreInput();
        }

        /// <summary>
        /// Get a value from the global namespace of this interpreter.
        /// </summary>
        /// <param name="varName">name of global variable to get</param>
        /// <returns>Value of the named variable, or null if not found</returns>
        public Value GetGlobalValue(string varName) {
            var c = vm?.GlobalContext;
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
            vm?.GlobalContext.SetVar(varName, value);
        }

        /// <summary>
        /// Report a MiniScript error to the user.  The default implementation 
        /// simply invokes ErrorOutput with the error description.  If you want
        /// to do something different, then make an Interpreter subclass, and
        /// override this method.
        /// </summary>
        /// <param name="mse">exception that was thrown</param>
        protected virtual void ReportError(MiniscriptException mse) {
            ErrorOutput.Invoke(mse.Description());
        }

        public void DumpTopContext() {
            vm.DumpTopContext();
        }

    }

}