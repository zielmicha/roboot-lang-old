namespace Roboot.Compiler {
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Linq;
    using System.Linq.Expressions;

    static class ExprUtil {
        public static Expression MakeList<T>(IEnumerable<Expression> expr) {
            NewExpression newExpr = Expression.New(typeof(List<T>));
            MethodInfo addMethod = typeof(List<T>).GetMethod("Add");
            return Expression.ListInit(newExpr, expr.Select(x => Expression.ElementInit(addMethod, x)));
        }

        public static Expression MakeTuple<A, B>(Expression a, Expression b) {
            return Expression.New(typeof(Tuple<A, B>).GetConstructor(new Type[] { typeof(A), typeof(B) }), a, b);
        }

        public static Expression Empty() {
            return Expression.Constant(null);
        }

        public static Expression GetItem<T>(Expression obj, Expression index) {
            return Expression.Call(obj, typeof(T).GetMethod("get_Item"), index);
        }

        public static Expression CreateCollection<T>(IEnumerable<Expression> items) {
            return Expression.ListInit(
                Expression.New(typeof(T).GetConstructor(new Type[]{})),
                items.Select(x => Expression.ElementInit(typeof(T).GetMethod("Add"), x)).ToList());
        }

        public static Expression CreateKeyValuePair<K, V>(Expression a, Expression b) {
            return Expression.New(typeof(KeyValuePair<K, V>).GetConstructor(new Type[]{typeof(K), typeof(V)}), a, b);
        }

        public static object EvaluateNow(Expression expr) { // Expression
            var stmt = Expression.Lambda<Func<object>>(ExprUtil.DeclareAllVariables(expr));
            return stmt.Compile()();
        }

        public static Expression EvalOnce(Expression value, ICollection<Expression> instrs) {
            var param = Expression.Parameter(value.Type, "tmp");
            instrs.Add(Expression.Assign(param, value));
            return param;
        }

        public static BlockExpression DeclareAllVariables(Expression expr, List<ParameterExpression> except=null) {
            List<ParameterExpression> alreadyDeclared = new List<ParameterExpression>();
            if (except != null) alreadyDeclared.AddRange(except);
            alreadyDeclared.AddRange(GetAllChildren(expr).OfType<LambdaExpression>().SelectMany(l => l.Parameters));
            alreadyDeclared.AddRange(GetAllChildren(expr).OfType<BlockExpression>().SelectMany(l => l.Variables));

            return Expression.Block(
                expr.Type,
                GetAllChildren(expr)
                .OfType<ParameterExpression>()
                .Distinct()
                .Except(alreadyDeclared).ToList(),
                new List<Expression>() {expr});
        }

        public static IReadOnlyList<Expression> GetAllChildren(Expression expr) {
            var visitor = new AllChildrenVisitor();
            visitor.Visit(expr);
            return visitor.result;
        }

        private class AllChildrenVisitor : ExpressionVisitor {
            internal List<Expression> result = new List<Expression>();

            public override Expression Visit(Expression expr) {
                result.Add(expr);
                return base.Visit(expr);
            }
        }
    }
}
