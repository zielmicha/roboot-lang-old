namespace Roboot.Tests {
    using System;
    using System.Collections.Immutable;
    using Roboot.Ast;
    using Roboot.AstBuilder;
    using Roboot.Runtime;
    using Roboot.Compiler;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    class TestSimple {
        static void Main(string[] args) {
            // var db = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=mydb.db");
            // db.Open();
            // var cmd = new Microsoft.Data.Sqlite.SqliteCommand("create table foo (a int)", db);
            // cmd.ExecuteNonQuery();
            RuntimeContext.InitThread();
            Assert.AreEqual(EvalCode("(1+2+(3-7) == -1) == true"), true);
            Assert.AreEqual(EvalCode("(abs (-1))"), 1L);
            Assert.AreEqual(EvalCode("10 div 2"), 5L);
            Assert.AreEqual(EvalCode("Array Int"), typeof(IImmutableList<Int64>));
            Assert.AreEqual(EvalCode("(0).abs"), 0L);
            Assert.AreEqual(EvalCode("\"foobar\""), "foobar");
            Assert.AreEqual(EvalCode("parseInt \"5\""), 5L);
            //Assert.AreEqual(RuntimeUtil.MakeList(new long[] {1, 2, 3}), RuntimeUtil.MakeList(new long[] {1, 2, 3}));
            //Assert.AreEqual(EvalCode("[1, 2, 3]"), RuntimeUtil.MakeList(new long[] {1, 2, 3}));
            Assert.AreEqual(EvalCode("if true (1) else (2)"), 1L);
            Assert.AreEqual(EvalCode("(1, 2)"), Tuple.Create(1L, 2L));
            Assert.AreEqual(EvalCode("(1, )"), Tuple.Create(1L));
            Assert.AreEqual(EvalCode("(let f=5; f)"), 5L);
            Assert.AreEqual(EvalCode("(let a=1; let b=2; a+b)"), 3L);
            Assert.AreEqual(EvalCode("(let a: Int=1; let b=2; a+b)"), 3L);
            Assert.AreEqual(EvalCode("if true 1 else 2"), 1L);
            Assert.AreEqual(EvalCode("if false 1 else 2"), 2L);
            Assert.AreEqual(EvalCode("5"), 5L);
            Assert.AreEqual(EvalCode("(let f = (x => x + 1); f) 5"), 6L);
            Assert.AreEqual(EvalCode("five"), 5L);
            Console.WriteLine(EvalCode("inc"));
            Assert.AreEqual(EvalCode("inc 5"), 6L);
            // Console.WriteLine(EvalCode("\"123\"|parseInt"));
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
