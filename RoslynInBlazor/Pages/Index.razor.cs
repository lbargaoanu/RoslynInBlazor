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
using Microsoft.CodeAnalysis.VisualBasic;

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
            var compilation = VisualBasicCompilation.Create(
                "calc.dll",
                options: new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, concurrentBuild: false),
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
Imports System

Public Class Calculator
    Public Shared Function Evaluate() As Object
        Return 6 * 7
    End Function

    Public Structure MapRequest
        Public ReadOnly RequestedTypes As TypePair
        Public ReadOnly RuntimeTypes As TypePair

        Public Sub New(ByVal requestedTypes As TypePair, ByVal runtimeTypes As TypePair)
            RequestedTypes = requestedTypes
            RuntimeTypes = runtimeTypes
        End Sub

        Public Shared Operator =(ByVal left As MapRequest, ByVal right As MapRequest) As Boolean
            Return left.Equals(right)
        End Operator

        Public Shared Operator <>(ByVal left As MapRequest, ByVal right As MapRequest) As Boolean
            Return Not left.Equals(right)
        End Operator
    End Structure

    Public  Structure TypePair
        Public ReadOnly SourceType As Type
        Public ReadOnly DestinationType As Type

        Public Sub New(ByVal sourceType As Type, ByVal destinationType As Type)
            SourceType = sourceType
            DestinationType = destinationType
        End Sub

        Public ReadOnly Property IsConstructedGenericType As Boolean
            Get
                Return SourceType.IsConstructedGenericType OrElse DestinationType.IsConstructedGenericType
            End Get
        End Property

        Public ReadOnly Property IsGenericTypeDefinition As Boolean
            Get
                Return SourceType.IsGenericTypeDefinition OrElse DestinationType.IsGenericTypeDefinition
            End Get
        End Property

        Public ReadOnly Property ContainsGenericParameters As Boolean
            Get
                Return SourceType.ContainsGenericParameters OrElse DestinationType.ContainsGenericParameters
            End Get
        End Property

        Public Function CloseGenericTypes(ByVal closedTypes As TypePair) As TypePair
            Dim sourceArguments = closedTypes.SourceType.GenericTypeArguments
            Dim destinationArguments = closedTypes.DestinationType.GenericTypeArguments

            If sourceArguments.Length = 0 Then
                sourceArguments = destinationArguments
            ElseIf destinationArguments.Length = 0 Then
                destinationArguments = sourceArguments
            End If

            Dim closedSourceType = If(SourceType.IsGenericTypeDefinition, SourceType.MakeGenericType(sourceArguments), SourceType)
            Dim closedDestinationType = If(DestinationType.IsGenericTypeDefinition, DestinationType.MakeGenericType(destinationArguments), DestinationType)
            Return New TypePair(closedSourceType, closedDestinationType)
        End Function

        Public Function GetTypeDefinitionIfGeneric() As TypePair
            Return New TypePair(GetTypeDefinitionIfGeneric(SourceType), GetTypeDefinitionIfGeneric(DestinationType))
        End Function

        Private Shared Function GetTypeDefinitionIfGeneric(ByVal type As Type) As Type
            Return If(type.IsGenericType, type.GetGenericTypeDefinition(), type)
        End Function

        Public Shared Operator =(ByVal left As TypePair, ByVal right As TypePair) As Boolean
            Return left.Equals(right)
        End Operator

        Public Shared Operator<>(ByVal left As TypePair, ByVal right As TypePair) As Boolean
            Return Not left.Equals(right)
        End Operator

    End Structure
End Class
";
    }
}