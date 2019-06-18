namespace MetaComputer.Tests {
    using System;
    using MetaComputer.Ast;
    using MetaComputer.AstBuilder;
    using MetaComputer.Runtime;
    using MetaComputer.Compiler;

    class TestSimple {
        static void Main(string[] args)
        {
            var expr = AstBuilder.ParseExpr(AstBuilder.CreateParser("test", "1+2+(3-7)"));
            Console.WriteLine(expr);

            Module baseModule = new BaseModule();
            var compiler = new FunctionCompiler(new ModuleScope(baseModule));
            var compiledValue = compiler.CompileExpr(expr);
            Console.WriteLine("code: " + Util.ExpressionStringBuilder.ExpressionToString(compiledValue.Expression));
            Console.WriteLine(ExprUtil.EvaluateNow(compiledValue.Expression));
        }
    }
}
