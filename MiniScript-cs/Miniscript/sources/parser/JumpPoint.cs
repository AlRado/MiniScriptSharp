namespace Miniscript.sources.parser {

    // JumpPoint: represents a place in the code we will need to jump to later
    // (typically, the top of a loop of some sort).
    class JumpPoint {

        public int lineNum; // line number to jump to		
        public string keyword; // jump type, by keyword: "while", "for", etc.

    }

}