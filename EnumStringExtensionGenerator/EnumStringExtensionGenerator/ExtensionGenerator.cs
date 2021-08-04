using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EnumStringExtensionGenerator
{
    /// <summary>
    /// Utility class to track all the potentially-interesting enum types that are found
    /// </summary>
    public class EnumWatchingSyntaxReceiver : ISyntaxReceiver
    {
        public HashSet<EnumDeclarationSyntax> DeclaredEnumsWithAttributes { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is EnumDeclarationSyntax declaration
                && declaration.Kind() == SyntaxKind.EnumDeclaration
                && declaration.AttributeLists.Any())
            {
                DeclaredEnumsWithAttributes.Add(declaration);
            }
        }
    }

    [Generator]
    public class ExtensionGenerator : ISourceGenerator
    {
        public const string LocalisationAttributeName = "GenerateLocalisationAttribute";
        public static readonly string LocalisationAttributeFileName = $"{LocalisationAttributeName}.cs";

        public void Initialize(GeneratorInitializationContext context)
        {
            // Using this allows us to avoid having to recompile all the code every time, which can become expensive
            // in large projects.
            context.RegisterForSyntaxNotifications(() => new EnumWatchingSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                // uncomment to debug the actual build of the target project
                //Debugger.Launch();
                var compilation = context.Compilation;

                var enumsToCheck = (context.SyntaxReceiver as EnumWatchingSyntaxReceiver)?.DeclaredEnumsWithAttributes
                                   ?? new HashSet<EnumDeclarationSyntax>();

                // If we have no enums we need to check, then don't bother adding the localisation attribute or doing
                // any other work
                if (enumsToCheck.Count == 0)
                {
                    return;
                }

                // First off - add our attribute to the source tree so we can reference it
                compilation = AddEmbeddedResourceAsSource(context, compilation, LocalisationAttributeFileName,
                    Assembly.GetExecutingAssembly());

                foreach (var enumDeclarationSyntax in enumsToCheck)
                {
                    var typeSymbol = compilation.GetEnumSymbol(enumDeclarationSyntax);

                    var info = typeSymbol.CreateEnumInfo();
                    if (info.Errors.Any())
                    {
                        ReportErrors(context, info, enumDeclarationSyntax.GetLocation());
                    }

                    if (info.RequestedExtension != null)
                    {
                        var fileContents = info.GenerateExtensionClassFile();
                        context.AddSource(info.ExtensionClassName + ".cs", fileContents);
                    }
                }
            }
            catch (Exception ex)
            {
                var error = new GenerationError("", GenerationErrorCode.UnspecifiedError);
                error.AddData(GenerationError.PropertyNames.ExceptionMessage, ex.ToString());
            }
        }

        private static void ReportErrors(GeneratorExecutionContext context, EnumInfo info, Location location)
        {
            var errors = info.Errors.ToList();

            foreach (var error in errors)
            {
                context.ReportDiagnostic(error.CreateDiagnostic(location));
            }
        }

        /// <summary>
        /// Extracts the specified embedded resource from the target assembly, and adds it into the compilation.
        /// </summary>
        /// <param name="context">The execution context</param>
        /// <param name="compilation">The current <see cref="Compilation"/> object</param>
        /// <param name="embeddedResourceName">The name of the embedded resource to extract</param>
        /// <param name="assemblyContainingDesiredResource">The assembly containing the resource</param>
        /// <returns>The updated <see cref="Compilation"/> with the resource added</returns>
        private static Compilation AddEmbeddedResourceAsSource(GeneratorExecutionContext context,
            Compilation compilation,
            string embeddedResourceName,
            Assembly assemblyContainingDesiredResource)
        {
            var firstSyntaxTree = compilation.SyntaxTrees.FirstOrDefault();
            if (firstSyntaxTree != null)
            {
                var attributeSource = ExtractFileFromAssemblyResource(embeddedResourceName, assemblyContainingDesiredResource);
                context.AddSource(embeddedResourceName, attributeSource);

                var options = (CSharpParseOptions)firstSyntaxTree.Options;
                var attributeSyntaxTree = CSharpSyntaxTree.ParseText(attributeSource, options);

                // We have to redefine compilation here because the old value doesn't contain our attributes
                compilation = compilation.AddSyntaxTrees(attributeSyntaxTree);
            }

            return compilation;
        }

        private static string ExtractFileFromAssemblyResource(string name, Assembly assembly)
        {
            // Determine path
            var resourcePath = assembly.GetManifestResourceNames()
                .Single(str => str.EndsWith(name));

            using Stream stream = assembly.GetManifestResourceStream(resourcePath);
            using StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

    }
}
