namespace MetaComputer {
    using System;
    using System.IO;
    using Antlr4.Runtime;
    using MetaComputer.Grammar;

    class Program {
        public static void Main(String[] args) {
            Console.WriteLine("hello");
            var input = File.ReadAllText("examples/simple.mco");

            var tlexer = new McGrammarLexer(new AntlrInputStream(input));
            foreach (var token in tlexer.GetAllTokens()) {
                // Console.WriteLine(tlexer.TokenNames[token.Type] + " " + token.Text);
            }

            var lexer = new McGrammarLexer(new AntlrInputStream(input));
            var parser = new McGrammarParser(new CommonTokenStream(lexer));
            parser.AddErrorListener(new ConsoleErrorListener());
            parser.ErrorHandler = new BailErrorStrategy();
            var tree = parser.program();

            Console.WriteLine(tree);
        }
    }

    public class ConsoleErrorListener : IAntlrErrorListener<IToken>
    {
        public virtual void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            Console.Error.WriteLine("line " + line + ":" + charPositionInLine + " " + msg);
        }
    }

}
