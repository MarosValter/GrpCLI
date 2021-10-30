using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerator.Models;
using System;
using System.Linq;

namespace SourceGenerator
{
    internal static class SyntaxHelper
    {
        internal static DataTypeModel GetTypeInfo(TypeSyntax typeSyntax, DataTypeModel model = null)
        {
            if (model == null)
            {
                model = new DataTypeModel();
            }

            return typeSyntax switch
            {
                GenericNameSyntax genericTypeSyntax => GetTypeInfo(GetGenericArgument(genericTypeSyntax, model), model),
                AliasQualifiedNameSyntax aliasTypeSyntax => GetTypeInfo(GetAliasName(aliasTypeSyntax), model),
                QualifiedNameSyntax qualifiedTypeSyntax => GetDataType(qualifiedTypeSyntax, model),
                SimpleNameSyntax simpleNameSyntax => GetDataType(simpleNameSyntax, model),
                _ => throw new NotSupportedException()
            };
        }

        private static DataTypeModel GetDataType(QualifiedNameSyntax type, DataTypeModel model)
        {
            model.Namespace = type.Left.ToString();
            return GetDataType(type.Right, model);
        }

        private static DataTypeModel GetDataType(SimpleNameSyntax type, DataTypeModel model)
        {
            model.Type = type.Identifier.ValueText;
            return model;
        }

        private static TypeSyntax GetAliasName(AliasQualifiedNameSyntax alias)
        {
            return alias.Name;
        }

        private static TypeSyntax GetGenericArgument(GenericNameSyntax genericType, DataTypeModel model, int position = 0)
        {
            model.Repeated = true;
            return genericType.TypeArgumentList.Arguments.ElementAt(position);
        }
    }
}
