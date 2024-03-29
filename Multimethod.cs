namespace Roboot.Runtime {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Roboot.Util;

    public class MethodCase {
        public readonly Compiler.FunctionScope Scope;
        public readonly Ast.MatchCase Body;

        public MethodCase(Compiler.FunctionScope scope, Ast.MatchCase body) {
            this.Scope = scope;
            this.Body = body;
        }
    }

    public class Method : ICallable {
        public Method(string name, MethodCase implementation) : this(name) {
            this.Implementations.Add(implementation);
        }

        public Method(string name, IEnumerable<MethodCase> implementations = null) {
            this.Name = name;
            if (implementations != null)
                this.Implementations.AddRange(implementations);
            this.Compiled = new Lazy<Func<Params, object>>(() => Compile());
        }

        public object Call(Params parameters) {
            return Compiled.Value(parameters);
        }

        public readonly List<MethodCase> Implementations = new List<MethodCase>();

        public readonly bool IsBase;

        public readonly string Name;

        public static Method Merge(IEnumerable<Method> methods, string newName) {
            return new Method(
                name: newName,
                implementations: methods.SelectMany(m => m.Implementations)
            );
        }

        private readonly Lazy<Func<Params, object>> Compiled;

        public override string ToString() {
            return $"(Method {Name})";
        }

        private Func<Params, object> Compile() {
            var parameters = Expression.Parameter(typeof(Params), "parameters");
            var body = Compiler.FunctionCompiler.CompileMatchCases(
                matchWith: Compiler.Value.Dynamic(parameters),
                cases: Implementations.Select(impl => (new Compiler.FunctionCompiler(impl.Scope), impl.Body)).ToList(),
                failureMessage: Expression.Constant($"call to {Name}"));
            var paramList = new List<ParameterExpression>() { parameters };
            var lambda = Expression.Lambda(Compiler.ExprUtil.DeclareAllVariables(body.Expression, paramList), this.Name, paramList);
            // Console.WriteLine("code: " + Util.ExpressionStringBuilder.ExpressionToString(lambda));
            return RuntimeContext.CurrentContext.AssemblyBuilder.AddMethod(lambda).CreateDelegate<Func<Params, object>>();
        }
    }
}
