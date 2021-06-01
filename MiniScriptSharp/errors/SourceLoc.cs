namespace MiniScriptSharp.Errors {

    public class SourceLoc {

        private readonly string context; // file name, etc. (optional)
        private readonly int lineNum;

        public SourceLoc(string context, int lineNum) {
            this.context = context;
            this.lineNum = lineNum;
        }

        public override string ToString() {
            return $"[{(string.IsNullOrEmpty(context) ? "" : context + " ")}line {lineNum}]";
        }

    }

}