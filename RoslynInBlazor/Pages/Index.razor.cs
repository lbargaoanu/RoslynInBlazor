using System;
using System.Activities;
using System.Activities.Validation;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Designer.BackEnd;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualBasic.Activities;

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
            await RunXaml();
        }
        public async Task<Activity> RunXaml()
        {
            var xaml = await HttpClient.GetStringAsync("SalaryCalculation.xaml");
            var watch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                VisualBasicSettings.Default.CompilerFactory = references => new VbBlazorJitCompiler(_loadedAssemblies, references);
                var root = ActivityXamlServices.Load(new StringReader(xaml)/*, new ActivityXamlServicesSettings { VbCompiler = new VbBlazorAotCompiler(_loadedAssemblies) }*/);
                WorkflowInvoker.Invoke(root);
                return root;
            }
            finally
            {
                watch.Stop();
                Console.WriteLine("Elapsed: " + watch.ElapsedMilliseconds);
            }
        }
        async Task LoadAssemblies()
        {
            Assembly.Load("System.Xaml");
            Assembly.Load("System.Linq.Expressions");
            Assembly.Load("Microsoft.VisualBasic");
            Assembly.Load("Microsoft.VisualBasic.Core");
            Assembly.Load("System.ComponentModel.Primitives");
            Assembly.Load("System.CodeDom");
            await Task.WhenAll(AssemblyLoadContext.Default.Assemblies.Select(a => (a.GetName().Name, a.Location)).Where(a => a.Name.Length > 0 && !a.Name.Contains(".resources") &&
                 !_loadedAssemblies.ContainsKey(a.Name)).Select(async assemblyItem =>
                 {
                     Console.WriteLine("Loading " + assemblyItem.Name);
                     var stream = await HttpClient.GetStreamAsync(Path.Combine("_framework", assemblyItem.Name + ".dll"));
                     Console.WriteLine("Loaded " + assemblyItem.Name);
                     _loadedAssemblies.Add(assemblyItem.Name, MetadataReference.CreateFromStream(stream));
                 }));
        }
    }
    public class Employee
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Salary { get; set; }

        public Employee()
        {
        }

        public Employee(string firstName, string lastName, int salary)
        {
            this.FirstName = firstName;
            this.LastName = lastName;
            this.Salary = salary;
        }
    }
    public struct SalaryStats
    {
        public double MinSalary { get; set; }
        public double MaxSalary { get; set; }
        public double AvgSalary { get; set; }
    }

}