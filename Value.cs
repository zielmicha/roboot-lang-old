namespace Roboot.Compiler {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq.Expressions;
    using Roboot.Runtime;

    public class Value {
        private Value() { }

        private Value _type;

        public Value Type {
            get {
                if (_type != null)
                    return _type;
                else if (ImmediateValue != null)
                    return Immediate(ImmediateValue.GetType()); // return Immediate(new ExactType(ImmediateValue));
                else
                    throw new ArgumentException("null type and no ImmediateValue?");
            }
        }

        public Expression Expression { get; private set; }

        public object ImmediateValue { get; private set; }

        public Value EvalOnce(ICollection<Expression> instrs) {
            if (ImmediateValue != null)
                return this;

            return Dynamic(ExprUtil.EvalOnce(Expression, instrs), Type);
        }

        public Type AsClrType() {
            if (this.ImmediateValue != null) {
                return TypeBox.TryUnbox(this.ImmediateValue).GetOrDefault(typeof(object));
            }
            return typeof(object);
        }

        public Type GetClrType() {
            return Type.AsClrType();
        }

        public static Value Unit() {
            return Value.Immediate(Runtime.Unit.Instance);
        }

        public static Value Unit(Expression expression) {
            return Dynamic(Expression.Block(expression, Expression.Constant(Runtime.Unit.Instance)));
        }

        public static Value Dynamic(Expression expression, Value type = null) {
            if (type != null && type.AsClrType() != expression.Type) {
                throw new ArgumentException($"CLR type mismatch expected={type.AsClrType()} actual={expression.Type}");
            }
            if (expression.Type == typeof(void)) throw new ArgumentException("expression has void type");

            return new Value {
                _type = type ?? Immediate(expression.Type),
                Expression = expression,
                ImmediateValue = null
            };
        }

        public static Value Immediate(object value, Value type = null) {
            return new Value {
                _type = type,
                Expression = Expression.Constant(value),
                ImmediateValue = value
            };
        }

        public static Value Seq(IReadOnlyList<Expression> exprs, Value val) {
            // Perform `exprs` and then return `val`.
            List<Expression> expr1 = new List<Expression>(exprs);
            expr1.Add(val.Expression);
            return Value.Dynamic(
                Expression.Block(expr1),
                val.Type);
        }

        public override string ToString() {
            if (ImmediateValue != null)
                return $"Value({ImmediateValue})";

            if (Type.ImmediateValue != null)
                return $"Value(of type {Type.ImmediateValue})";

            return "Value";
        }
    }

}
