namespace Firefly.PSCloudFormation.LambdaPackaging.PEP508
{
    using sly.lexer;

    /// <summary>
    /// Lexer tokens
    /// </summary>
    internal enum MetadataToken
    {
        // float number
        [Lexeme(GenericToken.String, "'")]
        [Lexeme(GenericToken.String, "\"")]
        STRING = 1,

        [Lexeme(GenericToken.Identifier, IdentifierType.Custom, "A-Za-z_", "_0-9A-Za-z")]
        IDENTIFIER = 2,

        [Lexeme(GenericToken.KeyWord, "or")]
        OR = 10,

        [Lexeme(GenericToken.KeyWord, "and")]
        AND = 11,

        [Lexeme(GenericToken.KeyWord, "not")]
        NOT = 12,

        [Lexeme(GenericToken.KeyWord, "in")]
        IN = 13,

        [Lexeme(GenericToken.SugarToken, ">")]
        GREATER = 30,

        [Lexeme(GenericToken.SugarToken, "<")]
        LESSER = 31,

        [Lexeme(GenericToken.SugarToken, ">=")]
        GREATEREQUAL = 32,

        [Lexeme(GenericToken.SugarToken, "<=")]
        LESSEREQUAL = 33,

        [Lexeme(GenericToken.SugarToken, "==")]
        EQUALS = 34,

        [Lexeme(GenericToken.SugarToken, "!=")]
        NOTEQUALS = 35,

        [Lexeme(GenericToken.SugarToken, "<>")]
        ALTNOTEQUALS = 36,

        // a left paranthesis (
        [Lexeme(GenericToken.SugarToken, "(")]
        LPAREN = 40,

        // a right paranthesis )
        [Lexeme(GenericToken.SugarToken, ")")]
        RPAREN = 41,
    }
}