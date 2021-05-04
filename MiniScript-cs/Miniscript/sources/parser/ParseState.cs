using System.Collections.Generic;
using System.Linq;
using Miniscript.sources.tac;
using Miniscript.sources.types;

namespace Miniscript.sources.parser {

		internal class ParseState {
			public List<Line> code = new List<Line>();
			public List<BackPatch> backpatches = new List<BackPatch>();
			public List<JumpPoint> jumpPoints = new List<JumpPoint>();
			public int nextTempNum = 0;

			public void Add(Line line) {
				code.Add(line);
			}

			/// <summary>
			/// Add the last code line as a backpatch point, to be patched
			/// (in rhsA) when we encounter a line with the given waitFor.
			/// </summary>
			/// <param name="waitFor">Wait for.</param>
			public void AddBackpatch(string waitFor) {
				backpatches.Add(new BackPatch() { lineNum=code.Count-1, waitingFor=waitFor });
			}

			public void AddJumpPoint(string jumpKeyword) {
				jumpPoints.Add(new JumpPoint() { lineNum = code.Count, keyword = jumpKeyword });
			}

			public JumpPoint CloseJumpPoint(string keyword) {
				int idx = jumpPoints.Count - 1;
				if (idx < 0 || jumpPoints[idx].keyword != keyword) {
					throw new CompilerException(string.Format("'end {0}' without matching '{0}'", keyword));
				}
				JumpPoint result = jumpPoints[idx];
				jumpPoints.RemoveAt(idx);
				return result;
			}

			// Return whether the given line is a jump target.
			public bool IsJumpTarget(int lineNum) {
				for (int i=0; i < code.Count; i++) {
					var op = code[i].op;
					if ((op == Line.Op.GotoA || op == Line.Op.GotoAifB 
					 || op == Line.Op.GotoAifNotB || op == Line.Op.GotoAifTrulyB)
					 && code[i].rhsA is ValNumber && code[i].rhsA.IntValue() == lineNum) return true;
				}
				for (int i=0; i<jumpPoints.Count(); i++) {
					if (jumpPoints[i].lineNum == lineNum) return true;
				}
				return false;
			}

			/// <summary>
			/// Call this method when we've found an 'end' keyword, and want
			/// to patch up any jumps that were waiting for that.  Patch the
			/// matching backpatch (and any after it) to the current code end.
			/// </summary>
			/// <param name="keywordFound">Keyword found.</param>
			/// <param name="reservingLines">Extra lines (after the current position) to patch to.</param> 
			public void Patch(string keywordFound, int reservingLines=0) {
				Patch(keywordFound, false, reservingLines);
			}

			/// <summary>
			/// Call this method when we've found an 'end' keyword, and want
			/// to patch up any jumps that were waiting for that.  Patch the
			/// matching backpatch (and any after it) to the current code end.
			/// </summary>
			/// <param name="keywordFound">Keyword found.</param>
			/// <param name="alsoBreak">If true, also patch "break"; otherwise skip it.</param> 
			/// <param name="reservingLines">Extra lines (after the current position) to patch to.</param> 
			public void Patch(string keywordFound, bool alsoBreak, int reservingLines=0) {
				Value target = TAC.Num(code.Count + reservingLines);
				bool done = false;
				for (int idx = backpatches.Count - 1; idx >= 0 && !done; idx--) {
					bool patchIt = false;
					if (backpatches[idx].waitingFor == keywordFound) patchIt = done = true;
					else if (backpatches[idx].waitingFor == "break") {
						// Not the expected keyword, but "break"; this is always OK,
						// but we may or may not patch it depending on the call.
						patchIt = alsoBreak;
					} else {
						// Not the expected patch, and not "break"; we have a mismatched block start/end.
						throw new CompilerException("'" + keywordFound + "' skips expected '" + backpatches[idx].waitingFor + "'");
					}
					if (patchIt) {
						code[backpatches[idx].lineNum].rhsA = target;
						backpatches.RemoveAt(idx);
					}
				}
				// Make sure we found one...
				if (!done) throw new CompilerException("'" + keywordFound + "' without matching block starter");
			}

			/// <summary>
			/// Patches up all the branches for a single open if block.  That includes
			/// the last "else" block, as well as one or more "end if" jumps.
			/// </summary>
			public void PatchIfBlock() {
				Value target = TAC.Num(code.Count);

				int idx = backpatches.Count - 1;
				while (idx >= 0) {
					BackPatch bp = backpatches[idx];
					if (bp.waitingFor == "if:MARK") {
						// There's the special marker that indicates the true start of this if block.
						backpatches.RemoveAt(idx);
						return;
					} else if (bp.waitingFor == "end if" || bp.waitingFor == "else") {
						code[bp.lineNum].rhsA = target;
						backpatches.RemoveAt(idx);
					} else if (backpatches[idx].waitingFor == "break") {
						// Not the expected keyword, but "break"; this is always OK.
					} else {
						// Not the expected patch, and not "break"; we have a mismatched block start/end.
						throw new CompilerException("'end if' without matching 'if'");
					}
					idx--;
				}
				// If we get here, we never found the expected if:MARK.  That's an error.
				throw new CompilerException("'end if' without matching 'if'");
			}
		}
		

}