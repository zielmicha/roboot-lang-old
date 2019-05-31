namespace MetaComputer.Runtime {

    static class ModuleHelper {
        public static void RegisterMethod(this Module m, string name, Multimethod method) {
            m.SetValue(name, method);
        }

        public static void RegisterMethod(this Module m, string name, bool isBase=false) {
            m.SetValue(name, new Multimethod(name: name, isBase: isBase));
        }

        public static void AddImpl(this Module m, string name, Function function) {
            ((Multimethod)m.GetValue(name)).Implementations.Add(function);
        }
    }
}
