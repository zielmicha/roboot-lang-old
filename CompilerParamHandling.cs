namespace Roboot.Compiler {
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Linq;
    using System.Linq.Expressions;
    using Roboot.Runtime;
    using Roboot.Ast;

    partial class FunctionCompiler {
        public Expression CompileMatch(Value matchWith, Ast.Params parameters, LabelTarget failLabel) {
            var positional = parameters.ParamList.Where(x => !x.IsNamed).ToList();
            var named = parameters.ParamList.Where(x => x.IsNamed).ToList();

            var instr = new List<Expression>();

            Expression paramsValue = Expression.TypeAs(matchWith.Expression, typeof(Runtime.Params));
            paramsValue = ExprUtil.EvalOnce(paramsValue, instr);

            instr.Add(Expression.IfThen(
                Expression.Equal(paramsValue, Expression.Constant(null)),
                Expression.Goto(failLabel)));

            Expression positionalValues = Expression.Field(paramsValue, "Arguments");
            Expression namedValues = Expression.Field(paramsValue, "NamedArguments");

            instr.Add(Expression.IfThen(
                Expression.GreaterThan(
                    Expression.Property(positionalValues, typeof(IReadOnlyCollection<object>).GetProperty("Count")),
                    Expression.Constant(positional.Count)),
                Expression.Goto(failLabel)));

            for (int i=0; i < positional.Count; i ++) {
                Param target = positional[i];
                Expression value = ExprUtil.GetItem<IReadOnlyList<object>>(positionalValues, Expression.Constant(i));
                Expression match = CompileMatch(Value.Dynamic(value), target.Value, failLabel);

                Expression onDefault = Expression.Goto(failLabel);

                if (target.DefaultValue.IsSome()) {
                    Value defaultValue = CompileExpr(target.DefaultValue.Get());
                    onDefault = CompileMatch(defaultValue, target.Value, failLabel);
                }

                instr.Add(Expression.IfThenElse(
                    Expression.LessThan(
                        Expression.Constant(i),
                        Expression.Property(positionalValues, typeof(IReadOnlyCollection<object>).GetProperty("Count"))),
                    match, onDefault));
            }

            var usedNamedParams = Expression.Parameter(typeof(int), "usedNamedParams");

            foreach (Param target in named) {
                Expression value = ExprUtil.GetItem<IReadOnlyDictionary<string, object>>(namedValues, Expression.Constant(target.Name));

                Expression match = CompileMatch(Value.Dynamic(value), target.Value, failLabel);
                Expression onDefault = ExprUtil.Empty();

                if (target.DefaultValue.IsSome()) {
                    Value defaultValue = CompileExpr(target.DefaultValue.Get());
                    onDefault = CompileMatch(defaultValue, target.Value, failLabel);
                }

                instr.Add(Expression.IfThenElse(
                    Expression.Call(namedValues, typeof(IReadOnlyDictionary<string, object>).GetMethod("ContainsKey"), Expression.Constant(target.Name)),
                    Expression.Block(new Expression[] {
                        Expression.AddAssign(usedNamedParams, Expression.Constant(1)),
                        match
                    }),
                    onDefault
                ));
            }

            instr.Add(Expression.IfThen(
                Expression.NotEqual(
                    Expression.Property(namedValues, typeof(IReadOnlyCollection<KeyValuePair<string, object>>).GetProperty("Count")),
                    Expression.Constant(named.Count)),
                Expression.Goto(failLabel)));

            return Expression.Block(instr);
        }
    }
}
