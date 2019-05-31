namespace MetaComputer.Runtime {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MetaComputer.Util;

    class Module {
        private List<Module> importedModules = new List<Module>();
        private Dictionary<string, object> values = new Dictionary<string, object>();

        private static object MergeCandidates(List<object> values, string name) {
            if (values.Count == 1) return values[0];

            foreach (object o in values) {
                if (!(o is Multimethod)) {
                    throw new Exception($"{name}: attempt to merge non-method value {o}");
                }
            }

            return Multimethod.Merge(values.Select(x => (Multimethod)x), newName: name);
        }

        public void SetValue(string name, object value) {
            values[name] = value;
        }

        public object GetValue(string name) {
            return values[name];
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
    }
}
