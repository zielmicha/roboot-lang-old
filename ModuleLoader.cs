namespace Roboot.Compiler {
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

    class ModuleLoader {
        private readonly Runtime.Module module;
        private readonly List<(string name, object typeValue, TypeBuilder nativeTypeBuilder)> typesInProgress = new List<(string name, object typeValue, TypeBuilder nativeTypeBuilder)>();

        public ModuleLoader(Runtime.Module module) {
            this.module = module;
        }

        public void LoadCode(string path, string data) {
            var stmts = AstBuilder.ParseModule(AstBuilder.CreateParser(path, data));
            // TODO: sort stmt in topological ordering

            foreach (var stmt in stmts)
                StmtInitType(stmt);

            foreach (var stmt in stmts)
                StmtBuildType(stmt);

            for (var type in typesInProgress)
                FinishType(type);

            foreach (var stmt in stmts)
                StmtDefine(stmt);
        }

        public void StmtInitType(ModuleStmt stmt) {
            if (stmt is ModuleDataTypeStmt s) {
                if (s.FunDef.Params.Count == 0) {
                    module.SetTypeScopeValue(
                        s.Name,
                        new TypeBox((value) => module.SetValue(s.Name, value)));
                } else {
                    var compiler = new FunctionCompiler(new ModuleTypeScope(this.module));
                    var methodCase = compiler.FundefToMethodCase(s.FunDef);
                    var method = new Method(s.Name, methodCase);

                    var (normalValue, typeScopeValue) = TypeBox.CreateGenericTypeBox(method);
                    module.SetValue(s.Name, normalValue);
                    module.SetTypeScopeValue(s.Name, typeScopeValue);
                }
            }
        }

        public void StmtBuildType(ModuleStmt stmt) {
            if (stmt is ModuleDataTypeStmt s) {
                if (s.FunDef.Params.Count == 0) {
                    var compiler = new FunctionCompiler(new ModuleTypeScope(this.module));
                    var compiledValue = compiler.CompileExpr(s.FunDef.Body);

                    var typeValue =
                        RuntimeUtil.RunWithoutSideEffects<object>(() => ExprUtil.EvaluateNow(compiledValue.Expression));

                    var (newTypeValue, nativeTypeBuilder) = (ValueTuple<object, TypeBuilder>)module.CallMethod("nativeStartType", Runtime.Params.Make(s.Name, typeValue));
                    typesInProgress.Add((s.Name, newTypeValue, nativeTypeBuilder));
                    ((TypeBox)module.typeScopeValues[s.Name]).SetTypeBuilder(nativeTypeBuilder);
                }
            }
        }

        public void FinishType((string name, object typeValue, TypeBuilder nativeTypeBuilder) type) {
            module.CallMethod("nativeFinishType", Runtime.Params.Make(type.typeValue, type.nativeTypeBuilder));
        }

        public void StmtDefine(ModuleStmt stmt) {
            switch (stmt) {
                case ModuleLetStmt s:
                    LoadStmt(s);
                    return;
                case ModuleFunStmt s:
                    LoadStmt(s);
                    return;
                case ModuleDataTypeStmt s:
                    // ignore
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


    public class ModuleTypeScope : IScope {
        public readonly Runtime.Module Module;

        public ModuleTypeScope(Runtime.Module module) {
            this.Module = module;
        }

        public Value Lookup(string name) {
            var v = this.Module.GetValue(name);
            return Value.Immediate(v);
        }
    }

}
