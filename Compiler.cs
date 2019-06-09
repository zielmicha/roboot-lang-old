namespace MetaComputer.Compiler {
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Linq;
    using System.Linq.Expressions;
    using MetaComputer.Runtime;
    using MetaComputer.Ast;

    interface IScope {
        Value Lookup(string name);
    }

    class FunctionScope : IScope {
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

    class ModuleScope : IScope {
        public MetaComputer.Runtime.Module module;

        public Value Lookup(string name) {
            var v = module.Lookup(name, includeLocal: true);
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

            var e = Expression.TypeAs(
                Expression.Call(typeof(RuntimeUtil).GetMethod("Coerce"), x.Expression, type.Expression),
                type.AsClrType());

            return (null, null, null);
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

        public (Value success, Value cost) CompileMatchCase(Value matchWith, MatchCase matchCase) {
            LabelTarget failLabel = Expression.Label();
            LabelTarget successLabel = Expression.Label();
            var successVar = Expression.Parameter(typeof(bool));
            var costVar = Expression.Parameter(typeof(int));

            var compiler = new FunctionCompiler(scope);
            var instr = new List<Expression>();

            foreach (var variable in matchCase.ImplicitVariables) {
                implicitVars[variable.name] = null;
            }

            instr.Add(compiler.CompileMatchValue(matchWith, matchCase.MatchedValue, failLabel));

            foreach (var variable in matchCase.ImplicitVariables) {
                if (implicitVars[variable.name] == null) {
                    throw new Exception($"{variable.name} was not instantiated at {matchCase.Location}");
                }
                var coerceResult = compiler.CompileCoerce(implicitVars[variable.name],
                                                          CompileExpr(variable.type));

                instr.Add(Expression.IfThen(
                    coerceResult.success.Expression,
                    Expression.Block(
                        Expression.AddAssign(costVar, coerceResult.cost.Expression),
                        scope.CreateVariable(variable.name, coerceResult.value))));
            }

            instr.Add(Expression.Goto(successLabel));
            instr.Add(Expression.Label(failLabel));
            instr.Add(Expression.Assign(successVar, Expression.Constant(false)));

            instr.Add(Expression.Label(successLabel));
            instr.Add(Expression.Assign(successVar, Expression.Constant(true)));

            return (Value.Dynamic(successVar), Value.Dynamic(costVar));
        }

        public Value CompileBlockStmt(BlockLet stmt) {
            Value v = CompileExpr(stmt.Value);
            v = CompileCoerceOrThrow(v, CompileExpr(stmt.Type));
            Expression expr = scope.CreateVariable(stmt.Name, v);

            return Value.Unit(expr);
        }

        public readonly static Guid MetaComputerLanguageGuid = Guid.Parse("50d39029-86bc-4787-8817-2404811cf8ac");

        public DebugInfoExpression MakeDebugInfo(Location loc) {
            var docInfo = Expression.SymbolDocument(loc.Filename, MetaComputerLanguageGuid);
            return Expression.DebugInfo(docInfo, loc.StartLine, loc.StartColumn, loc.EndLine, loc.EndColumn);
        }

        private Value CompileExprDispatch(Expr expr) {
            switch (expr) {
                case Call e: return CompileExpr(e);
                case Name e: return CompileExpr(e);
                case IntLiteral e: return CompileExpr(e);
                case StringLiteral e: return CompileExpr(e);
                case Block e: return CompileExpr(e);
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

        private Value CompileExpr(Block call) {
            var compiler = new FunctionCompiler(scope);

            var body = new List<Expression>();
            Value lastValue = null;
            foreach (BlockStmt stmt in call.Stmts) {
                lastValue = compiler.CompileBlockStmt(stmt);
                body.Add(lastValue.Expression);
            }

            return Value.Dynamic(Expression.Block(body.ToArray()), lastValue.Type);
        }
        
        private Value CompileExpr(Call call) {
            var instrs = new List<Expression>();
            var value = CompileExpr(call.Func).EvalOnce(instrs);
            var valueCallable = Expression.Convert(value.Expression, typeof(ICallable)); // use Coerce?
            // TODO: special case when function is ImmediateValue
            // TODO: special case when function type is known

            var paramsValue = CompileExpr(call.MakeParamsNode());

            instrs.Add(Expression.Call(valueCallable,
                                       typeof(ICallable).GetMethod("call"),
                                       paramsValue.Expression));

            return Value.Dynamic(Expression.Block(instrs));
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
