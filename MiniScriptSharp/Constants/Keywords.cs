/*
 * Keywords.cs
 * 
 * This file defines a little Keywords class, which contains all the 
 * MiniScript reserved words (break, for, etc.).  It might be useful 
 * if you are doing something like syntax coloring, or want to make 
 * sure some user-entered identifier isn’t going to conflict with a 
 * reserved word.
 */

using System;
using static MiniScriptSharp.Constants.Consts;

namespace MiniScriptSharp.Constants {
	
	public static class Keywords {
		public static readonly string[] All = {
			BREAK,
			CONTINUE,
			ELSE,
			END,
			FOR,
			FUNCTION,
			IF,
			IN,
			ISA,
			NEW,
			NULL,
			THEN,
			REPEAT,
			RETURN,
			WHILE,
			AND,
			OR,
			NOT,
			TRUE,
			FALSE
		};

		public static bool IsKeyword(string text) {
			return Array.IndexOf(All, text) >= 0;
		}

	}
}

