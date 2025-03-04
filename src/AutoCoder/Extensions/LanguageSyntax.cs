using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System;
using System.Linq;

namespace AdvancedLogging.AutoCoder
{
    /// <summary>
    /// Provides extension methods for analyzing language syntax.
    /// </summary>
    static class LanguageSyntax
    {
        /// <summary>
        /// Determines if the field declaration syntax is of the specified type.
        /// </summary>
        /// <param name="fds">The field declaration syntax.</param>
        /// <param name="_Type">The type to check against.</param>
        /// <returns>True if the field declaration syntax is of the specified type; otherwise, false.</returns>
        public static bool IsType(this FieldDeclarationSyntax fds, Type _Type)
        {
            foreach (VariableDeclaratorSyntax declarator in fds.Declarators)
            {
                // If at least one starting character is uppercase, must register an action
                if (declarator.Names.Any(p => p.Identifier.ToString().ToLower() == _Type.Name.ToLower()))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Determines if the field declaration syntax is of the specified type.
        /// </summary>
        /// <param name="fds">The field declaration syntax.</param>
        /// <param name="_Type">The type to check against.</param>
        /// <returns>True if the field declaration syntax is of the specified type; otherwise, false.</returns>
        public static bool IsType(this FieldDeclarationSyntax fds, String _Type)
        {
            foreach (VariableDeclaratorSyntax declarator in fds.Declarators)
            {
                // If at least one starting character is uppercase, must register an action
                if (declarator.Names.Any(p => p.Identifier.ToString().ToLower().Contains(_Type.ToLower())))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Determines if the property statement syntax is of the specified type.
        /// </summary>
        /// <param name="pss">The property statement syntax.</param>
        /// <param name="_Type">The type to check against.</param>
        /// <returns>True if the property statement syntax is of the specified type; otherwise, false.</returns>
        public static bool IsType(this PropertyStatementSyntax pss, String _Type)
        {
            if (pss.AsClause is null)
            {
                foreach (AttributeListSyntax declarator in pss.AttributeLists)
                {
                    // If at least one starting character is uppercase, must register an action
                    if (declarator.Attributes.Any(p => p.Name.ToString().ToLower().Contains(_Type.ToLower())))
                        return true;
                }
            }
            else
                return pss.AsClause.ToString().Contains(_Type.ToLower());
            return false;
        }

        /// <summary>
        /// Determines if the property block syntax is of the specified type.
        /// </summary>
        /// <param name="pbs">The property block syntax.</param>
        /// <param name="_Type">The type to check against.</param>
        /// <returns>True if the property block syntax is of the specified type; otherwise, false.</returns>
        public static bool IsType(this PropertyBlockSyntax pbs, String _Type)
        {
            foreach (AccessorBlockSyntax declarator in pbs.Accessors)
            {
                // If at least one starting character is uppercase, must register an action
                if (declarator.ToString().ToLower().Contains(_Type.ToLower()))
                    return true;
            }
            return false;
        }
    }
}
