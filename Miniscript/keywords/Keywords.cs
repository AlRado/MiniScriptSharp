/*	MiniscriptKeywords.cs

This file defines a little Keywords class, which contains all the 
MiniScript reserved words (break, for, etc.).  It might be useful 
if you are doing something like syntax coloring, or want to make 
sure some user-entered identifier isn’t going to conflict with a 
reserved word.

*/

using System;

namespace Miniscript.keywords {
	public static class Keywords {
		public static readonly string[] All = {
			Consts.BREAK,
			Consts.CONTINUE,
			Consts.ELSE,
			Consts.END,
			Consts.FOR,
			Consts.FUNCTION,
			Consts.IF,
			Consts.IN,
			Consts.ISA,
			Consts.NEW,
			Consts.NULL,
			Consts.THEN,
			Consts.REPEAT,
			Consts.RETURN,
			Consts.WHILE,
			Consts.AND,
			Consts.OR,
			Consts.NOT,
			Consts.TRUE,
			Consts.FALSE
		};

		public static bool IsKeyword(string text) {
			return Array.IndexOf(All, text) >= 0;
		}

	}
}

