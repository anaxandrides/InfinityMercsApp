using System.Linq.Expressions;
using System.Reflection;

namespace InfinityMercsApp.Infrastructure.Repositories.InMemory;

/// <summary>
/// Rewrites a filter so it means in memory what sqlite-net makes it mean in SQL: <c>Contains</c> becomes
/// <c>instr</c> there, which minds case, while <c>StartsWith</c> and <c>EndsWith</c> become LIKE, which ignores it; and
/// a text column that is NULL matches none of them rather than throwing.
/// </summary>
internal sealed class SqliteTextSemantics : ExpressionVisitor
{
    public static SqliteTextSemantics Instance { get; } = new();

    private static readonly MethodInfo Contains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string), typeof(StringComparison)])
        ?? throw new MissingMethodException(nameof(String), nameof(string.Contains));

    private static readonly MethodInfo StartsWith = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string), typeof(StringComparison)])
        ?? throw new MissingMethodException(nameof(String), nameof(string.StartsWith));

    private static readonly MethodInfo EndsWith = typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string), typeof(StringComparison)])
        ?? throw new MissingMethodException(nameof(String), nameof(string.EndsWith));

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Object is { } text && node.Method.DeclaringType == typeof(string) &&
            node.Arguments is [{ Type: var argumentType } argument] && argumentType == typeof(string) &&
            LikeFor(node.Method.Name) is { } like)
        {
            var visitedText = Visit(text);
            var comparison = like == Contains ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            var call = Expression.Call(visitedText, like, Visit(argument), Expression.Constant(comparison));
            return Expression.AndAlso(Expression.NotEqual(visitedText, Expression.Constant(null, typeof(string))), call);
        }

        return base.VisitMethodCall(node);
    }

    private static MethodInfo? LikeFor(string methodName) => methodName switch
    {
        nameof(string.Contains) => Contains,
        nameof(string.StartsWith) => StartsWith,
        nameof(string.EndsWith) => EndsWith,
        _ => null
    };
}
