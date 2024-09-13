using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace ZEA.Architecture.Patterns.StrongTypes.Generator.Tests;

public class SampleIncrementalSourceGeneratorTests
{
	private const string VectorClassText = @"
namespace TestNamespace;

[Generators.Report]
public partial class Vector3
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}";

	private const string ExpectedGeneratedClassText = @"// <auto-generated/>

using System;
using System.Collections.Generic;

namespace TestNamespace;

partial class Vector3
{
    public IEnumerable<string> Report()
    {
        yield return $""X:{this.X}"";
        yield return $""Y:{this.Y}"";
        yield return $""Z:{this.Z}"";
    }
}
";

	[Fact]
	public void GenerateReportMethod()
	{
		// Create an instance of the source generator.
		var generator = new SampleIncrementalSourceGenerator();

		// Source generators should be tested using 'GeneratorDriver'.
		var driver = CSharpGeneratorDriver.Create(generator);

		// We need to create a compilation with the required source code.
		var compilation = CSharpCompilation.Create(
			nameof(SampleSourceGeneratorTests),
			new[]
			{
				CSharpSyntaxTree.ParseText(VectorClassText)
			},
			new[]
			{
				// To support 'System.Attribute' inheritance, add reference to 'System.Private.CoreLib'.
				MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
			}
		);

		// Run generators and retrieve all results.
		var runResult = driver.RunGenerators(compilation).GetRunResult();

		// All generated files can be found in 'RunResults.GeneratedTrees'.
		var generatedFileSyntax = runResult.GeneratedTrees.Single(t => t.FilePath.EndsWith("Vector3.g.cs"));

		// Complex generators should be tested using text comparison.
		Assert.Equal(
			ExpectedGeneratedClassText,
			generatedFileSyntax.GetText().ToString(),
			ignoreLineEndingDifferences: true
		);
	}
}