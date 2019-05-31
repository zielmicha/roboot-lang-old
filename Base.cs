namespace MetaComputer.Runtime {
    using System;
    using System.Collections.Generic;

    [System.AttributeUsage(System.AttributeTargets.Method)]
    class BaseMethodImpl : System.Attribute {
        public string Name;
    }

    class BaseModule : Module {
        public Multimethod GetTypeMethod = new Multimethod(name: "get_type", isBase: true);

        public BaseModule() {
            RegisterMethod("get_type", GetTypeMethod);
            RegisterMethod("to_string", isBase: true);

            // operators
            RegisterMethod("+");
            RegisterMethod("-");
            RegisterMethod("*");
            RegisterMethod("[]");
        }
    }
}
