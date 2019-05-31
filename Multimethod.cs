namespace MetaComputer.Runtime {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using MetaComputer.Util;

    class Param {
        public IType Type { get; }

        public string Name { get; }

        public Optional<object> DefaultValue { get; }
    }

    struct FunctionArgs<T> {
        public List<T> Args;
        public List<KeyValuePair<string, T>> NamedArgs;

        public FunctionArgs(List<T> args, List<KeyValuePair<string, T>> namedArgs) {
            this.Args = args;
            this.NamedArgs = namedArgs;
        }
    }

    interface IFunction {
        Value CompileInvocation(FunctionArgs<Value> args);

        object Invoke(FunctionArgs<object> args);
    }

    class Function : IFunction {
        public IReadOnlyList<Param> Params { get; } = new List<Param>();

        public IReadOnlyList<Param> NamedParams { get; } = new List<Param>();

        public string Name { get; }

        public IType ReturnType { get; }

        public Optional<Expression> InlinableExpression = Optional<Expression>.None();

        private object Delegate;

        private List<Value> MatchFunctionArgs(FunctionArgs<Value> inArgs) {
            if (inArgs.Args.Count > Params.Count) {
                throw new Exception($"too many positional parameters given to function {Name}");
            }

            List<Value> result = new List<Value>();
            for (int i=0; i < Params.Count; i ++) {
                var param = Params[i];
                if (inArgs.Args.Count >= i) {
                    if (param.DefaultValue.IsSome())
                        result.Add(Value.Immediate(param.DefaultValue.Get()));
                    else
                        throw new Exception($"missing value for positional parameter {param.Name}");
                } else {
                    result.Add(Compiler.CompilerUtil.TypeAssertion(inArgs.Args[i], param.Type));
                }
            }

            var namedArgsDict = new Dictionary<string, Value>(inArgs.NamedArgs);
            foreach (Param param in NamedParams) {
                if (namedArgsDict.ContainsKey(param.Name)) {
                    result.Add(Compiler.CompilerUtil.TypeAssertion(namedArgsDict[param.Name], param.Type));
                    namedArgsDict.Remove(param.Name);
                } else {
                    if (param.DefaultValue.IsSome())
                        result.Add(Value.Immediate(param.DefaultValue.Get()));
                    else
                        throw new Exception($"missing value for named parameter {param.Name}");
                }
            }

            if (namedArgsDict.Count > 0) {
                throw new Exception($"function {Name} doesn't take argument {namedArgsDict.First().Key}");
            }

            return result;
        }
        
        private Type GetDelegateType() {
            List<Type> paramTypes = Params.Concat(NamedParams).Select(x => x.Type.GetClrType()).ToList();
            paramTypes.Prepend(ReturnType.GetClrType());
            return Expression.GetFuncType(paramTypes.ToArray());
        }

        public Value CompileInvocation(FunctionArgs<Value> inArgs) {
            List<Value> matched = MatchFunctionArgs(inArgs);

            // TODO: inlining
            Type delegateType = GetDelegateType();
            return Value.Dynamic(expression: Expression.Call(Expression.Constant(Delegate), delegateType.GetMethod("Invoke"), matched.Select(x => x.Expression)), type: ReturnType);
        }

        public object Invoke(FunctionArgs<object> args) {
            return null;
        }
    }

    class Multimethod {
        public Multimethod(string name, bool isBase=false, IEnumerable<Function> implementations=null) {
            this.Name = name;
            this.IsBase = isBase;
            if (implementations != null)
                this.Implementations.AddRange(implementations);
        }

        public readonly List<Function> Implementations = new List<Function>();

        public readonly bool IsBase;

        public readonly string Name;

        public object Invoke1(object Arg) {
            return null;
        }

        public static Multimethod Merge(IEnumerable<Multimethod> methods, string newName) {
            return new Multimethod(
                name: newName,
                implementations: methods.SelectMany(m => m.Implementations)
            );
        }
    }
}
