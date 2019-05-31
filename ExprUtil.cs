namespace MetaComputer.Compiler {
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
    }
}
