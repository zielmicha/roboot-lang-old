namespace Roboot.Base {
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Reflection;
    using Roboot.Util;
    using Roboot.Runtime;

    public class BaseModule : NativeModule {
        public BaseModule() {
            SetValue("Any", typeof(object));
            SetValue("Never", typeof(Never));

            SetValue("Float", typeof(double));
            RegisterNativeMethod<Func<double, double, double>>("+", (a, b) => checked(a + b));
            RegisterNativeMethod<Func<double, double, double>>("-", (a, b) => checked(a - b));
            RegisterNativeMethod<Func<double, double>>("-", (a) => checked(-a));
            RegisterNativeMethod<Func<double, double, double>>("*", (a, b) => checked(a * b));

            SetValue("Int", typeof(Int64));
            RegisterNativeMethod<Func<Int64, Int64, Int64>>("+", (a, b) => checked(a + b));
            RegisterNativeMethod<Func<Int64, Int64, Int64>>("-", (a, b) => checked(a - b));
            RegisterNativeMethod<Func<Int64, Int64>>("-", (a) => checked(-a));
            RegisterNativeMethod<Func<Int64, Int64, Int64>>("*", (a, b) => checked(a * b));
            RegisterNativeMethod<Func<Int64, Int64>>("abs", (a) => checked(a < 0 ? -a : a));
            RegisterNativeMethod<Func<Int64, Int64, Int64>>("div", (a, b) => checked(a / b));

            RegisterNativeMethod<Func<object, object, bool>>("==", (a, b) => a.Equals(b));
            RegisterNativeMethod<Func<double, double, bool>>("==", (a, b) => Double.IsNaN(a) ? Double.IsNaN(b) : a.Equals(b));

            SetValue("Bool", typeof(bool));
            SetValue("true", true);
            SetValue("false", false);

            SetValue("String", typeof(string)); // TODO: immutable strings

            LoadRobootCode(Assembly.GetExecutingAssembly(), "roboot.base");
        }

        [NativeMethod(name: "Array")]
        public object array(object itemType) {
            return typeof(IImmutableList<>).MakeGenericType((Type)itemType);
        }

        [NativeMethod]
        public long parseInt(string s) {
            return long.Parse(s);
        }
    }
}
