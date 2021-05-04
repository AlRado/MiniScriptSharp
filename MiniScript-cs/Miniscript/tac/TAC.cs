/*	MiniscriptTAC.cs

This file defines the three-address code (TAC) which represents compiled
Minisript code.  TAC is sort of a pseudo-assembly language, composed of
simple instructions containing an opcode and up to three variable/value 
references.

This is all internal Minisript virtual machine code.  You don't need to
deal with it directly (see MiniscriptInterpreter instead).

*/

using System;
using System.Collections.Generic;
using Miniscript.intrinsic;
using Miniscript.types;

namespace Miniscript.tac {

	public static class TAC {

		public static void Dump(List<Line> lines, int lineNumToHighlight, int indent=0) {
			var lineNum = 0;
			foreach (var line in lines) {
				var s = (lineNum == lineNumToHighlight ? "> " : "  ") + (lineNum++) + ". ";
				Console.WriteLine(s + line);
				if (line.op != Line.Op.BindAssignA) continue;
				
				var func = (ValFunction)line.rhsA;
				Dump(func.function.code, -1, indent+1);
			}
		}

		public static ValTemp LTemp(int tempNum) {
			return new ValTemp(tempNum);
		}
		
		public static ValVar LVar(string identifier) {
			return identifier == "self" ? ValVar.self : new ValVar(identifier);
		}
		
		public static ValTemp RTemp(int tempNum) {
			return new ValTemp(tempNum);
		}
		
		public static ValNumber Num(double value) {
			return new ValNumber(value);
		}
		
		public static ValString Str(string value) {
			return new ValString(value);
		}
		
		public static ValNumber IntrinsicByName(string name) {
			return new ValNumber(Intrinsic.GetByName(name).id);
		}
		
	}
}

