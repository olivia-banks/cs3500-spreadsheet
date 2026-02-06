namespace FormulaTests;

using Formula;

/// <summary>
///     <para>
///         Tests various rules and chokepoints associated with the Formula classe,
///         by way of correct and incorrect inputs.
///     </para>
/// </summary>
[TestClass]
public class FormulaTests
{
    // NOTE: Many tests here have been moved into their respective test classes in FormulaTests.Expressions
    //  to improve organization and clarity. This class now primarily serves to hold integration tests
    //  that cover multiple rules at once.
    
    // GRADER: Tests covering canonicalization, dependency searching, evaluation, and hashing may be found
    //  in the FormulaTests.Expressions namespace, in their respective test classes.

    /// <summary>
    ///     <para>
    ///         Does we handle zero-division errors correctly by returning a <see cref="FormulaError"/>
    ///     </para>
    /// </summary>
    [TestMethod]
    public void Formula_Evaluate_HandlesDivideByZero_ReturnsFormulaError()
    {
        var formula = new Formula("4 / 0");
        var result = formula.Evaluate(s => 0);
        
        Assert.IsInstanceOfType(result, typeof(FormulaError));
    }
    
    /// <summary>
    ///     <para>
    ///         Test that <see cref="Formula"/> can handle equality comparisons with another <see cref="Formula"/>
    ///         that is mathematically equivalent but in a different form, without throwing exceptions or being
    ///         incorrect.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void FormulaEquals_HandlesDifferentFormsCorrectly_ReturnsTrue()
    {
        var formulaA = new Formula("4 / 1");
        var formulaB = new Formula("4e0 / 1.0");
        
        Assert.IsTrue(formulaA.Equals(formulaB));
    }
    
    /// <summary>
    ///     <para>
    ///         Test that <see cref="Formula"/> can handle equality comparisons with something that isn't a
    ///         <see cref="Formula"/> without throwing exceptions or being incorrect.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void FormulaEquals_HandlesNonFormulaComparison_ReturnsFalse()
    {
        var formula = new Formula("4 / 1");
        const string nonFormula = "4 / 1";
        
        // ReSharper disable once SuspiciousTypeConversion.Global
        Assert.IsFalse(formula.Equals(nonFormula));
    }

    /// <summary>
    ///     <para>
    ///         Test that <see cref="Formula"/> can handle equality comparisons with null without throwing
    ///         exceptions or being incorrect.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void FormulaEquals_HandlesNullComparison_ReturnsFalse()
    {
        var formula = new Formula("4 / 1");

        Assert.IsFalse(formula.Equals(null));
    }
    
    /// <summary>
    ///     <para>
    ///         Test that <see cref="Formula"/> can handle equality comparisons with itself without throwing
    ///         exceptions or being incorrect.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void FormulaEquals_HandlesSelfComparison_ReturnsTrue()
    {
        var formula = new Formula("4 / 1");
        
        // ReSharper disable once EqualExpressionComparison
        Assert.IsTrue(formula == formula);
    }
    
    /// <summary>
    ///     <para>
    ///         Test that <see cref="Formula"/> can handle equality comparisons another formula, when there is a
    ///         bad formula in the mix, without throwing exceptions or being incorrect.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void FormulaEquals_HandlesComparisonWithBadFormula_ReturnsFalse()
    {
        var formulaA = new Formula("4 / 1");
        var formulaB = new Formula("4 / 0");
        
        Assert.IsTrue(formulaA != formulaB);
    }
    
    /// <summary>
    ///     <para>
    ///         Test that <see cref="Formula"/> returns a stable hash code for the same formula, even if it is in a
    ///         different form. This is primarily a test for the consistency of the hash code implementation.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void FormulaGetHashCode_HandlesEquivalentFormulas_ReturnsSameHashCode()
    {
        var formulaA = new Formula("4 / 1");
        var formulaB = new Formula("4e0 / 1.0");
        
        Assert.AreEqual(formulaA.GetHashCode(), formulaB.GetHashCode());
    }
    
    /// <para>
    ///     <summary>
    ///         Test that <see cref="Formula"/> returns the same hash code for the same formula, when called multiple
    ///         times. This is primarily a test for the stability of the hash code implementation.
    ///     </summary>
    /// </para>
    [TestMethod]
    public void FormulaGetHashCode_HandlesMultipleCalls_ReturnsSameHashCode()
    {
        var formula = new Formula("4 / 1");
        var hashCode1 = formula.GetHashCode();
        var hashCode2 = formula.GetHashCode();
        
        Assert.AreEqual(hashCode1, hashCode2);
    }
    
    /// <para>
    ///     <summary>
    ///         Test that <see cref="Formula.GetVariables"/> returns a correct, non-duplicated set of references
    ///         cells.
    ///     </summary>
    /// </para>
    [TestMethod]
    public void FormulaGetVariables_ReturnsCorrectSetOfVariables()
    {
        var formula = new Formula("x1 + y2 - z3 + x1 * y2");
        var variables = formula.GetVariables();
        
        Assert.IsTrue(variables.SetEquals([ "X1", "Y2", "Z3" ]));
    }
    
    /// <para>
    ///     <summary>
    ///         Test that <see cref="Formula.Evaluate" /> is able to handle a formula that references variables, and
    ///         that it correctly uses the provided lookup function to evaluate the formula. This is primarily a test
    ///         for the integration of variable lookup and evaluation.
    ///     </summary>
    /// </para>
    [TestMethod]
    public void FormulaEvaluate_HandlesVariableReferences_ReturnsCorrectValue()
    {
        var formula = new Formula("x1 + y2 - z3");
        var result = formula.Evaluate(s =>
        {
            return s switch
            {
                "X1" => 10,
                "Y2" => 5,
                "Z3" => 3,
                _ => throw new ArgumentException("Unexpected variable")
            };
        });
        
        Assert.AreEqual(12.0, result);
    }

    /// <summary>
    ///     <para>
    ///         These are integration tests, as written for PS1. There are too many tests to check the
    ///         outcomes of every rule individually, so these tests generally check that formulas
    ///         that should be valid are accepted, and formulas that should be invalid are rejected.
    ///     </para>
    ///     <para>
    ///         It is assumed that if these tests pass, the individual rules are being enforced
    ///         correctly, and all we need to do is check some internal state like with <see cref="Formula.ToString"/>
    ///         and <see cref="Formula.GetVariables"/>.
    ///     </para>
    ///     <para>
    ///         Generally speaking, this is not how I would write tests. I would do integration testing,
    ///         and then validate that internal program states change how I expect them to when given a
    ///         set of inputs, or to directly stoke rare code paths.
    ///     </para>
    /// </summary>
    [TestClass]
    public class ParsingIntegrationTests
    {
        /// <summary>
        ///     <para>
        ///         Test that a single valid variable token is accepted. This is primarily a test for rule
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestOnlyVariableReference_IsValid()
        {
            _ = new Formula(@"x4 + (4)");
        }


        /// <summary>
        ///     <para>
        ///         Test that a single numeric literal is accepted. This is primarily a test for rule
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestOnlyNumericLiteral_IsValid()
        {
            _ = new Formula(@"42");
        }


        /// <summary>
        ///     <para>
        ///         Test that an empty formula string is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestNoTokens_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@""));
        }


        /// <summary>
        ///     <para>
        ///         Test that a formula containing only whitespace is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestWhitespaceOnly_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"  "));
        }


        /// <summary>
        ///     <para>
        ///         Test that a formula containing only escape characters is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestEscapeCharactersOnly_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"\r\n\t"));
        }


        /// <summary>
        ///     <para>
        ///         Test that a formula containing only nonstandard whitespace is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestSpecialWhitespaceOnly_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"​ "));
        }


        /// <summary>
        ///     <para>
        ///         Test that mixed standard and special whitespace is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestSpecialAndTypicalMixWhitespace_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@" ​  "));
        }


        /// <summary>
        ///     <para>
        ///         Test that a formula containing only valid tokens is accepted. This is primarily a test for rule
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestFormulaOfValidTokens_IsValid()
        {
            _ = new Formula(@"42 + 9 / 4 * (4 + 4)");
        }


        /// <summary>
        ///     <para>
        ///         Test that a single invalid token is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestInvalidTokenAsOnlyToken_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"@"));
        }


        /// <summary>
        ///     <para>
        ///         Test that an invalid token following an operator is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestInvalidTokenAfterOperator_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"3 + #"));
        }


        /// <summary>
        ///     <para>
        ///         Test that a special character following an operator is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestInvalidFreezeOperatorAfterOperator_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"98 + $"));
        }


        /// <summary>
        ///     <para>
        ///         Test that an invalid token inside parentheses is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestInvalidTokenAsIllegalSecondaryOperator_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"(5 + ~3)"));
        }


        /// <summary>
        ///     <para>
        ///         Test that an invalid token following a number is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestInvalidTokenAfterNumericLiteral_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"8 % (3 + 1)"));
        }


        /// <summary>
        ///     <para>
        ///         Test that an invalid token following a variable is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestInvalidTokenAfterVariableReference_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"x3 & y2"));
        }


        /// <summary>
        ///     <para>
        ///         Test that an invalid first token is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestInvalidTokenAsFirstToken_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"& 8"));
        }


        /// <summary>
        ///     <para>
        ///         Test that an invalid last token is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestInvalidTokenAsLastToken_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"42 !"));
        }


        /// <summary>
        ///     <para>
        ///         Test that a number immediately followed by an invalid character is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestNumericLiteralImmediatelyFollowedByInvalidChar_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"42#"));
        }


        /// <summary>
        ///     <para>
        ///         Test that a number preceded by an invalid character is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestNumericLiteralPrecededByInvalidChar_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"@1"));
        }


        /// <summary>
        ///     <para>
        ///         Test that special characters embedded in numeric tokens are rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestSpecialCharInsideNumericTokens_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"2%7"));
        }


        /// <summary>
        ///     <para>
        ///         Test that numeric literals with multiple decimals are rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestMultiDecimalNumericLiteral_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"2 + 1.2.3"));
        }


        /// <summary>
        ///     <para>
        ///         Test that properly closed parentheses are accepted. This is primarily a test for rule
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestFormulaWithCorrectlyClosedParenthesis_IsValid()
        {
            _ = new Formula(@"9 + (0)");
        }


        /// <summary>
        ///     <para>
        ///         Test that a formula starting with a closing parenthesis is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestStartWithClosingParenthesisAndImbalanced_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@")1 + 4 * 2"));
        }


        /// <summary>
        ///     <para>
        ///         Test that starting with a closing parenthesis is rejected even if counts balance. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestStartWithClosingParenthesisAndBalanced_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@")4 + 2 * 1("));
        }


        /// <summary>
        ///     <para>
        ///         Test that a closing parenthesis without a matching opening parenthesis is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void
            FormulaConstructor_TestClosingParenthesisAfterOperatorWithoutOpeningParenthesis_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"x1 / ) + 1"));
        }


        /// <summary>
        ///     <para>
        ///         Test that an unmatched closing parenthesis is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestClosingParenthesisWithoutOpeningParenthesis_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"5 + 3)"));
        }


        /// <summary>
        ///     <para>
        ///         Test that extra closing parentheses are rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestClosingParenthesisAtEndAndImbalanced_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"(5 + 3))"));
        }


        /// <summary>
        ///     <para>
        ///         Test that balanced but improperly ordered parentheses are rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestBalancedParenthesisYetOtherwiseMismatched_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"1 + )2( + 3"));
        }


        /// <summary>
        ///     <para>
        ///         Test that missing closing parentheses are rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestStartWithOpeningParenthesisAndImbalanced_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"(1 + (2) + 3"));
        }


        /// <summary>
        ///     <para>
        ///         Test that nested balanced parentheses are accepted. This is primarily a test for rule
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestFormulaWithNestedBalancedParenthesis_IsValid()
        {
            _ = new Formula(@"(1 + 3 / (3 - 2) * 5)");
        }


        /// <summary>
        ///     <para>
        ///         Test that missing opening parentheses are rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestImbalancedMissingOpeningParenthesis_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"(1 + 2"));
        }


        /// <summary>
        ///     <para>
        ///         Test that missing nested opening parentheses are rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestImbalancedMissingNestedOpeningParenthesis_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"((x9 * y6) + z4"));
        }


        /// <summary>
        ///     <para>
        ///         Test that a formula starting with a valid token is accepted.  This is primarily a test for rule
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestFormulaWithValidFirstToken_IsValid()
        {
            _ = new Formula(@"(9 + 9)");
        }


        /// <summary>
        ///     <para>
        ///         Test that a formula starting with an operator is rejected.  This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestInvalidFirstTokenFollowedByNumericLiteral_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"+ 5"));
        }


        /// <summary>
        ///     <para>
        ///         Test that a leading operator before a variable is rejected.  This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestInvalidFirstTokenFollowedByVariableReference_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"- x6"));
        }


        /// <summary>
        ///     <para>
        ///         Test that a leading operator before parentheses is rejected.  This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestInvalidFirstTokenFollowedByParenthesis_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"* (3 + 2)"));
        }


        /// <summary>
        ///     <para>
        ///         Test that a formula ending with a variable is accepted. This is primarily a test for rule
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestFormulaWithValidLastToken_IsValid()
        {
            _ = new Formula(@"4 + r2");
        }


        /// <summary>
        ///     <para>
        ///         Test that a formula ending with an operator after a number is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestInvalidLastTokenPrecededByNumericLiteral_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"42 +"));
        }


        /// <summary>
        ///     <para>
        ///         Test that a formula ending with an operator after a variable is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestInvalidLastTokenPrecededByVariableReference_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"- y7"));
        }


        /// <summary>
        ///     <para>
        ///         Test that a formula ending with an operator after parentheses is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestInvalidLastTokenPrecededByParenthesis_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"(x1 - y1) *"));
        }


        /// <summary>
        ///     <para>
        ///         Test that a valid subexpression inside parentheses is accepted. This is primarily a test for rule
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestFormulaWithCorrectlyFormedSubexpressionInsideParenthesis_IsValid()
        {
            _ = new Formula(@"u3 + (3 / z2)");
        }


        /// <summary>
        ///     <para>
        ///         Test that an operator immediately following an opening parenthesis is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestOperatorAfterOpeningParenthesis_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"( + 5)"));
        }


        /// <summary>
        ///     <para>
        ///         Test that empty parentheses are rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestClosingParenthesisAfterOpeningParenthesis_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"( )"));
        }


        /// <summary>
        ///     <para>
        ///         Test that consecutive operators are rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestOperatorAfterOperator_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"5 + - 3"));
        }


        /// <summary>
        ///     <para>
        ///         Test that a closing parenthesis immediately after an operator is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestClosingParenthesisAfterOperator_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"(5 + )"));
        }


        /// <summary>
        ///     <para>
        ///         Test that nested empty parentheses are rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void
            FormulaConstructor_TestClosingParenthesisAfterOpeningParenthesisNested_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"( ( ) )"));
        }


        /// <summary>
        ///     <para>
        ///         Test that a valid token following a closing parenthesis is accepted. This is primarily a test for rule
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestFormulaWithCorrectlyFormedSubexpressionPostParenthesis_IsValid()
        {
            _ = new Formula(@"4 + (3 / t4) * q8");
        }


        /// <summary>
        ///     <para>
        ///         Test that two numbers without an operator between them are rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestNumericLiteralAfterNumericLiteral_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"5 3"));
        }


        /// <summary>
        ///     <para>
        ///         Test that two variables without an operator between them are rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestVariableReferenceAfterVariableReference_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"x1 y1"));
        }


        /// <summary>
        ///     <para>
        ///         Test that an opening parenthesis immediately following a closing parenthesis is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestOpeningParenthesisAfterClosingParenthesis_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"(5) ("));
        }


        /// <summary>
        ///     <para>
        ///         Test that an opening parenthesis immediately following a number is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestOpeningParenthesisAfterNumericLiteral_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"5 (3 + 2)"));
        }


        /// <summary>
        ///     <para>
        ///         Test that a number immediately following a variable is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestNumericLiteralAfterVariableReference_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"x3 10"));
        }


        /// <summary>
        ///     <para>
        ///         Test that a variable immediately following a closing parenthesis is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestVariableReferenceAfterClosingParenthesis_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"(y5) z7"));
        }


        /// <summary>
        ///     <para>
        ///         Test that an opening parenthesis immediately following a variable is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestOpeningParenthesisAfterVariable_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"a3 (b1)"));
        }


        /// <summary>
        ///     <para>
        ///         Test that a number immediately following a closing parenthesis is rejected. This is primarily a test for rule
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestNumericLiteralAfterClosingParenthesis_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"(1 + 2) 3"));
        }


        /// <summary>
        ///     <para>
        ///         Test a formula containing a negative decimal literal, and ensure it's rejected. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestNegativeDecimalNumericLiteral_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"2 + -4"));
        }


        /// <summary>
        ///     <para>
        ///         Test that a formula containing only an operator is rejected. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestOnlySingleOperator_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"+"));
        }


        /// <summary>
        ///     <para>
        ///         Test that empty parentheses are rejected. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestOnlyEmptyParenthesis_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"()"));
        }


        /// <summary>
        ///     <para>
        ///         Test that empty parentheses containing whitespace are rejected. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestOnlyEmptyParenthesisWithSpace_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"( )"));
        }


        /// <summary>
        ///     <para>
        ///         Test that empty parentheses used as an operator operand are rejected. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestOperatorArgumentIsEmptyParenthesis_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"2 + ()"));
        }


        /// <summary>
        ///     <para>
        ///         Test that a very long but valid variable name is reported as too long. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestExtremelyLongVariableReference_IsTooLong()
        {
            Assert.ThrowsExactly<FormulaFormatException>(() => new Formula(@"wIlLALoNgbItoFTeXtiNMoCkInGCaSebReAktHefOrMuLacLaSsaaa42"));
        }


        /// <summary>
        ///     <para>
        ///         Test that very long integer numeric literals are accepted. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestLongDecimalNumericLiteralLiteral_IsValid()
        {
            _ = new Formula(@"1234567890123456789012345678901234567890");
        }


        /// <summary>
        ///     <para>
        ///         Test that very long floating-point numeric literals are accepted. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestLongFloatingPointNumericLiteral_IsValid()
        {
            _ = new Formula(@"3.141592653589793238462643383279502884197169399375105820974944592307816406286");
        }


        /// <summary>
        ///     <para>
        ///         Test that scientific notation with a very large positive exponent is accepted. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestPositiveDecimalNumericLiteralWithVeryLargeMagnitude_IsValid()
        {
            _ = new Formula(@"1e999");
        }


        /// <summary>
        ///     <para>
        ///         Test that scientific notation with a very large negative exponent is accepted. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestNegativeDecimalNumericLiteralWithVeryLargeMagnitude_IsValid()
        {
            _ = new Formula(@"1E-999");
        }


        /// <summary>
        ///     <para>
        ///         Test that scientific notation with large coefficient and exponent is accepted. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestDecimalNumericLiteralWithLargeCoefficientAndLargeExponent_IsValid()
        {
            _ = new Formula(@"9.999999999999999E999");
        }


        /// <summary>
        ///     <para>
        ///         Test that a valid variable formed from "random" letters and digits, a la a keyboard mash, is rejected. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestAlphanumericKeyboardMash_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"mewu3XCfom5eqfFQWF96inwo4eguJJJJJJJ0"));
        }


        /// <summary>
        ///     <para>
        ///         Test that "random" letters and digits with special characters, a la a keyboard mash, are rejected. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestAlphanumericSpecialCharacterKeyboardMash_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"asdfjkl;1!"));
        }


        /// <summary>
        ///     <para>
        ///         Test that "random" special characters, a la a keyboard mash, are rejected. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestSpecialCharacterKeyboardMash_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"!@#$%^&*()_+1\/.,;'[]\1`"));
        }


        /// <summary>
        ///     <para>
        ///         Test that scientific notation is accepted when denoted with a lowercase e.
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestScientificNotationWithLittleE_IsValid()
        {
            _ = new Formula(@"1e1");
        }


        /// <summary>
        ///     <para>
        ///         Test that scientific notation is accepted when denoted with an uppercase e.
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestScientificNotationWithBigE_IsValid()
        {
            _ = new Formula(@"1E1");
        }


        /// <summary>
        ///     <para>
        ///         Test that scientific notation with a missing exponent is rejected. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestScientificNotationMissingExponent_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"42e"));
        }


        /// <summary>
        ///     <para>
        ///         Test that scientific notation with a negative exponent is accepted. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestScientificNotationWithNegativeExponent_IsValid()
        {
            _ = new Formula(@"94e-4");
        }


        /// <summary>
        ///     <para>
        ///         Test that scientific notation with an exponent prefixed by zeros is accepted. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestScientificNotationWithExponentPrefixedByZeros_IsValid()
        {
            _ = new Formula(@"11E007");
        }


        /// <summary>
        ///     <para>
        ///         Test that scientific notation with a significand prefixed by zeros is accepted. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestScientificNotationWithSignificandPrefixedByZeros_IsValid()
        {
            _ = new Formula(@"007E8");
        }


        /// <summary>
        ///     <para>
        ///         Test that variables containing special characters are rejected. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestVariableReferenceWithSpecialCharacter_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"We!Rd22"));
        }


        /// <summary>
        ///     <para>
        ///         Test that variables a fixer character in an invalid position are rejected. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestVariableReferenceWithInvalidFixerCharacter_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"WieRd0$E22"));
        }


        /// <summary>
        ///     <para>
        ///         Test that mixed-case variable names without digits are accepted. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestVariableReferenceWithMixedCaseRow_IsValid()
        {
            _ = new Formula(@"CraZY8");
        }


        /// <summary>
        ///     <para>
        ///         Test that variables without a numeric suffix are rejected. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestVariableTokenWithNoColumnSpecifier_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"cra + 3"));
        }


        /// <summary>
        ///     <para>
        ///         Test that variables containing underscores are rejected. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestCorrectVariableReferenceWithUnderscore_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"ab_c3"));
        }


        /// <summary>
        ///     <para>
        ///         Test that variables ending in underscores are rejected. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestVariableReferenceEndingInUnderscore_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"ab3_"));
        }


        /// <summary>
        ///     <para>
        ///         Test that variables starting with underscores are rejected. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestVariableReferenceStartingInUnderscore_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"_ref9"));
        }


        /// <summary>
        ///     <para>
        ///         Test that variables ending in letters are rejected. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestVariableReferenceEndingInLetter_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"r1c"));
        }


        /// <summary>
        ///     <para>
        ///         Test that invalid tokens appended to a valid expression are rejected. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void
            FormulaConstructor_TestValidExpressionFollowedByInvalidTokensAndNumber_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"1 + abc$1"));
        }


        /// <summary>
        ///     <para>
        ///         Test that a variable following a complete expression without an operator is rejected. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestValidExpressionFollowedByVariableAndSpace_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"(5 + x) y"));
        }


        /// <summary>
        ///     <para>
        ///         Test that scientific notation split by whitespace is rejected. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestValidScientificNotationWithInsertedSpace_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"1E 10"));
        }


        /// <summary>
        ///     <para>
        ///         Test that multiple consecutive operators are rejected. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestMultipleConsecutiveOperators_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"5++3"));
        }


        /// <summary>
        ///     <para>
        ///         Test that numbers split by whitespace inside a decimal are rejected. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestNumberWithSpaceInDecimal_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"1. 2"));
        }


        /// <summary>
        ///     <para>
        ///         Test that a floating point number, consisting of multiple decimal points, will be rejected. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestFloatingPointLiteralWithMultipleDecimalPoints_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"4.5.6 + r2"));
        }


        /// <summary>
        ///     <para>
        ///         Test that a numeric literal may be prefixed by any number of preceding zeros. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestNumericLiteralWithLeadingZeros_IsValid()
        {
            _ = new Formula(@"007 + b1 + nd9");
        }


        /// <summary>
        ///     <para>
        ///         Test that invalid numeric tokens following a valid expression are rejected. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestValidExpressionFollowedByInvalidToken_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"a1 + 1.2.3"));
        }


        /// <summary>
        ///     <para>
        ///         Test that invalid tokens following a parenthesized expression are rejected. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestValidNestedExpressionFollowedByInvalidToken_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"(5 + 3)a"));
        }


        /// <summary>
        ///     <para>
        ///         Test that variables separated only by whitespace without an operator are rejected. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestTwoValidVariablesRunTogetherWithSeparator_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"abc123 def456"));
        }


        /// <summary>
        ///     <para>
        ///         Test that adjacent variables without separators are rejected. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestTwoValidVariablesRunTogetherWithNoSeparator_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"abc123def456"));
        }


        /// <summary>
        ///     <para>
        ///         Test that excessive whitespace around tokens is ignored successfully. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestMultipleSpacesAroundTokens_IsValid()
        {
            _ = new Formula(@"   1   +   2   ");
        }


        /// <summary>
        ///     <para>
        ///         Test that formulas without spaces between tokens are accepted. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestNoSpacesBetweenTokens_IsValid()
        {
            _ = new Formula(@"1+2");
        }


        /// <summary>
        ///     <para>
        ///         Test that multiple spaces between tokens are accepted. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestMultipleSpacesBetweenTokens_IsValid()
        {
            _ = new Formula(@"1 +    2");
        }


        /// <summary>
        ///     <para>
        ///         Test that tabs and newlines are not treated as valid whitespace. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should throw a FormulaFormatException exception.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestTabsAndNewlinesAsWhitespace_ThrowsFormulaFormatException()
        {
            Assert.Throws<FormulaFormatException>(() => _ = new Formula(@"1\t+\n2"));
        }


        /// <summary>
        ///     <para>
        ///         Test that very large formulas with many valid tokens are accepted. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestManyTokensInSameFormula_IsValid()
        {
            _ = new Formula(
                @"1 + a1 + 2 + b2 + 3 + c3 + 4 + d4 + 5 + e5 + 6 + f6 + 7 + g7 + 8 + h8 + 9 + i9 + 10 + j10 + 11 + k11 + 12 + l12 + 13 + m13 + 14 + n14 + 15 + o15 + 16 + p16 + 17 + q17 + 18 + r18 + 19 + s19 + 20 + t20 + 21 + u21 + 22 + v22 + 23 + w23 + 24 + x24 + 25 + y25 + 26 + z26 + 27 + a27 + 28 + b28 + 29 + c29 + 30 + d30 + 31 + e31 + 32 + f32 + 33 + g33 + 34 + h34 + 35 + i35 + 36 + j36 + 37 + k37 + 38 + l38 + 39 + m39 + 40 + n40 + 41 + o41 + 42 + p42 + 43 + q43 + 44 + r44 + 45 + s45 + 46 + t46 + 47 + u47 + 48 + v48 + 49 + w49 + 50 + x50");
        }


        /// <summary>
        ///     <para>
        ///         Test that extremely deep nesting of parentheses is accepted when balanced. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestDeeplyNestedToken_IsValid()
        {
            _ = new Formula(
                @"((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((1))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))");
        }


        /// <summary>
        ///     <para>
        ///         Test that deeply nested mixed operator expressions are accepted. This is a test that isn't specific to any one rule, but will hopefully catch edge-cases.
        ///         This should not throw.
        ///     </para>
        /// </summary>
        [TestMethod]
        public void FormulaConstructor_TestFormulaWithManyNests_IsValid()
        {
            _ = new Formula(
                @"1 + (2 * (3 - (4 / (5 + (6 * (7 - (8 / (9 + (10 * (11 - (12 / (13 + (14 * (15 - (16 / (17 + (18 * (19 - (20 / (21 + (22 * (23 - (24 / (25 + (26 * (27 - (28 / (29 + (30 * (31 - (32 / (33 + (34 * (35 - (36 / (37 + (38 * (39 - (40 / (41 + (42 * (43 - (44 / (45 + (46 * (47 - (48 / (49 + (50)))))))))))))))))))))))))))))))))))))))))))))))))");
        }
    }
}