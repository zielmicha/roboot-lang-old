namespace MetaComputer.Compiler {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.SymbolStore;
    using System.Diagnostics;
    using System.Linq.Expressions;
    using System.Linq;
    using System.Reflection.Emit;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public class CompiledMethod {
        private MethodInfo methodInfo;

        public CompiledMethod(MethodInfo methodInfo) {
            Debug.Assert(methodInfo != null);
            this.methodInfo = methodInfo;
        }

        public T CreateDelegate<T>() {
            return (T) (object) Delegate.CreateDelegate(typeof(T), methodInfo);
        }
    }

    class ConstantRewriterStage1 : ExpressionVisitor {
        internal TypeBuilder constantFieldStorageType;
        internal Dictionary<ConstantExpression, string> constantFields = new Dictionary<ConstantExpression, string>();

        private static bool CanEmitILConstant(Type type) { // from System.Linq.Expressions.Compiler.ILGen
            switch (Type.GetTypeCode(type)) {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Char:
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Decimal:
                case TypeCode.String:
                    return true;
            }
            return false;
        }

        protected override System.Linq.Expressions.Expression VisitConstant(System.Linq.Expressions.ConstantExpression node) {
            if (!CanEmitILConstant(node.Type) && node.Value != null) {
                string name = "const" + constantFields.Count;
                constantFieldStorageType.DefineField(name, node.Type, FieldAttributes.Public | FieldAttributes.Static);
                constantFields[node] = name;
            }
            return node;
        }
    }

    class ConstantRewriterStage2 : ExpressionVisitor {
        internal Type constantFieldStorageType;
        internal Dictionary<ConstantExpression, string> constantFields;

        protected override System.Linq.Expressions.Expression VisitConstant(System.Linq.Expressions.ConstantExpression node) {
            if (constantFields.ContainsKey(node)) {
                string fieldName = constantFields[node];
                return Expression.Field(null, constantFieldStorageType, fieldName);
            }
            return node;
        }
    }
    
    public class ExpressionAssemblyBuilder {
        private readonly AssemblyBuilder assemblyBuilder;
        private readonly ModuleBuilder moduleBuilder;
        private int functionCounter = 0;

        class MyDebugInfoGenerator : DebugInfoGenerator {
            private ExpressionAssemblyBuilder builder;

            public override void MarkSequencePoint(LambdaExpression lambda, int ilOffset, DebugInfoExpression expression) {

            }
        }

        public ExpressionAssemblyBuilder() {
            AssemblyName assemblyName = new AssemblyName();
            assemblyName.Name = "MetaComputerProgram";
            assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave); // or RunAndSave/RunAndCollect

            Type daType = typeof(DebuggableAttribute);
            ConstructorInfo daCtor = daType.GetConstructor(new Type[] { typeof(DebuggableAttribute.DebuggingModes) });

            CustomAttributeBuilder daBuilder = new CustomAttributeBuilder(daCtor, new object[] {
                    DebuggableAttribute.DebuggingModes.DisableOptimizations |
                    DebuggableAttribute.DebuggingModes.Default });
            assemblyBuilder.SetCustomAttribute(daBuilder);

            moduleBuilder = assemblyBuilder.DefineDynamicModule("metacomputerprogram", "MetaComputerProgram.dll", true);
        }
        
        public CompiledMethod AddMethod(LambdaExpression lambda) {
            int functionId = Interlocked.Increment(ref functionCounter);
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Function" + functionId, TypeAttributes.Public | TypeAttributes.Class);
            MethodBuilder methodBuilder = typeBuilder.DefineMethod("Call", MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.Public, typeof(void), new Type[] { typeof(int) });

            var constantRewriterStage1 = new ConstantRewriterStage1();
            constantRewriterStage1.constantFieldStorageType = moduleBuilder.DefineType("FunctionConst" + functionId, TypeAttributes.Public | TypeAttributes.Class);
            lambda = (LambdaExpression)constantRewriterStage1.Visit(lambda);

            var constantRewriterStage2 = new ConstantRewriterStage2();
            constantRewriterStage2.constantFieldStorageType = constantRewriterStage1.constantFieldStorageType.CreateType();
            constantRewriterStage2.constantFields = constantRewriterStage1.constantFields;
            lambda = (LambdaExpression)constantRewriterStage2.Visit(lambda);

            foreach (var p in constantRewriterStage1.constantFields) {
                constantRewriterStage2.constantFieldStorageType.GetField(p.Value).SetValue(null, p.Key.Value);
            }

            lambda.CompileToMethod(methodBuilder, new MyDebugInfoGenerator());

            Type typeInstance = typeBuilder.CreateType();

            // assemblyBuilder.Save("generated.dll");
            return new CompiledMethod(typeInstance.GetMethod("Call"));
        }
    }
}
