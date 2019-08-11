namespace Roboot.Runtime {
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Linq;
    using Roboot.Util;

    public class Module {
        private List<Module> importedModules = new List<Module>();
        private Dictionary<string, object> values = new Dictionary<string, object>();
        internal Dictionary<string, object> typeScopeValues = new Dictionary<string, object>();

        private static object MergeCandidates(List<object> values, string name) {
            if (values.Count == 1) return values[0];

            foreach (object o in values) {
                if (!(o is Method)) {
                    throw new Exception($"{name}: attempt to merge non-method value {o}");
                }
            }

            return Method.Merge(values.Select(x => (Method)x), newName: name);
        }

        public void AddImportedModule(Module module) {
            if (!importedModules.Contains(module))
                importedModules.Add(module);
        }

        public void SetValue(string name, object value) {
            values[name] = value;
        }

        public object GetValue(string name) {
            return values[name];
        }

        public object GetTypeScopeValue(string name) {
            return typeScopeValues.ContainsKey(name) ?
                typeScopeValues[name] :
                values[name];
        }

        public void SetTypeScopeValue(string name, object value) {
            typeScopeValues[name] = value;
        }

        public void RegisterMethod(string name, Method body) {
            if (!values.ContainsKey(name))
                values[name] = new Method(name);

            if (values[name] is Method m)
                values[name] = Method.Merge(new List<Method>() { m, body }, name);
            else
                throw new Exception($"attempt to register method {name}, but {name} is already a non-method value");
        }

        /// Lookups up a name in the current module.
        /// Returns None if it is not found.
        /// Throws ConflictException if there are multiple conflicting candidates. Merges multimethods.
        public Optional<object> Lookup(string name, bool includeLocal) {
            var candidates = new List<object>();
            if (values.ContainsKey(name)) {
                candidates.Add(values[name]);
            }

            if (includeLocal) {
                foreach (Module imported in importedModules) {
                    var v = imported.Lookup(name, includeLocal: false);
                    if (v.IsSome())
                        candidates.Add(v.Get());
                }
            }

            if (candidates.Count == 0) {
                return Optional<object>.None();
            }

            return Optional<object>.Some(MergeCandidates(candidates, name));
        }

        public void LoadRobootCode(Assembly assembly, string resourcePath) {
            byte[] data;
            using (var stream = assembly.GetManifestResourceStream(resourcePath)) {
                data = new byte[stream.Length];
                stream.Read(data, 0, (int)stream.Length);
            }

            LoadRobootCode("embedded://" + resourcePath.Replace('.', '/') + ".rbt", System.Text.Encoding.UTF8.GetString(data));
        }

        public void LoadRobootCode(string path, string data) {
            new Compiler.ModuleLoader(this).LoadCode(path, data);
        }


        // Helper methods

        public object CallMethod(string name, Runtime.Params parameters) {
            var method = this.Lookup(name, includeLocal: true);
            if (method.IsNone())
                throw new Exception($"no such method {name}");
            return ((ICallable)method.Get()).Call(parameters);
        }
    }
}
