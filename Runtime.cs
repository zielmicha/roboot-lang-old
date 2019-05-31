namespace MetaComputer.Runtime {
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using MetaComputer.Util;

    class Context {
        public BaseModule BaseModule;
    }

    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class McExport : System.Attribute {
    }
    
    class Runtime {
        private static ThreadLocal<Context> ContextStorage = new ThreadLocal<Context>();

        public static Context CurrentContext {
            get { return ContextStorage.Value; }
        }

        public static object TypeAssertion(object value, object type) {
            return value;
        }

        public static IType GetRuntimeType(object value) {
            return (IType)CurrentContext.BaseModule.GetTypeMethod.Invoke1(value);
        }
    }

    struct Symbol {
        public string Str { get; }

        private Symbol(string s) {
            this.Str = s;
        }

        public override int GetHashCode() {
            return Str.GetHashCode();
        }

        public override bool Equals(object s) {
            switch (s) {
                case Symbol sym: return Str.Equals(sym.Str);
                default: return false;
            }
        }
    }

    class ConflictException : Exception {
        ConflictException(string message) : base(message) {}
    }

}
