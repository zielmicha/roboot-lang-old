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
                    expression: Expression.Parameter(value.Type.GetClrType(), name));
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

    static class CompilerUtil {
        public static Value TypeAssertion(Value v, IType type) {
            if (v.Type == type)
                return v;

            return Value.Dynamic(
                Expression.Call(typeof(Runtime).GetMethod("TypeAssertion"),
                                v.Expression,
                                Expression.Constant(type)));
        }
    }
    
    class FunctionCompiler {
        private IScope scope;

        public Value CompileExpr(Expr expr) {
            Value v = CompileExprDispatch(expr);
            return new Value {
                Type = v.Type,
                ImmediateValue = v.ImmediateValue,
                Expression = Expression.Block(
                    MakeDebugInfo(expr.Location),
                    v.Expression)
            };
        }

        public Value CompileBlockStmt(BlockStmt stmt) {
            switch (stmt) {
                case BlockLet s: return CompileBlockStmt(s);
                default:
                    throw new Exception("unknown AST node " + stmt.GetType());
            }
        }

        public Value TypeAssertion(Value v, Expr typeExpr) {
            Value typeVal = CompileExpr(typeExpr);
            if (typeVal.ImmediateValue != null && typeVal.ImmediateValue is IType) {
                IType targetType = (IType)typeVal.ImmediateValue;
                if (targetType == v.Type)
                    return v;
            }

            return Value.Dynamic(
                Expression.Call(typeof(Runtime).GetMethod("TypeAssertion"),
                                v.Expression,
                                typeVal.Expression));
        }

        public Value CompileBlockStmt(BlockLet stmt) {
            Value v = CompileExpr(stmt.Value);
            v = TypeAssertion(v, stmt.Type);
            Expression expr = ((FunctionScope)scope).CreateVariable(stmt.Name, v);

            return Value.Dynamic(
                type: UnitType.Instance,
                expression: expr);
        }

        public readonly static Guid MetaComputerLanguageGuid = Guid.Parse("50d39029-86bc-4787-8817-2404811cf8ac");

        public DebugInfoExpression MakeDebugInfo(Location loc) {
            var docInfo = Expression.SymbolDocument(loc.Filename, MetaComputerLanguageGuid);
            return Expression.DebugInfo(docInfo, loc.StartLine, loc.StartColumn, loc.EndLine, loc.EndColumn);
        }

        private Value CompileExprDispatch(Expr expr) {
            switch (expr) {
                case Call e: return CompileExpr(e);
                case Expr e: return CompileExpr(e);
                default:
                    throw new Exception("unknown AST node " + expr.GetType());
            }
        }
        
        public Value CompileExpr(Name name) {
            return scope.Lookup(name.Str);
        }

        public Value CompileExpr(Block call) {
            IScope prevScope = scope;
            scope = new FunctionScope(prevScope);

            var body = new List<Expression>();
            Value lastValue = null;
            foreach (BlockStmt stmt in call.Stmts) {
                lastValue = CompileBlockStmt(stmt);
                body.Add(lastValue.Expression);
            }
            return new Value {
                Type = lastValue.Type,
                ImmediateValue = null,
                Expression = Expression.Block(body.ToArray())
            };
        }
        
        public Value CompileExpr(Call call) {
            Value func = CompileExpr(call.Func);
            List<Value> args = call.Args.Select(x => CompileExpr(x)).ToList();
            List<KeyValuePair<string, Value>> namedArgs = call.NamedArgs.Select(x => KeyValuePair.Create(x.Item1, CompileExpr(x.Item2))).ToList();

            if (func.ImmediateValue != null) {
                var ifunc = (IFunction)func.ImmediateValue;
                return ifunc.CompileInvocation(new FunctionArgs<Value>(args, namedArgs));
            }

            // Fallback: function type not known
            var invokeMethod = typeof(IFunction).GetMethod("Invoke");
            var argsConstructor = typeof(FunctionArgs<object>).GetConstructors()[0];
            var c = Expression.Call(func.Expression, invokeMethod,
                        Expression.New(
                            argsConstructor,
                            ExprUtil.MakeList<object>(args.Select(x => x.Expression)),
                            ExprUtil.MakeList<Tuple<Symbol, object>>(namedArgs.Select(x => ExprUtil.MakeTuple<Symbol, object>(Expression.Constant(x.Key), x.Value.Expression)))));
            return new Value { Expression = c, Type = AnyType.Instance, ImmediateValue = null };
        }

        public Value CompileExpr(FunDefExpr fundef) {
            return null;
        }

        public Value CompileFunctionBody(Expr body, List<KeyValuePair<string, Value>> argValues) {
            FunctionScope scope = new FunctionScope();
            var body = new List<Expression>();

            foreach (var arg in argValues) {
                body.Add(scope.CreateAndAssignVariable(arg.Key, arg.Value.Type));
            }

        }
    }
}
