using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Collections.Generic;

namespace MyRunnyApp.Services
{
    public class CodeExecutionService
    {
        private static readonly List<MetadataReference> _cachedReferences;

        static CodeExecutionService()
        {
            _cachedReferences = new List<MetadataReference>
            {
                MetadataReference.CreateFromImage(GetEmbeddedResource("System.Private.CoreLib.dll")),
                MetadataReference.CreateFromImage(GetEmbeddedResource("System.Console.dll")),
                MetadataReference.CreateFromImage(GetEmbeddedResource("System.Runtime.dll"))
            };
        }

        public (bool Success, string Output, string Errors) CompileAndRun(string code, string input = "")
        {
            try
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(code);

                var compilation = CSharpCompilation.Create(
                    "DynamicAssembly",
                    new[] { syntaxTree },
                    _cachedReferences,
                    new CSharpCompilationOptions(OutputKind.ConsoleApplication));

                using var ms = new MemoryStream();
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    var errors = string.Join("\n", result.Diagnostics
                        .Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)
                        .Select(d => d.GetMessage()));
                    return (false, "", errors);
                }

                ms.Seek(0, SeekOrigin.Begin);
                var loadedAssembly = AssemblyLoadContext.Default.LoadFromStream(ms);
                var entryPoint = loadedAssembly.EntryPoint;
                if (entryPoint == null)
                    return (false, "", "No entry point found.");

                using var output = new StringWriter();
                Console.SetOut(output);

                using var inputReader = new StringReader(input);
                Console.SetIn(inputReader);

                var parameters = entryPoint.GetParameters();
                if (parameters.Length == 0)
                {
                    entryPoint.Invoke(null, null);
                }
                else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string[]))
                {
                    entryPoint.Invoke(null, new object[] { null });
                }
                else
                {
                    return (false, "", "Invalid Main method signature.");
                }

                return (true, output.ToString(), "");
            }
            catch (Exception ex)
            {
                return (false, "", $"{ex.Message}\nInner Exception: {ex.InnerException?.Message}");
            }
        }

        private static byte[] GetEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string fullResourceName = $"MyRunnyApp.Resources.{resourceName}";
            using var stream = assembly.GetManifestResourceStream(fullResourceName);
            if (stream == null)
                throw new InvalidOperationException($"Resource {fullResourceName} not found.");

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
    }
}