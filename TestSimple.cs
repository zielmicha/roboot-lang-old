namespace Roboot.Tests {
    using System;
    using Roboot.Ast;
    using Roboot.AstBuilder;
    using Roboot.Runtime;
    using Roboot.Compiler;

    class TestSimple {
        static void Main(string[] args) {
            // var db = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=mydb.db");
            // db.Open();
            // var cmd = new Microsoft.Data.Sqlite.SqliteCommand("create table foo (a int)", db);
            // cmd.ExecuteNonQuery();
            RuntimeContext.InitThread();
            Console.WriteLine(EvalCode("(1+2+(3-7) == -1) == true"));
            Console.WriteLine(EvalCode("(abs (-1))"));
            Console.WriteLine(EvalCode("10 div 2"));
            Console.WriteLine(EvalCode("Array Int"));
            Console.WriteLine(EvalCode("(0).abs"));
            Console.WriteLine(EvalCode("\"foobar\""));
            Console.WriteLine(EvalCode("parseInt \"5\""));
            //Console.WriteLine(EvalCode("\"12345\"|parseInt"));
        }

        static object EvalCode(string code) {
            var expr = AstBuilder.ParseExpr(AstBuilder.CreateParser("test", code));
            Console.WriteLine(expr);

            Module baseModule = new Base.BaseModule();
            var compiler = new FunctionCompiler(new ModuleScope(baseModule));
            var compiledValue = compiler.CompileExpr(expr);
            // Console.WriteLine("code: " + Util.ExpressionStringBuilder.ExpressionToString(compiledValue.Expression));
            return ExprUtil.EvaluateNow(compiledValue.Expression);
        }
    }
}
