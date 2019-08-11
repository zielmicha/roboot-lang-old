namespace Roboot.Runtime {
    using System;
    using System.Collections.Immutable;
    using System.Collections.Generic;
    using System.Threading;
    using Roboot.Util;

    public class Environment {
        public Module BaseModule;

        public Dictionary<string, Module> LoadedModules = new Dictionary<string, Module>();

        public Module GetModule(string name) {
            return LoadedModules[name];
        }
    }

    public class Context {
        public Environment Env = new Environment();

        public Compiler.ExpressionAssemblyBuilder AssemblyBuilder = new Compiler.ExpressionAssemblyBuilder();
    }

    public class ConflictException : Exception {
        ConflictException(string message) : base(message) { }
    }

    public static class RuntimeContext {
        private static ThreadLocal<Context> ContextStorage = new ThreadLocal<Context>();

        public static Context CurrentContext {
            get { return ContextStorage.Value; }
        }

        public static Environment CurrentEnv {
            get { return CurrentContext.Env; }
        }

        public static void InitThread() {
            ContextStorage.Value = new Context();
            CurrentEnv.LoadedModules["Base"] =
                CurrentEnv.BaseModule = new Base.BaseModule();
        }
    }

    public class RuntimeUtil {
        public static void ThrowBadCoercion(object source, object target) {
            throw new Exception($"cannot coerce {source} (type: {source.GetType()}) into {target}");
        }
        public static void ThrowBadDotnetConvert(object source, object target) {
            throw new Exception($"cannot convert {source} (type: {source.GetType()}) into {target}");
        }

        public static void ThrowAmbigousMatch(object value, object failureMessage, object allCasesMessage) {
            throw new Exception($"{failureMessage}: ambigous match for {value}\n{allCasesMessage}");
        }

        public static void ThrowNoMatch(object value, object failureMessage, object allCasesMessage) {
            throw new Exception($"{failureMessage}: no matching case for {value}\n{allCasesMessage}");
        }

        public static void DebugPrintInt(string msg, int value) {
            Console.WriteLine($"DebugPrintInt {msg} {value}");
        }

        public static IImmutableList<T> MakeList<T>(T[] values) {
            return ImmutableList<T>.Empty.AddRange(values);
        }

        public static T RunWithoutSideEffects<T>(Func<T> f) {
            // TODO: implement
            return f();
        }

        public static object Coerce(object source, object target, out int cost) {
            var targetTypeOpt = TypeBox.TryUnbox(target);
            if (targetTypeOpt.IsSome()) {
                var targetType = targetTypeOpt.Get();
                if (source.GetType() == targetType) {
                    cost = 0;
                    return source;
                }

                if (source.GetType().IsSubclassOf(targetType)) {
                    cost = 100;
                    return source;
                }
            }

            cost = -1;
            return null;
        }

        public Type AsClrType(object o) {
            if (o is Type t) return t;
            return typeof(object);
        }
    }
}
