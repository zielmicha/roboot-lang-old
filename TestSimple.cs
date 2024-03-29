namespace Roboot.Tests {
    using System;
    using System.Collections.Immutable;
    using Roboot.Ast;
    using Roboot.AstBuilder;
    using Roboot.Runtime;
    using Roboot.Compiler;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    class TestSimple {
        static Module module;

        static void Main(string[] args) {
            // var db = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=mydb.db");
            // db.Open();
            // var cmd = new Microsoft.Data.Sqlite.SqliteCommand("create table foo (a int)", db);
            // cmd.ExecuteNonQuery();
            RuntimeContext.InitThread();
            module = new Module();
            module.LoadRobootCode(System.Reflection.Assembly.GetExecutingAssembly(), "roboot.basetest");

            Assert.AreEqual(EvalCode("(1+2+(3-7) == -1) == true"), true);
            Assert.AreEqual(EvalCode("(abs (-1))"), 1L);
            Assert.AreEqual(EvalCode("10 div 2"), 5L);
            Assert.AreEqual(EvalCode("Array Int"), typeof(IImmutableList<Int64>));
            Assert.AreEqual(EvalCode("0|abs"), 0L);
            Assert.AreEqual(EvalCode("\"foobar\""), "foobar");
            Assert.AreEqual(EvalCode("intOfString \"5\""), 5L);
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
            Assert.AreEqual(EvalCode("(let f = (a b => a + b); f 1 2)"), 3L);
            Assert.AreEqual(EvalCode("(let f = (g x => g x); let g = (x => x + 1); f g 2)"), 3L);
            Assert.AreEqual(EvalCode("(let f = (g x => g x); let g = (x => x + 1); g | f 2)"), 3L);
            Assert.AreEqual(EvalCode("(fun f g x => g x; let g = (x => x + 1); g | f 2)"), 3L);
            Assert.AreEqual(EvalCode("slowAdd 5 6"), 11L);

            Console.WriteLine(EvalCode("struct (a: Int; b: Int)"));
            Console.WriteLine(EvalCode("Hello"));
            Console.WriteLine(EvalCode("makeZoo ~field3:5"));
            Assert.AreEqual(EvalCode("field3 (makeZoo ~field3:5)"), 5L);
            Assert.AreEqual(EvalCode("(let z = makeZoo ~field3:5; let h = makeHello ~field1:1 ~field2:2 ~zoo:z; h|zoo|field3)"), 5L);
            Assert.AreEqual(EvalCode("(let z = makeZoo ~field3:5; let h = makeHello ~field1:1 ~field2:2 ~zoo:z; h.zoo.field3)"), 5L);

            //Assert.AreEqual(EvalCode("toString (sexp \"foo\")"), "foo");
            //Assert.AreEqual(EvalCode("(let a = [1,2,3]; a.length)"), 3L);
            //Assert.AreEqual(EvalCode("toString (sexp [sexp \"foo\", sexp \"bar\"])"), "foo,bar");

            //Console.WriteLine(EvalCode("field1 5"));
            // Assert.AreEqual(EvalCode("struct (a: Int; b: Int)"), 11L);
            // Console.WriteLine(EvalCode("\"123\"|parseInt"));
        }

        static object EvalCode(string code) {
            var expr = AstBuilder.ParseExpr(AstBuilder.CreateParser("test", code));
            Console.WriteLine(expr);

            var compiler = new FunctionCompiler(new ModuleScope(module));
            var compiledValue = compiler.CompileExpr(expr);
            // Console.WriteLine("code: " + Util.ExpressionStringBuilder.ExpressionToString(compiledValue.Expression));
            return ExprUtil.EvaluateNow(compiledValue.Expression);
        }
    }
}
