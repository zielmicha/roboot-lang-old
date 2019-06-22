namespace Roboot.Tests {
    using System;
    using Roboot.Ast;
    using Roboot.AstBuilder;
    using Roboot.Runtime;
    using Roboot.Compiler;

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
