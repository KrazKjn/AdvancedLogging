using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace AdvancedLogging.AutoCoder
{
    public class RetryParameterRewriter : CSharpSyntaxRewriter
    {
        private readonly Dictionary<string, bool> _httpMethods;
        private readonly Dictionary<string, bool> _sqlMethods;

        public RetryParameterRewriter(Dictionary<string, bool> httpMethods, Dictionary<string, bool> sqlMethods)
        {
            _httpMethods = httpMethods;
            _sqlMethods = sqlMethods;
        }

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var methodName = memberAccess.Name.Identifier.Text;

                if (_httpMethods.ContainsKey(methodName) && _httpMethods[methodName])
                {
                    if (!node.ArgumentList.Arguments.Any(a => a.ToString().Contains("MaxAutoRetriesHttp")))
                    {
                        var newArguments = node.ArgumentList.AddArguments(
                            SyntaxFactory.Argument(SyntaxFactory.ParseExpression("ApplicationSettings.MaxAutoRetriesHttp")),
                            SyntaxFactory.Argument(SyntaxFactory.ParseExpression("ApplicationSettings.AutoRetrySleepMsHttp")),
                            SyntaxFactory.Argument(SyntaxFactory.ParseExpression("ApplicationSettings.AutoTimeoutIncrementMsHttp"))
                        );
                        return node.WithArgumentList(newArguments);
                    }
                }
                else if (_sqlMethods.ContainsKey(methodName) && _sqlMethods[methodName])
                {
                    if (!node.ArgumentList.Arguments.Any(a => a.ToString().Contains("MaxAutoRetriesSql")))
                    {
                        var newArguments = node.ArgumentList.AddArguments(
                            SyntaxFactory.Argument(SyntaxFactory.ParseExpression("ApplicationSettings.MaxAutoRetriesSql")),
                            SyntaxFactory.Argument(SyntaxFactory.ParseExpression("ApplicationSettings.AutoRetrySleepMsSql")),
                            SyntaxFactory.Argument(SyntaxFactory.ParseExpression("ApplicationSettings.AutoTimeoutIncrementSecondsSql"))
                        );
                        return node.WithArgumentList(newArguments);
                    }
                }
            }

            return base.VisitInvocationExpression(node);
        }
    }
}
