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
    using Roboot.Util;

    class ModuleLoader {
        private readonly Runtime.Module module;
        private readonly List<(string name, object typeValue, TypeBox typeBox)> typesInProgress = new List<(string name, object typeValue, TypeBox typeBox)>();
        private List<ModuleStmt> stmtQueue;

        public ModuleLoader(Runtime.Module module) {
            this.module = module;
        }

        public void LoadCode(string path, string data) {
            stmtQueue = AstBuilder.ParseModule(AstBuilder.CreateParser(path, data));
            // TODO: sort stmt in topological ordering

            for (var round = 0; round < 20; round++) {
                if (stmtQueue.Count() == 0) return;

                var stmts = stmtQueue.ToList();
                stmtQueue = new List<ModuleStmt>();
                typesInProgress.Clear();

                foreach (var stmt in stmts)
                    StmtInitial(stmt);

                foreach (var stmt in stmts)
                    StmtInitType(stmt);

                foreach (var stmt in stmts)
                    StmtBuildType(stmt);

                foreach (var type in typesInProgress)
                    FinishType(type);

                foreach (var stmt in stmts)
                    StmtDefine(stmt);
            }

            throw new Exception("macro recursion limit exceeded");
        }

        public void StmtInitial(ModuleStmt stmt) {
            if (stmt is ModuleImportStmt s) {
                Runtime.Module importedModule = RuntimeContext.CurrentEnv.GetModule(s.Name);
                module.AddImportedModule(importedModule);
            }
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
                stmtQueue.AddRange(ProcessDataTypeMacro(s));

                if (s.FunDef.Params.Count == 0) {
                    var compiler = new FunctionCompiler(new ModuleTypeScope(this.module));
                    var compiledValue = compiler.CompileExpr(s.FunDef.Body);

                    var typeValue =
                        RuntimeUtil.RunWithoutSideEffects<object>(() => ExprUtil.EvaluateNow(compiledValue.Expression));

                    var (newTypeValue, nativeTypeBuilder) = Helpers.Cast<ValueTuple<object, TypeBuilder>>(module.CallMethod("nativeStartType", Runtime.Params.Make(s.Name, typeValue)));
                    var box = Helpers.Cast<TypeBox>(module.typeScopeValues[s.Name]);
                    typesInProgress.Add((s.Name, newTypeValue, box));
                    box.SetTypeBuilder(nativeTypeBuilder);
                }
            }
        }

        public void FinishType((string name, object typeValue, TypeBox typeBox) type) {
            module.CallMethod("nativeFinishType", Runtime.Params.Make(type.typeValue, type.typeBox.GetTypeBuilder()));
            type.typeBox.Finish();
        }

        public IList<ModuleStmt> ProcessDataTypeMacro(Ast.ModuleDataTypeStmt s) {
            var compiler = new FunctionCompiler(new ModuleTypeScope(this.module));
            var methodCase = compiler.FundefToMethodCase(s.FunDef);
            var method = new Method(s.Name, methodCase);
            var parameters = new Runtime.Params(
                arguments: s.FunDef.Params.Where(x => x.Kind == ParamDefKind.positional).Select(x => new Placeholder()).ToList(),
                namedArguments: s.FunDef.Params.Where(x => x.Kind == ParamDefKind.named).ToDictionary(
                    x => x.Name, x => (object)new Placeholder()));

            var placeholderTypeValue =
                RuntimeUtil.RunWithoutSideEffects<object>(() => method.Call(parameters));

            var expr = new Ast.Call(new Ast.Name("dataTypeMacro"), new List<Expr>() {
                    new Ast.StringLiteral(s.Name),
                    new Ast.NativeValue(placeholderTypeValue),
                    new Ast.NativeValue(parameters)
                });
            return EvalMacro(expr);
        }

        public IList<ModuleStmt> EvalMacro(Expr expr) {
            var compiler = new FunctionCompiler(new ModuleScope(this.module));
            var compiledValue = compiler.CompileExpr(expr);

            var stmts =
                RuntimeUtil.RunWithoutSideEffects<object>(() => ExprUtil.EvaluateNow(compiledValue.Expression));
            return Helpers.Cast<IList<ModuleStmt>>(stmts);
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
                case ModuleImportStmt s:
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
            module.RegisterMethod(stmt.Name, value);
        }

        private object Evaluate(Expr expr) {
            var compiler = new FunctionCompiler(new ModuleScope(this.module));
            var compiledValue = compiler.CompileExpr(expr);

            return RuntimeUtil.RunWithoutSideEffects<object>(() => ExprUtil.EvaluateNow(compiledValue.Expression));
        }
    }


    public class ModuleTypeScope : IScope {
        public readonly ModuleScope parent;
        public readonly Runtime.Module module;

        public ModuleTypeScope(Runtime.Module module) {
            this.module = module;
            this.parent = new ModuleScope(module);
        }

        public Value Lookup(string name) {
            if (module.typeScopeValues.ContainsKey(name))
                return Value.Immediate(module.typeScopeValues[name]);
            return this.parent.Lookup(name);
        }
    }

}
