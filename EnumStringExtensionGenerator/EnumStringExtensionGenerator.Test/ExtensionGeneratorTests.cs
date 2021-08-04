using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using NUnit.Framework;

namespace EnumStringExtensionGenerator.Test
{
    public class ExtensionGeneratorTests
    {
        private const string LocalisationClassName = "LocalisedStrings";
        private const string EnumName = "SimpleEnum";
        private const string NamespaceOne = "Test.Namespace";
        private const string NamespaceTwo = "Test2.Namespace2";
        private readonly string[] _enumValues = {"One", "Two"};
        private readonly Func<string, (string Name, string ReturnValue)> _standardGenerator = EnumName.PairGenerator();

        /// <summary>
        /// Helper method to generate test cases based on different combinations of matching or missing namespaces.
        /// </summary>
        private static IEnumerable<object[]> GenerateTwoNamespaceCases()
        {
            return new[]
            {
                new object[]{NamespaceOne, NamespaceOne},
                new object[]{NamespaceOne, NamespaceTwo},
                new []{(object) null, NamespaceOne},
                new []{NamespaceOne, (object) null},
                new object[]{null, null}
            };
        }

        [Test]
        public void NoCode()
        {
            var compilation = CreateCompilation();
            var newComp = RunCompilation(compilation, out var generatorDiags, new ExtensionGenerator());
            Assert.That(newComp.SyntaxTrees.Count(), Is.EqualTo(0));
            AssertNoCompilationErrors(generatorDiags);
        }

        [Test]
        public void NoEnum()
        {
            // Similar to the NoCode test, but in this case we have some code to compile, just no enums.
            var localisedStrings = LocalisationClassName
                .MakeLocalisedStrings(_enumValues.Select(_standardGenerator).ToArray())
                .InNamespace(NamespaceOne);

            var compilation = CreateCompilation(localisedStrings);
            var newComp = RunCompilation(compilation, out var generatorDiags, new ExtensionGenerator());
            Assert.That(newComp.SyntaxTrees.Count(), Is.EqualTo(1));
            AssertNoCompilationErrors(generatorDiags);
        }

        [TestCase(NamespaceOne)]
        [TestCase(null)]
        public void NoLocalisationAttribute(string namespaceName)
        {
            var simpleEnumSource = EnumName.MakeEnumWithValues(_enumValues)
                .InNamespace(namespaceName);
            var compilation = CreateCompilation(simpleEnumSource);
            var newComp = RunCompilation(compilation, out var generatorDiags, new ExtensionGenerator());
            Assert.That(newComp.SyntaxTrees.Count(), Is.EqualTo(1));
            AssertNoCompilationErrors(generatorDiags);
        }

        [Test(Description = "Verifies that failing to include the source generator results in compilation errors; just used to verify that the other tests are actually working")]
        public void MissingGenerationCausesCompilationErrors()
        {
            var simpleEnumSource = EnumName.MakeEnumWithValues(_enumValues)
                .WithLocalisation(NamespaceOne, LocalisationClassName)
                .InNamespace(NamespaceOne)
                .UsingExtensionNamespace();
            var localisedStrings = LocalisationClassName
                .MakeLocalisedStrings(_enumValues.Select(_standardGenerator).ToArray())
                .InNamespace(NamespaceOne);
            var testClass = SampleSourceCodeGenerator.CreateCallingFile(NamespaceOne, EnumName, _enumValues);

            var compliation = CreateCompilation(simpleEnumSource, localisedStrings, testClass);
            var newComp = RunCompilation(compliation, out var generatorDiags);
            Assert.That(newComp.SyntaxTrees.Count(), Is.EqualTo(3));
            AssertNoCompilationErrors(generatorDiags);

            // And now run the actual compilation and make sure it fails since we didn't include the source generator.
            var finalResult = RunFinal(newComp);
            Assert.That(finalResult.Success, Is.False);
        }

        [TestCaseSource(nameof(GenerateTwoNamespaceCases))]
        public void NoDefaultNeeded(string enumNamespace, string localisationNamespace)
        {
            var simpleEnumSource = EnumName.MakeEnumWithValues(_enumValues)
                .WithLocalisation(localisationNamespace, LocalisationClassName)
                .InNamespace(enumNamespace)
                .UsingExtensionNamespace();
            var localisedStrings = LocalisationClassName
                .MakeLocalisedStrings(_enumValues.Select(_standardGenerator).ToArray())
                .InNamespace(localisationNamespace);
            var testClass = SampleSourceCodeGenerator.CreateCallingFile(enumNamespace, EnumName, _enumValues);

            var compliation = CreateCompilation(simpleEnumSource, localisedStrings, testClass);
            var newComp = RunCompilation(compliation, out var generatorDiags, new ExtensionGenerator());
            Assert.That(newComp.SyntaxTrees.Count(), Is.EqualTo(5));
            AssertNoCompilationErrors(generatorDiags);

            // This second compile step allows us to verify whether the output code is working correctly
            var finalResult = RunFinal(newComp);
            AssertFinalCompilationSucceeded(finalResult);
        }

        [TestCaseSource(nameof(GenerateTwoNamespaceCases))]
        public void ValidRequiredDefault(string enumNamespace, string localisationNamespace)
        {
            var pairGenerator = EnumName.PairGenerator();
            var defaultPair = pairGenerator("Default");
            var localisationPairs = new[] { defaultPair };

            var simpleEnumSource = EnumName.MakeEnumWithValues(_enumValues)
                .WithLocalisation(localisationNamespace, LocalisationClassName, defaultPair.Name)
                .InNamespace(enumNamespace)
                .UsingExtensionNamespace();

            var localisedStrings = LocalisationClassName
                .MakeLocalisedStrings(localisationPairs)
                .InNamespace(localisationNamespace);

            var testClass = SampleSourceCodeGenerator.CreateCallingFile(enumNamespace, EnumName, _enumValues);

            var compliation = CreateCompilation(simpleEnumSource, localisedStrings, testClass);
            var newComp = RunCompilation(compliation, out var generatorDiags, new ExtensionGenerator());
            Assert.That(newComp.SyntaxTrees.Count(), Is.EqualTo(5));
            AssertNoCompilationErrors(generatorDiags);

            // This second compile step allows us to verify whether the output code is working correctly
            var finalResult = RunFinal(newComp);
            AssertFinalCompilationSucceeded(finalResult);
        }

        [TestCaseSource(nameof(GenerateTwoNamespaceCases))]
        public void MissingRequiredDefault(string enumNamespace, string localisationNamespace)
        {
            var simpleEnumSource = EnumName.MakeEnumWithValues(_enumValues)
                .WithLocalisation(localisationNamespace, LocalisationClassName)
                .InNamespace(enumNamespace)
                .UsingExtensionNamespace();

            var onlyOnePair = _standardGenerator("One");
            var localisedStrings = LocalisationClassName
                .MakeLocalisedStrings(onlyOnePair)
                .InNamespace(localisationNamespace);

            var testClass = SampleSourceCodeGenerator.CreateCallingFile(enumNamespace, EnumName, _enumValues);

            var compliation = CreateCompilation(simpleEnumSource, localisedStrings, testClass);
            var newComp = RunCompilation(compliation, out var generatorDiags, new ExtensionGenerator());
            Assert.That(newComp.SyntaxTrees.Count(), Is.EqualTo(5));
            Assert.That(generatorDiags.Length, Is.EqualTo(1));
            Assert.That(generatorDiags[0].Id, Is.EqualTo(GenerationErrorCode.MissingRequiredDefaultPropertyName.Id()));
        }

        [TestCaseSource(nameof(GenerateTwoNamespaceCases))]
        public void InvalidUnnecessaryDefault(string enumNamespace, string localisationNamespace)
        {
            var defaultPair = _standardGenerator("Default");
            var localisationPairs = _enumValues.Select(_standardGenerator).Concat(new[] { defaultPair }).ToArray();

            var simpleEnumSource = EnumName.MakeEnumWithValues(_enumValues)
                .WithLocalisation(localisationNamespace, LocalisationClassName, "FaultyDefault")
                .InNamespace(enumNamespace)
                .UsingExtensionNamespace();

            var localisedStrings = LocalisationClassName
                .MakeLocalisedStrings(localisationPairs)
                .InNamespace(localisationNamespace);

            var testClass = SampleSourceCodeGenerator.CreateCallingFile(enumNamespace, EnumName, _enumValues);

            var compliation = CreateCompilation(simpleEnumSource, localisedStrings, testClass);
            var newComp = RunCompilation(compliation, out var generatorDiags, new ExtensionGenerator());
            Assert.That(newComp.SyntaxTrees.Count(), Is.EqualTo(5));
            Assert.That(generatorDiags.Length, Is.EqualTo(1));
            Assert.That(generatorDiags[0].Id, Is.EqualTo(GenerationErrorCode.DefaultPropertyNameInvalid.Id()));
        }

        [TestCaseSource(nameof(GenerateTwoNamespaceCases))]
        public void InvalidAndRequiredDefault(string enumNamespace, string localisationNamespace)
        {
            var simpleEnumSource = EnumName.MakeEnumWithValues(_enumValues)
                .WithLocalisation(localisationNamespace, LocalisationClassName, "FaultyDefault")
                .InNamespace(enumNamespace)
                .UsingExtensionNamespace();

            var onlyOnePair = _standardGenerator(_enumValues[0]);
            var localisedStrings = LocalisationClassName
                .MakeLocalisedStrings(onlyOnePair)
                .InNamespace(localisationNamespace);

            var testClass = SampleSourceCodeGenerator.CreateCallingFile(enumNamespace, EnumName, _enumValues);

            var compliation = CreateCompilation(simpleEnumSource, localisedStrings, testClass);
            var newComp = RunCompilation(compliation, out var generatorDiags, new ExtensionGenerator());

            Assert.That(newComp.SyntaxTrees.Count(), Is.EqualTo(5));
            Assert.That(generatorDiags.Length, Is.EqualTo(2));
            AssertDiagnosticIds(generatorDiags, 
                GenerationErrorCode.DefaultPropertyNameInvalid,
                GenerationErrorCode.MissingRequiredDefaultPropertyName);
        }

        #region Helper asserts

        private static void AssertDiagnosticIds(ImmutableArray<Diagnostic> generatorDiags, params GenerationErrorCode[] expectedErrors)
        {
            var actualDiagnosticIds = generatorDiags.Select(diag => diag.Id);
            var expectedDiagnosticIds = expectedErrors.Select(code => code.Id());
            Assert.That(actualDiagnosticIds, Is.EquivalentTo(expectedDiagnosticIds));
        }

        private static void AssertFinalCompilationSucceeded(EmitResult finalResult)
        {
            if (!finalResult.Success)
            {
                foreach (var diagnostic in finalResult.Diagnostics)
                {
                    // This is just for ease of debugging after - it's a lot easier to figure out what went wrong
                    // with compilation if we actually list it in the console.
                    Console.WriteLine("{0}", diagnostic);
                }
            }

            Assert.That(finalResult.Success, Is.True);
        }

        private static void AssertNoCompilationErrors(ImmutableArray<Diagnostic> generatorDiags)
        {
            if (!generatorDiags.IsEmpty)
            {
                foreach (var diagnostic in generatorDiags)
                {
                    Console.WriteLine($"{diagnostic.Id}: {diagnostic.GetMessage()}");
                }

                Assert.Fail(
                    $"There should have been no errors in compilation; instead, we encountered {generatorDiags.Length}");
            }
        }

        #endregion

        #region Compilation helpers

        private static Compilation CreateCompilation(params string[] sources)
        {
            return CSharpCompilation.Create(
                assemblyName: "compilation",
                syntaxTrees: sources.Where(source => !string.IsNullOrWhiteSpace(source)).Select(source => 
                    CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))),
                references: new[] {MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location)},
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication)
            );
        }

        private static GeneratorDriver CreateDriver(Compilation compilation, params ISourceGenerator[] generators) => CSharpGeneratorDriver.Create(
            generators: ImmutableArray.Create(generators),
            additionalTexts: ImmutableArray<AdditionalText>.Empty,
            parseOptions: (CSharpParseOptions)compilation.SyntaxTrees.FirstOrDefault()?.Options ?? new CSharpParseOptions(LanguageVersion.Preview),
            optionsProvider: null
        );

        private static Compilation RunCompilation(Compilation compilation, out ImmutableArray<Diagnostic> diagnostics, params ISourceGenerator[] generators)
        {
            CreateDriver(compilation, generators).RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out diagnostics);

            return updatedCompilation;
        }

        private static EmitResult RunFinal(Compilation compilation)
        {
            using Stream assemblyStream = new MemoryStream();

            var assemblyLocations = TrustedPlatformAssembly.GetDllsToReference();
            var metadata = assemblyLocations.Select(location => MetadataReference.CreateFromFile(location));

            var finalCompilation = compilation.AddReferences(metadata);
            return finalCompilation.Emit(assemblyStream);
        }

        #endregion
    }
}