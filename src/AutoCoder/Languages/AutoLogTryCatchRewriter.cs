using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace AdvancedLogging.AutoCoder
{
    public class AutoLogTryCatchRewriter : CSharpSyntaxRewriter
    {
        private readonly CodeItems _codeItems;
        private readonly bool _addAutoLog;
        private readonly bool _addTryCatch;

        public AutoLogTryCatchRewriter(CodeItems codeItems) : base()
        {
            _codeItems = codeItems;
            _addAutoLog = (_codeItems & CodeItems.AutoLog) == CodeItems.AutoLog;
            _addTryCatch = (_codeItems & CodeItems.TryCatch) == CodeItems.TryCatch;
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            if (node.Body == null || !node.Body.Statements.Any())
            {
                return base.VisitConstructorDeclaration(node);
            }

            var newBody = GetNewBody(node.Body, node.ParameterList.Parameters);
            return node.WithBody(newBody);
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.Body == null || !node.Body.Statements.Any())
            {
                return base.VisitMethodDeclaration(node);
            }

            var newBody = GetNewBody(node.Body, node.ParameterList.Parameters);
            return node.WithBody(newBody);
        }

        public override SyntaxNode VisitAccessorDeclaration(AccessorDeclarationSyntax node)
        {
            if (node.Body == null || !node.Body.Statements.Any())
            {
                return base.VisitAccessorDeclaration(node);
            }

            var newBody = GetNewBody(node.Body, null);
            return node.WithBody(newBody);
        }

        private BlockSyntax GetNewBody(BlockSyntax originalBody, SeparatedSyntaxList<ParameterSyntax>? parameters)
        {
            BlockSyntax bodyToWrap = originalBody;

            if (_addTryCatch)
            {
                string paramsString = "System.Reflection.MethodBase.GetCurrentMethod(), error:true, exception:exOuter";
                if (parameters != null && parameters.Value.Any())
                {
                    var paramNames = parameters.Value.Where(p => !p.Modifiers.Any(m => m.IsKind(SyntaxKind.OutKeyword)))
                        .Select(p => p.Identifier.ValueText);
                    if (paramNames.Any())
                    {
                        paramsString = $"new {{ {string.Join(", ", paramNames)} }}, {paramsString}";
                    }
                }

                var catchStatement = SyntaxFactory.ParseStatement($"vAutoLogFunction.LogFunction({paramsString}); throw;");

                var catchClause = SyntaxFactory.CatchClause(
                    SyntaxFactory.CatchDeclaration(
                        SyntaxFactory.IdentifierName("Exception"),
                        SyntaxFactory.Identifier("exOuter")),
                    null,
                    SyntaxFactory.Block(catchStatement));

                var tryStatement = SyntaxFactory.TryStatement(bodyToWrap, new SyntaxList<CatchClauseSyntax>().Add(catchClause), null);
                bodyToWrap = SyntaxFactory.Block(tryStatement);
            }

            if (_addAutoLog)
            {
                string paramsString = "";
                if (parameters != null && parameters.Value.Any())
                {
                    var paramNames = parameters.Value.Where(p => !p.Modifiers.Any(m => m.IsKind(SyntaxKind.OutKeyword)))
                        .Select(p => p.Identifier.ValueText);
                    if (paramNames.Any())
                    {
                        paramsString = $"new {{ {string.Join(", ", paramNames)} }}";
                    }
                }

                var usingStatement = (UsingStatementSyntax)SyntaxFactory.ParseStatement($"using (var vAutoLogFunction = new AutoLogFunction({paramsString})) {{ }}")
                    .WithStatement(bodyToWrap)
                    .NormalizeWhitespace();

                bodyToWrap = SyntaxFactory.Block(usingStatement);
            }

            return bodyToWrap;
        }
    }
}
