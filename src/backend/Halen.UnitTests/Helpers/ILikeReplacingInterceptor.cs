using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Halen.UnitTests.Helpers;

/// <summary>
/// Query expression interceptor that replaces EF.Functions.ILike(x, pattern)
/// calls with a case-insensitive client-evaluable equivalent for InMemory testing.
///
/// In production (Npgsql), EF translates ILike to PostgreSQL's ILIKE operator.
/// The InMemory provider evaluates queries client-side, but the Npgsql ILike
/// extension method throws when invoked directly. This interceptor rewrites the
/// expression tree to use a custom helper that does case-insensitive matching.
/// </summary>
public class ILikeReplacingInterceptor : IQueryExpressionInterceptor
{
    public Expression QueryCompilationStarting(Expression queryExpression, QueryExpressionEventData eventData)
    {
        return new ILikeReplacingVisitor().Visit(queryExpression);
    }

    private sealed class ILikeReplacingVisitor : ExpressionVisitor
    {
        private static readonly MethodInfo ILikeMethod2 =
            typeof(NpgsqlDbFunctionsExtensions).GetMethod(
                nameof(NpgsqlDbFunctionsExtensions.ILike),
                [typeof(DbFunctions), typeof(string), typeof(string)])!;

        private static readonly MethodInfo EvaluateMethod =
            typeof(ILikeReplacingVisitor).GetMethod(
                nameof(ClientILike),
                BindingFlags.Static | BindingFlags.Public)!;

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method == ILikeMethod2)
            {
                // Replace EF.Functions.ILike(_, matchExpression, pattern)
                // with ClientILike(matchExpression, pattern)
                var matchExpression = Visit(node.Arguments[1]);
                var pattern = Visit(node.Arguments[2]);
                return Expression.Call(EvaluateMethod, matchExpression, pattern);
            }

            return base.VisitMethodCall(node);
        }

        /// <summary>
        /// Client-side ILIKE evaluation supporting % wildcards.
        /// </summary>
        public static bool ClientILike(string? matchExpression, string? pattern)
        {
            if (matchExpression is null || pattern is null) return false;

            var trimmed = pattern;
            var startsWith = !trimmed.StartsWith('%');
            var endsWith = !trimmed.EndsWith('%');
            trimmed = trimmed.Trim('%');

            if (startsWith && endsWith)
                return matchExpression.Equals(trimmed, StringComparison.OrdinalIgnoreCase);
            if (startsWith)
                return matchExpression.StartsWith(trimmed, StringComparison.OrdinalIgnoreCase);
            if (endsWith)
                return matchExpression.EndsWith(trimmed, StringComparison.OrdinalIgnoreCase);

            return matchExpression.Contains(trimmed, StringComparison.OrdinalIgnoreCase);
        }
    }
}
