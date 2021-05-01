/*	MiniscriptLexer.cs

This file is used internally during parsing of the code, breaking source
code text into a series of tokens.

Unless you’re writing a fancy Minisript code editor, you probably don’t 
need to worry about this stuff. 

*/
using System;
using System.Collections.Generic;
using Miniscript.sources.lexer;

namespace Miniscript {
				
	public class Lexer {
		public int lineNum = 1;	// start at 1, so we report 1-based line numbers
		public int position;
		
		string input;
		int inputLength;

		Queue<Token> pending;

		public bool AtEnd {
			get { return position >= inputLength && pending.Count == 0; }
		}

		public Lexer(string input) {
			this.input = input;
			inputLength = input.Length;
			position = 0;
			pending = new Queue<Token>();
		}

		public Token Peek() {
			if (pending.Count == 0) {
				if (AtEnd) return Token.EOL;
				pending.Enqueue(Dequeue());
			}
			return pending.Peek();
		}

		public Token Dequeue() {
			if (pending.Count > 0) return pending.Dequeue();

			int oldPos = position;
			SkipWhitespaceAndComment();

			if (AtEnd) return Token.EOL;

			Token result = new Token();
			result.afterSpace = (position > oldPos);
			int startPos = position;
			char c = input[position++];

			// Handle two-character operators first.
			if (!AtEnd) {
				char c2 = input[position];
				if (c == '=' && c2 == '=') result.tokenType = TokenType.OpEqual;
				if (c == '!' && c2 == '=') result.tokenType = TokenType.OpNotEqual;
				if (c == '>' && c2 == '=') result.tokenType = TokenType.OpGreatEqual;
				if (c == '<' && c2 == '=') result.tokenType = TokenType.OpLessEqual;

				if (result.tokenType != TokenType.Unknown) {
					position++;
					return result;
				}
			}

			// Handle one-char operators next.
			if (c == '+') result.tokenType = TokenType.OpPlus;
			else if (c == '-') result.tokenType = TokenType.OpMinus;
			else if (c == '*') result.tokenType = TokenType.OpTimes;
			else if (c == '/') result.tokenType = TokenType.OpDivide;
			else if (c == '%') result.tokenType = TokenType.OpMod;
			else if (c == '^') result.tokenType = TokenType.OpPower;
			else if (c == '(') result.tokenType = TokenType.LParen;
			else if (c == ')') result.tokenType = TokenType.RParen;
			else if (c == '[') result.tokenType = TokenType.LSquare;
			else if (c == ']') result.tokenType = TokenType.RSquare;
			else if (c == '{') result.tokenType = TokenType.LCurly;
			else if (c == '}') result.tokenType = TokenType.RCurly;
			else if (c == ',') result.tokenType = TokenType.Comma;
			else if (c == ':') result.tokenType = TokenType.Colon;
			else if (c == '=') result.tokenType = TokenType.OpAssign;
			else if (c == '<') result.tokenType = TokenType.OpLesser;
			else if (c == '>') result.tokenType = TokenType.OpGreater;
			else if (c == '@') result.tokenType = TokenType.AddressOf;
			else if (c == ';' || c == '\n') {
				result.tokenType = TokenType.EOL;
				result.text = c == ';' ? ";" : "\n";
				if (c != ';') lineNum++;
			}
			if (c == '\r') {
				// Careful; DOS may use \r\n, so we need to check for that too.
				result.tokenType = TokenType.EOL;
				if (position < inputLength && input[position] == '\n') {
					position++;
					result.text = "\r\n";
				} else {
					result.text = "\r";
				}
				lineNum++;
			}
			if (result.tokenType != TokenType.Unknown) return result;

			// Then, handle more extended tokens.

			if (c == '.') {
				// A token that starts with a dot is just Type.Dot, UNLESS
				// it is followed by a number, in which case it's a decimal number.
				if (position >= inputLength || !IsNumeric(input[position])) {
					result.tokenType = TokenType.Dot;
					return result;
				}
			}

			if (c == '.' || IsNumeric(c)) {
				result.tokenType = TokenType.Number;
				while (position < inputLength) {
					char lastc = c;
					c = input[position];
					if (IsNumeric(c) || c == '.' || c == 'E' || c == 'e' ||
					    ((c == '-' || c == '+') && (lastc == 'E' || lastc == 'e'))) {
						position++;
					} else break;
				}
			} else if (IsIdentifier(c)) {
				while (position < inputLength) {
					if (IsIdentifier(input[position])) position++;
					else break;
				}
				result.text = input.Substring(startPos, position - startPos);
				result.tokenType = (Keywords.IsKeyword(result.text) ? TokenType.Keyword : TokenType.Identifier);
				if (result.text == "end") {
					// As a special case: when we see "end", grab the next keyword (after whitespace)
					// too, and conjoin it, so our token is "end if", "end function", etc.
					Token nextWord = Dequeue();
					if (nextWord != null && nextWord.tokenType == TokenType.Keyword) {
						result.text = result.text + " " + nextWord.text;
					} else {
						// Oops, didn't find another keyword.  User error.
						throw new LexerException("'end' without following keyword ('if', 'function', etc.)");
					}
				} else if (result.text == "else") {
					// And similarly, conjoin an "if" after "else" (to make "else if").
					var p = position;
					while (p < inputLength && (input[p]==' ' || input[p]=='\t')) p++;
					if (p+1 < inputLength && input.Substring(p,2) == "if" &&
							(p+2 >= inputLength || IsWhitespace(input[p+2]))) {
						result.text = "else if";
						position = p + 2;
					}
				}
				return result;
			} else if (c == '"') {
				// Lex a string... to the closing ", but skipping (and singling) a doubled double quote ("")
				result.tokenType = TokenType.String;
				bool haveDoubledQuotes = false;
				startPos = position;
				bool gotEndQuote = false;
				while (position < inputLength) {
					c = input[position++];
					if (c == '"') {
						if (position < inputLength && input[position] == '"') {
							// This is just a doubled quote.
							haveDoubledQuotes = true;
							position++;
						} else {
							// This is the closing quote, marking the end of the string.
							gotEndQuote = true;
							break;
						}
					}
				}
				if (!gotEndQuote) throw new LexerException("missing closing quote (\")");
				result.text = input.Substring(startPos, position-startPos-1);
				if (haveDoubledQuotes) result.text = result.text.Replace("\"\"", "\"");
				return result;

			} else {
				result.tokenType = TokenType.Unknown;
			}

			result.text = input.Substring(startPos, position - startPos);
			return result;
		}

		void SkipWhitespaceAndComment() {
			while (!AtEnd && IsWhitespace(input[position])) {
				position++;
			}

			if (position < input.Length - 1 && input[position] == '/' && input[position + 1] == '/') {
				// Comment.  Skip to end of line.
				position += 2;
				while (!AtEnd && input[position] != '\n') position++;
			}
		}
		
		public static bool IsNumeric(char c) {
			return c >= '0' && c <= '9';
		}

		public static bool IsIdentifier(char c) {
			return c == '_'
				|| (c >= 'a' && c <= 'z')
				|| (c >= 'A' && c <= 'Z')
				|| (c >= '0' && c <= '9')
				|| c > '\u009F';
		}

		public static bool IsWhitespace(char c) {
			return c == ' ' || c == '\t';
		}
		
		public bool IsAtWhitespace() {
			// Caution: ignores queue, and uses only current position
			return AtEnd || IsWhitespace(input[position]);
		}

		public static bool IsInStringLiteral(int charPos, string source, int startPos=0) {
			bool inString = false;
			for (int i=startPos; i<charPos; i++) {
				if (source[i] == '"') inString = !inString;
			}
			return inString;
		}

		public static int CommentStartPos(string source, int startPos) {
			// Find the first occurrence of "//" in this line that
			// is not within a string literal.
			int commentStart = startPos-2;
			while (true) {
				commentStart = source.IndexOf("//", commentStart + 2);
				if (commentStart < 0) break;	// no comment found
				if (!IsInStringLiteral(commentStart, source, startPos)) break;	// valid comment
			}
			return commentStart;
		}
		
		public static string TrimComment(string source) {
			int startPos = source.LastIndexOf('\n') + 1;
			int commentStart = CommentStartPos(source, startPos);
			if (commentStart >= 0) return source.Substring(startPos, commentStart - startPos);
			return source;
		}

		// Find the last token in the given source, ignoring any whitespace
		// or comment at the end of that line.
		public static Token LastToken(string source) {
			// Start by finding the start and logical  end of the last line.
			int startPos = source.LastIndexOf('\n') + 1;
			int commentStart = CommentStartPos(source, startPos);
			
			// Walk back from end of string or start of comment, skipping whitespace.
			int endPos = (commentStart >= 0 ? commentStart-1 : source.Length - 1);
			while (endPos >= 0 && IsWhitespace(source[endPos])) endPos--;
			if (endPos < 0) return Token.EOL;
			
			// Find the start of that last token.
			// There are several cases to consider here.
			int tokStart = endPos;
			char c = source[endPos];
			if (IsIdentifier(c)) {
				while (tokStart > startPos && IsIdentifier(source[tokStart-1])) tokStart--;
			} else if (c == '"') {
				bool inQuote = true;
				while (tokStart > startPos) {
					tokStart--;
					if (source[tokStart] == '"') {
						inQuote = !inQuote;
						if (!inQuote && tokStart > startPos && source[tokStart-1] != '"') break;
					}
				}
			} else if (c == '=' && tokStart > startPos) {
				char c2 = source[tokStart-1];
				if (c2 == '>' || c2 == '<' || c2 == '=' || c2 == '!') tokStart--;
			}
			
			// Now use the standard lexer to grab just that bit.
			Lexer lex = new Lexer(source);
			lex.position = tokStart;
			return lex.Dequeue();
		}

		public static void Check(Token tok, TokenType type, string text=null, int lineNum=0) {
			UnitTest.ErrorIfNull(tok);
			if (tok == null) return;
			UnitTest.ErrorIf(tok.tokenType != type, "Token type: expected "
						+ type + ", but got " + tok.tokenType);

			UnitTest.ErrorIf(text != null && tok.text != text,
						"Token text: expected " + text + ", but got " + tok.text);

		}

		public static void CheckLineNum(int actual, int expected) {
			UnitTest.ErrorIf(actual != expected, "Lexer line number: expected "
				+ expected + ", but got " + actual);
		}

		public static void RunUnitTests() {
			Lexer lex = new Lexer("42  * 3.14158");
			Check(lex.Dequeue(), TokenType.Number, "42");
			CheckLineNum(lex.lineNum, 1);
			Check(lex.Dequeue(), TokenType.OpTimes);
			Check(lex.Dequeue(), TokenType.Number, "3.14158");
			UnitTest.ErrorIf(!lex.AtEnd, "AtEnd not set when it should be");
			CheckLineNum(lex.lineNum, 1);

			lex = new Lexer("6*(.1-foo) end if // and a comment!");
			Check(lex.Dequeue(), TokenType.Number, "6");
			CheckLineNum(lex.lineNum, 1);
			Check(lex.Dequeue(), TokenType.OpTimes);
			Check(lex.Dequeue(), TokenType.LParen);
			Check(lex.Dequeue(), TokenType.Number, ".1");
			Check(lex.Dequeue(), TokenType.OpMinus);
			Check(lex.Peek(), TokenType.Identifier, "foo");
			Check(lex.Peek(), TokenType.Identifier, "foo");
			Check(lex.Dequeue(), TokenType.Identifier, "foo");
			Check(lex.Dequeue(), TokenType.RParen);
			Check(lex.Dequeue(), TokenType.Keyword, "end if");
			Check(lex.Dequeue(), TokenType.EOL);
			UnitTest.ErrorIf(!lex.AtEnd, "AtEnd not set when it should be");
			CheckLineNum(lex.lineNum, 1);

			lex = new Lexer("\"foo\" \"isn't \"\"real\"\"\" \"now \"\"\"\" double!\"");
			Check(lex.Dequeue(), TokenType.String, "foo");
			Check(lex.Dequeue(), TokenType.String, "isn't \"real\"");
			Check(lex.Dequeue(), TokenType.String, "now \"\" double!");
			UnitTest.ErrorIf(!lex.AtEnd, "AtEnd not set when it should be");

			lex = new Lexer("foo\nbar\rbaz\r\nbamf");
			Check(lex.Dequeue(), TokenType.Identifier, "foo");
			CheckLineNum(lex.lineNum, 1);
			Check(lex.Dequeue(), TokenType.EOL);
			Check(lex.Dequeue(), TokenType.Identifier, "bar");
			CheckLineNum(lex.lineNum, 2);
			Check(lex.Dequeue(), TokenType.EOL);
			Check(lex.Dequeue(), TokenType.Identifier, "baz");
			CheckLineNum(lex.lineNum, 3);
			Check(lex.Dequeue(), TokenType.EOL);
			Check(lex.Dequeue(), TokenType.Identifier, "bamf");
			CheckLineNum(lex.lineNum, 4);
			Check(lex.Dequeue(), TokenType.EOL);
			UnitTest.ErrorIf(!lex.AtEnd, "AtEnd not set when it should be");
			
			Check(LastToken("x=42 // foo"), TokenType.Number, "42");
			Check(LastToken("x = [1, 2, // foo"), TokenType.Comma);
			Check(LastToken("x = [1, 2 // foo"), TokenType.Number, "2");
			Check(LastToken("x = [1, 2 // foo // and \"more\" foo"), TokenType.Number, "2");
			Check(LastToken("x = [\"foo\", \"//bar\"]"), TokenType.RSquare);
			Check(LastToken("print 1 // line 1\nprint 2"), TokenType.Number, "2");			
			Check(LastToken("print \"Hi\"\"Quote\" // foo bar"), TokenType.String, "Hi\"Quote");			
		}
	}
}

