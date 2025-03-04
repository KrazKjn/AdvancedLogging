using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System.Collections.Generic;

namespace AdvancedLogging.AutoCoder
{
    /// <summary>
    /// A class that collects all import statements in a Visual Basic syntax tree.
    /// </summary>
    class ImportsCollector : SyntaxWalker
    {
        /// <summary>
        /// A list that stores all the import statements found in the syntax tree.
        /// </summary>
        public readonly List<ImportsStatementSyntax> Imports = new List<ImportsStatementSyntax>();

        /// <summary>
        /// Visits the specified syntax node and collects import statements.
        /// </summary>
        /// <param name="node">The syntax node to visit.</param>
        public override void Visit(SyntaxNode node) //MembersImportsClauseSyntax
        {
            //if (node..Name.GetText() == "System" || node.Name.GetText().StartsWith("System."))

            //if (node.Parent != null)
            //    Imports.Add(node.Parent);
            base.Visit(node);
        }
    }
}
