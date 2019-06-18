namespace MetaComputer.Runtime {
    using System.Collections.Generic;

    class UnitValue {
        private UnitValue() {}

        public static UnitValue Instance = new UnitValue();
    }

    class Params {
        public readonly IReadOnlyList<object> Arguments;

        public readonly IReadOnlyDictionary<string, object> NamedArguments;

        public Params(IReadOnlyList<object> arguments, IReadOnlyDictionary<string, object> namedArguments) {
            this.Arguments = arguments;
            this.NamedArguments = namedArguments;
        }
    }

    interface ICallable {
        object Call(Params parameters);
    }
    
    class AnyType {
        public static object Instance = typeof(object);
    }

    class ExactType {
        public object Value { get; }

        public ExactType(object value) {
            this.Value = value;
        }
    }
}
