namespace MetaComputer.Runtime {
    using System;

    interface IType {
        bool MatchValue(object value);

        bool? IsSubtype(IType ofType);

        bool? IsSupertype(IType ofType);

        Type GetClrType();
    }

    static class TypeUtil {
        static bool IsSubtype(IType type, IType ofType) {
            return type.IsSubtype(ofType: ofType) == true || ofType.IsSupertype(ofType: type) == true;
        }
    }

    class ClrType<T> : IType {
        public readonly static ClrType<T> Instance = new ClrType<T>();

        public bool MatchValue(object value) {
            return typeof(T).IsAssignableFrom(value.GetType());
        }

        public bool? IsSubtype(IType ofType) {
            return null;
        }

        public bool? IsSupertype(IType ofType) {
            return null;
        }

        public Type GetClrType() {
            return typeof(T);
        }
    }

    class AnyType : IType {
        public readonly static AnyType Instance = new AnyType();

        public bool MatchValue(object value) {
            return true;
        }

        public bool? IsSubtype(IType ofType) {
            return null;
        }

        public bool? IsSupertype(IType ofType) {
            return true;
        }

        public Type GetClrType() {
            return typeof(object);
        }
    }

    struct Unit {}

    class UnitType : ClrType<Unit> {}
}
