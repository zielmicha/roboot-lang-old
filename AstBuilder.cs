namespace Roboot.AstBuilder {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Roboot.Grammar;
    using Roboot.Ast;
    using Antlr4.Runtime;
    using Antlr4.Runtime.Tree;

    class AstBuilder {
        public static RobootGrammarParser CreateParser(string fileName, string data) {
            // var input = File.ReadAllText("examples/simple.mco");
            var inputStream = new AntlrInputStream(data);
            inputStream.name = fileName;
            var lexer = new RobootGrammarLexer(inputStream);
            var parser = new RobootGrammarParser(new CommonTokenStream(lexer));
            parser.AddErrorListener(new ConsoleErrorListener());
            parser.ErrorHandler = new BailErrorStrategy();
            return parser;
        }

        public static Expr ParseExpr(RobootGrammarParser parser) {
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
    
    internal class ExprVisitor : RobootGrammarBaseVisitor<Expr> {
        public override Expr Visit(IParseTree i) {
            Expr result = base.Visit(i);
            if (result.Location == null)
                result.Location = MakeLocationUtil.MakeLocation((ParserRuleContext)i);
            return result;
        }

        public override Expr VisitFundef_expr(RobootGrammarParser.Fundef_exprContext e) {
            throw new ArgumentException("invalid fundef");
        }

        public override Expr VisitExpr3(RobootGrammarParser.Expr3Context e) {
            return VisitExprN(e.children);
        }

        public override Expr VisitExpr4(RobootGrammarParser.Expr4Context e) {
            return VisitExprN(e.children);
        }

        public override Expr VisitExpr5(RobootGrammarParser.Expr5Context e) {
            return VisitExprN(e.children);
        }

        public override Expr VisitExpr6(RobootGrammarParser.Expr6Context e) {
            return VisitExprN(e.children);
        }

        public override Expr VisitExpr7(RobootGrammarParser.Expr7Context e) {
            return VisitExprN(e.children);
        }

        public override Expr VisitExpr8(RobootGrammarParser.Expr8Context e) {
            return VisitExprN(e.children);
        }

        public override Expr VisitExpr9(RobootGrammarParser.Expr9Context e) {
            if (e.children.Count == 2) {
                Debug.Assert(e.children[0].GetText() == "-");
                return new Call(
                        func: new Name("-"),
                        args: new List<Expr>() { Visit(e.children[1]) });
            } else {
                return VisitExprN(e.children);
            }
        }

        public override Expr VisitAtom(RobootGrammarParser.AtomContext e) {
            var value = e.children[0];
            if (value is ITerminalNode terminalNode) {
                if (terminalNode.Symbol.Type == RobootGrammarLexer.INT)
                    return new IntLiteral(Int64.Parse(terminalNode.ToString()));

                if (terminalNode.Symbol.Type == RobootGrammarLexer.STRING) {
                    string s = terminalNode.ToString();
                    return new StringLiteral(s.Substring(1, s.Count() - 2));
                }
            }

            return VisitChildren(e);
        }

        public override Expr VisitIdent(RobootGrammarParser.IdentContext e) {
            var node = (ITerminalNode)e.children[0];
            return new Name(node.ToString());
        }
        
        public override Expr VisitExpr_atom(RobootGrammarParser.Expr_atomContext e) {
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

            if (op == "." || op == "|") {
                var firstExpr = Visit(e[0]);
                List<Expr> firstArgs = null;
                if (op == ".") firstArgs = new List<Expr>(){ firstExpr };
                if (op == "|") firstArgs = new List<Expr>(){ new Name("map"), firstExpr };
                var rhs = Visit(e[2]);
                if (rhs is Call call) {
                    return new Call(
                        func: call.Func,
                        args: firstArgs.Concat(call.Args).ToList(),
                        namedArgs: call.NamedArgs
                    );
                } else {
                    return new Call(
                        func: rhs,
                        args: firstArgs
                    );
                }
            }
            
            return new Call(
                func: new Name(op),
                args: new List<Expr>() { Visit(e[0]), Visit(e[2]) }
            );
        }

        public override Expr VisitFuncall(RobootGrammarParser.FuncallContext e) {
            if (e.children.Count == 1) {
                return Visit(e.children[0]);
            } else {
                var arguments = new List<RobootGrammarParser.FuncallargContext>();
                RobootGrammarParser.FuncallContext current = e;
                while (current.children.Count > 1) {
                    arguments.Add((RobootGrammarParser.FuncallargContext)current.children[1]);
                    current = (RobootGrammarParser.FuncallContext)current.children[0];
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
