namespace MetaComputer.Runtime {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using MetaComputer.Util;
    using MetaComputer.Ast;

    [System.AttributeUsage(System.AttributeTargets.Method)]
    class NativeMethod : System.Attribute {
        public string Name { get; set; } = "";
    }

    class NativeModule : Module {
        public void RegisterNativeMethod<T>(string name, Expression<T> e) {
            RegisterMethod(name, new Method(name, WrapNativeFunc(e)));
        }

        public MethodCase WrapNativeFunc(LambdaExpression e) {
            var matchCase = new MatchCase(
                implicitVariables: e.Parameters.Select(p => (name: p.Name, type: (Expr)new NativeValue(p.Type))).ToList(),
                matchedValue: new Ast.Params(e.Parameters.Select(p => new Param(p.Name, new Name(p.Name))).ToList()),
                body: new CallNative(e, e.Parameters.Select(p => new Name(p.Name)).ToList())
            );

            return new MethodCase(new Compiler.FunctionScope(new Compiler.ModuleScope(this)), matchCase);
        }
    }
}
