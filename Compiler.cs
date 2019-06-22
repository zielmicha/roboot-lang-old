namespace Roboot.Compiler {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Linq;
    using System.Linq.Expressions;
    using Roboot.Runtime;
    using Roboot.Ast;

    public interface IScope {
        Value Lookup(string name);
    }

    public class FunctionScope : IScope {
        private IScope parent;
        private Dictionary<string, Value> values = new Dictionary<string, Value>();

        public FunctionScope(IScope parent) {
            this.parent = parent;
        }

        public Expression CreateVariable(string name, Value value) {
            if (value.ImmediateValue != null) {
                values[name] = value;
                return ExprUtil.Empty();
            } else {
                var variable = Value.Dynamic(
                    type: value.Type,
                    expression: Expression.Parameter(value.GetClrType(), name));
                values[name] = variable;
                return Expression.Assign(variable.Expression, value.Expression);
            }
        }

        public Value Lookup(string name) {
            if (values.ContainsKey(name))
                return values[name];
            else
                return parent.Lookup(name);
        }
    }

    public class ModuleScope : IScope {
        public readonly Runtime.Module Module;

        public ModuleScope(Runtime.Module module) {
            this.Module = module;
        }

        public Value Lookup(string name) {
            var v = this.Module.Lookup(name, includeLocal: true);
            if (v.IsNone())
                throw new Exception($"no such name {name}");

            return Value.Immediate(v.Get());
        }
    }
    
    partial class FunctionCompiler {
        private FunctionScope scope;
        private Dictionary<string, Value> implicitVars = new Dictionary<string, Value>();

        public FunctionCompiler(IScope parentScope) {
            scope = new FunctionScope(parentScope);
        }
        
        public Value CompileExpr(Expr expr) {
            Value v = CompileExprDispatch(expr);
            if (expr.Location == null)
                return v;

            return Value.Dynamic(
                Expression.Block(
                    MakeDebugInfo(expr.Location),
                    v.Expression), v.Type);
        }

        public Value CompileBlockStmt(BlockStmt stmt) {
            switch (stmt) {
                case BlockLet s: return CompileBlockStmt(s);
                default:
                    throw new Exception("unknown AST node " + stmt.GetType());
            }
        }

        public bool? AreEqual(Value a, Value b) {
            if (a == b)
                return true;
            if (a.ImmediateValue != null && b.ImmediateValue != null && a.ImmediateValue == b.ImmediateValue)
                return true;
            return null;
        }
        
        public (Value success, Value cost, Value value) CompileCoerce(Value x, Value type) {
            if (type.ImmediateValue != null) {
                if (AreEqual(x.Type, type) == true)
                    return (Value.Immediate(true), Value.Immediate(0), x);
            }

            // TODO: develop sophisticated Dijkstra-algorithm based user defined coercion system

            var cost = Expression.Parameter(typeof(int), "cost");
            Expression e = Expression.Call(typeof(RuntimeUtil).GetMethod("Coerce"), x.Expression, type.Expression, cost);

            var instrs = new List<Expression>();
            e = ExprUtil.EvalOnce(e, instrs);
            return (Value.Seq(instrs, Value.Dynamic(Expression.NotEqual(cost, Expression.Constant(-1)))), Value.Dynamic(cost), Value.Dynamic(Expression.Convert(e, type.AsClrType())));
        }

        public Value CompileCoerceOrThrow(Value x, Value type) {
            var result = CompileCoerce(x, type);
            return Value.Dynamic(Expression.IfThenElse(
                result.success.Expression,
                x.Expression,
                Expression.Call(typeof(RuntimeUtil).GetMethod("ThrowBadCoercion"), x.Expression, type.Expression)), type: result.success.Type);
        }

        public Value CompileEq(Value a, Value b) {
            if (a.ImmediateValue != null && b.ImmediateValue != null)
                return Value.Immediate(a.ImmediateValue.Equals(b.ImmediateValue));

            return Value.Dynamic(Expression.Call(typeof(object).GetMethod("Equals"), a.Expression, b.Expression));
        }

        public Expression CompileMatch(Value matchWith, Expr expr, LabelTarget failLabel) {
            switch (expr) {
            case Ast.Params e:
                return CompileMatch(matchWith, e, failLabel);
            case Ast.Name e:
                return CompileMatch(matchWith, e, failLabel);
            default:
                return CompileMatchValue(matchWith, expr, failLabel);
            }
        }

        public Expression CompileMatch(Value matchWith, Ast.Name name, LabelTarget failLabel) {
            if (implicitVars.ContainsKey(name.Str)) {
                if (implicitVars[name.Str] == null) {
                    implicitVars[name.Str] = matchWith;
                    return ExprUtil.Empty();
                } else {
                    Value isEq = CompileEq(matchWith, implicitVars[name.Str]);
                    return Expression.IfThen(Expression.Not(isEq.Expression), Expression.Goto(failLabel));
                }
            } else {
                return CompileMatchValue(matchWith, name, failLabel);
            }
        }

        public Expression CompileMatchValue(Value matchWith, Expr expr, LabelTarget failLabel) {
            Value value = CompileExpr(expr);
            Value isEq = CompileEq(matchWith, value);
            return Expression.IfThen(Expression.Not(isEq.Expression), Expression.Goto(failLabel));
        }

        public (Value success, Value cost, Value result) CompileMatchCase(Value matchWith, MatchCase matchCase) {
            LabelTarget failLabel = Expression.Label("fail");
            LabelTarget finishLabel = Expression.Label("finish");
            var successVar = Expression.Parameter(typeof(bool), "success");
            var costVar = Expression.Parameter(typeof(int), "cost");

            var compiler = new FunctionCompiler(scope);
            var instr = new List<Expression>();

            foreach (var variable in matchCase.ImplicitVariables) {
                compiler.implicitVars[variable.name] = null;
            }

            instr.Add(compiler.CompileMatch(matchWith, matchCase.MatchedValue, failLabel));

            foreach (var variable in matchCase.ImplicitVariables) {
                if (compiler.implicitVars[variable.name] == null) {
                    throw new Exception($"{variable.name} was not instantiated at {matchCase.Location}");
                }
                var coerceResult = compiler.CompileCoerce(compiler.implicitVars[variable.name],
                                                          CompileExpr(variable.type));

                instr.Add(Expression.IfThenElse(
                    coerceResult.success.Expression,
                    Expression.Block(
                        Expression.AddAssign(costVar, coerceResult.cost.Expression),
                        scope.CreateVariable(variable.name, coerceResult.value)),
                    Expression.Goto(failLabel)));
            }

            instr.Add(Expression.Assign(successVar, Expression.Constant(true)));
            instr.Add(Expression.Goto(finishLabel));

            instr.Add(Expression.Label(failLabel));
            instr.Add(Expression.Assign(successVar, Expression.Constant(false)));

            instr.Add(Expression.Label(finishLabel));

            return (Value.Seq(instr, Value.Dynamic(successVar)), Value.Dynamic(costVar), compiler.CompileExpr(matchCase.Body));
        }

        public static Value CompileMatchCases(Value matchWith, List<(FunctionCompiler, MatchCase)> cases) {
            var instrs = new List<Expression>();
            matchWith = matchWith.EvalOnce(instrs);

            var bestCost = Expression.Parameter(typeof(int), "bestCost");
            var bestCase = Expression.Parameter(typeof(int), "bestCase");
            instrs.Add(Expression.Assign(bestCost, Expression.Constant(int.MaxValue)));
            instrs.Add(Expression.Assign(bestCase, Expression.Constant(-1)));

            var caseBodies = new List<Value>();
            for (var caseI = 0; caseI < cases.Count; caseI ++) {
                var (compiler, matchCase) = cases[caseI];
                var (sucessVar, costVar, caseBody) = compiler.CompileMatchCase(matchWith, matchCase);
                caseBodies.Add(caseBody);
                instrs.Add(Expression.IfThen(sucessVar.Expression,
                    Expression.IfThenElse(
                        Expression.Equal(costVar.Expression, bestCost), Expression.Assign(bestCase, Expression.Constant(-2)),
                        Expression.IfThen(
                            Expression.LessThan(costVar.Expression, bestCost),
                                Expression.Block(Expression.Assign(bestCost, costVar.Expression), Expression.Assign(bestCase, Expression.Constant(caseI)))))));
            }

            instrs.Add(Expression.IfThen(Expression.Equal(bestCase, Expression.Constant(-2)), Expression.Call(typeof(RuntimeUtil).GetMethod("ThrowAmbigousMatch"))));
            instrs.Add(Expression.IfThen(Expression.Equal(bestCase, Expression.Constant(-1)), Expression.Call(typeof(RuntimeUtil).GetMethod("ThrowNoMatch"))));
            // instrs.Add(Expression.Call(typeof(RuntimeUtil).GetMethod("DebugPrintInt"), Expression.Constant("bestCase"), bestCase));

            var resultVar = Expression.Parameter(typeof(object), "result");

            for (var caseI = 0; caseI < cases.Count; caseI ++) {
                instrs.Add(Expression.IfThen(Expression.Equal(bestCase, Expression.Constant(caseI)),
                                             Expression.Assign(resultVar, Expression.Convert(caseBodies[caseI].Expression, typeof(object)))));
            }

            return Value.Seq(instrs, Value.Dynamic(resultVar));
        }

        public Value CompileBlockStmt(BlockLet stmt) {
            Value v = CompileExpr(stmt.Value);
            v = CompileCoerceOrThrow(v, CompileExpr(stmt.Type));
            Expression expr = scope.CreateVariable(stmt.Name, v);

            return Value.Unit(expr);
        }

        public readonly static Guid RobootLanguageGuid = Guid.Parse("50d39029-86bc-4787-8817-2404811cf8ac");

        public DebugInfoExpression MakeDebugInfo(Location loc) {
            var docInfo = Expression.SymbolDocument(loc.Filename, RobootLanguageGuid);
            return Expression.DebugInfo(docInfo, loc.StartLine, loc.StartColumn, loc.EndLine, loc.EndColumn);
        }

        private Value CompileExprDispatch(Expr expr) {
            switch (expr) {
                case Call e: return CompileExpr(e);
                case Name e: return CompileExpr(e);
                case IntLiteral e: return CompileExpr(e);
                case StringLiteral e: return CompileExpr(e);
                case Block e: return CompileExpr(e);
                case Ast.Params e: return CompileExpr(e);
                case NativeValue e: return CompileExpr(e);
                case CallNative e: return CompileExpr(e);
                default:
                    throw new Exception("unknown AST node " + expr.GetType());
            }
        }

        private Value CompileExpr(IntLiteral name) {
            return Value.Immediate(name.Value);
        }

        private Value CompileExpr(StringLiteral name) {
            return Value.Immediate(name.Value);
        }

        private Value CompileExpr(Name name) {
            return scope.Lookup(name.Str);
        }

        private Value CompileExpr(NativeValue nativeValue) {
            return Value.Immediate(nativeValue.Value);
        }

        private Value CompileExpr(Block block) {
            var compiler = new FunctionCompiler(scope);

            var body = new List<Expression>();
            Value lastValue = null;
            foreach (BlockStmt stmt in block.Stmts) {
                lastValue = compiler.CompileBlockStmt(stmt);
                body.Add(lastValue.Expression);
            }

            return Value.Dynamic(Expression.Block(body.ToArray()), lastValue.Type);
        }

        public Value CompileExpr(CallNative call) {
            var callExpr = Expression.Invoke(call.Func,
                                             call.Args.Select(x => CompileExpr(x).Expression));
            return Value.Dynamic(callExpr, type: Value.Immediate(call.ReturnType));
        }
        
        private Value CompileExpr(Call call) {
            var instrs = new List<Expression>();
            var value = CompileExpr(call.Func).EvalOnce(instrs);
            var valueCallable = Expression.Convert(value.Expression, typeof(ICallable)); // use Coerce?
            // TODO: special case when function is ImmediateValue
            // TODO: special case when function type is known

            var paramsValue = CompileExpr(call.MakeParamsNode());

            instrs.Add(Expression.Call(valueCallable,
                                       typeof(ICallable).GetMethod("Call"),
                                       paramsValue.Expression));

            return Value.Dynamic(Expression.Block(instrs));
        }

        private Value CompileExpr(Ast.Params parameters) {
            var positional = ExprUtil.CreateCollection<List<object>>(parameters.ParamList.Where(param => !param.IsNamed).Select(param => Expression.Convert(CompileExpr(param.Value).Expression, typeof(object))));
            var named = ExprUtil.CreateCollection<Dictionary<string, object>>(parameters.ParamList.Where(param => param.IsNamed)
                .Select(param => ExprUtil.CreateKeyValuePair<string, object>(Expression.Constant(param.Name), Expression.Convert(CompileExpr(param.Value).Expression, typeof(object)))));
            return Value.Dynamic(Expression.New(typeof(Runtime.Params).GetConstructors()[0], positional, named));
        }

        public Value CompileExpr(FunDefExpr fundef) {
            return null;
        }

        public Value CompileFunctionBody(Expr body, List<KeyValuePair<string, Value>> argValues) {
            var compiler = new FunctionCompiler(scope);
            var instrs = new List<Expression>();

            foreach (var arg in argValues) {
                instrs.Add(scope.CreateVariable(arg.Key, arg.Value));
            }

            return Value.Seq(instrs, compiler.CompileExpr(body));
        }
    }
}
