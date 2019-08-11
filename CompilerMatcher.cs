namespace Roboot.Compiler {
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Linq;
    using System.Linq.Expressions;
    using Roboot.Runtime;
    using Roboot.Ast;
    using Roboot.Util;

    public enum MatchKind {
        exact, subtype
    }

    public class Matcher {
        private readonly FunctionCompiler compiler;
        private readonly Dictionary<string, Expr> typeConstraints = new Dictionary<string, Expr>();
        internal Dictionary<string, Value> implicitVars = new Dictionary<string, Value>();

        public Matcher(FunctionCompiler compiler, List<(string name, Optional<Expr> type)> implicitVariables) {
            this.compiler = compiler;

            foreach (var variable in implicitVariables) {
                if (variable.type.IsSome())
                    typeConstraints[variable.name] = variable.type.Get();
                implicitVars[variable.name] = null;
            }
        }

        public Expression CompileMatch(Value matchWith, Expr expr, LabelTarget failLabel, MatchKind matchKind) {
            switch (expr) {
                case Ast.Params e:
                    return CompileMatch(matchWith, e, failLabel, matchKind);
                case Ast.Name e:
                    return CompileMatch(matchWith, e, failLabel, matchKind);
                case Ast.Call e:
                    return CompileMatch(matchWith, e, failLabel, matchKind);
                default:
                    return CompileMatchValue(matchWith, expr, failLabel, matchKind);
            }
        }

        public Expression CompileMatch(Value matchWith, Ast.Name name, LabelTarget failLabel, MatchKind matchKind) {
            if (implicitVars.ContainsKey(name.Str)) {
                if (implicitVars[name.Str] == null) {
                    implicitVars[name.Str] = matchWith;
                    return ExprUtil.Empty();
                } else {
                    Value isEq = compiler.CompileEq(matchWith, implicitVars[name.Str]);
                    return Expression.IfThen(Expression.Not(isEq.Expression), Expression.Goto(failLabel));
                }
            } else {
                return CompileMatchValue(matchWith, name, failLabel, matchKind);
            }
        }

        public Expression CompileMatch(Value matchWith, Ast.Call call, LabelTarget failLabel, MatchKind matchKind) {
            return CompileMatchValue(matchWith, call, failLabel, matchKind);
        }

        public Expression CompileMatchValue(Value matchWith, Expr expr, LabelTarget failLabel, MatchKind matchKind) {
            Value value = compiler.CompileExpr(expr);
            switch (matchKind) {
                case MatchKind.exact:
                    Value isEq = compiler.CompileEq(matchWith, value);
                    return Expression.IfThen(Expression.Not(isEq.Expression), Expression.Goto(failLabel));

                case MatchKind.subtype:
                    throw new Exception("todo");
            }
                    throw new Exception("todo");
        }

        public Expression CompileMatch(Value matchWith, Ast.Params parameters, LabelTarget failLabel, MatchKind matchKind) {
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

            for (int i = 0; i < positional.Count; i++) {
                Param target = positional[i];
                Expression value = ExprUtil.GetItem<IReadOnlyList<object>>(positionalValues, Expression.Constant(i));
                Expression match = CompileMatch(Value.Dynamic(value), target.Value, failLabel, matchKind);

                Expression onDefault = Expression.Goto(failLabel);

                if (target.DefaultValue.IsSome()) {
                    Value defaultValue = compiler.CompileExpr(target.DefaultValue.Get());
                    onDefault = CompileMatch(defaultValue, target.Value, failLabel, matchKind);
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

                Expression match = CompileMatch(Value.Dynamic(value), target.Value, failLabel, matchKind);
                Expression onDefault = ExprUtil.Empty();

                if (target.DefaultValue.IsSome()) {
                    Value defaultValue = compiler.CompileExpr(target.DefaultValue.Get());
                    onDefault = CompileMatch(defaultValue, target.Value, failLabel, matchKind);
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
