namespace MetaComputer {
    using System;
    using System.IO;
    using Antlr4.Runtime;
    using MetaComputer.Grammar;

    class Program {
        public static void Main(String[] args) {
            Console.WriteLine("hello");
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
