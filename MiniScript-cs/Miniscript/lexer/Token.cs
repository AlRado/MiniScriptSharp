namespace Miniscript.lexer {

    public class Token {
        
        public static readonly Token EOL = new Token() {TokenType = TokenType.EOL};

        public TokenType TokenType;
        public string Text; // may be null for things like operators, whose text is fixed
        public bool AfterSpace;

        public Token(TokenType type = TokenType.Unknown, string text = null) {
            this.TokenType = type;
            this.Text = text;
        }

        public override string ToString() {
            return Text == null ? TokenType.ToString() : $"{TokenType}({Text})";
        }

    }

}