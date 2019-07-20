namespace Roboot.Runtime {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Roboot.Util;
    using Roboot.Ast;

    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class NativeMethod : System.Attribute {
        public NativeMethod(string name = "") {
            this.Name = name;
        }

        public readonly string Name;
    }

    public class NativeModule : Module {
        public NativeModule() {
            foreach (MethodInfo method in this.GetType().GetMethods()) {
                var nativeMethod = method.GetCustomAttribute(typeof(NativeMethod)) as NativeMethod;
                if (nativeMethod != null) {
                    var name = nativeMethod.Name != "" ? nativeMethod.Name : method.Name;
                    var e = MakeCallingExpression(method);
                    RegisterMethod(name, new Method(name, WrapNativeFunc(e)));
                }
            }
        }

        private LambdaExpression MakeCallingExpression(MethodInfo method) {
            var parameters = method.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToList();
            return Expression.Lambda(Expression.Call(Expression.Constant(this), method, parameters), parameters);
        }

        public void RegisterNativeMethod<T>(string name, Expression<T> e) {
            RegisterMethod(name, new Method(name, WrapNativeFunc(e)));
        }

        public MethodCase WrapNativeFunc(LambdaExpression e) {
            var matchCase = new MatchCase(
                implicitVariables: e.Parameters.Select(p => (name: p.Name, type: Optional<Expr>.Some((Expr)new NativeValue(p.Type)))).ToList(),
                matchedValue: new Ast.Params(e.Parameters.Select(p => new Param(p.Name, new Name(p.Name))).ToList()),
                body: new CallNative(e, e.Parameters.Select(p => new Name(p.Name)).ToList(), e.Body.Type)
            );

            return new MethodCase(new Compiler.FunctionScope(new Compiler.ModuleScope(this)), matchCase);
        }
    }
}
