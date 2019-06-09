namespace MetaComputer.Runtime {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using MetaComputer.Util;

    interface IFunction {
        Value MatchArguments(Params paramObjs);

        object Invoke(FunctionArgs<object> args);
    }

    class Multimethod {
        public Multimethod(string name, IEnumerable<Function> implementations=null) {
            this.Name = name;
            if (implementations != null)
                this.Implementations.AddRange(implementations);
        }

        public readonly List<Function> Implementations = new List<Function>();

        public readonly bool IsBase;

        public readonly string Name;

        public static Multimethod Merge(IEnumerable<Multimethod> methods, string newName) {
            return new Multimethod(
                name: newName,
                implementations: methods.SelectMany(m => m.Implementations)
            );
        }
    }
}
