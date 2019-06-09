namespace MetaComputer.Runtime {
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using MetaComputer.Util;

    class Environment {
        public BaseModule BaseModule;
    }

    class Context {
        public Environment Env;
    }

    class ConflictException : Exception {
        ConflictException(string message) : base(message) {}
    }

    class RuntimeContext {
        private static ThreadLocal<Context> ContextStorage = new ThreadLocal<Context>();

        public static Context CurrentContext {
            get { return ContextStorage.Value; }
        }

        public static Environment CurrentEnv {
            get { return CurrentContext.Env; }
        }
    }

    class RuntimeUtil {
        public static void ThrowBadCoercion(object source, object target) {
            throw new Exception($"cannot coerce {source} (type: {source.GetType()}) into {target}");
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

            cost = 0;
            return null;
        }
    }
}
