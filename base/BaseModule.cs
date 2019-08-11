namespace Roboot.Base {
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Linq;
    using System.Linq.Expressions;
    using Roboot.Util;
    using Roboot.Runtime;

    public class BaseModule : NativeModule {
        public BaseModule() {
            SetValue("Any", typeof(object));
            SetValue("Never", typeof(Never));

            SetValue("Float", typeof(double));
            RegisterNativeMethod<Func<double, double, double>>("+", (a, b) => checked(a + b));
            RegisterNativeMethod<Func<double, double, double>>("-", (a, b) => checked(a - b));
            RegisterNativeMethod<Func<double, double>>("-", (a) => checked(-a));
            RegisterNativeMethod<Func<double, double, double>>("*", (a, b) => checked(a * b));

            SetValue("Int", typeof(Int64));
            RegisterNativeMethod<Func<Int64, Int64, Int64>>("+", (a, b) => checked(a + b));
            RegisterNativeMethod<Func<Int64, Int64, Int64>>("-", (a, b) => checked(a - b));
            RegisterNativeMethod<Func<Int64, Int64>>("-", (a) => checked(-a));
            RegisterNativeMethod<Func<Int64, Int64, Int64>>("*", (a, b) => checked(a * b));
            RegisterNativeMethod<Func<Int64, Int64>>("abs", (a) => checked(a < 0 ? -a : a));
            RegisterNativeMethod<Func<Int64, Int64, Int64>>("div", (a, b) => checked(a / b));

            RegisterNativeMethod<Func<object, object, bool>>("==", (a, b) => a.Equals(b));
            RegisterNativeMethod<Func<double, double, bool>>("==", (a, b) => Double.IsNaN(a) ? Double.IsNaN(b) : a.Equals(b));

            SetValue("Bool", typeof(bool));
            SetValue("true", true);
            SetValue("false", false);

            SetValue("String", typeof(string)); // TODO: immutable strings

            LoadRobootCode(Assembly.GetExecutingAssembly(), "roboot.base");
        }

        [NativeMethod]
        public long intOfString(string s) {
            return long.Parse(s);
        }

        // Array type

        [NativeMethod(name: "Array")]
        public object array(object itemType) {
            return typeof(IImmutableList<>).MakeGenericType(TypeBox.Unbox(itemType));
        }

        [NativeMethod]
        public object length(System.Collections.IList array) {
            return array.Count;
        }

        // Struct types runtime support

        [NativeMethod]
        public ValueTuple<object, TypeBuilder> nativeStartType(string name, Runtime.Struct typeValue) {
            string fullName = name;
            TypeBuilder typeBuilder = RuntimeContext.CurrentContext.AssemblyBuilder.ModuleBuilder.DefineType(fullName, TypeAttributes.Public | TypeAttributes.Class);

            return (typeValue, typeBuilder);
        }

        [NativeMethod]
        public Unit nativeFinishType(Runtime.Struct typeValue, TypeBuilder typeBuilder) {
            foreach (var field in typeValue.Fields) {
                typeBuilder.DefineField(field.Name, TypeBox.Unbox(field.Type), FieldAttributes.Public | FieldAttributes.InitOnly);
            }

            var constuctor = typeBuilder.DefineConstructor(
                MethodAttributes.Public, CallingConventions.Standard,
                typeValue.Fields.Select(field => TypeBox.Unbox(field.Type)).ToArray()
            );

            ILGenerator ilgen = constuctor.GetILGenerator();
            for (int i = 0; i < typeValue.Fields.Count(); i++) {
                ilgen.Emit(OpCodes.Ldarg_0);
                ilgen.Emit(OpCodes.Ldarg, i + 1);
                ilgen.Emit(OpCodes.Stfld, typeBuilder.GetField(typeValue.Fields[i].Name));
            }
            ilgen.Emit(OpCodes.Ret);

            return Unit.Instance;
        }

        [NativeMethod]
        public IList<Ast.ModuleStmt> dataTypeMacro(string name, Runtime.Struct typeValue, Runtime.Params placeholderParams) {
            // TODO: generic

            var stmts = new List<Ast.ModuleStmt>();

            // Add field getters
            foreach (var field in typeValue.Fields) {
                stmts.Add(new Ast.ModuleFunStmt(
                              field.Name,
                              new Ast.FunDefExpr(
                                  new List<Ast.ParamDef>() { new Ast.ParamDef("o",
                                                                              Ast.ParamDefKind.positional,
                                                                              type: Optional<Ast.Expr>.Some(new Ast.Name(name))) },
                                  new Ast.NativeGetField(new Ast.Name("o"), field.Name)))
                          );
            }

            // Add constructor
            string constuctorName = "make" + name; //name.Substring(0, 1).ToLower() + name.Substring(1);

            stmts.Add(new Ast.ModuleFunStmt(
                          constuctorName,
                          new Ast.FunDefExpr(
                              typeValue.Fields.Select(field => new Ast.ParamDef(
                                                          field.Name, Ast.ParamDefKind.named,
                                                          type: Optional.Some<Ast.Expr>(new Ast.NativeValue(field.Type)))).ToList(),
                              new Ast.NativeConstruct(new Ast.Name(name),
                                                      typeValue.Fields.Select(field => (Ast.Expr)new Ast.Name(field.Name)).ToList())
                          )
                      ));

            return stmts;
        }
    }

}
