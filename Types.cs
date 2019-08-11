namespace Roboot.Runtime {
    using System.Collections.Immutable;
    using System.Collections.Generic;
    using System.Linq;

    public class Unit {
        private Unit() { }

        public static Unit Instance = new Unit();
    }

    public class Params {
        public readonly IReadOnlyList<object> Arguments;

        public readonly IReadOnlyDictionary<string, object> NamedArguments;

        public Params(IReadOnlyList<object> arguments, IReadOnlyDictionary<string, object> namedArguments) {
            this.Arguments = arguments;
            this.NamedArguments = namedArguments;
        }

        public static Params Make(params object[] args) {
            return new Params(args.ToList(), ImmutableDictionary<string, object>.Empty);
        }

        public override string ToString() {
            var args = this.Arguments.Select(x => x.ToString()).ToList();
            args.AddRange(this.NamedArguments.Select(x => $"~{ x.Key}:{ x.Value}"));
            return $"(Params {string.Join(" ", args)})";
        }
    }

    public class Placeholder {
        public Placeholder() { }
    }

    public interface ICallable {
        object Call(Params parameters);
    }

    public class Never {
        private Never() { }
    }

    public class AnyType {
        public static object Instance = typeof(object);
    }

    public class ExactType {
        public object Value { get; }

        public ExactType(object value) {
            this.Value = value;
        }
    }
}
