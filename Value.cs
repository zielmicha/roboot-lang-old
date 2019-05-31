namespace MetaComputer.Runtime {
    using System.Linq.Expressions;

    class Value {
        public IType Type { get; set; }

        public Expression Expression { get; set; }

        public object ImmediateValue { get; set; }

        public static Value Dynamic(Expression expression, IType type=null) {
            return new Value {
                Type = type ?? AnyType.Instance,
                Expression = expression,
                ImmediateValue = null
            };
        }

        public static Value Immediate(object value) {
            return new Value {
                Type = Runtime.GetRuntimeType(value),
                Expression = Expression.Constant(value),
                ImmediateValue = value
            };
        }
    }

}
