using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using ZEA.Communications.Messaging.MassTransit.Generators.Attributes;

namespace ZEA.Communications.Messaging.MassTransit.Generators;

[Generator]
public class MassTransitConsumerRegistrationGenerator : ISourceGenerator
{
	public void Initialize(GeneratorInitializationContext context)
	{
		// Register a syntax receiver that will collect candidate classes
		context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
	}

	public void Execute(GeneratorExecutionContext context)
	{
		// Retrieve the populated receiver 
		if (context.SyntaxReceiver is not SyntaxReceiver receiver)
			return;

		// Get the MassTransitConsumerAttribute symbol
		var attributeSymbol = FindTypeByName(context.Compilation, nameof(MassTransitConsumerAttribute));

		if (attributeSymbol == null)
		{
			// Attribute not found; nothing to generate
			return;
		}

		// Get the IConsumer<T> symbol from MassTransit
		var consumerInterfaceSymbol = context.Compilation.GetTypeByMetadataName("MassTransit.IConsumer`1");

		if (consumerInterfaceSymbol == null)
		{
			// IConsumer<T> interface not found; ensure MassTransit is referenced
			return;
		}

		// Collect all consumer information
		var consumers = new List<ConsumerInfo>();

		foreach (var classDeclaration in receiver.CandidateClasses)
		{
			var model = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
			var classSymbol = model.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

			if (classSymbol is null)
				continue;

			var attributeData = classSymbol.GetAttributes()
				.FirstOrDefault(ad => ad.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) == true);

			if (attributeData is null)
				continue;

			// Extract attribute arguments (optional, depending on whether needed for Consumer registration)
			var entityName = attributeData.ConstructorArguments.Length > 0 ? attributeData.ConstructorArguments[0].Value as string : null;
			var subscriptionName = attributeData.ConstructorArguments.Length > 1
				? attributeData.ConstructorArguments[1].Value as string
				: null;
			var queueName = attributeData.ConstructorArguments.Length > 2 ? attributeData.ConstructorArguments[2].Value as string : null;

			if (entityName is null || subscriptionName is null || queueName is null)
				continue;

			// Get the message type from IConsumer<T>
			var interfaceName = GetConsumerInterface(classSymbol, consumerInterfaceSymbol);

			if (interfaceName == "object")
				continue; // Skip if message type not found

			consumers.Add(
				new ConsumerInfo
				{
					ClassName = classSymbol.ToDisplayString(),
					InterfaceName = interfaceName,
					EntityName = entityName,
					SubscriptionName = subscriptionName,
					QueueName = queueName
				}
			);
		}

		if (!consumers.Any())
			return;

		// Generate the Consumer registration code
		var sourceBuilder = new StringBuilder(
			"""
			
			    using MassTransit;
			    using Microsoft.Extensions.DependencyInjection;
			    
			    namespace MassTransitSourceGenerator.Generated
			    {
			        public static class MassTransitConsumersRegistration
			        {
			            public static void AddMassTransitConsumers(this IBusRegistrationConfigurator cfg)
			            {
			    
			"""
		);

		foreach (var consumer in consumers)
		{
			sourceBuilder.AppendLine(
				$"    cfg.AddConsumer<{consumer.ClassName}>();"
			);
		}

		sourceBuilder.AppendLine(
			"""
			        }
			    }
			}
			"""
		);
		// Add the generated source to the compilation
		context.AddSource("MassTransitConsumersRegistration.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
	}

	private static string GetConsumerInterface(
		INamedTypeSymbol classSymbol,
		INamedTypeSymbol consumerInterfaceSymbol)
	{
		// Find the IConsumer<T> interface and get T
		var implementedInterface = classSymbol.AllInterfaces.FirstOrDefault(
			i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, consumerInterfaceSymbol)
		);

		if (implementedInterface == null)
			return "object"; // Fallback

		var messageType = implementedInterface.TypeArguments[0];
		// Return the fully qualified name to avoid namespace issues
		return messageType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
	}

	/// <summary>
	/// Recursively searches the global namespace for a type with the specified name.
	/// </summary>
	private static INamedTypeSymbol? FindTypeByName(
		Compilation compilation,
		string typeName)
	{
		var globalNamespace = compilation.GlobalNamespace;
		var queue = new Queue<INamespaceSymbol>();
		queue.Enqueue(globalNamespace);

		while (queue.Count > 0)
		{
			var currentNamespace = queue.Dequeue();

			foreach (var member in currentNamespace.GetMembers())
			{
				switch (member)
				{
					case INamespaceSymbol namespaceMember:
						queue.Enqueue(namespaceMember);
						break;
					case INamedTypeSymbol typeMember when typeMember.Name == typeName:
						return typeMember;
				}
			}
		}

		return null;
	}

	/// <summary>
	/// Receiver that collects classes with attributes
	/// </summary>
	private class SyntaxReceiver : ISyntaxReceiver
	{
		public List<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax> CandidateClasses { get; } = [];

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			// Look for classes with attributes
			if (syntaxNode is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDeclaration)
			{
				CandidateClasses.Add(classDeclaration);
			}
		}
	}

	private class ConsumerInfo
	{
		public string ClassName { get; init; } = string.Empty;
		public string InterfaceName { get; init; } = string.Empty;
		public string EntityName { get; init; } = string.Empty;
		public string SubscriptionName { get; init; } = string.Empty;
		public string QueueName { get; init; } = string.Empty;
	}
}