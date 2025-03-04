using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using System;
using System.Diagnostics;

namespace AdvancedLogging.AutoCoder
{
    public class VBDeeperWalker : VisualBasicSyntaxWalker
    {
        static int Tabs = 0;

        // Constructor: Initializes the walker with the specified depth to ensure VisitToken() is called.
        public VBDeeperWalker() : base(SyntaxWalkerDepth.Token)
        {
        }

        // Visit: Called for each SyntaxNode in the syntax tree.
        // Increases the indentation level, logs the node kind, and then visits child nodes.
        public override void Visit(SyntaxNode node)
        {
            Tabs++;
            string indents = new String('\t', Tabs);
            string output = indents + node.Kind();
            if (output.Contains("Block"))
            {
                Debug.WriteLine("Block");
            }
            else
                output += " [" + node.ToString() + "]";
            Debug.WriteLine(output);
            base.Visit(node);
            Tabs--;
        }

        // VisitToken: Called for each SyntaxToken in the syntax tree.
        // Logs the token with the current indentation level.
        public override void VisitToken(SyntaxToken token)
        {
            string indents = new String('\t', Tabs);
            string output = indents + token;
            Debug.WriteLine(output);
            base.VisitToken(token);
        }
    }
}
