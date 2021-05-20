/*	Parser.cs

This file is responsible for parsing MiniScript source code, and converting
it into an internal format (a three-address byte code) that is considerably
faster to execute.

This is normally wrapped by the Interpreter class, so you probably don't
need to deal with Parser directly.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Miniscript.errors;
using Miniscript.interpreter;
using Miniscript.intrinsic;
using Miniscript.lexer;
using Miniscript.tac;
using Miniscript.types;
using static Miniscript.keywords.Consts;

namespace Miniscript.parser {
	public class Parser {

		private string errorContext;	// name of file, etc., used for error reporting

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
		private ParseState pendingState;

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
			outputStack ??= new Stack<ParseState>();
			while (outputStack.Count > 1) outputStack.Pop();
			output = outputStack.Peek();
			output.BackPatches.Clear();
			output.JumpPoints.Clear();
			output.nextTempNum = 0;
		}

		public bool NeedMoreInput() {
			if (!string.IsNullOrEmpty(partialInput)) return true;
			if (outputStack.Count > 1) return true;
			if (output.BackPatches.Count > 0) return true;
			return false;
		}

		public void Parse(string sourceCode, bool replMode=false) {
			if (replMode) {
				// Check for an incomplete final line by finding the last (non-comment) token.
				var lastTok = Lexer.LastToken(sourceCode);
				// Almost any token at the end will signify line continuation, except:
				var isPartial = lastTok.TokenType switch {
					TokenType.EOL => false,
					TokenType.Identifier => false,
					TokenType.Keyword => false,
					TokenType.Number => false,
					TokenType.RCurly => false,
					TokenType.RParen => false,
					TokenType.RSquare => false,
					TokenType.String => false,
					TokenType.Unknown => false,
					_ => true
				};
				if (isPartial) {
					partialInput += Lexer.TrimComment(sourceCode);
					return;
				}
			}
			var tokens = new Lexer(partialInput + sourceCode);
			partialInput = null;
			ParseMultipleLines(tokens);

			if (replMode || !NeedMoreInput()) return;
			
			// Whoops, we need more input but we don't have any.  This is an error.
			tokens.LineNum++;	// (so we report PAST the last line, making it clear this is an EOF problem)
			if (outputStack.Count > 1) {
				throw new CompilerException(errorContext, tokens.LineNum,
					"'function' without matching 'end function'");
			} else if (output.BackPatches.Count > 0) {
				var bp = output.BackPatches[output.BackPatches.Count - 1];
				var msg = bp.WaitingFor switch {
					END_FOR => "'for' without matching 'end for'",
					END_IF => "'if' without matching 'end if'",
					END_WHILE => "'while' without matching 'end while'",
					_ => "unmatched block opener"
				};
				throw new CompilerException(errorContext, tokens.LineNum, msg);
			}
		}

		/// <summary>
		/// Create a virtual machine loaded with the code we have parsed.
		/// </summary>
		/// <param name="standardOutput"></param>
		/// <returns></returns>
		public Machine CreateVM(TextOutputMethod standardOutput) {
			var root = new Context(output.Code);
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
			var locals = new ValVar(LOCALS);
			output.Add(new Line(TAC.LTemp(0), Op.ReturnA, locals));
			// Then wrap the whole thing in a Function.
			var result = new Function(output.Code);
			return result;
		}

		public void REPL(string line) {
			Parse(line);
		
			var vm = CreateVM(null);
			while (!vm.Done) vm.Step();
		}

		private void AllowLineBreak(Lexer tokens) {
			while (tokens.Peek().TokenType == TokenType.EOL && !tokens.AtEnd) tokens.Dequeue();
		}

		private delegate Value ExpressionParsingMethod(Lexer tokens, bool asLval=false, bool statementStart=false);

		/// <summary>
		/// Parse multiple statements until we run out of tokens, or reach 'end function'.
		/// </summary>
		/// <param name="tokens">Tokens.</param>
		private void ParseMultipleLines(Lexer tokens) {
			while (!tokens.AtEnd) {
				// Skip any blank lines
				if (tokens.Peek().TokenType == TokenType.EOL) {
					tokens.Dequeue();
					continue;
				}

				// Prepare a source code location for error reporting
				var location = new SourceLoc(errorContext, tokens.LineNum);

				// Pop our context if we reach 'end function'.
				if (tokens.Peek().TokenType == TokenType.Keyword && tokens.Peek().Text == END_FUNCTION) {
					tokens.Dequeue();
					if (outputStack.Count > 1) {
						outputStack.Pop();
						output = outputStack.Peek();
					} else {
						var e = new CompilerException("'end function' without matching block starter") {
							location = location
						};
						throw e;
					}
					continue;
				}

				// Parse one line (statement).
				var outputStart = output.Code.Count;
				try {
					ParseStatement(tokens);
				} catch (MiniscriptException mse) {
					mse.location ??= location;
					throw mse;
				}
				// Fill in the location info for all the TAC lines we just generated.
				for (int i = outputStart; i < output.Code.Count; i++) {
					output.Code[i].Location = location;
				}
			}
		}

		private void ParseStatement(Lexer tokens, bool allowExtra=false) {
			if (tokens.Peek().TokenType == TokenType.Keyword && tokens.Peek().Text != NOT
				&& tokens.Peek().Text != TRUE && tokens.Peek().Text != FALSE) {
				// Handle statements that begin with a keyword.
				var keyword = tokens.Dequeue().Text;
				switch (keyword) {
					case RETURN: {
							Value returnValue = null;
							if (tokens.Peek().TokenType != TokenType.EOL) {
								returnValue = ParseExpr(tokens);
							}
							output.Add(new Line(TAC.LTemp(0), Op.ReturnA, returnValue));
						}
						break;
					case IF: {
							var condition = ParseExpr(tokens);
							RequireToken(tokens, TokenType.Keyword, THEN);
							// OK, now we need to emit a conditional branch, but keep track of this
							// on a stack so that when we get the corresponding "else" or  "end if", 
							// we can come back and patch that jump to the right place.
							output.Add(new Line(null, Op.GotoAifNotB, null, condition));

							// ...but if blocks also need a special marker in the backpack stack
							// so we know where to stop when patching up (possibly multiple) 'end if' jumps.
							// We'll push a special dummy backpatch here that we look for in PatchIfBlock.
							output.AddBackPatch(IF_MARK);
							output.AddBackPatch(ELSE);
							
							// Allow for the special one-statement if: if the next token after "then"
							// is not EOL, then parse a statement, and do the same for any else or
							// else-if blocks, until we get to EOL (and then implicitly do "end if").
							if (tokens.Peek().TokenType != TokenType.EOL) {
								ParseStatement(tokens, true);  // parses a single statement for the "then" body
								if (tokens.Peek().TokenType == TokenType.Keyword && tokens.Peek().Text == ELSE) {
									tokens.Dequeue();	// skip "else"
									StartElseClause();
									ParseStatement(tokens, true);		// parse a single statement for the "else" body
								} else {
									RequireEitherToken(tokens, TokenType.Keyword, ELSE, TokenType.EOL);
								}
								output.PatchIfBlock();	// terminate the single-line if
							} else {
								tokens.Dequeue();	// skip EOL
							}
					}
						return;
					case ELSE:
						StartElseClause();
						break;
					case ELSE_IF: {
							StartElseClause();
							var condition = ParseExpr(tokens);
							RequireToken(tokens, TokenType.Keyword, THEN);
							output.Add(new Line(null, Op.GotoAifNotB, null, condition));
							output.AddBackPatch(ELSE);
						}
						break;
					case END_IF:
						// OK, this is tricky.  We might have an open "else" block or we might not.
						// And, we might have multiple open "end if" jumps (one for the if part,
						// and another for each else-if part).  Patch all that as a special case.
						output.PatchIfBlock();
						break;
					case WHILE: {
							// We need to note the current line, so we can jump back up to it at the end.
							output.AddJumpPoint(keyword);

							// Then parse the condition.
							var condition = ParseExpr(tokens);

							// OK, now we need to emit a conditional branch, but keep track of this
							// on a stack so that when we get the corresponding "end while", 
							// we can come back and patch that jump to the right place.
							output.Add(new Line(null, Op.GotoAifNotB, null, condition));
							output.AddBackPatch(END_WHILE);
						}
						break;
					case END_WHILE: {
							// Unconditional jump back to the top of the while loop.
							var jump = output.CloseJumpPoint(WHILE);
							output.Add(new Line(null, Op.GotoA, TAC.Num(jump.LineNum)));
							// Then, backpatch the open "while" branch to here, right after the loop.
							// And also patch any "break" branches emitted after that point.
							output.Patch(keyword, true);
						}
						break;
					case FOR: {
							// Get the loop variable, "in" keyword, and expression to loop over.
							// (Note that the expression is only evaluated once, before the loop.)
							var loopVarTok = RequireToken(tokens, TokenType.Identifier);
							var loopVar = new ValVar(loopVarTok.Text);
							RequireToken(tokens, TokenType.Keyword, IN);
							var stuff = ParseExpr(tokens);
							if (stuff == null) {
								throw new CompilerException(errorContext, tokens.LineNum,
									"sequence expression expected for 'for' loop");
							}

							// Create an index variable to iterate over the sequence, initialized to -1.
							var idxVar = new ValVar("__" + loopVarTok.Text + "_idx");
							output.Add(new Line(idxVar, Op.AssignA, TAC.Num(-1)));

							// We need to note the current line, so we can jump back up to it at the end.
							output.AddJumpPoint(keyword);

							// Now increment the index variable, and branch to the end if it's too big.
							// (We'll have to backpatch this branch later.)
							output.Add(new Line(idxVar, Op.APlusB, idxVar, TAC.Num(1)));
							var sizeOfSeq = new ValTemp(output.nextTempNum++);
							output.Add(new Line(sizeOfSeq, Op.LengthOfA, stuff));
							var isTooBig = new ValTemp(output.nextTempNum++);
							output.Add(new Line(isTooBig, Op.AGreatOrEqualB, idxVar, sizeOfSeq));
							output.Add(new Line(null, Op.GotoAifB, null, isTooBig));
							output.AddBackPatch(END_FOR);

							// Otherwise, get the sequence value into our loop variable.
							output.Add(new Line(loopVar, Op.ElemBofIterA, stuff, idxVar));
						}
						break;
					case END_FOR: {
							// Unconditional jump back to the top of the for loop.
							var jump = output.CloseJumpPoint(FOR);
							output.Add(new Line(null, Op.GotoA, TAC.Num(jump.LineNum)));
							// Then, backpatch the open "for" branch to here, right after the loop.
							// And also patch any "break" branches emitted after that point.
							output.Patch(keyword, true);
						}
						break;
					case BREAK: {
							// Emit a jump to the end, to get patched up later.
							output.Add(new Line(null, Op.GotoA));
							output.AddBackPatch(BREAK);
						}
						break;
					case CONTINUE: {
							// Jump unconditionally back to the current open jump point.
							if (output.JumpPoints.Count == 0) {
								throw new CompilerException(errorContext, tokens.LineNum, "'continue' without open loop block");
							}
							JumpPoint jump = output.JumpPoints.Last();
							output.Add(new Line(null, Op.GotoA, TAC.Num(jump.LineNum)));
						}
						break;
					default:
						throw new CompilerException(errorContext, tokens.LineNum, $"unexpected keyword '{keyword}' at start of line");
				}
			} else {
				ParseAssignment(tokens, allowExtra);
			}

			// A statement should consume everything to the end of the line.
			if (!allowExtra) RequireToken(tokens, TokenType.EOL);

			// Finally, if we have a pending state, because we encountered a function(),
			// then push it onto our stack now that we're Done with that statement.
			if (pendingState == null) return;
			
			output = pendingState;
			outputStack.Push(output);
			pendingState = null;
		}

		private void StartElseClause() {
			// Back-patch the open if block, but leaving room for the jump:
			// Emit the jump from the current location, which is the end of an if-block,
			// to the end of the else block (which we'll have to back-patch later).
			output.Add(new Line(null, Op.GotoA, null));
			// Back-patch the previously open if-block to jump here (right past the goto).
			output.Patch(ELSE);
			// And open a new back-patch for this goto (which will jump all the way to the end if).
			output.AddBackPatch(END_IF);
		}

		private void ParseAssignment(Lexer tokens, bool allowExtra=false) {
			var expr = ParseExpr(tokens, true, true);
			Value lhs, rhs;
			var peek = tokens.Peek();
			switch (peek.TokenType) {
				case TokenType.EOL:
				case TokenType.Keyword when peek.Text == ELSE:
					// No explicit assignment; store an implicit result
					rhs = FullyEvaluate(expr);
					output.Add(new Line(null, Op.AssignImplicit, rhs));
					return;
				case TokenType.OpAssign:
					tokens.Dequeue();	// skip '='
					lhs = expr;
					rhs = ParseExpr(tokens);
					break;
				default: {
					// This looks like a command statement.  Parse the rest
					// of the line as arguments to a function call.
					var funcRef = expr;
					var argCount = 0;
					while (true) {
						var arg = ParseExpr(tokens);
						output.Add(new Line(null, Op.PushParam, arg));
						argCount++;
						if (tokens.Peek().TokenType == TokenType.EOL) break;
						if (tokens.Peek().TokenType == TokenType.Keyword && tokens.Peek().Text == ELSE) break;
						if (tokens.Peek().TokenType == TokenType.Comma) {
							tokens.Dequeue();
							AllowLineBreak(tokens);
							continue;
						}
						if (RequireEitherToken(tokens, TokenType.Comma, TokenType.EOL).TokenType == TokenType.EOL) break;
					}
					var result = new ValTemp(output.nextTempNum++);
					output.Add(new Line(result, Op.CallFunctionA, funcRef, TAC.Num(argCount)));					
					output.Add(new Line(null, Op.AssignImplicit, result));
					return;
				}
			}

			switch (rhs) {
				// OK, now, in many cases our last TAC line at this point is an assignment to our RHS temp.
				// In that case, as a simple (but very useful) optimization, we can simply patch that to 
				// assign to our lhs instead.  BUT, we must not do this if there are any jumps to the next
				// line, as may happen due to short-cut evaluation (issue #6).
				case ValTemp _ when output.Code.Count > 0 && !output.IsJumpTarget(output.Code.Count): {
					var line = output.Code[output.Code.Count - 1];
					if (line.Lhs.Equals(rhs)) {
						// Yep, that's the case.  Patch it up.
						line.Lhs = lhs;
						return;
					}

					break;
				}
				// If the last line was us creating and assigning a function, then we don't add a second assign
				// op, we instead just update that line with the proper LHS
				case ValFunction _ when output.Code.Count > 0: {
					var line = output.Code[output.Code.Count - 1];
					if (line.Op == Op.BindAssignA) {
						line.Lhs = lhs;
						return;
					}

					break;
				}
			}

			// In any other case, do an assignment statement to our lhs.
			output.Add(new Line(lhs, Op.AssignA, rhs));
		}

		private Value ParseExpr(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseFunction;
			return nextLevel(tokens, asLval, statementStart);
		}

		private Value ParseFunction(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseOr;
			var tok = tokens.Peek();
			if (tok.TokenType != TokenType.Keyword || tok.Text != FUNCTION) return nextLevel(tokens, asLval, statementStart);
			tokens.Dequeue();

			RequireToken(tokens, TokenType.LParen);

			var func = new Function(null);

			while (tokens.Peek().TokenType != TokenType.RParen) {
				// parse a parameter: a comma-separated list of
				//			identifier
				//	or...	identifier = expr
				var id = tokens.Dequeue();
				if (id.TokenType != TokenType.Identifier) throw new CompilerException(errorContext, tokens.LineNum,
					$"got {id} where an identifier is required");
				Value defaultValue = null;
				if (tokens.Peek().TokenType == TokenType.OpAssign) {
					tokens.Dequeue();	// skip '='
					defaultValue = ParseExpr(tokens);
				}
				func.Parameters.Add(new Param(id.Text, defaultValue));
				if (tokens.Peek().TokenType == TokenType.RParen) break;
				RequireToken(tokens, TokenType.Comma);
			}

			RequireToken(tokens, TokenType.RParen);

			// Now, we need to parse the function body into its own parsing context.
			// But don't push it yet -- we're in the middle of parsing some expression
			// or statement in the current context, and need to finish that.
			if (pendingState != null) throw new CompilerException(errorContext, tokens.LineNum,
				"can't start two functions in one statement");
			pendingState = new ParseState {nextTempNum = 1};
			// (since 0 is used to hold return value)

			// Create a function object attached to the new parse state code.
			func.Code = pendingState.Code;
			var valFunc = new ValFunction(func);
			output.Add(new Line(null, Op.BindAssignA, valFunc));
			return valFunc;
		}

		private Value ParseOr(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseAnd;
			var val = nextLevel(tokens, asLval, statementStart);
			List<Line> jumpLines = null;
			var tok = tokens.Peek();
			while (tok.TokenType == TokenType.Keyword && tok.Text == OR) {
				tokens.Dequeue();		// discard "or"
				val = FullyEvaluate(val);

				AllowLineBreak(tokens); // allow a line break after a binary operator

				// Set up a short-circuit jump based on the current value; 
				// we'll fill in the jump destination later.  Note that the
				// usual GotoAifB opcode won't work here, without breaking
				// our calculation of intermediate truth.  We need to jump
				// only if our truth value is >= 1 (i.e. absolutely true).
				var jump = new Line(null, Op.GotoAifTrulyB, null, val);
				output.Add(jump);
				jumpLines ??= new List<Line>();
				jumpLines.Add(jump);

				var opB = nextLevel(tokens);
				var tempNum = output.nextTempNum++;
				output.Add(new Line(TAC.LTemp(tempNum), Op.AOrB, val, opB));
				val = TAC.RTemp(tempNum);

				tok = tokens.Peek();
			}

			// Now, if we have any short-circuit jumps, those are going to need
			// to copy the short-circuit result (always 1) to our output temp.
			// And anything else needs to skip over that.  So:
			if (jumpLines != null) {
				output.Add(new Line(null, Op.GotoA, TAC.Num(output.Code.Count+2)));	// skip over this line:
				output.Add(new Line(val, Op.AssignA, ValNumber.One));	// result = 1
				foreach (var jump in jumpLines) {
					jump.RhsA = TAC.Num(output.Code.Count-1);	// short-circuit to the above result=1 line
				}
			}

			return val;
		}

		private Value ParseAnd(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseNot;
			var val = nextLevel(tokens, asLval, statementStart);
			List<Line> jumpLines = null;
			var tok = tokens.Peek();
			while (tok.TokenType == TokenType.Keyword && tok.Text == AND) {
				tokens.Dequeue();		// discard "and"
				val = FullyEvaluate(val);

				AllowLineBreak(tokens); // allow a line break after a binary operator

				// Set up a short-circuit jump based on the current value; 
				// we'll fill in the jump destination later.
				var jump = new Line(null, Op.GotoAifNotB, null, val);
				output.Add(jump);
				jumpLines ??= new List<Line>();
				jumpLines.Add(jump);

				var opB = nextLevel(tokens);
				var tempNum = output.nextTempNum++;
				output.Add(new Line(TAC.LTemp(tempNum), Op.AAndB, val, opB));
				val = TAC.RTemp(tempNum);

				tok = tokens.Peek();
			}

			// Now, if we have any short-circuit jumps, those are going to need
			// to copy the short-circuit result (always 0) to our output temp.
			// And anything else needs to skip over that.  So:
			if (jumpLines != null) {
				output.Add(new Line(null, Op.GotoA, TAC.Num(output.Code.Count+2)));	// skip over this line:
				output.Add(new Line(val, Op.AssignA, ValNumber.Zero));	// result = 0
				foreach (var jump in jumpLines) {
					jump.RhsA = TAC.Num(output.Code.Count-1);	// short-circuit to the above result=0 line
				}
			}

			return val;
		}

		private Value ParseNot(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseIsA;
			var tok = tokens.Peek();
			Value val;
			if (tok.TokenType == TokenType.Keyword && tok.Text == NOT) {
				tokens.Dequeue();		// discard "not"

				AllowLineBreak(tokens); // allow a line break after a unary operator

				val = nextLevel(tokens);
				var tempNum = output.nextTempNum++;
				output.Add(new Line(TAC.LTemp(tempNum), Op.NotA, val));
				val = TAC.RTemp(tempNum);
			} else {
				val = nextLevel(tokens, asLval, statementStart);
			}
			return val;
		}

		private Value ParseIsA(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseComparisons;
			var val = nextLevel(tokens, asLval, statementStart);
			if (tokens.Peek().TokenType != TokenType.Keyword || tokens.Peek().Text != ISA) return val;
			
			tokens.Dequeue();		// discard the isa operator
			AllowLineBreak(tokens); // allow a line break after a binary operator
			var opB = nextLevel(tokens);
			var tempNum = output.nextTempNum++;
			output.Add(new Line(TAC.LTemp(tempNum), Op.AisaB, val, opB));
			val = TAC.RTemp(tempNum);
			return val;
		}
		
		private Value ParseComparisons(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseAddSub;
			var val = nextLevel(tokens, asLval, statementStart);
			var opA = val;
			var opcode = ComparisonOp(tokens.Peek().TokenType);
			// Parse a string of comparisons, all multiplied together
			// (so every comparison must be true for the whole expression to be true).
			var firstComparison = true;
			while (opcode != Op.Noop) {
				tokens.Dequeue();	// discard the operator (we have the opcode)
				opA = FullyEvaluate(opA);

				AllowLineBreak(tokens); // allow a line break after a binary operator

				var opB = nextLevel(tokens);
				var tempNum = output.nextTempNum++;
				output.Add(new Line(TAC.LTemp(tempNum), opcode,	opA, opB));
				if (firstComparison) {
					firstComparison = false;
				} else {
					tempNum = output.nextTempNum++;
					output.Add(new Line(TAC.LTemp(tempNum), Op.ATimesB, val, TAC.RTemp(tempNum - 1)));
				}
				val = TAC.RTemp(tempNum);
				opA = opB;
				opcode = ComparisonOp(tokens.Peek().TokenType);
			}
			return val;
		}

		// Find the TAC operator that corresponds to the given token type,
		// for comparisons.  If it's not a comparison operator, return Op.Noop.
		private static Op ComparisonOp(TokenType tokenType) {
			return tokenType switch {
				TokenType.OpEqual => Op.AEqualB,
				TokenType.OpNotEqual => Op.ANotEqualB,
				TokenType.OpGreater => Op.AGreaterThanB,
				TokenType.OpGreatEqual => Op.AGreatOrEqualB,
				TokenType.OpLesser => Op.ALessThanB,
				TokenType.OpLessEqual => Op.ALessOrEqualB,
				_ => Op.Noop
			};
		}

		private Value ParseAddSub(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseMultDiv;
			var val = nextLevel(tokens, asLval, statementStart);
			var tok = tokens.Peek();
			while (tok.TokenType == TokenType.OpPlus || 
					(tok.TokenType == TokenType.OpMinus
					&& (!statementStart || !tok.AfterSpace  || tokens.IsAtWhitespace()))) {
				tokens.Dequeue();

				AllowLineBreak(tokens); // allow a line break after a binary operator

				val = FullyEvaluate(val);
				var opB = nextLevel(tokens);
				var tempNum = output.nextTempNum++;
				output.Add(new Line(TAC.LTemp(tempNum), 
					tok.TokenType == TokenType.OpPlus ? Op.APlusB : Op.AMinusB,
					val, opB));
				val = TAC.RTemp(tempNum);

				tok = tokens.Peek();
			}
			return val;
		}

		private Value ParseMultDiv(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseUnaryMinus;
			var val = nextLevel(tokens, asLval, statementStart);
			var tok = tokens.Peek();
			while (tok.TokenType == TokenType.OpTimes || tok.TokenType == TokenType.OpDivide || tok.TokenType == TokenType.OpMod) {
				tokens.Dequeue();

				AllowLineBreak(tokens); // allow a line break after a binary operator

				val = FullyEvaluate(val);
				var opB = nextLevel(tokens);
				var tempNum = output.nextTempNum++;
				switch (tok.TokenType) {
					case TokenType.OpTimes:
						output.Add(new Line(TAC.LTemp(tempNum), Op.ATimesB, val, opB));
						break;
					case TokenType.OpDivide:
						output.Add(new Line(TAC.LTemp(tempNum), Op.ADividedByB, val, opB));
						break;
					case TokenType.OpMod:
						output.Add(new Line(TAC.LTemp(tempNum), Op.AModB, val, opB));
						break;
				}
				val = TAC.RTemp(tempNum);

				tok = tokens.Peek();
			}
			return val;
		}
			
		private Value ParseUnaryMinus(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseNew;
			if (tokens.Peek().TokenType != TokenType.OpMinus) return nextLevel(tokens, asLval, statementStart);
			tokens.Dequeue();		// skip '-'

			AllowLineBreak(tokens); // allow a line break after a unary operator

			var val = nextLevel(tokens);
			if (val is ValNumber valnum) {
				// If what follows is a numeric literal, just invert it and be Done!
				valnum.Value = -valnum.Value;
				return valnum;
			}
			// Otherwise, subtract it from 0 and return a new temporary.
			var tempNum = output.nextTempNum++;
			output.Add(new Line(TAC.LTemp(tempNum), Op.AMinusB, TAC.Num(0), val));

			return TAC.RTemp(tempNum);
		}

		private Value ParseNew(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseAddressOf;
			if (tokens.Peek().TokenType != TokenType.Keyword || tokens.Peek().Text != NEW) return nextLevel(tokens, asLval, statementStart);
			tokens.Dequeue();		// skip 'new'

			AllowLineBreak(tokens); // allow a line break after a unary operator

			// Grab a reference to our __isa value
			var isa = nextLevel(tokens);
			// Now, create a new map, and set __isa on it to that.
			// NOTE: we must be sure this map gets created at runtime, not here at parse time.
			// Since it is a mutable object, we need to return a different one each time
			// this code executes (in a loop, function, etc.).  So, we use Op.CopyA below!
			var map = new ValMap();
			map.SetElem(ValString.MagicIsA, isa);
			Value result = new ValTemp(output.nextTempNum++);
			output.Add(new Line(result, Op.CopyA, map));
			return result;
		}

		private Value ParseAddressOf(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParsePower;
			if (tokens.Peek().TokenType != TokenType.AddressOf) return nextLevel(tokens, asLval, statementStart);
			tokens.Dequeue();
			AllowLineBreak(tokens); // allow a line break after a unary operator
			var val = nextLevel(tokens, true, statementStart);
			switch (val) {
				case ValVar valVar:
					valVar.NoInvoke = true;
					break;
				case ValSeqElem seqElem:
					seqElem.NoInvoke = true;
					break;
			}
			return val;
		}

		private Value ParsePower(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseCallExpr;
			var val = nextLevel(tokens, asLval, statementStart);
			var tok = tokens.Peek();
			while (tok.TokenType == TokenType.OpPower) {
				tokens.Dequeue();

				AllowLineBreak(tokens); // allow a line break after a binary operator

				val = FullyEvaluate(val);
				var opB = nextLevel(tokens);
				var tempNum = output.nextTempNum++;
				output.Add(new Line(TAC.LTemp(tempNum), Op.APowB, val, opB));
				val = TAC.RTemp(tempNum);

				tok = tokens.Peek();
			}
			return val;
		}


		private Value FullyEvaluate(Value val) {
			switch (val) {
				case ValVar valVar: {
					// If var was protected with @, then return it as-is; don't attempt to call it.
					if (valVar.NoInvoke) return valVar;
					// Don't invoke super; leave as-is so we can do special handling
					// of it at runtime.  Also, as an optimization, same for "self".
					if (valVar.Identifier == SUPER || valVar.Identifier == SELF) return valVar;
					// Evaluate a variable (which might be a function we need to call).				
					var temp = new ValTemp(output.nextTempNum++);
					output.Add(new Line(temp, Op.CallFunctionA, valVar, ValNumber.Zero));
					return temp;
				}
				case ValSeqElem seqElem: {
					// If sequence element was protected with @, then return it as-is; don't attempt to call it.
					if (seqElem.NoInvoke) return seqElem;
					// Evaluate a sequence lookup (which might be a function we need to call).				
					var temp = new ValTemp(output.nextTempNum++);
					output.Add(new Line(temp, Op.CallFunctionA, seqElem, ValNumber.Zero));
					return temp;
				}
				default:
					return val;
			}
		}
		
		private Value ParseCallExpr(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseMap;
			var val = nextLevel(tokens, asLval, statementStart);
			while (true) {
				if (tokens.Peek().TokenType == TokenType.Dot) {
					tokens.Dequeue();	// discard '.'
					AllowLineBreak(tokens); // allow a line break after a binary operator
					var nextIdent = RequireToken(tokens, TokenType.Identifier);
					// We're chaining sequences here; look up (by invoking)
					// the previous part of the sequence, so we can build on it.
					val = FullyEvaluate(val);
					// Now build the lookup.
					val = new ValSeqElem(val, new ValString(nextIdent.Text));
					if (tokens.Peek().TokenType == TokenType.LParen && !tokens.Peek().AfterSpace) {
						// If this new element is followed by parens, we need to
						// parse it as a call right away.
						val = ParseCallArgs(val, tokens);
						//val = FullyEvaluate(val);
					}				
				} else if (tokens.Peek().TokenType == TokenType.LSquare && !tokens.Peek().AfterSpace) {
					tokens.Dequeue();	// discard '['
					AllowLineBreak(tokens); // allow a line break after open bracket
					val = FullyEvaluate(val);
	
					if (tokens.Peek().TokenType == TokenType.Colon) {	// e.g., foo[:4]
						tokens.Dequeue();	// discard ':'
						AllowLineBreak(tokens); // allow a line break after colon
						Value index2 = null;
						if (tokens.Peek().TokenType != TokenType.RSquare) index2 = ParseExpr(tokens);
						var temp = new ValTemp(output.nextTempNum++);
						Intrinsic.CompileSlice(output.Code, val, null, index2, temp.TempNum);
						val = temp;
					} else {
						var index = ParseExpr(tokens);
						if (tokens.Peek().TokenType == TokenType.Colon) {	// e.g., foo[2:4] or foo[2:]
							tokens.Dequeue();	// discard ':'
							AllowLineBreak(tokens); // allow a line break after colon
							Value index2 = null;
							if (tokens.Peek().TokenType != TokenType.RSquare) index2 = ParseExpr(tokens);
							var temp = new ValTemp(output.nextTempNum++);
							Intrinsic.CompileSlice(output.Code, val, index, index2, temp.TempNum);
							val = temp;
						} else {			// e.g., foo[3]  (not a slice at all)
							if (statementStart) {
								// At the start of a statement, we don't want to compile the
								// last sequence lookup, because we might have to convert it into
								// an assignment.  But we want to compile any previous one.
								if (val is ValSeqElem) {
									var vsVal = (ValSeqElem)val;
									var temp = new ValTemp(output.nextTempNum++);
									output.Add(new Line(temp, Op.ElemBofA, vsVal.Sequence, vsVal.Index));
									val = temp;
								}
								val = new ValSeqElem(val, index);
							} else {
								// Anywhere else in an expression, we can compile the lookup right away.
								var temp = new ValTemp(output.nextTempNum++);
								output.Add(new Line(temp, Op.ElemBofA, val, index));
								val = temp;
							}
						}
					}
	
					RequireToken(tokens, TokenType.RSquare);
				} else if ((val is ValVar valVar && !valVar.NoInvoke) || val is ValSeqElem) {
					// Got a variable... it might refer to a function!
					if (!asLval || (tokens.Peek().TokenType == TokenType.LParen && !tokens.Peek().AfterSpace)) {
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
			if (tokens.Peek().TokenType != TokenType.LCurly) return nextLevel(tokens, asLval, statementStart);
			
			tokens.Dequeue();
			// NOTE: we must be sure this map gets created at runtime, not here at parse time.
			// Since it is a mutable object, we need to return a different one each time
			// this code executes (in a loop, function, etc.).  So, we use Op.CopyA below!
			var map = new ValMap();
			if (tokens.Peek().TokenType == TokenType.RCurly) {
				tokens.Dequeue();
			} else while (true) {
				AllowLineBreak(tokens); // allow a line break after a comma or open brace

				// Allow the map to close with a } on its own line. 
				if (tokens.Peek().TokenType == TokenType.RCurly) {
					tokens.Dequeue();
					break;
				}

				var key = ParseExpr(tokens);
				RequireToken(tokens, TokenType.Colon);
				AllowLineBreak(tokens); // allow a line break after a colon
				var value = ParseExpr(tokens);
				map.Map[key ?? ValNull.Instance] = value;
				
				if (RequireEitherToken(tokens, TokenType.Comma, TokenType.RCurly).TokenType == TokenType.RCurly) break;
			}
			Value result = new ValTemp(output.nextTempNum++);
			output.Add(new Line(result, Op.CopyA, map));
			return result;
		}

		//		list	:= '[' expr [, expr, ...] ']'
		//				 | quantity
		private Value ParseList(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseQuantity;
			if (tokens.Peek().TokenType != TokenType.LSquare) return nextLevel(tokens, asLval, statementStart);
			tokens.Dequeue();
			// NOTE: we must be sure this list gets created at runtime, not here at parse time.
			// Since it is a mutable object, we need to return a different one each time
			// this code executes (in a loop, function, etc.).  So, we use Op.CopyA below!
			var list = new ValList();
			if (tokens.Peek().TokenType == TokenType.RSquare) {
				tokens.Dequeue();
			} else while (true) {
				AllowLineBreak(tokens); // allow a line break after a comma or open bracket

				// Allow the list to close with a ] on its own line. 
				if (tokens.Peek().TokenType == TokenType.RSquare) {
					tokens.Dequeue();
					break;
				}

				var elem = ParseExpr(tokens);
				list.Values.Add(elem);
				if (RequireEitherToken(tokens, TokenType.Comma, TokenType.RSquare).TokenType == TokenType.RSquare) break;
			}
			if (statementStart) return list;	// return the list as-is for indexed assignment (foo[3]=42)
			Value result = new ValTemp(output.nextTempNum++);
			output.Add(new Line(result, Op.CopyA, list));	// use COPY on this mutable list!
			return result;
		}

		//		quantity := '(' expr ')'
		//				  | call
		private Value ParseQuantity(Lexer tokens, bool asLval=false, bool statementStart=false) {
			ExpressionParsingMethod nextLevel = ParseAtom;
			if (tokens.Peek().TokenType != TokenType.LParen) return nextLevel(tokens, asLval, statementStart);
			
			tokens.Dequeue();
			AllowLineBreak(tokens); // allow a line break after an open paren
			var val = ParseExpr(tokens);
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
			var argCount = 0;
			if (tokens.Peek().TokenType == TokenType.LParen) {
				tokens.Dequeue();		// remove '('
				if (tokens.Peek().TokenType == TokenType.RParen) {
					tokens.Dequeue();
				} else while (true) {
					AllowLineBreak(tokens); // allow a line break after a comma or open paren
					var arg = ParseExpr(tokens);
					output.Add(new Line(null, Op.PushParam, arg));
					argCount++;
					if (RequireEitherToken(tokens, TokenType.Comma, TokenType.RParen).TokenType == TokenType.RParen) break;
				}
			}
			var result = new ValTemp(output.nextTempNum++);
			output.Add(new Line(result, Op.CallFunctionA, funcRef, TAC.Num(argCount)));
			return result;
		}
			
		private Value ParseAtom(Lexer tokens, bool asLval=false, bool statementStart=false) {
			var tok = !tokens.AtEnd ? tokens.Dequeue() : Token.Eol;
			switch (tok.TokenType) {
				case TokenType.Number: {
					double d;
					if (double.TryParse(tok.Text, NumberStyles.Number | NumberStyles.AllowExponent, 
						CultureInfo.InvariantCulture, out d)) return new ValNumber(d);
					throw new CompilerException("invalid numeric literal: " + tok.Text);
				}
				case TokenType.String:
					return new ValString(tok.Text);
				case TokenType.Identifier:
					return tok.Text == SELF ? ValVar.Self : new ValVar(tok.Text);
				case TokenType.Keyword:
					switch (tok.Text) {
						case NULL:	return null;
						case TRUE:	return ValNumber.One;
						case FALSE:	return ValNumber.Zero;
					}

					break;
			}
			throw new CompilerException($"got {tok} where number, string, or identifier is required");
		}
		
		/// <summary>
		/// The given token type and text is required. So, consume the next token,
		/// and if it doesn't match, throw an error.
		/// </summary>
		/// <param name="tokens">Token queue.</param>
		/// <param name="type">Required token type.</param>
		/// <param name="text">Required token text (if applicable).</param>
		private Token RequireToken(Lexer tokens, TokenType type, string text=null) {
			var got = (tokens.AtEnd ? Token.Eol : tokens.Dequeue());
			if (got.TokenType == type && (text == null || got.Text == text)) return got;
			
			var expected = new Token(type, text);
			throw new CompilerException(errorContext, tokens.LineNum, $"got {got} where {expected} is required");
		}

		private Token RequireEitherToken(Lexer tokens, TokenType type1, string text1, TokenType type2, string text2=null) {
			var got = (tokens.AtEnd ? Token.Eol : tokens.Dequeue());
			if ((got.TokenType == type1 || got.TokenType == type2) &&
			    ((text1 == null || got.Text == text1) || (text2 == null || got.Text == text2))) return got;
			
			var expected1 = new Token(type1, text1);
			var expected2 = new Token(type2, text2);
			throw new CompilerException(errorContext, tokens.LineNum, $"got {got} where {expected1} or {expected2} is required");
		}

		private Token RequireEitherToken(Lexer tokens, TokenType type1, TokenType type2, string text2=null) {
			return RequireEitherToken(tokens, type1, null, type2, text2);
		}

		private static void TestValidParse(string src, bool dumpTac=false) {
			var parser = new Parser();
			try {
				parser.Parse(src);
			} catch (Exception e) {
				Console.WriteLine($"{e} while parsing:");
				Console.WriteLine(src);
			}
			if (dumpTac && parser.output != null) TAC.Dump(parser.output.Code, -1);
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

