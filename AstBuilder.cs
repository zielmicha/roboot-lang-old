namespace MetaComputer.AstBuilder {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using MetaComputer.Grammar;
    using MetaComputer.Ast;
    using Antlr4.Runtime;
    using Antlr4.Runtime.Tree;

    class AstBuilder {
        public static McGrammarParser CreateParser(string fileName, string data) {
            // var input = File.ReadAllText("examples/simple.mco");
            var inputStream = new AntlrInputStream(data);
            inputStream.name = fileName;
            var lexer = new McGrammarLexer(inputStream);
            var parser = new McGrammarParser(new CommonTokenStream(lexer));
            parser.AddErrorListener(new ConsoleErrorListener());
            parser.ErrorHandler = new BailErrorStrategy();
            return parser;
        }

        public static Expr ParseExpr(McGrammarParser parser) {
            var parseTree = parser.expr();
            return new ExprVisitor().Visit(parseTree);
        }
    }

    public class ConsoleErrorListener : IAntlrErrorListener<IToken>
    {
        public virtual void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            Console.Error.WriteLine("line " + line + ":" + charPositionInLine + " " + msg);
        }
    }
    
    static class MakeLocationUtil {
        public static Location MakeLocation(this ParserRuleContext ctx) {
            return new Location() {
                Filename = ctx.Start.InputStream.SourceName,
                StartColumn = ctx.Start.Column + 1,
                StartLine = ctx.Start.Line,
                EndColumn = ctx.Stop.Column + 1,
                EndLine = ctx.Stop.Line,
            };
        }
    }
    
    internal class ExprVisitor : McGrammarBaseVisitor<Expr> {
        public override Expr Visit(IParseTree i) {
            Expr result = base.Visit(i);
            if (result.Location == null)
                result.Location = MakeLocationUtil.MakeLocation((ParserRuleContext)i);
            return result;
        }

        public override Expr VisitFundef_expr(McGrammarParser.Fundef_exprContext e) {
            throw new ArgumentException("invalid fundef");
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

        public override Expr VisitExpr9(McGrammarParser.Expr9Context e) {
            if (e.children.Count == 2) {
                Debug.Assert(e.children[0].GetText() == "-");
                return new Call(
                        func: new Name("-"),
                        args: new List<Expr>() { Visit(e.children[1]) });
            } else {
                return VisitExprN(e.children);
            }
        }

        public override Expr VisitAtom(McGrammarParser.AtomContext e) {
            var value = e.children[0];
            if (value is ITerminalNode terminalNode) {
                if (terminalNode.Symbol.Type == McGrammarLexer.INT)
                    return new IntLiteral(Int64.Parse(terminalNode.ToString()));
            }

            return VisitChildren(e);
        }

        public override Expr VisitIdent(McGrammarParser.IdentContext e) {
            var node = (ITerminalNode)e.children[0];
            return new Name(node.ToString());
        }
        
        public override Expr VisitExpr_atom(McGrammarParser.Expr_atomContext e) {
            if (e.children[0] is ITerminalNode terminalNode) {
                if (terminalNode.GetText() == "(")
                    return Visit(e.children[1]);

                throw new ArgumentException($"unknown expr_atom {e.GetText()}");
            }
            return VisitChildren(e);
        }

        private Expr VisitExprN(IList<IParseTree> e) {
            if (e.Count == 1)
                return Visit(e[0]);

            Debug.Assert(e.Count == 3);

            string op = e[1].GetText();
            return new Call(
                func: new Name(op),
                args: new List<Expr>() { Visit(e[0]), Visit(e[2]) }
            );
        }

        public override Expr VisitFuncall(McGrammarParser.FuncallContext e) {
            if (e.children.Count == 1) {
                return Visit(e.children[0]);
            } else {
                var arguments = new List<McGrammarParser.FuncallargContext>();
                McGrammarParser.FuncallContext current = e;
                while (current.children.Count > 1) {
                    arguments.Add((McGrammarParser.FuncallargContext)current.children[1]);
                    current = (McGrammarParser.FuncallContext)current.children[0];
                }
                var funExpr = Visit(current);

                arguments.Reverse();
                var args = new List<Expr>();
                var namedArgs = new List<KeyValuePair<string, Expr>>();

                foreach (var arg in arguments) {
                    if (arg.children.Count == 1) {
                        args.Add(Visit(arg));
                    } else {
                        var argName = ((Name)Visit(arg.children[1])).Str;
                        var argValue = Visit(arg.children[3]);
                        namedArgs.Add(new KeyValuePair<string, Expr>(argName, argValue));
                    }
                }

                return new Call(funExpr, args, namedArgs);
            }
        }

    }
}
