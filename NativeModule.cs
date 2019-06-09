namespace MetaComputer.Runtime {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MetaComputer.Util;

    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class NativeMethod : System.Attribute {
        public string Name { get; set; } = "";
    }

    public class NativeModule : Module {
        void RegisterNativeMethod(MethodInfo info) {

        }
    }
}
