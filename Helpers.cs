namespace Roboot.Util {
    using System;

    public class Helpers {
        public static T Cast<T>(object source) {
            if (!(source is T)) {
                throw new Exception($"cannot cast {source} (type: {source.GetType()} to {typeof(T)}");
            }
            return (T)source;
        }
    }
}
