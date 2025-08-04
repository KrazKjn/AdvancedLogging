using Xunit;
using AdvancedLogging.AutoCoder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;

namespace AdvancedLogging.UnitTests.AutoCoder
{
    public class AutoCoderTests
    {
        [Fact]
        public void AutoLogTryCatchRewriter_Should_Wrap_Method()
        {
            // Arrange
            var code = @"
public class MyClass
{
    public void MyMethod()
    {
        Console.WriteLine(""Hello"");
    }
}";
            var tree = CSharpSyntaxTree.ParseText(code);
            var rewriter = new AutoLogTryCatchRewriter(CodeItems.AutoLog | CodeItems.TryCatch | CodeItems.Method);

            // Act
            var newRoot = rewriter.Visit(tree.GetRoot());
            var newCode = newRoot.ToFullString();

            // Assert
            Assert.Contains("using (var vAutoLogFunction = new AutoLogFunction())", newCode);
            Assert.Contains("try", newCode);
            Assert.Contains("catch (Exception exOuter)", newCode);
        }

        [Fact]
        public void RetryParameterRewriter_Should_Add_Http_Parameters()
        {
            // Arrange
            var code = @"
public class MyClass
{
    public void MyMethod()
    {
        var client = new HttpClient();
        client.GetAsync(""http://test.com"");
    }
}";
            var tree = CSharpSyntaxTree.ParseText(code);
            var httpMethods = new Dictionary<string, bool> { { "GetAsync", true } };
            var sqlMethods = new Dictionary<string, bool>();
            var rewriter = new RetryParameterRewriter(httpMethods, sqlMethods);

            // Act
            var newRoot = rewriter.Visit(tree.GetRoot());
            var newCode = newRoot.ToFullString();

            // Assert
            Assert.Contains("ApplicationSettings.MaxAutoRetriesHttp", newCode);
        }

        [Fact]
        public void RetryParameterRewriter_Should_Add_Sql_Parameters()
        {
            // Arrange
            var code = @"
public class MyClass
{
    public void MyMethod()
    {
        var command = new SqlCommand();
        command.ExecuteNonQuery();
    }
}";
            var tree = CSharpSyntaxTree.ParseText(code);
            var httpMethods = new Dictionary<string, bool>();
            var sqlMethods = new Dictionary<string, bool> { { "ExecuteNonQuery", true } };
            var rewriter = new RetryParameterRewriter(httpMethods, sqlMethods);

            // Act
            var newRoot = rewriter.Visit(tree.GetRoot());
            var newCode = newRoot.ToFullString();

            // Assert
            Assert.Contains("ApplicationSettings.MaxAutoRetriesSql", newCode);
        }
    }
}
