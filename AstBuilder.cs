namespace Roboot.AstBuilder {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Roboot.Grammar;
    using Roboot.Ast;
    using Roboot.Util;
    using Antlr4.Runtime;
    using Antlr4.Runtime.Tree;

    class AstBuilder {
        public static RobootGrammarParser CreateParser(string fileName, string data) {
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

        public static List<ModuleStmt> ParseModule(RobootGrammarParser parser) {
            var parseTree = parser.module();
            var result = new List<ModuleStmt>();
            for (int i = 0; i < parseTree.children.Count; i += 2)
                result.Add(ToModuleStmt((RobootGrammarParser.Module_stmtContext)parseTree.children[i]));
            return result;
        }

        private static ModuleStmt ToModuleStmt(RobootGrammarParser.Module_stmtContext e) {
            var visitor = new ExprVisitor();
            switch (e.children[0]) {
                case RobootGrammarParser.Let_stmtContext letStmt: {
                        var name = ((Name)visitor.Visit(letStmt.children[1])).Str;
                        if (letStmt.children.Count == 4)
                            return new ModuleLetStmt(name: name,
                                                     type: Optional<Expr>.None(),
                                                     value: visitor.Visit(letStmt.children[3]));
                        else
                            return new ModuleLetStmt(name: name,
                                                     type: Optional<Expr>.Some(visitor.Visit(letStmt.children[3])),
                                                     value: visitor.Visit(letStmt.children[5]));
                    }
                case RobootGrammarParser.Fun_stmtContext funStmt: {
                        var name = ((Name)visitor.Visit(funStmt.children[1])).Str;
                        return new ModuleFunStmt(name, (FunDefExpr)visitor.Visit(funStmt.children[2]));
                    }
            }
            throw new ArgumentException($"unknown module stmt {e.children[0].GetType()}");
        }
    }

    public class ConsoleErrorListener : IAntlrErrorListener<IToken> {
        public virtual void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e) {
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

        public override Expr VisitExpr_block(RobootGrammarParser.Expr_blockContext e) {
            var result = new List<BlockStmt>();
            for (var i = 0; i < e.children.Count; i += 2) {
                var node = e.children[i];
                result.Add(this.VisitBlockStmt((RobootGrammarParser.Block_stmtContext)node));
            }
            return new Block(result);
        }

        private BlockStmt VisitBlockStmt(RobootGrammarParser.Block_stmtContext e) {
            switch (e.children[0]) {
                case RobootGrammarParser.ExprContext expr:
                    return new BlockExpr(Visit(expr));
                case RobootGrammarParser.Let_stmtContext letStmt: {
                        var name = ((Name)Visit(letStmt.children[1])).Str;
                        if (letStmt.children.Count == 4)
                            return new BlockLet(name: name,
                                                type: Optional<Expr>.None(),
                                                value: Visit(letStmt.children[3]));
                        else
                            return new BlockLet(name: name,
                                                type: Optional<Expr>.Some(Visit(letStmt.children[3])),
                                                value: Visit(letStmt.children[5]));

                    }
                case RobootGrammarParser.Fun_stmtContext funDef: {
                        var name = ((Name)Visit(funDef.children[1])).Str;
                        return new BlockLet(name: name,
                                            type: Optional<Expr>.None(),
                                            value: Visit(funDef.children[2]));
                    }
            }
            throw new ArgumentException($"unknown blockstmt {e.children[0].GetType()}");
        }

        public override Expr VisitFundef_expr(RobootGrammarParser.Fundef_exprContext e) {
            var parameters = new List<IParseTree>();
            for (int i = 0; i < e.children.Count - 2; i++)
                parameters.Add(e.children[i]);

            var body = Visit(e.children[e.children.Count - 1]);
            return new FunDefExpr(parameters.Select(p => VisitParamDef((RobootGrammarParser.Fundef_argContext)p)).ToList(), body);
        }

        private ParamDef VisitParamDef(RobootGrammarParser.Fundef_argContext e) {
            int pos = 0;
            ParamDefKind kind = ParamDefKind.positional;
            Optional<Expr> type = Optional<Expr>.None();
            Optional<Expr> defaultValue = Optional<Expr>.None();
            string name;

            if (e.children[0].GetText() == "~") {
                pos++;
                kind = ParamDefKind.named;
            }
            if (e.children[0].GetText() == "~~") {
                pos++;
                kind = ParamDefKind.implicit_;
            }

            if (e.children[pos].GetText() == "(") {
                name = e.children[pos + 1].GetText();
                pos += 2;
                if (e.children[pos].GetText() == ":") {
                    type = Optional<Expr>.Some(Visit(e.children[pos + 1]));
                    pos += 2;
                }
                if (e.children[pos].GetText() == "=") {
                    defaultValue = Optional<Expr>.Some(Visit(e.children[pos + 1]));
                    pos += 2;
                }
            } else {
                name = e.children[pos].GetText();
            }

            return new ParamDef(name, kind, defaultValue, type);
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

        public override Expr VisitIf_expr(RobootGrammarParser.If_exprContext e) {
            return new If(
                cond: Visit(e.children[1]),
                then: Visit(e.children[2]),
                else_: e.children.Count == 3 ? Optional<Expr>.None() : Optional<Expr>.Some(Visit(e.children[4]))
            );
        }

        public override Expr VisitExpr_atom(RobootGrammarParser.Expr_atomContext e) {
            if (e.children[0] is ITerminalNode terminalNode) {
                if (terminalNode.GetText() == "(") {
                    if (e.children[1] is RobootGrammarParser.Expr_tupleContext itemCtx) {
                        return new MakeTuple(VisitExprTuple(itemCtx));
                    } else {
                        return Visit(e.children[1]);
                    }
                }

                if (terminalNode.GetText() == "[") {
                    return new MakeList(
                        VisitExprTuple((RobootGrammarParser.Expr_tupleContext)e.children[1])
                    );
                }

                throw new ArgumentException($"unknown expr_atom {e.GetText()}");
            }
            return VisitChildren(e);
        }

        private List<Expr> VisitExprTuple(RobootGrammarParser.Expr_tupleContext e) {
            List<Expr> result = new List<Expr>();
            for (var i = 0; i < e.children.Count; i += 2) {
                result.Add(Visit(e.children[i]));
            }
            return result;
        }

        private Expr VisitExprN(IList<IParseTree> e) {
            if (e.Count == 1)
                return Visit(e[0]);

            Debug.Assert(e.Count == 3);

            string op = e[1].GetText();

            if (op == "$" || op == "|") {
                var firstExpr = Visit(e[0]);
                List<Expr> firstArgs = null;
                if (op == "$") firstArgs = new List<Expr>() { firstExpr };
                if (op == "|") firstArgs = new List<Expr>() { new Name("map"), firstExpr };
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
