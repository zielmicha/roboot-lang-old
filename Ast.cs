namespace MetaComputer.Ast {
    using System;
    using System.Collections.Generic;
    using System.Linq;

    struct Location {
        public string Filename;
        public int StartLine;
        public int StartColumn;
        public int EndLine;
        public int EndColumn;
    }

    abstract class Node {
        public Location Location { get; }
    }

    abstract class BlockStmt : Node {
    }

    abstract class Expr : Node {
    }

    class ModuleStmt : Node {

    }

    class ModuleDefStmt : ModuleStmt {
        public List<ModuleStmt> Body;
    }

    class ModuleLetStmt : ModuleStmt {

    }

    class BlockLet : BlockStmt {
        public readonly string Name;
        public readonly Expr Type;
        public readonly Expr Value;

        public BlockLet(string name, Expr type, Expr value) {
            this.Name = name;
            this.Type = type;
            this.Value = value;
        }
    }

    //

    class Call : Expr {
        public readonly Expr Func;
        public readonly IReadOnlyList<Expr> Args;
        public readonly IReadOnlyList<Tuple<string, Expr>> NamedArgs;

        public Call(Expr func, List<Expr> args, List<Tuple<string, Expr>> namedArgs = null) {
            this.Func = func;
            this.Args = args;
            this.NamedArgs = namedArgs ?? new List<Tuple<string, Expr>>();
        }

        public override string ToString() {
            var parameters = new List<String>();
            parameters.AddRange(Args.Select(x => x.ToString()));
            parameters.AddRange(NamedArgs.Select(x => "~{x.Item0}:{x.Item1}"));
            var p = string.Join(",", parameters);
            return $"{Func}({p})";
        }
    }

    class Block : Expr {
        public readonly IReadOnlyList<BlockStmt> Stmts;

        public Block(List<BlockStmt> stmts) {
            this.Stmts = stmts;
        }
    }

    class Name : Expr {
        public readonly string Str;

        public Name(string str) {
            this.Str = str;
        }

        public override string ToString() {
            return Str;
        }
    }

    class FunDefExpr : Expr {
        public Expr Body;
    }
}
