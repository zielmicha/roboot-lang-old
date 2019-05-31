namespace MetaComputer.AstBuilder {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using MetaComputer.Grammar;
    using MetaComputer.Ast;
    using Antlr4.Runtime;
    using Antlr4.Runtime.Tree;

    static class MakeLocationUtil {
        static Location MakeLocation(this ParserRuleContext ctx) {
            return new Location() {
                Filename = ctx.Start.InputStream.SourceName,
                StartColumn = ctx.Start.Column,
                StartLine = ctx.Start.Line,
                EndColumn = ctx.Stop.Column,
                EndLine = ctx.Stop.Line,
            };
        }
    }
    
    internal class ExprVisitor : McGrammarBaseVisitor<Expr> {
        public override Expr VisitFundef_expr(McGrammarParser.Fundef_exprContext e) {
            return null;
        }

        public override Expr VisitExpr3(McGrammarParser.Expr3Context e) {
            return VisitExprN(e.children);
        }

        public override Expr VisitExpr4(McGrammarParser.Expr4Context e) {
            return VisitExprN(e.children);
        }

        public override Expr VisitExpr5(McGrammarParser.Expr5Context e) {
            return VisitExprN(e.children);
        }

        public override Expr VisitExpr6(McGrammarParser.Expr6Context e) {
            return VisitExprN(e.children);
        }

        public override Expr VisitExpr7(McGrammarParser.Expr7Context e) {
            return VisitExprN(e.children);
        }

        public override Expr VisitExpr8(McGrammarParser.Expr8Context e) {
            return VisitExprN(e.children);
        }

        private Expr VisitExprN(IList<IParseTree> e) {
            if (e.Count == 1)
                return Visit(e[0]);

            Debug.Assert (e.Count == 3);

            string op = e[1].GetText();
            return new Call(
                func: new Name(op),
                args: new List<Expr>() { Visit(e[0]), Visit(e[1]) }
            );
        }

        public override Expr VisitFuncall(McGrammarParser.FuncallContext e) {
            return VisitExprN(e.children);
        }

    }
}
