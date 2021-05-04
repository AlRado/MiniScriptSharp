namespace Miniscript.errors {

    public class SourceLoc {

        public string context; // file name, etc. (optional)
        public int lineNum;

        public SourceLoc(string context, int lineNum) {
            this.context = context;
            this.lineNum = lineNum;
        }

        public override string ToString() {
            return $"[{(string.IsNullOrEmpty(context) ? "" : context + " ")}line {lineNum}]";
        }

    }

}