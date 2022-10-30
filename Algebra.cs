using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Reflection.Differentiation
{
    public abstract class Algebra
    {
        private static readonly MethodInfo Sin = typeof(Math).GetMethod(nameof(Math.Sin));
        private static readonly MethodInfo Cos = typeof(Math).GetMethod(nameof(Math.Cos));
        private static readonly ConstantExpression Zero = Expression.Constant(0d, typeof(double));
        private static readonly ConstantExpression One = Expression.Constant(1d, typeof(double));
        private static readonly ConstantExpression MinusOne = Expression.Constant(-1d, typeof(double));

        public static Expression<Func<double, double>> Differentiate(Expression<Func<double, double>> function)
        {
            return Expression.Lambda<Func<double, double>>(ParseDerivative(function.Body), function.Parameters);
        }

        private static Expression ParseDerivative(Expression expression)
        {
            switch (expression)
            {
                case BinaryExpression binaryExpr:
                {
                    switch (expression.NodeType)
                    {
                        case ExpressionType.Add:
                            return Expression.Add(ParseDerivative(binaryExpr.Left), ParseDerivative(binaryExpr.Right));
                        case ExpressionType.Subtract:
                            return Expression.Subtract(ParseDerivative(binaryExpr.Left),
                                ParseDerivative(binaryExpr.Right));
                        case ExpressionType.Multiply:
                            switch (binaryExpr.Left)
                            {
                                case ConstantExpression _ when binaryExpr.Right is ConstantExpression:
                                    return Zero;
                                case ConstantExpression _ when binaryExpr.Right is ParameterExpression:
                                    return binaryExpr.Left;
                                case ParameterExpression _ when binaryExpr.Right is ConstantExpression:
                                    return binaryExpr.Right;
                                default:
                                    return Expression.Add(
                                        Expression.Multiply(ParseDerivative(binaryExpr.Left), binaryExpr.Right),
                                        Expression.Multiply(binaryExpr.Left, ParseDerivative(binaryExpr.Right)));
                            }
                        case ExpressionType.Divide:
                            switch (binaryExpr.Left)
                            {
                                case ConstantExpression _ when binaryExpr.Right is ConstantExpression:
                                    return Zero;
                                case ConstantExpression _ when binaryExpr.Right is ParameterExpression:
                                    return Expression.Divide(binaryExpr.Left,
                                        Expression.Multiply(binaryExpr.Right, binaryExpr.Right));
                                case ParameterExpression _ when binaryExpr.Right is ConstantExpression:
                                    return Expression.Divide(One, binaryExpr.Right);
                                default:
                                    return Expression.Divide(
                                        Expression.Subtract(
                                            Expression.Multiply(ParseDerivative(binaryExpr.Left), binaryExpr.Right),
                                            Expression.Multiply(binaryExpr.Left, ParseDerivative(binaryExpr.Right))),
                                        Expression.Multiply(binaryExpr.Right, binaryExpr.Right));
                            }
                        default:
                            throw new ArgumentException($"The operation {nameof(expression.NodeType)} is not supported!");
                    }
                }
                case MethodCallExpression methodCall when methodCall.Method == Sin:
                    switch (methodCall.Arguments[0])
                    {
                        case ParameterExpression _:
                            return Expression.Call(Cos, methodCall.Arguments[0]);
                        case BinaryExpression _:
                            return Expression.Multiply(ParseDerivative(methodCall.Arguments[0]),
                                Expression.Call(Cos, methodCall.Arguments[0]));
                        default:
                            throw new ArgumentException($"The operation {methodCall.Arguments[0]} is not supported!");
                    }
                case MethodCallExpression methodCall when methodCall.Method == Cos:
                    switch (methodCall.Arguments[0])
                    {
                        case ParameterExpression _:
                            return Expression.Multiply(MinusOne, Expression.Call(Sin, methodCall.Arguments[0]));
                        case BinaryExpression _:
                            return Expression.Multiply(ParseDerivative(methodCall.Arguments[0]),
                                Expression.Multiply(MinusOne, Expression.Call(Sin, methodCall.Arguments[0])));
                        default:
                            throw new ArgumentException($"The operation {methodCall.Arguments[0]} is not supported!");
                    }
                default:
                    return expression.NodeType == ExpressionType.Constant ? Zero
                        : expression.NodeType == ExpressionType.Parameter ? One
                        : throw new ArgumentException($"The operation {expression} is not supported!");
            }
        }
    }
}