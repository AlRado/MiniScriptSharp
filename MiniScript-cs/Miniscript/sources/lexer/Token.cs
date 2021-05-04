namespace Miniscript.sources.lexer {

    public class Token {
        
        public static Token EOL = new Token() {tokenType = TokenType.EOL};

        public TokenType tokenType;
        public string text; // may be null for things like operators, whose text is fixed
        public bool afterSpace;

        public Token(TokenType type = TokenType.Unknown, string text = null) {
            this.tokenType = type;
            this.text = text;
        }

        public override string ToString() {
            return text == null ? tokenType.ToString() : $"{tokenType}({text})";
        }

    }

}