namespace Roboot.Runtime {
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Roboot.Util;

    public class Environment {
        public BaseModule BaseModule;
    }

    public class Context {
        public Environment Env;

        public Compiler.ExpressionAssemblyBuilder AssemblyBuilder = new Compiler.ExpressionAssemblyBuilder();
    }

    public class ConflictException : Exception {
        ConflictException(string message) : base(message) {}
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
        }
    }

    public class RuntimeUtil {
        public static void ThrowBadCoercion(object source, object target) {
            throw new Exception($"cannot coerce {source} (type: {source.GetType()}) into {target}");
        }

        public static void ThrowAmbigousMatch() {
            throw new Exception($"ambigous match");
        }

        public static void ThrowNoMatch() {
            throw new Exception($"no match");
        }

        public static void DebugPrintInt(string msg, int value) {
            Console.WriteLine($"DebugPrintInt {msg} {value}");
        }

        public static object Coerce(object source, object target, out int cost) {
            if (target is Type targetType) {
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
    }
}
