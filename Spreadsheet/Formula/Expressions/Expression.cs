using Formula.Frontend;

namespace Formula.Expressions;

/// <summary>
///     <para>
///         The base class for all expression types in the formula AST.
///     </para>
/// </summary>
public abstract record Expression(SyntaxSpan Span)
{

}