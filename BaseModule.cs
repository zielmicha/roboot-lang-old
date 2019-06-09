namespace MetaComputer.Runtime {
    using System;
    using System.Collections.Generic;
    using MetaComputer.Util;

    class BaseModule : NativeModule {
        BaseModule() {
            SetValue("Int", typeof(Int64));
        }
    }
}
