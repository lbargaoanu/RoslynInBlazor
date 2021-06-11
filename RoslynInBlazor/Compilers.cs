using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Activities;
namespace Designer.BackEnd
{
    using ReferencesDictionary = IReadOnlyDictionary<string, MetadataReference>;
    public class VbBlazorJitCompiler : VbJitCompiler
    {
        public VbBlazorJitCompiler(ReferencesDictionary allReferences, HashSet<Assembly> referencedAssemblies) : base(new HashSet<Assembly>())
        {
            MetadataReferences = referencedAssemblies.Select(a => allReferences[a.GetName().Name]).ToArray();
            Console.WriteLine(string.Join("\n", referencedAssemblies));
        }
    }
    public class VbBlazorAotCompiler : VbAotCompiler
    {
        private IEnumerable<string> _requiredReferences;
        public ReferencesDictionary AllReferences { get; }
        public VbBlazorAotCompiler(ReferencesDictionary allReferences) => AllReferences = allReferences;
        protected override Script<object> Create(string code, ScriptOptions options) => base.Create(code, options.WithReferences(_requiredReferences.Select(name => AllReferences[name])));
        public override TextExpressionCompilerResults Compile(ClassToCompile classToCompile)
        {
            _requiredReferences = classToCompile.ReferencedAssemblies.Select(a => a.GetName().Name);
            Console.WriteLine(string.Join("\n", _requiredReferences));
            return base.Compile(classToCompile with { ReferencedAssemblies = Array.Empty<Assembly>() });
        }
    }
}