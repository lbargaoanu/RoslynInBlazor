using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
namespace RoslynInBlazor.Pages
{
    public partial class Index
    {
        static Index()
        {
            System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.ConsoleTraceListener());
        }
        protected override async Task OnInitializedAsync()
        {
            object result = await CSharpScript.EvaluateAsync("1 + 2");
        }
    }
}