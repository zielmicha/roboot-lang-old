namespace Roboot.Compiler {
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Reflection;
    using System.Linq;
    using System.Linq.Expressions;
    using Roboot.Runtime;
    using Roboot.Ast;

    partial class FunctionCompiler {
        public Value GetCommonType(Value t1, Value t2) {
            if (AreEqual(t1, t2) == true) return t1;
            return Value.Immediate(typeof(object));
        }

        public bool? AreEqual(Value a, Value b) {
            if (a == b)
                return true;
            if (a.ImmediateValue != null && b.ImmediateValue != null && a.ImmediateValue.Equals(b.ImmediateValue))
                return true;
            return null;
        }
    }
}
