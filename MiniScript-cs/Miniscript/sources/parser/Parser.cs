/*	MiniscriptParser.cs

This file is responsible for parsing Minisript source code, and converting
it into an internal format (a three-address byte code) that is considerably
faster to execute.

This is normally wrapped by the Interpreter class, so you probably don't
need to deal with Parser directly.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Miniscript.sources.lexer;
using Miniscript.sources.parser;
using Miniscript.sources.tac;
using Miniscript.sources.types;

namespace Miniscript {
	public class Parser {

		public string errorContext;	// name of file, etc., used for error reporting

		// Partial input, in the case where line continuation has been used.
		private string partialInput;

		// List of open code blocks we're working on (while compiling a function,
		// we push a new one onto this stack, compile to that, and then pop it
		// off when we reach the end of the function).
		private Stack<ParseState> outputStack;

		// Handy reference to the top of outputStack.
		private ParseState output;

		// A new parse state that needs to be pushed onto the stack, as soon as we
		// finish with the current line we're working on:
		private ParseState pendingState = null;

		public Parser() {
			Reset();
		}

		/// <summary>
		/// Completely clear out and reset our parse state, throwing out
		/// any code and intermediate results.
		/// </summary>
		public void Reset() {
			output = new ParseState();
			if (outputStack == null) outputStack = new Stack<ParseState>();
			else outputStack.Clear();
			outputStack.Push(output);
		}

		/// <summary>
		/// Partially reset, abandoning backpatches, but keeping already-
		/// compiled code.  This would be used in a REPL, when the user
		/// may want to reset and continue after a botched loop or function.
		/// </summary>
		public void PartialReset() {
			if (outputStack == null) outputStack = new Stack<ParseState>();
			while (outputStack.Count > 1) outputStack.Pop();
			output = outputStack.Peek();
			output.backpatches.Clear();
			output.jumpPoints.Clear();
			output.nextTempNum = 0;
		}

		public bool NeedMoreInput() {
			if (!string.IsNullOrEmpty(partialInput)) return true;
			if (outputStack.Count > 1) return true;
			if (output.backpatches.Count > 0) return true;
			return false;
		}

		public void Parse(string sourceCode, bool replMode=false) {
			if (replMode) {
				// Check for an incomplete final line by finding the last (non-comment) token.
				bool isPartial;
				Token lastTok = Lexer.LastToken(sourceCode);
				// Almost any token at the end will signify line continuation, except:
				switch (lastTok.tokenType) {
				case TokenType.EOL:
				case TokenType.Identifier:
				case TokenType.Keyword:
				case TokenType.Number:
				case TokenType.RCurly:
				case TokenType.RParen:
				case TokenType.RSquare:
				case TokenType.String:
				case TokenType.Unknown:
					isPartial = false;
					break;
				default:
					isPartial = true;
					break;
				}				
				if (isPartial) {
					partialInput += Lexer.TrimComment(sourceCode);
					return;
				}
			}
			Lexer tokens = new Lexer(partialInput + sourceCode);
			partialInput = null;
			ParseMultipleLines(tokens);

			if (!replMode && NeedMoreInput()) {
				// Whoops, we need more input but we don't have any.  This is an error.
				tokens.lineNum++;	// (so we report PAST the last line, making it clear this is an EOF problem)
				if (outputStack.Count > 1) {
					throw new CompilerException(errorContext, tokens.lineNum,
						"'function' without matching 'end function'");
				} else if (output.backpatches.Count > 0) {
					BackPatch bp = output.backpatches[output.backpatches.Count - 1];
					string msg;
					switch (bp.waitingFor) {
					case "end for":
						msg = "'for' without matching 'end for'";
						break;
					case "end if":
						msg = "'if' without matching 'end if'";
						break;
					case "end while":
						msg = "'while' without matching 'end while'";
						break;
					default:
						msg = "unmatched block opener";
						break;
					}
					throw new CompilerException(errorContext, tokens.lineNum, msg);
				}
			}
		}

		/// <summary>
		/// Create a virtual machine loaded with the code we have parsed.
		/// </summary>
		/// <param name="standardOutput"></param>
		/// <returns></returns>
		public Machine CreateVM(TextOutputMethod standardOutput) {
			Context root = new Context(output.code);
			return new Machine(root, standardOutput);
		}
		
		/// <summary>
		/// Create a Function with the code we have parsed, for use as
		/// an import.  That means, it runs all that code, then at the
		/// end it returns `locals` so that the caller can get its symbols.
		/// </summary>
		/// <returns></returns>
		public Function CreateImport() {
			// Add one additional line to return `locals` as the function return value.
			ValVar locals = new ValVar("locals");
			output.Add(new Line(TAC.LTemp(0), Line.Op.ReturnA, locals));
			// Then wrap the whole thing in a Function.
			var result = new Function(output.code);
			return result;
		}

		public void REPL(string line) {
			Parse(line);
		
			Machine vm = CreateVM(null);
			while (!vm.done) vm.Step();
		}

		private void AllowLineBreak(Lexer tokens) {
			while (tokens.Peek().tokenType == TokenType.EOL && !tokens.AtEnd) tokens.Dequeue();
		}

		private delegate Value ExpressionParsingMethod(Lexer tokens, bool asLval=false, bool statementStart=false);

		/// <summary>
		/// Parse multiple statements until we run out of tokens, or reach 'end function'.
		/// </summary>
		/// <param name="tokens">Tokens.</param>
		private void ParseMultipleLines(Lexer tokens) {
			while (!tokens.AtEnd) {
				// Skip any blank lines
				if (tokens.Peek().tokenType == TokenType.EOL) {
					tokens.Dequeue();
					continue;
				}

				// Prepare a source code location for error reporting
				SourceLoc location = new SourceLoc(errorContext, tokens.lineNum);

				// Pop our context if we reach 'end function'.
				if (tokens.Peek().tokenType == TokenType.Keyword && tokens.Peek().text == "end function") {
					tokens.Dequeue();
					if (outputStack.Count > 1) {
						outputStack.Pop();
						output = outputStack.Peek();
					} else {
						CompilerException e = new CompilerException("'end function' without matching block starter");
						e.location = location;
						throw e;
					}
					continue;
				}

				// Parse one line (statement).
				int outputStart = output.code.Count;
				try {
					ParseStatement(tokens);
				} catch (MiniscriptException mse) {
					if (mse.location == null) mse.location = location;
					throw mse;
				}
				// Fill in the location info for all the TAC lines we just generated.
				for (int i = outputStart; i < output.code.Count; i++) {
					output.code[i].location = location;
				}
			}
		}

		private void ParseStatement(Lexer tokens, bool allowExtra=false) {
			if (tokens.Peek().tokenType == TokenType.Keyword && tokens.Peek().text != "not"
				&& tokens.Peek().text != "true" && tokens.Peek().text != "false") {
				// Handle statements that begin with a keyword.
				string keyword = tokens.Dequeue().text;
				switch (keyword) {
				case "return":
					{
						Value returnValue = null;
						if (tokens.Peek().tokenType != TokenType.EOL) {
							returnValue = ParseExpr(tokens);
						}
						output.Add(new Line(TAC.LTemp(0), Line.Op.ReturnA, returnValue));
					}
					break;
				case "if":
					{
						Value condition = ParseExpr(tokens);
						RequireToken(tokens, TokenType.Keyword, "then");
						// OK, now we need to emit a conditional branch, but keep track of this
						// on a stack so that when we get the corresponding "else" or  "end if", 
						// we can come back and patch that jump to the right place.
						output.Add(new Line(null, Line.Op.GotoAifNotB, null, condition));

						// ...but if blocks also need a special marker in the backpack stack
						// so we know where to stop when patching up (possibly multiple) 'end if' jumps.
						// We'll push a special dummy backpatch here that we look for in PatchIfBlock.
						output.AddBackpatch("if:MARK");
						output.AddBackpatch("else");
						
						// Allow for the special one-statement if: if the next token after "then"
						// is not EOL, then parse a statement, and do the same for any else or
						// else-if blocks, until we get to EOL (and then implicitly do "end if").
						if (tokens.Peek().tokenType != TokenType.EOL) {
							ParseStatement(tokens, true);  // parses a single statement for the "then" body
							if (tokens.Peek().tokenType == TokenType.Keyword && tokens.Peek().text == "else") {
								tokens.Dequeue();	// skip "else"
								StartElseClause();
								ParseStatement(tokens, true);		// parse a single statement for the "else" body
							} else {
								RequireEitherToken(tokens, TokenType.Keyword, "else", TokenType.EOL);
							}
							output.PatchIfBlock();	// terminate the single-line if
						} else {
							tokens.Dequeue();	// skip EOL
						}
					}
					return;
				case "else":
					StartElseClause();
					break;
				case "else if":
					{
						StartElseClause();
						Value condition = ParseExpr(tokens);
						RequireToken(tokens, TokenType.Keyword, "then");
						output.Add(new Line(null, Line.Op.GotoAifNotB, null, condition));
						output.AddBackpatch("else");
					}
					break;
				case "end if":
					// OK, this is tricky.  We might have an open "else" block or we might not.
					// And, we might have multiple open "end if" jumps (one for the if part,
					// and another for each else-if part).  Patch all that as a special case.
					output.PatchIfBlock();
					break;
				case "while":
					{
						// We need to note the current line, so we can jump back up to it at the end.
						output.AddJumpPoint(keyword);

						// Then parse the condition.
						Value condition = ParseExpr(tokens);

						// OK, now we need to emit a conditional branch, but keep track of this
						// on a stack so that when we get the corresponding "end while", 
						// we can come back and patch that jump to the right place.
						output.Add(new Line(null, Line.Op.GotoAifNotB, null, condition));
						output.AddBackpatch("end while");
					}
					break;
				case "end while":
					{
						// Unconditional jump back to the top of the while loop.
						JumpPoint jump = output.CloseJumpPoint("while");
						output.Add(new Line(null, Line.Op.GotoA, TAC.Num(jump.lineNum)));
						// Then, backpatch the open "while" branch to here, right after the loop.
						// And also patch any "break" branches emitted after that point.
						output.Patch(keyword, true);
					}
					break;
				case "for":
					{
						// Get the loop variable, "in" keyword, and expression to loop over.
						// (Note that the expression is only evaluated once, before the loop.)
						Token loopVarTok = RequireToken(tokens, TokenType.Identifier);
						ValVar loopVar = new ValVar(loopVarTok.text);
						RequireToken(tokens, TokenType.Keyword, "in");
						Value stuff = ParseExpr(tokens);
						if (stuff == null) {
							throw new CompilerException(errorContext, tokens.lineNum,
								"sequence expression expected for 'for' loop");
						}

						// Create an index variable to iterate over the sequence, initialized to -1.
						ValVar idxVar = new ValVar("__" + loopVarTok.text + "_idx");
						output.Add(new Line(idxVar, Line.Op.AssignA, TAC.Num(-1)));

						// We need to note the current line, so we can jump back up to it at the end.
						output.AddJumpPoint(keyword);

						// Now increment the index variable, and branch to the end if it's too big.
						// (We'll have to backpatch this branch later.)
						output.Add(new Line(idxVar, Line.Op.APlusB, idxVar, TAC.Num(1)));
						ValTemp sizeOfSeq = new ValTemp(output.nextTempNum++);
						output.Add(new Line(sizeOfSeq, Line.Op.LengthOfA, stuff));
						ValTemp isTooBig = new ValTemp(output.nextTempNum++);
						output.Add(new Line(isTooBig, Line.Op.AGreatOrEqualB, idxVar, sizeOfSeq));
						output.Add(new Line(null, Line.Op.GotoAifB, null, isTooBig));
						output.AddBackpatch("end for");

						// Otherwise, get the sequence value into our loop variable.
						output.Add(new Line(loopVar, Line.Op.ElemBofIterA, stuff, idxVar));
					}
					break;
				case "end for":
					{
						// Unconditional jump back to the top of the for loop.
						JumpPoint jump = output.CloseJumpPoint("for");
						output.Add(new Line(null, Line.Op.GotoA, TAC.Num(jump.lineNum)));
						// Then, backpatch the open "for" branch to here, right after the loop.
						// And also patch any "break" branches emitted after that point.
						output.Patch(keyword, true);
					}
					break;
				case "break":
					{
						// Emit a jump to the end, to get patched up later.
						output.Add(new Line(null, Line.Op.GotoA));
						output.AddBackpatch("break");
					}
					break;
				case "continue":
					{
						// Jump unconditionally back to the current open jump point.
						if (output.jumpPoints.Count == 0) {
							throw new CompilerException(errorContext, tokens.lineNum,
								"'continue' without open loop block");
						}
						JumpPoint jump = output.jumpPoints.Last();
						output.Add(new Line(null, Line.Op.GotoA, TAC.Num(jump.lineNum)));
					}
					break;
				default:
					throw new CompilerException(errorContext, tokens.lineNum,
						"unexpected keyword '" + keyword + "' at start of line");
				}
			} else {
				ParseAssignment(tokens, allowExtra);
			}

			// A statement should consume everything to the end of the line.
			if (!allowExtra) RequireToken(tokens, TokenType.EOL);

			// Finally, if we have a pending state, because we encountered a function(),
			// then push it onto our stack now that we're done with that statement.
			if (pendingState != null) {
//				Console.WriteLine("PUSHING NEW PARSE STATE");
				output = pendingState;
				outputStack.Push(output);
				pendingState = null;
			}

		}

		private void StartElseClause() {
			// Back-patch the open if block, but leaving room for the jump:
			// Emit the jump from the current location, which is the end of an if-block,
			// to the end of the else block (which we'll have to back-patch later).
			output.Add(new Line(null, Line.Op.GotoA, null));
			// Back-patch the previously open if-block to jump here (right past the goto).
			output.Patch("else");
			// And open a new back-patch for this goto (which will jump all the way to the end if).
			output.AddBackpatch("end if");
		}

		private void ParseAssignment(Lexer tokens, bool allowExtra=false) {
			Value expr = ParseExpr(tokens, true, true);
			Value lhs, rhs;
			Token peek = tokens.Peek();
			if (peek.tokenType == TokenType.EOL ||
					(peek.tokenType == TokenType.Keyword && peek.text == "else")) {
				// No explicit assignment; store an implicit result
				rhs = FullyEvaluate(expr);
				output.Add(new Line(null, Line.Op.AssignImplicit, rhs));
				return;
			}
			if (peek.tokenType == TokenType.OpAssign) {
				tokens.Dequeue();	// skip '='
				lhs = expr;
				rhs = ParseExpr(tokens);
			} else {
				// This looks like a command statement.  Parse the rest
				// of the line as arguments to a function call.
				Value funcRef = expr;
				int argCount = 0;
				while (true) {
					Value arg = ParseExpr(tokens);
					output.Add(new Line(null, Line.Op.PushParam, arg));
					argCount++;
					if (tokens.Peek().tokenType == TokenType.EOL) break;
					if (tokens.Peek().tokenType == TokenType.Keyword && tokens.Peek().text == "else") break;
					if (tokens.Peek().tokenType == TokenType.Comma) {
						tokens.Dequeue();
						AllowLineBreak(tokens);
						continue;
					}
					if (RequireEitherToken(tokens, TokenType.Comma, TokenType.EOL).tokenType == TokenType.EOL) break;
				}
				ValTemp result = new ValTemp(output.nextTempNum++);
				output.Add(new Line(result, Line.Op.CallFunctionA, funcRef, TAC.Num(argCount)));					
				output.Add(new Line(null, Line.Op.AssignImplicit, result));
				return;
			}

			// OK, now, in many cases our last TAC line at this point is an assignment to our RHS temp.
			// In that case, as a simple (but very useful) optimization, we can simply patch that to 
			// assign to our lhs instead.  BUT, we must not do this if there are any jumps to the next
			// line, as may happen due to short-cut evaluation (issue #6).
			if (rhs is ValTemp && output.code.Count > 0 && !output.IsJumpTarget(output.code.Count)) {			
				Line line = output.code[output.code.Count - 1];
				if (line.lhs.Equals(rhs)) {
					// Yep, that's the case.  Patch it up.
					line.lhs = lhs;
					return;
				}
			}
			
            // If the last line was us creating and assigning a function, then we don't add a second assign
            // op, we instead just update that line with the proper LHS
            if (rhs is ValFunction && output.code.Count > 0) {
                Line line = output.code[output.code.Count - 1];
                if (line.op == Line.Op.BindAssignA) {
                    line.lhs = lhs;
                    return;
                }
            }

			// In any other case, do an assignment statement to our lhs.
			output.Add(new Line(lhs, Line.Op.AssignA, rhs));
		}

		private Value ParseExpr(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseFunction;
			return nextLevel(tokens, asLval, statementStart);
		}

		private Value ParseFunction(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseOr;
			Token tok = tokens.Peek();
			if (tok.tokenType != TokenType.Keyword || tok.text != "function") return nextLevel(tokens, asLval, statementStart);
			tokens.Dequeue();

			RequireToken(tokens, TokenType.LParen);

			Function func = new Function(null);

			while (tokens.Peek().tokenType != TokenType.RParen) {
				// parse a parameter: a comma-separated list of
				//			identifier
				//	or...	identifier = expr
				Token id = tokens.Dequeue();
				if (id.tokenType != TokenType.Identifier) throw new CompilerException(errorContext, tokens.lineNum,
					"got " + id + " where an identifier is required");
				Value defaultValue = null;
				if (tokens.Peek().tokenType == TokenType.OpAssign) {
					tokens.Dequeue();	// skip '='
					defaultValue = ParseExpr(tokens);
				}
				func.parameters.Add(new Function.Param(id.text, defaultValue));
				if (tokens.Peek().tokenType == TokenType.RParen) break;
				RequireToken(tokens, TokenType.Comma);
			}

			RequireToken(tokens, TokenType.RParen);

			// Now, we need to parse the function body into its own parsing context.
			// But don't push it yet -- we're in the middle of parsing some expression
			// or statement in the current context, and need to finish that.
			if (pendingState != null) throw new CompilerException(errorContext, tokens.lineNum,
				"can't start two functions in one statement");
			pendingState = new ParseState();
			pendingState.nextTempNum = 1;	// (since 0 is used to hold return value)

//			Console.WriteLine("STARTED FUNCTION");

			// Create a function object attached to the new parse state code.
			func.code = pendingState.code;
			var valFunc = new ValFunction(func);
			output.Add(new Line(null, Line.Op.BindAssignA, valFunc));
			return valFunc;
		}

		private Value ParseOr(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseAnd;
			Value val = nextLevel(tokens, asLval, statementStart);
			List<Line> jumpLines = null;
			Token tok = tokens.Peek();
			while (tok.tokenType == TokenType.Keyword && tok.text == "or") {
				tokens.Dequeue();		// discard "or"
				val = FullyEvaluate(val);

				AllowLineBreak(tokens); // allow a line break after a binary operator

				// Set up a short-circuit jump based on the current value; 
				// we'll fill in the jump destination later.  Note that the
				// usual GotoAifB opcode won't work here, without breaking
				// our calculation of intermediate truth.  We need to jump
				// only if our truth value is >= 1 (i.e. absolutely true).
				Line jump = new Line(null, Line.Op.GotoAifTrulyB, null, val);
				output.Add(jump);
				if (jumpLines == null) jumpLines = new List<Line>();
				jumpLines.Add(jump);

				Value opB = nextLevel(tokens);
				int tempNum = output.nextTempNum++;
				output.Add(new Line(TAC.LTemp(tempNum), Line.Op.AOrB, val, opB));
				val = TAC.RTemp(tempNum);

				tok = tokens.Peek();
			}

			// Now, if we have any short-circuit jumps, those are going to need
			// to copy the short-circuit result (always 1) to our output temp.
			// And anything else needs to skip over that.  So:
			if (jumpLines != null) {
				output.Add(new Line(null, Line.Op.GotoA, TAC.Num(output.code.Count+2)));	// skip over this line:
				output.Add(new Line(val, Line.Op.AssignA, ValNumber.one));	// result = 1
				foreach (Line jump in jumpLines) {
					jump.rhsA = TAC.Num(output.code.Count-1);	// short-circuit to the above result=1 line
				}
			}

			return val;
		}

		private Value ParseAnd(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseNot;
			Value val = nextLevel(tokens, asLval, statementStart);
			List<Line> jumpLines = null;
			Token tok = tokens.Peek();
			while (tok.tokenType == TokenType.Keyword && tok.text == "and") {
				tokens.Dequeue();		// discard "and"
				val = FullyEvaluate(val);

				AllowLineBreak(tokens); // allow a line break after a binary operator

				// Set up a short-circuit jump based on the current value; 
				// we'll fill in the jump destination later.
				Line jump = new Line(null, Line.Op.GotoAifNotB, null, val);
				output.Add(jump);
				if (jumpLines == null) jumpLines = new List<Line>();
				jumpLines.Add(jump);

				Value opB = nextLevel(tokens);
				int tempNum = output.nextTempNum++;
				output.Add(new Line(TAC.LTemp(tempNum), Line.Op.AAndB, val, opB));
				val = TAC.RTemp(tempNum);

				tok = tokens.Peek();
			}

			// Now, if we have any short-circuit jumps, those are going to need
			// to copy the short-circuit result (always 0) to our output temp.
			// And anything else needs to skip over that.  So:
			if (jumpLines != null) {
				output.Add(new Line(null, Line.Op.GotoA, TAC.Num(output.code.Count+2)));	// skip over this line:
				output.Add(new Line(val, Line.Op.AssignA, ValNumber.zero));	// result = 0
				foreach (Line jump in jumpLines) {
					jump.rhsA = TAC.Num(output.code.Count-1);	// short-circuit to the above result=0 line
				}
			}

			return val;
		}

		private Value ParseNot(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseIsA;
			Token tok = tokens.Peek();
			Value val;
			if (tok.tokenType == TokenType.Keyword && tok.text == "not") {
				tokens.Dequeue();		// discard "not"

				AllowLineBreak(tokens); // allow a line break after a unary operator

				val = nextLevel(tokens);
				int tempNum = output.nextTempNum++;
				output.Add(new Line(TAC.LTemp(tempNum), Line.Op.NotA, val));
				val = TAC.RTemp(tempNum);
			} else {
				val = nextLevel(tokens, asLval, statementStart
				);
			}
			return val;
		}

		private Value ParseIsA(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseComparisons;
			Value val = nextLevel(tokens, asLval, statementStart);
			if (tokens.Peek().tokenType == TokenType.Keyword && tokens.Peek().text == "isa") {
				tokens.Dequeue();		// discard the isa operator
				AllowLineBreak(tokens); // allow a line break after a binary operator
				Value opB = nextLevel(tokens);
				int tempNum = output.nextTempNum++;
				output.Add(new Line(TAC.LTemp(tempNum), Line.Op.AisaB, val, opB));
				val = TAC.RTemp(tempNum);
			}
			return val;
		}
		
		private Value ParseComparisons(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseAddSub;
			Value val = nextLevel(tokens, asLval, statementStart);
			Value opA = val;
			Line.Op opcode = ComparisonOp(tokens.Peek().tokenType);
			// Parse a string of comparisons, all multiplied together
			// (so every comparison must be true for the whole expression to be true).
			bool firstComparison = true;
			while (opcode != Line.Op.Noop) {
				tokens.Dequeue();	// discard the operator (we have the opcode)
				opA = FullyEvaluate(opA);

				AllowLineBreak(tokens); // allow a line break after a binary operator

				Value opB = nextLevel(tokens);
				int tempNum = output.nextTempNum++;
				output.Add(new Line(TAC.LTemp(tempNum), opcode,	opA, opB));
				if (firstComparison) {
					firstComparison = false;
				} else {
					tempNum = output.nextTempNum++;
					output.Add(new Line(TAC.LTemp(tempNum), Line.Op.ATimesB, val, TAC.RTemp(tempNum - 1)));
				}
				val = TAC.RTemp(tempNum);
				opA = opB;
				opcode = ComparisonOp(tokens.Peek().tokenType);
			}
			return val;
		}

		// Find the TAC operator that corresponds to the given token type,
		// for comparisons.  If it's not a comparison operator, return Line.Op.Noop.
		private static Line.Op ComparisonOp(TokenType tokenType) {
			switch (tokenType) {
			case TokenType.OpEqual:		return Line.Op.AEqualB;
			case TokenType.OpNotEqual:		return Line.Op.ANotEqualB;
			case TokenType.OpGreater:		return Line.Op.AGreaterThanB;
			case TokenType.OpGreatEqual:	return Line.Op.AGreatOrEqualB;
			case TokenType.OpLesser:		return Line.Op.ALessThanB;
			case TokenType.OpLessEqual:	return Line.Op.ALessOrEqualB;
			default: return Line.Op.Noop;
			}
		}

		private Value ParseAddSub(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseMultDiv;
			Value val = nextLevel(tokens, asLval, statementStart);
			Token tok = tokens.Peek();
			while (tok.tokenType == TokenType.OpPlus || 
					(tok.tokenType == TokenType.OpMinus
					&& (!statementStart || !tok.afterSpace  || tokens.IsAtWhitespace()))) {
				tokens.Dequeue();

				AllowLineBreak(tokens); // allow a line break after a binary operator

				val = FullyEvaluate(val);
				Value opB = nextLevel(tokens);
				int tempNum = output.nextTempNum++;
				output.Add(new Line(TAC.LTemp(tempNum), 
					tok.tokenType == TokenType.OpPlus ? Line.Op.APlusB : Line.Op.AMinusB,
					val, opB));
				val = TAC.RTemp(tempNum);

				tok = tokens.Peek();
			}
			return val;
		}

		private Value ParseMultDiv(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseUnaryMinus;
			Value val = nextLevel(tokens, asLval, statementStart);
			Token tok = tokens.Peek();
			while (tok.tokenType == TokenType.OpTimes || tok.tokenType == TokenType.OpDivide || tok.tokenType == TokenType.OpMod) {
				tokens.Dequeue();

				AllowLineBreak(tokens); // allow a line break after a binary operator

				val = FullyEvaluate(val);
				Value opB = nextLevel(tokens);
				int tempNum = output.nextTempNum++;
				switch (tok.tokenType) {
				case TokenType.OpTimes:
					output.Add(new Line(TAC.LTemp(tempNum), Line.Op.ATimesB, val, opB));
					break;
				case TokenType.OpDivide:
					output.Add(new Line(TAC.LTemp(tempNum), Line.Op.ADividedByB, val, opB));
					break;
				case TokenType.OpMod:
					output.Add(new Line(TAC.LTemp(tempNum), Line.Op.AModB, val, opB));
					break;
				}
				val = TAC.RTemp(tempNum);

				tok = tokens.Peek();
			}
			return val;
		}
			
		private Value ParseUnaryMinus(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseNew;
			if (tokens.Peek().tokenType != TokenType.OpMinus) return nextLevel(tokens, asLval, statementStart);
			tokens.Dequeue();		// skip '-'

			AllowLineBreak(tokens); // allow a line break after a unary operator

			Value val = nextLevel(tokens);
			if (val is ValNumber) {
				// If what follows is a numeric literal, just invert it and be done!
				ValNumber valnum = (ValNumber)val;
				valnum.value = -valnum.value;
				return valnum;
			}
			// Otherwise, subtract it from 0 and return a new temporary.
			int tempNum = output.nextTempNum++;
			output.Add(new Line(TAC.LTemp(tempNum), Line.Op.AMinusB, TAC.Num(0), val));

			return TAC.RTemp(tempNum);
		}

		private Value ParseNew(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseAddressOf;
			if (tokens.Peek().tokenType != TokenType.Keyword || tokens.Peek().text != "new") return nextLevel(tokens, asLval, statementStart);
			tokens.Dequeue();		// skip 'new'

			AllowLineBreak(tokens); // allow a line break after a unary operator

			// Grab a reference to our __isa value
			Value isa = nextLevel(tokens);
			// Now, create a new map, and set __isa on it to that.
			// NOTE: we must be sure this map gets created at runtime, not here at parse time.
			// Since it is a mutable object, we need to return a different one each time
			// this code executes (in a loop, function, etc.).  So, we use Op.CopyA below!
			ValMap map = new ValMap();
			map.SetElem(ValString.magicIsA, isa);
			Value result = new ValTemp(output.nextTempNum++);
			output.Add(new Line(result, Line.Op.CopyA, map));
			return result;
		}

		private Value ParseAddressOf(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParsePower;
			if (tokens.Peek().tokenType != TokenType.AddressOf) return nextLevel(tokens, asLval, statementStart);
			tokens.Dequeue();
			AllowLineBreak(tokens); // allow a line break after a unary operator
			Value val = nextLevel(tokens, true, statementStart);
			if (val is ValVar) {
				((ValVar)val).noInvoke = true;
			} else if (val is ValSeqElem) {
				((ValSeqElem)val).noInvoke = true;
			}
			return val;
		}

		private Value ParsePower(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseCallExpr;
			Value val = nextLevel(tokens, asLval, statementStart);
			Token tok = tokens.Peek();
			while (tok.tokenType == TokenType.OpPower) {
				tokens.Dequeue();

				AllowLineBreak(tokens); // allow a line break after a binary operator

				val = FullyEvaluate(val);
				Value opB = nextLevel(tokens);
				int tempNum = output.nextTempNum++;
				output.Add(new Line(TAC.LTemp(tempNum), Line.Op.APowB, val, opB));
				val = TAC.RTemp(tempNum);

				tok = tokens.Peek();
			}
			return val;
		}


		private Value FullyEvaluate(Value val) {
			if (val is ValVar) {
				ValVar var = (ValVar)val;
				// If var was protected with @, then return it as-is; don't attempt to call it.
				if (var.noInvoke) return val;
				// Don't invoke super; leave as-is so we can do special handling
				// of it at runtime.  Also, as an optimization, same for "self".
				if (var.identifier == "super" || var.identifier == "self") return val;
				// Evaluate a variable (which might be a function we need to call).				
				ValTemp temp = new ValTemp(output.nextTempNum++);
				output.Add(new Line(temp, Line.Op.CallFunctionA, val, ValNumber.zero));
				return temp;
			} else if (val is ValSeqElem) {
				ValSeqElem elem = ((ValSeqElem)val);
				// If sequence element was protected with @, then return it as-is; don't attempt to call it.
				if (elem.noInvoke) return val;
				// Evaluate a sequence lookup (which might be a function we need to call).				
				ValTemp temp = new ValTemp(output.nextTempNum++);
				output.Add(new Line(temp, Line.Op.CallFunctionA, val, ValNumber.zero));
				return temp;
			}
			return val;
		}
		
		private Value ParseCallExpr(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseMap;
			Value val = nextLevel(tokens, asLval, statementStart);
			while (true) {
				if (tokens.Peek().tokenType == TokenType.Dot) {
					tokens.Dequeue();	// discard '.'
					AllowLineBreak(tokens); // allow a line break after a binary operator
					Token nextIdent = RequireToken(tokens, TokenType.Identifier);
					// We're chaining sequences here; look up (by invoking)
					// the previous part of the sequence, so we can build on it.
					val = FullyEvaluate(val);
					// Now build the lookup.
					val = new ValSeqElem(val, new ValString(nextIdent.text));
					if (tokens.Peek().tokenType == TokenType.LParen && !tokens.Peek().afterSpace) {
						// If this new element is followed by parens, we need to
						// parse it as a call right away.
						val = ParseCallArgs(val, tokens);
						//val = FullyEvaluate(val);
					}				
				} else if (tokens.Peek().tokenType == TokenType.LSquare && !tokens.Peek().afterSpace) {
					tokens.Dequeue();	// discard '['
					AllowLineBreak(tokens); // allow a line break after open bracket
					val = FullyEvaluate(val);
	
					if (tokens.Peek().tokenType == TokenType.Colon) {	// e.g., foo[:4]
						tokens.Dequeue();	// discard ':'
						AllowLineBreak(tokens); // allow a line break after colon
						Value index2 = null;
						if (tokens.Peek().tokenType != TokenType.RSquare) index2 = ParseExpr(tokens);
						ValTemp temp = new ValTemp(output.nextTempNum++);
						Intrinsic.CompileSlice(output.code, val, null, index2, temp.tempNum);
						val = temp;
					} else {
						Value index = ParseExpr(tokens);
						if (tokens.Peek().tokenType == TokenType.Colon) {	// e.g., foo[2:4] or foo[2:]
							tokens.Dequeue();	// discard ':'
							AllowLineBreak(tokens); // allow a line break after colon
							Value index2 = null;
							if (tokens.Peek().tokenType != TokenType.RSquare) index2 = ParseExpr(tokens);
							ValTemp temp = new ValTemp(output.nextTempNum++);
							Intrinsic.CompileSlice(output.code, val, index, index2, temp.tempNum);
							val = temp;
						} else {			// e.g., foo[3]  (not a slice at all)
							if (statementStart) {
								// At the start of a statement, we don't want to compile the
								// last sequence lookup, because we might have to convert it into
								// an assignment.  But we want to compile any previous one.
								if (val is ValSeqElem) {
									ValSeqElem vsVal = (ValSeqElem)val;
									ValTemp temp = new ValTemp(output.nextTempNum++);
									output.Add(new Line(temp, Line.Op.ElemBofA, vsVal.sequence, vsVal.index));
									val = temp;
								}
								val = new ValSeqElem(val, index);
							} else {
								// Anywhere else in an expression, we can compile the lookup right away.
								ValTemp temp = new ValTemp(output.nextTempNum++);
								output.Add(new Line(temp, Line.Op.ElemBofA, val, index));
								val = temp;
							}
						}
					}
	
					RequireToken(tokens, TokenType.RSquare);
				} else if ((val is ValVar && !((ValVar)val).noInvoke) || val is ValSeqElem) {
					// Got a variable... it might refer to a function!
					if (!asLval || (tokens.Peek().tokenType == TokenType.LParen && !tokens.Peek().afterSpace)) {
						// If followed by parens, definitely a function call, possibly with arguments!
						// If not, well, let's call it anyway unless we need an lvalue.
						val = ParseCallArgs(val, tokens);
					} else break;
				} else break;
			}
			
			return val;
		}

		private Value ParseMap(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseList;
			if (tokens.Peek().tokenType != TokenType.LCurly) return nextLevel(tokens, asLval, statementStart);
			tokens.Dequeue();
			// NOTE: we must be sure this map gets created at runtime, not here at parse time.
			// Since it is a mutable object, we need to return a different one each time
			// this code executes (in a loop, function, etc.).  So, we use Op.CopyA below!
			ValMap map = new ValMap();
			if (tokens.Peek().tokenType == TokenType.RCurly) {
				tokens.Dequeue();
			} else while (true) {
				AllowLineBreak(tokens); // allow a line break after a comma or open brace

				// Allow the map to close with a } on its own line. 
				if (tokens.Peek().tokenType == TokenType.RCurly) {
					tokens.Dequeue();
					break;
				}

				Value key = ParseExpr(tokens);
				RequireToken(tokens, TokenType.Colon);
				AllowLineBreak(tokens); // allow a line break after a colon
				Value value = ParseExpr(tokens);
				map.map[key ?? ValNull.instance] = value;
				
				if (RequireEitherToken(tokens, TokenType.Comma, TokenType.RCurly).tokenType == TokenType.RCurly) break;
			}
			Value result = new ValTemp(output.nextTempNum++);
			output.Add(new Line(result, Line.Op.CopyA, map));
			return result;
		}

		//		list	:= '[' expr [, expr, ...] ']'
		//				 | quantity
		private Value ParseList(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseQuantity;
			if (tokens.Peek().tokenType != TokenType.LSquare) return nextLevel(tokens, asLval, statementStart);
			tokens.Dequeue();
			// NOTE: we must be sure this list gets created at runtime, not here at parse time.
			// Since it is a mutable object, we need to return a different one each time
			// this code executes (in a loop, function, etc.).  So, we use Op.CopyA below!
			ValList list = new ValList();
			if (tokens.Peek().tokenType == TokenType.RSquare) {
				tokens.Dequeue();
			} else while (true) {
				AllowLineBreak(tokens); // allow a line break after a comma or open bracket

				// Allow the list to close with a ] on its own line. 
				if (tokens.Peek().tokenType == TokenType.RSquare) {
					tokens.Dequeue();
					break;
				}

				Value elem = ParseExpr(tokens);
				list.values.Add(elem);
				if (RequireEitherToken(tokens, TokenType.Comma, TokenType.RSquare).tokenType == TokenType.RSquare) break;
			}
			if (statementStart) return list;	// return the list as-is for indexed assignment (foo[3]=42)
			Value result = new ValTemp(output.nextTempNum++);
			output.Add(new Line(result, Line.Op.CopyA, list));	// use COPY on this mutable list!
			return result;
		}

		//		quantity := '(' expr ')'
		//				  | call
		private Value ParseQuantity(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseAtom;
			if (tokens.Peek().tokenType != TokenType.LParen) return nextLevel(tokens, asLval, statementStart);
			tokens.Dequeue();
			AllowLineBreak(tokens); // allow a line break after an open paren
			Value val = ParseExpr(tokens);
			RequireToken(tokens, TokenType.RParen);
			return val;
		}

		/// <summary>
		/// Helper method that gathers arguments, emitting SetParamAasB for each one,
		/// and then emits the actual call to the given function.  It works both for
		/// a parenthesized set of arguments, and for no parens (i.e. no arguments).
		/// </summary>
		/// <returns>The call arguments.</returns>
		/// <param name="funcRef">Function to invoke.</param>
		/// <param name="tokens">Token stream.</param>
		private Value ParseCallArgs(Value funcRef, Lexer tokens) {
			int argCount = 0;
			if (tokens.Peek().tokenType == TokenType.LParen) {
				tokens.Dequeue();		// remove '('
				if (tokens.Peek().tokenType == TokenType.RParen) {
					tokens.Dequeue();
				} else while (true) {
					AllowLineBreak(tokens); // allow a line break after a comma or open paren
					Value arg = ParseExpr(tokens);
					output.Add(new Line(null, Line.Op.PushParam, arg));
					argCount++;
					if (RequireEitherToken(tokens, TokenType.Comma, TokenType.RParen).tokenType == TokenType.RParen) break;
				}
			}
			ValTemp result = new ValTemp(output.nextTempNum++);
			output.Add(new Line(result, Line.Op.CallFunctionA, funcRef, TAC.Num(argCount)));
			return result;
		}
			
		private Value ParseAtom(Lexer tokens, bool asLval=false, bool statementStart=false) {
			Token tok = !tokens.AtEnd ? tokens.Dequeue() : Token.EOL;
			if (tok.tokenType == TokenType.Number) {
				double d;
				if (double.TryParse(tok.text, NumberStyles.Number | NumberStyles.AllowExponent, 
					CultureInfo.InvariantCulture, out d)) return new ValNumber(d);
				throw new CompilerException("invalid numeric literal: " + tok.text);
			} else if (tok.tokenType == TokenType.String) {
				return new ValString(tok.text);
			} else if (tok.tokenType == TokenType.Identifier) {
				if (tok.text == "self") return ValVar.self;
				return new ValVar(tok.text);
			} else if (tok.tokenType == TokenType.Keyword) {
				switch (tok.text) {
				case "null":	return null;
				case "true":	return ValNumber.one;
				case "false":	return ValNumber.zero;
				}
			}
			throw new CompilerException(string.Format("got {0} where number, string, or identifier is required", tok));
		}


		/// <summary>
		/// The given token type and text is required. So, consume the next token,
		/// and if it doesn't match, throw an error.
		/// </summary>
		/// <param name="tokens">Token queue.</param>
		/// <param name="type">Required token type.</param>
		/// <param name="text">Required token text (if applicable).</param>
		private Token RequireToken(Lexer tokens, TokenType type, string text=null) {
			Token got = (tokens.AtEnd ? Token.EOL : tokens.Dequeue());
			if (got.tokenType != type || (text != null && got.text != text)) {
				Token expected = new Token(type, text);
				throw new CompilerException(errorContext, tokens.lineNum, 
					string.Format("got {0} where {1} is required", got, expected));
			}
			return got;
		}

		private Token RequireEitherToken(Lexer tokens, TokenType type1, string text1, TokenType type2, string text2=null) {
			Token got = (tokens.AtEnd ? Token.EOL : tokens.Dequeue());
			if ((got.tokenType != type1 && got.tokenType != type2)
				|| ((text1 != null && got.text != text1) && (text2 != null && got.text != text2))) {
				Token expected1 = new Token(type1, text1);
				Token expected2 = new Token(type2, text2);
				throw new CompilerException(errorContext, tokens.lineNum, 
					string.Format("got {0} where {1} or {2} is required", got, expected1, expected2));
			}
			return got;
		}

		private Token RequireEitherToken(Lexer tokens, TokenType type1, TokenType type2, string text2=null) {
			return RequireEitherToken(tokens, type1, null, type2, text2);
		}

		private static void TestValidParse(string src, bool dumpTac=false) {
			Parser parser = new Parser();
			try {
				parser.Parse(src);
			} catch (System.Exception e) {
				Console.WriteLine(e.ToString() + " while parsing:");
				Console.WriteLine(src);
			}
			if (dumpTac && parser.output != null) TAC.Dump(parser.output.code, -1);
		}

		public static void RunUnitTests() {
			TestValidParse("pi < 4");
			TestValidParse("(pi < 4)");
			TestValidParse("if true then 20 else 30");
			TestValidParse("f = function(x)\nreturn x*3\nend function\nf(14)");
			TestValidParse("foo=\"bar\"\nindexes(foo*2)\nfoo.indexes");
			TestValidParse("x=[]\nx.push(42)");
			TestValidParse("list1=[10, 20, 30, 40, 50]; range(0, list1.len)");
			TestValidParse("f = function(x); print(\"foo\"); end function; print(false and f)");
			TestValidParse("print 42");
			TestValidParse("print true");
			TestValidParse("f = function(x)\nprint x\nend function\nf 42");
			TestValidParse("myList = [1, null, 3]");
			TestValidParse("while true; if true then; break; else; print 1; end if; end while");
			TestValidParse("x = 0 or\n1");
			TestValidParse("x = [1, 2, \n 3]");
			TestValidParse("range 1,\n10, 2");
		}
	}
}

