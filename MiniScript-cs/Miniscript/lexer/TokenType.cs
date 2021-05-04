namespace Miniscript.sources.lexer {

    public enum TokenType {

        Unknown,
        Keyword,
        Number,
        String,
        Identifier,
        OpAssign,
        OpPlus,
        OpMinus,
        OpTimes,
        OpDivide,
        OpMod,
        OpPower,
        OpEqual,
        OpNotEqual,
        OpGreater,
        OpGreatEqual,
        OpLesser,
        OpLessEqual,
        LParen,
        RParen,
        LSquare,
        RSquare,
        LCurly,
        RCurly,
        AddressOf,
        Comma,
        Dot,
        Colon,
        Comment,
        EOL

    }

}