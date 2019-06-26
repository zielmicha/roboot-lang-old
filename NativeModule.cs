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
        public NativeMethod(string name="") {
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
                implicitVariables: e.Parameters.Select(p => (name: p.Name, type: (Expr)new NativeValue(p.Type))).ToList(),
                matchedValue: new Ast.Params(e.Parameters.Select(p => new Param(p.Name, new Name(p.Name))).ToList()),
                body: new CallNative(e, e.Parameters.Select(p => new Name(p.Name)).ToList())
            );

            return new MethodCase(new Compiler.FunctionScope(new Compiler.ModuleScope(this)), matchCase);
        }

        public void LoadRobootCode(Assembly assembly, string resourcePath) {
            byte[] data;
            using (var stream = assembly.GetManifestResourceStream(resourcePath)) {
                data = new byte[stream.Length];
                stream.Read(data, 0, (int)stream.Length);
            }
            LoadRobootCode("embedded://" + resourcePath, System.Text.Encoding.UTF8.GetString(data));
        }

        public void LoadRobootCode(string path, string data) {
            new Compiler.ModuleLoader(this).LoadCode(path, data);
        }
    }
}
