namespace Roboot.Compiler {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Linq;
    using System.Linq.Expressions;
    using Roboot.Runtime;
    using Roboot.Ast;
    using Roboot.AstBuilder;
    using Roboot.Compiler;

    class ModuleLoader {
        private readonly Runtime.Module module;

        public ModuleLoader(Runtime.Module module) {
            this.module = module;
        }

        public void LoadCode(string path, string data) {
            var stmts = AstBuilder.ParseModule(AstBuilder.CreateParser(path, data));
            // TODO: sort stmt in topological ordering
            foreach (var stmt in stmts)
                LoadStmt(stmt);
        }

        public void LoadStmt(ModuleStmt stmt) {
            // Console.WriteLine($"load {stmt}");
            switch (stmt) {
                case ModuleLetStmt s:
                    LoadStmt(s);
                    return;
                case ModuleFunStmt s:
                    LoadStmt(s);
                    return;
                default:
                    throw new Exception($"unknown ModuleStmt {stmt}");
            }
        }

        public void LoadStmt(ModuleLetStmt stmt) {
            object value = Evaluate(
                stmt.Type.IsNone() ? stmt.Value : new Ast.Coerce(stmt.Value, stmt.Type.Get()));
            module.SetValue(stmt.Name, value);
        }

        public void LoadStmt(ModuleFunStmt stmt) {
            var compiler = new FunctionCompiler(new ModuleScope(this.module));
            var methodCase = compiler.FundefToMethodCase(stmt.FunDef);

            var value = new Method(stmt.Name, methodCase);
            module.SetValue(stmt.Name, value);
        }

        private object Evaluate(Expr expr) {
            var compiler = new FunctionCompiler(new ModuleScope(this.module));
            var compiledValue = compiler.CompileExpr(expr);

            return RuntimeUtil.RunWithoutSideEffects<object>(() => ExprUtil.EvaluateNow(compiledValue.Expression));
        }
    }
}
