namespace MiniScriptSharp.Parse {

    /// <summary>
    /// JumpPoint: represents a place in the code we will need to jump to later
    /// (typically, the top of a loop of some sort).
    /// </summary>
    internal class JumpPoint {

        public int LineNum; // line number to jump to		
        public string Keyword; // jump type, by keyword: "while", "for", etc.

    }

}