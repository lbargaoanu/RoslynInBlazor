using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

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
            await LoadAssemblies();
            var options = ScriptOptions.Default.WithReferences(_loadedAssemblies.Values);
            var script = CSharpScript.Create("1+2", options);
            await script.RunAsync();
        }
        async Task LoadAssemblies()
        {
            Assembly.Load("netstandard");
            await Task.WhenAll(AssemblyLoadContext.Default.Assemblies.Select(a => (a.GetName().Name, a.CodeBase)).Where(a => a.Name.Length > 0 && !a.Name.Contains(".resources") &&
                 !_loadedAssemblies.ContainsKey(a.Name)).Select(async assemblyItem =>
                 {
                     Console.WriteLine("Loading " + assemblyItem.Name);
                     var stream = await HttpClient.GetStreamAsync(Path.Combine("_framework", Path.GetFileName(assemblyItem.CodeBase)));
                     Console.WriteLine("Loaded " + assemblyItem.Name);
                     _loadedAssemblies.Add(assemblyItem.Name, MetadataReference.CreateFromStream(stream));
                 }));
        }
    }
}