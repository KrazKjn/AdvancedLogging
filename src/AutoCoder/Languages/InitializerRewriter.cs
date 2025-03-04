using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

namespace AdvancedLogging.AutoCoder
{
    /// <summary>
    /// Rewrites variable initializers in the syntax tree.
    /// </summary>
    public class InitializerRewriter : CSharpSyntaxRewriter
    {
        private readonly SemanticModel SemanticModel;
        private const int DefaultValue = 42;

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializerRewriter"/> class.
        /// </summary>
        /// <param name="semanticModel">The semantic model to use for symbol information.</param>
        public InitializerRewriter(SemanticModel semanticModel)
        {
            this.SemanticModel = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));
        }

        /// <summary>
        /// Visits a MethodDeclarationSyntax node.
        /// </summary>
        /// <param name="node">The MethodDeclarationSyntax node to visit.</param>
        /// <returns>The modified MethodDeclarationSyntax node, if applicable.</returns>
        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            return base.VisitMethodDeclaration(node);
        }

        /// <summary>
        /// Visits a ConstructorDeclarationSyntax node.
        /// </summary>
        /// <param name="node">The ConstructorDeclarationSyntax node to visit.</param>
        /// <returns>The modified ConstructorDeclarationSyntax node, if applicable.</returns>
        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            return base.VisitConstructorDeclaration(node);
        }

        /// <summary>
        /// Visits a DestructorDeclarationSyntax node.
        /// </summary>
        /// <param name="node">The DestructorDeclarationSyntax node to visit.</param>
        /// <returns>The modified DestructorDeclarationSyntax node, if applicable.</returns>
        public override SyntaxNode VisitDestructorDeclaration(DestructorDeclarationSyntax node)
        {
            return base.VisitDestructorDeclaration(node);
        }

        /// <summary>
        /// Visits an AccessorDeclarationSyntax node.
        /// </summary>
        /// <param name="node">The AccessorDeclarationSyntax node to visit.</param>
        /// <returns>The modified AccessorDeclarationSyntax node, if applicable.</returns>
        public override SyntaxNode VisitAccessorDeclaration(AccessorDeclarationSyntax node)
        {
            return base.VisitAccessorDeclaration(node);
        }

        /// <summary>
        /// Visits a VariableDeclarationSyntax node.
        /// </summary>
        /// <param name="node">The VariableDeclarationSyntax node to visit.</param>
        /// <returns>The modified VariableDeclarationSyntax node, if applicable.</returns>
        public override SyntaxNode VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            // determination of the type of the variable(s)
            var typeSymbol = (ITypeSymbol)this.SemanticModel.GetSymbolInfo(node.Type).Symbol;

            bool changed = false;

            // you could declare more than one variable with one expression
            SeparatedSyntaxList<VariableDeclaratorSyntax> vs = node.Variables;
            // we create a space to improve readability
            SyntaxTrivia space = SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " ");
            for (var i = 0; i < node.Variables.Count; i++)
            {
                if (IsIntType(typeSymbol) && NeedsInitialization(node.Variables[i]))
                {
                    vs = vs.Replace(vs.ElementAt(i), vs.ElementAt(i).WithInitializer(CreateInitializer(space)));
                    changed = true;
                }
            }

            if (changed)
            {
                return node.WithVariables(vs);
            }

            return base.VisitVariableDeclaration(node);
        }

        /// <summary>
        /// Determines if the type symbol represents an integer type.
        /// </summary>
        /// <param name="typeSymbol">The type symbol to check.</param>
        /// <returns>True if the type symbol represents an integer type; otherwise, false.</returns>
        private bool IsIntType(ITypeSymbol typeSymbol)
        {
            return typeSymbol.ToString() == "int";
        }

        /// <summary>
        /// Determines if a variable needs initialization.
        /// </summary>
        /// <param name="variable">The variable to check.</param>
        /// <returns>True if the variable needs initialization; otherwise, false.</returns>
        private bool NeedsInitialization(VariableDeclaratorSyntax variable)
        {
            return variable.Initializer == null || !variable.Initializer.Value.IsEquivalentTo(SyntaxFactory.ParseExpression(DefaultValue.ToString()));
        }

        /// <summary>
        /// Creates an initializer for a variable.
        /// </summary>
        /// <param name="space">The syntax trivia representing a space.</param>
        /// <returns>An EqualsValueClauseSyntax representing the initializer.</returns>
        private EqualsValueClauseSyntax CreateInitializer(SyntaxTrivia space)
        {
            ExpressionSyntax es = SyntaxFactory.ParseExpression(DefaultValue.ToString())
                .WithLeadingTrivia(space);

            return SyntaxFactory.EqualsValueClause(es)
                .WithLeadingTrivia(space);
        }
    }
}
