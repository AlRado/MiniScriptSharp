using System.Collections.Generic;
using System.Linq;
using MiniScriptSharp.Constants;
using MiniScriptSharp.Errors;
using MiniScriptSharp.Tac;
using MiniScriptSharp.Types;

namespace MiniScriptSharp.Parse {

		internal class ParseState {
			public List<Line> Code = new List<Line>();
			public List<BackPatch> BackPatches = new List<BackPatch>();
			public List<JumpPoint> JumpPoints = new List<JumpPoint>();
			public int nextTempNum = 0;

			public void Add(Line line) {
				Code.Add(line);
			}

			/// <summary>
			/// Add the last code line as a backpatch point, to be patched
			/// (in rhsA) when we encounter a line with the given waitFor.
			/// </summary>
			/// <param name="waitFor">Wait for.</param>
			public void AddBackPatch(string waitFor) {
				BackPatches.Add(new BackPatch() { LineNum=Code.Count-1, WaitingFor=waitFor });
			}

			public void AddJumpPoint(string jumpKeyword) {
				JumpPoints.Add(new JumpPoint() { LineNum = Code.Count, Keyword = jumpKeyword });
			}

			public JumpPoint CloseJumpPoint(string keyword) {
				var idx = JumpPoints.Count - 1;
				if (idx < 0 || JumpPoints[idx].Keyword != keyword) {
					throw new CompilerException($"'end {keyword}' without matching '{keyword}'");
				}
				var result = JumpPoints[idx];
				JumpPoints.RemoveAt(idx);
				return result;
			}

			// Return whether the given line is a jump target.
			public bool IsJumpTarget(int lineNum) {
				for (int i=0; i < Code.Count; i++) {
					var op = Code[i].Op;
					if ((op == Op.GotoA || op == Op.GotoAifB 
					 || op == Op.GotoAifNotB || op == Op.GotoAifTrulyB)
					 && Code[i].RhsA is ValNumber && Code[i].RhsA.IntValue() == lineNum) return true;
				}
				for (int i=0; i<JumpPoints.Count(); i++) {
					if (JumpPoints[i].LineNum == lineNum) return true;
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
				Value target = TAC.Num(Code.Count + reservingLines);
				var done = false;
				for (int idx = BackPatches.Count - 1; idx >= 0 && !done; idx--) {
					var patchIt = false;
					if (BackPatches[idx].WaitingFor == keywordFound) patchIt = done = true;
					else if (BackPatches[idx].WaitingFor == Consts.BREAK) {
						// Not the expected keyword, but "break"; this is always OK,
						// but we may or may not patch it depending on the call.
						patchIt = alsoBreak;
					} else {
						// Not the expected patch, and not "break"; we have a mismatched block start/end.
						throw new CompilerException("'" + keywordFound + "' skips expected '" + BackPatches[idx].WaitingFor + "'");
					}
					if (patchIt) {
						Code[BackPatches[idx].LineNum].RhsA = target;
						BackPatches.RemoveAt(idx);
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
				Value target = TAC.Num(Code.Count);

				var idx = BackPatches.Count - 1;
				while (idx >= 0) {
					var bp = BackPatches[idx];
					switch (bp.WaitingFor) {
						case Consts.IF_MARK:
							// There's the special marker that indicates the true start of this if block.
							BackPatches.RemoveAt(idx);
							return;
						case Consts.END_IF:
						case Consts.ELSE:
							Code[bp.LineNum].RhsA = target;
							BackPatches.RemoveAt(idx);
							break;
						default: {
							if (BackPatches[idx].WaitingFor == Consts.BREAK) {
								// Not the expected keyword, but "break"; this is always OK.
							} else {
								// Not the expected patch, and not "break"; we have a mismatched block start/end.
								throw new CompilerException("'end if' without matching 'if'");
							}

							break;
						}
					}
					idx--;
				}
				// If we get here, we never found the expected if:MARK.  That's an error.
				throw new CompilerException("'end if' without matching 'if'");
			}
		}
}