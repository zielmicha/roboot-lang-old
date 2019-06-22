namespace MetaComputer.Tests {
    using System;
    using MetaComputer.Ast;
    using MetaComputer.AstBuilder;
    using MetaComputer.Runtime;
    using MetaComputer.Compiler;

    class TestSimple {
        static void Main(string[] args) {
            RuntimeContext.InitThread();
            Console.WriteLine(EvalCode("(1+2+(3-7) == -1) == true"));
            Console.WriteLine(EvalCode("(abs (-1))"));
            Console.WriteLine(EvalCode("1 div 0"));
        }

        static object EvalCode(string code) {
            var expr = AstBuilder.ParseExpr(AstBuilder.CreateParser("test", code));
            Console.WriteLine(expr);

            Module baseModule = new BaseModule();
            var compiler = new FunctionCompiler(new ModuleScope(baseModule));
            var compiledValue = compiler.CompileExpr(expr);
            // Console.WriteLine("code: " + Util.ExpressionStringBuilder.ExpressionToString(compiledValue.Expression));
            return ExprUtil.EvaluateNow(compiledValue.Expression);
        }
    }
}
