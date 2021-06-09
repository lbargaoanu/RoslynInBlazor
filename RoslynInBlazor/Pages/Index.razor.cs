using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace RoslynInBlazor.Pages
{
    public partial class Index
    {
        private static Dictionary<string, MetadataReference> _loadedAssemblies = new();
        static Index()
        {
            System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.ConsoleTraceListener());
        }
        [Inject]
        public HttpClient HttpClient { get; set; }
        protected override async Task OnInitializedAsync()
        {
            typeof(IEquatable<string>).ToString();
            await LoadAssemblies();
            var watch = Stopwatch.StartNew();
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var compilation = CSharpCompilation.Create(
                "calc.dll",
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, concurrentBuild: false),
                syntaxTrees: new[] { tree },
                references: _loadedAssemblies.Values);
            Assembly compiledAssembly;
            using (var stream = new MemoryStream())
            {
                var compileResult = compilation.Emit(stream);
                Console.WriteLine(string.Join(" ",compileResult.Diagnostics.Select(d=>d.ToString())));
                compiledAssembly = Assembly.Load(stream.GetBuffer());
            }
            watch.Stop();
            Console.WriteLine("Compilation : " + watch.Elapsed.TotalSeconds);
            Type calculator = compiledAssembly.GetType("Calculator");
            MethodInfo evaluate = calculator.GetMethod("Evaluate");
            var answer = evaluate.Invoke(null, null).ToString();
        }
        async Task LoadAssemblies()
        {
            await Task.WhenAll(new[] { typeof(int).Assembly }.Select(a => (a.GetName().Name, a.CodeBase)).Where(a => a.Name.Length > 0 && !a.Name.Contains(".resources") &&
                 !_loadedAssemblies.ContainsKey(a.Name)).Select(async assemblyItem =>
                 {
                     Console.WriteLine("Loading " + assemblyItem.Name);
                     var stream = await HttpClient.GetStreamAsync(Path.Combine("_framework", Path.GetFileName(assemblyItem.CodeBase)));
                     Console.WriteLine("Loaded " + assemblyItem.Name);
                     _loadedAssemblies.Add(assemblyItem.Name, MetadataReference.CreateFromStream(stream));
                 }));
        }
        string text = @"
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