namespace MetaComputer.Runtime {
    using System;
    using System.Collections.Generic;
    using MetaComputer.Util;

    class BaseModule : NativeModule {
        public BaseModule() {
            SetValue("Any", typeof(object));

            SetValue("Float", typeof(double));
            RegisterNativeMethod<Func<double, double, double>>("+", (a, b) => checked(a + b));
            RegisterNativeMethod<Func<double, double, double>>("-", (a, b) => checked(a - b));
            RegisterNativeMethod<Func<double, double, double>>("*", (a, b) => checked(a * b));

            SetValue("Int", typeof(Int64));
            RegisterNativeMethod<Func<Int64, Int64, Int64>>("+", (a, b) => checked(a + b));
            RegisterNativeMethod<Func<Int64, Int64, Int64>>("-", (a, b) => checked(a - b));
            RegisterNativeMethod<Func<Int64, Int64, Int64>>("*", (a, b) => checked(a * b));
        }
    }
}
