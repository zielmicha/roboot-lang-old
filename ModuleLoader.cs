namespace Roboot.Compiler {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Linq;
    using System.Linq.Expressions;
    using Roboot.Runtime;
    using Roboot.Ast;

    class ModuleLoader {
        private readonly Runtime.Module module;

        public ModuleLoader(Runtime.Module module) {
            this.module = module;
        }

        public void LoadCode(string path, string data) {

        }
    }
}
