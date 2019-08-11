namespace Roboot.Runtime {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Linq;
    using System.Linq.Expressions;
    using Roboot.Runtime;
    using Roboot.Ast;
    using Roboot.AstBuilder;
    using Roboot.Compiler;
    using Roboot.Util;

    public class TypeBox {
        private Optional<TypeBuilder> typeBuilder = Optional<TypeBuilder>.None();
        private Optional<Type> finishedType = Optional<Type>.None();
        private readonly Action<object> onResolve;

        public TypeBox(Action<object> onResolve) {
            this.onResolve = onResolve;
        }

        public void SetTypeBuilder(TypeBuilder builder) {
            typeBuilder = Optional.Some(builder);
        }

        public TypeBuilder GetTypeBuilder() {
            if (finishedType.IsSome())
                throw new Exception("TypeBox is already finished");
            if (typeBuilder.IsNone())
                throw new Exception("trying to finish TypeBox which doesn't have TypeBuilder yet");

            return typeBuilder.Get();
        }

        public void Finish() {
            finishedType = Optional.Some(GetTypeBuilder().CreateType());
            typeBuilder = Optional<TypeBuilder>.None();
            onResolve(finishedType.Get());
        }

        public static (object normalValue, object typeScopeValue) CreateGenericTypeBox(Method method) {
            throw new Exception("generic");
        }

        public static Optional<Type> TryUnbox(object value) {
            if (value is Type t) return Optional.Some(t);
            if (value is TypeBox box) {
                if (box.typeBuilder.IsSome()) return Optional<Type>.Some(box.typeBuilder.Get());
                if (box.finishedType.IsSome()) return Optional<Type>.Some(box.finishedType.Get());
                throw new Exception($"attempting to unbox TypeBox that doesn't yet contain type/TypeBuilder");
            }
            return Optional<Type>.None();
        }

        public static Type Unbox(object value) {
            var v = TryUnbox(value);
            if (v.IsNone())
                throw new Exception($"expected Type or TypeBox, found {value.GetType()}");
            return v.Get();
        }
    }
}
