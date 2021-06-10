using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Compiler
{
    public class Compiler
    {
        public static string Compile(IEnumerable<MetadataReference> references)
        {
            var watch = Stopwatch.StartNew();
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var compilation = CSharpCompilation.Create(
                "calc.dll",
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, concurrentBuild: false),
                syntaxTrees: new[] { tree },
                references : references);
            Assembly compiledAssembly;
            using (var stream = new MemoryStream())
            {
                var compileResult = compilation.Emit(stream);
                Console.WriteLine(string.Join(" ", compileResult.Diagnostics.Select(d => d.ToString())));
                compiledAssembly = Assembly.Load(stream.GetBuffer());
            }
            watch.Stop();
            Console.WriteLine("Compilation : " + watch.Elapsed.TotalSeconds);
            var calculator = compiledAssembly.GetType("Calculator");
            var evaluate = calculator.GetMethod("Evaluate");
            return evaluate.Invoke(null, null).ToString();
        }
        static string text = @"
using System;
public class Calculator
{
    public static object Evaluate()
    {
        return 6*7;
    } 
    public readonly struct MapRequest : IEquatable<MapRequest>
{
    public readonly TypePair RequestedTypes;
    public readonly TypePair RuntimeTypes;
    public MapRequest(in TypePair requestedTypes, in TypePair runtimeTypes)
    {
        RequestedTypes = requestedTypes;
        RuntimeTypes = runtimeTypes;
    }
    public bool Equals(MapRequest other) =>
        RequestedTypes.Equals(other.RequestedTypes) && RuntimeTypes.Equals(other.RuntimeTypes);
    public override bool Equals(object obj) => obj is MapRequest other && Equals(other);
    public static bool operator ==(in MapRequest left, in MapRequest right) => left.Equals(right);
    public static bool operator !=(in MapRequest left, in MapRequest right) => !left.Equals(right);
}
public readonly struct TypePair : IEquatable<TypePair>
{
    public readonly Type SourceType;
    public readonly Type DestinationType;
    public TypePair(Type sourceType, Type destinationType)
    {
        SourceType = sourceType;
        DestinationType = destinationType;
    }
    public bool Equals(TypePair other) => SourceType == other.SourceType && DestinationType == other.DestinationType;
    public override bool Equals(object other) => other is TypePair otherPair && Equals(otherPair);
    public bool IsConstructedGenericType => SourceType.IsConstructedGenericType || DestinationType.IsConstructedGenericType;
    public bool IsGenericTypeDefinition => SourceType.IsGenericTypeDefinition || DestinationType.IsGenericTypeDefinition;
    public bool ContainsGenericParameters => SourceType.ContainsGenericParameters || DestinationType.ContainsGenericParameters;
    public TypePair CloseGenericTypes(in TypePair closedTypes)
    {
        var sourceArguments = closedTypes.SourceType.GenericTypeArguments;
        var destinationArguments = closedTypes.DestinationType.GenericTypeArguments;
        if (sourceArguments.Length == 0)
        {
            sourceArguments = destinationArguments;
        }
        else if (destinationArguments.Length == 0)
        {
            destinationArguments = sourceArguments;
        }
        var closedSourceType = SourceType.IsGenericTypeDefinition ? SourceType.MakeGenericType(sourceArguments) : SourceType;
        var closedDestinationType = DestinationType.IsGenericTypeDefinition ? DestinationType.MakeGenericType(destinationArguments) : DestinationType;
        return new TypePair(closedSourceType, closedDestinationType);
    }
    public TypePair GetTypeDefinitionIfGeneric() => new TypePair(GetTypeDefinitionIfGeneric(SourceType), GetTypeDefinitionIfGeneric(DestinationType));
    private static Type GetTypeDefinitionIfGeneric(Type type) => type.IsGenericType ? type.GetGenericTypeDefinition() : type;
    public static bool operator ==(in TypePair left, in TypePair right) => left.Equals(right);
    public static bool operator !=(in TypePair left, in TypePair right) => !left.Equals(right);
}
}";
    }
}