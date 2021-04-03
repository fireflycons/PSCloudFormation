// ReSharper disable UnusedMember.Global - All methods are invoked by reflection
namespace Firefly.PSCloudFormation.LambdaPackaging.PEP508
{
    using Firefly.PSCloudFormation.LambdaPackaging.PEP508.Model;

    using sly.lexer;
    using sly.parser.generator;

    /// <summary>
    /// Simple PEP508 parser.
    /// Currently only parses the python-like expression following semicolon in a <c>Requires-Dist</c> statement
    /// </summary>
    // ReSharper disable once StyleCop.SA1600
    // ReSharper disable once InconsistentNaming
    internal class PEP508Parser
    {
        /*
            <logical_expression>    ::= <logical_expression> "or" <and_expression>
                                    | <and_expression>

            <and_expression>        ::= <and_expression> "and" <not_expression>
                                    | <not_expression>

            <not_expression>        ::= "not" <relation>
                                    | <relation>
                   
            <relation>              ::= <relation> "==" <group>
                                    | <relation> "!=" <group>
                                    | <relation> "<>" <group>
                                    | <relation> ">" <group>
                                    | <relation> ">=" <group>
                                    | <relation> "<" <group>
                                    | <relation> "<=" <group>
                                    | <group>
                       
            <group>                 ::= <id>
                                    | "(" <logical_expression> ")"

            <id> ::= string or variable name
        */

        /// <summary>
        /// Root rule. Lowest precedence is OR
        /// </summary>
        /// <param name="left">The left expression.</param>
        /// <param name="operatorToken">The operator token.</param>
        /// <param name="right">The right expression.</param>
        /// <returns>New <see cref="BinaryOperation"/> expression for the OR</returns>
        [Production("logical_expression: and_expression OR logical_expression")]
        public IExpression Or(IExpression left, Token<MetadataToken> operatorToken, IExpression right)
        {
            return new BinaryOperation(left, operatorToken.TokenID, right);
        }

        /// <summary>
        /// AND production.
        /// </summary>
        /// <param name="left">The left expression.</param>
        /// <param name="operatorToken">The operator token.</param>
        /// <param name="right">The right expression.</param>
        /// <returns>New <see cref="BinaryOperation"/> expression for the AND</returns>
        [Production("and_expression: not_expression AND and_expression")]
        public IExpression And(IExpression left, Token<MetadataToken> operatorToken, IExpression right)
        {
            return new BinaryOperation(left, operatorToken.TokenID, right);
        }

        /// <summary>
        /// NOT production
        /// </summary>
        /// <param name="operatorToken">The operator token.</param>
        /// <param name="right">The right expression.</param>
        /// <returns>New <see cref="UnaryOperation"/> expression for the AND</returns>
        [Production("not_expression: NOT relation")]
        public IExpression Not(Token<MetadataToken> operatorToken, IExpression right)
        {
            return new UnaryOperation(operatorToken.TokenID, right);
        }

        /// <summary>
        /// RELATION production. Handles all comparison operators
        /// </summary>
        /// <param name="left">The left expression.</param>
        /// <param name="operatorToken">The operator token.</param>
        /// <param name="right">The right expression.</param>
        /// <returns>New <see cref="BinaryOperation"/> expression for the comparison</returns>
        [Production("relation : group GREATER relation")]
        [Production("relation : group LESSER relation")]
        [Production("relation : group GREATEREQUAL relation")]
        [Production("relation : group LESSEREQUAL relation")]
        [Production("relation : group EQUALS relation")]
        [Production("relation : group NOTEQUALS relation")]
        [Production("relation : group ALTNOTEQUALS relation")]
        [Production("relation : group IN relation")]
        public IExpression RelationFactor(IExpression left, Token<MetadataToken> operatorToken, IExpression right)
        {
            return new BinaryOperation(left, operatorToken.TokenID, right);
        }

        /// <summary>
        /// GROUP production. Handles expressions in parentheses.
        /// </summary>
        /// <param name="ingnore1">Ignored left parenthesis token.</param>
        /// <param name="groupValue">The group value.</param>
        /// <param name="ignore2">Ignored right parenthesis token.</param>
        /// <returns>New <see cref="Group"/> expression for the bracketed expression.</returns>
        [Production("group: LPAREN logical_expression RPAREN")]
        public IExpression Group(Token<MetadataToken> ingnore1, IExpression groupValue, Token<MetadataToken> ignore2)
        {
            return new Group(groupValue);
        }

        [Production("logical_expression: and_expression")]
        [Production("and_expression: not_expression")]
        [Production("not_expression: relation")]
        [Production("relation: group")]
        [Production("group : primary")]
        public IExpression Expression(IExpression primValue)
        {
            return primValue;
        }

        /// <summary>
        /// Matches a variable name and creates a variable expression to evaluate it
        /// </summary>
        /// <param name="idToken">The identifier token.</param>
        /// <returns>New <see cref="Variable"/> expression.</returns>
        [Production("primary: IDENTIFIER")]
        public IExpression PrimaryIdentifier(Token<MetadataToken> idToken)
        {
            return new Variable(idToken.StringWithoutQuotes);
        }

        /// <summary>
        /// Matches a string literal and creates a constant expression to get its value
        /// </summary>
        /// <param name="stringToken">The string token.</param>
        /// <returns>New <see cref="StringLiteral"/> expression.</returns>
        [Production("primary: STRING")]
        public IExpression PrimaryNumber(Token<MetadataToken> stringToken)
        {
            return new StringLiteral(stringToken.StringWithoutQuotes);
        }
    }
}