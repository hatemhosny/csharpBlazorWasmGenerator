using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace MyRunnyApp.Services
{
    public static class CodeRunner
    {
        private static readonly CodeExecutionService _codeExecutionService = new CodeExecutionService();

        [JSInvokable("RunCode")]
        public static Task<CodeResult> RunCode(string code, string input = "")
        {
            var result = _codeExecutionService.CompileAndRun(code, input);
            return Task.FromResult(new CodeResult
            {
                Success = result.Success,
                Output = result.Output,
                Errors = result.Errors
            });
        }
    }

    public class CodeResult
    {
        public bool Success { get; set; }
        public string Output { get; set; }
        public string Errors { get; set; }
    }
}