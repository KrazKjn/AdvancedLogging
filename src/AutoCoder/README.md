# AutoCoder

## Overview

AutoCoder is a C# project designed to automatically add AutoLogging and/or Unhandled Exception error handling to C# and VB.NET code. It leverages the Roslyn API to analyze and modify the code, selected Methods, Properties, Constructors, functions, etc. have the requested enhancements.

## Purpose

The primary purpose of AutoCoder is to ensure that function code blocks have AutoLogging and/or Unhandled Exception error handling.

## Features

- Analyzes C# code using the Roslyn API.
- Automatically adds AutoLogging and/or Unhandled Exception error handling.
- Supports rewriting code blocks of methods, constructors, destructors, and accessors.

## Prerequisites

- .NET Framework 4.8
- C# 7.3
- Visual Studio (or any compatible IDE)

## Installation

1. Clone the repository:
   git clone https://github.com/krazkjn/AdvancedLogging/AutoCoder.git

2. Open the solution in Visual Studio.

## Usage

1. **Initialize the Rewriter:**
    Create an instance of `InitializerRewriter` by passing a `SemanticModel` to its constructor.
 
       var rewriter = new InitializerRewriter(semanticModel);    
    
2. **Rewrite the Syntax Tree:**
    Use the `Visit` method to apply the rewriter to a syntax tree.
 
       var newRoot = rewriter.Visit(syntaxTree.GetRoot()); 
  
3. **Example:**
    Below is an example of how to use the `InitializerRewriter` in a console application.
            
        namespace AutoCoderExample
        {
            class Program
            {
                static void Main(string[] args)
                {
                    var code = @"
                    public class Example
                    {
                        public void Method()
                        {
                            int x;
                        }
                    }";

                    var tree = CSharpSyntaxTree.ParseText(code);
                    var compilation = CSharpCompilation.Create("AutoCoderExample")
                        .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                        .AddSyntaxTrees(tree);

                    var semanticModel = compilation.GetSemanticModel(tree);
                    var rewriter = new InitializerRewriter(semanticModel);
                    var newRoot = rewriter.Visit(tree.GetRoot());

                    Console.WriteLine(newRoot.ToFullString());
                }
            }
        }


## Contributing

Contributions are welcome! Please fork the repository and submit pull requests for any enhancements or bug fixes.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Contact

For any questions or issues, please open an issue on the GitHub repository or contact the maintainer.

---

This ReadMe file provides an overview of the AutoCoder project, its purpose, and instructions on how to use it. It also includes information on prerequisites, installation, usage examples, contributing, and licensing.
