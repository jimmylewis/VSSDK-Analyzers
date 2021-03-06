﻿// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

public static class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    public static DiagnosticResult Diagnostic()
        => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, XUnitVerifier>.Diagnostic();

    public static DiagnosticResult Diagnostic(string diagnosticId)
        => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, XUnitVerifier>.Diagnostic(diagnosticId);

    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
        => new DiagnosticResult(descriptor);

    public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new Test { TestCode = source };
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    public static Task VerifyCodeFixAsync(string source, string fixedSource)
        => VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);

    public static Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource)
        => VerifyCodeFixAsync(source, new[] { expected }, fixedSource);

    public static Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource)
    {
        var test = new Test
        {
            TestCode = source,
            FixedCode = fixedSource,
        };

        if (fixedSource == source)
        {
            test.FixedState.InheritanceMode = StateInheritanceMode.AutoInheritAll;
            test.FixedState.MarkupHandling = MarkupMode.Allow;
            test.BatchFixedState.InheritanceMode = StateInheritanceMode.AutoInheritAll;
            test.BatchFixedState.MarkupHandling = MarkupMode.Allow;
        }

        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    public class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier>
    {
        private static readonly MetadataReference PresentationFrameworkReference = MetadataReference.CreateFromFile(typeof(System.Windows.Controls.UserControl).Assembly.Location);
        private static readonly MetadataReference MPFReference = MetadataReference.CreateFromFile(typeof(Microsoft.VisualStudio.Shell.Package).Assembly.Location);

        private static readonly ImmutableArray<string> VSSDKPackageReferences = ImmutableArray.Create(new string[]
        {
            Path.Combine("Microsoft.VisualStudio.OLE.Interop", "7.10.6071", "lib", "Microsoft.VisualStudio.OLE.Interop.dll"),
            Path.Combine("Microsoft.VisualStudio.Shell.Interop", "7.10.6072", "lib\\net11", "Microsoft.VisualStudio.Shell.Interop.dll"),
            Path.Combine("Microsoft.VisualStudio.Shell.Interop.8.0", "8.0.50728", "lib\\net11", "Microsoft.VisualStudio.Shell.Interop.8.0.dll"),
            Path.Combine("Microsoft.VisualStudio.Shell.Interop.9.0", "9.0.30730", "lib\\net11", "Microsoft.VisualStudio.Shell.Interop.9.0.dll"),
            Path.Combine("Microsoft.VisualStudio.Shell.Interop.10.0", "10.0.30320", "lib\\net20", "Microsoft.VisualStudio.Shell.Interop.10.0.dll"),
            Path.Combine("Microsoft.VisualStudio.Shell.Interop.11.0", "11.0.61031", "lib\\net20", "Microsoft.VisualStudio.Shell.Interop.11.0.dll"),
            Path.Combine("Microsoft.VisualStudio.Shell.Interop.14.0", "14.3.26929", "lib\\net20", "Microsoft.VisualStudio.Shell.Interop.14.0.DesignTime.dll"),
            Path.Combine("Microsoft.VisualStudio.Shell.Interop.15.3.DesignTime", "15.0.26929", "lib\\net20", "Microsoft.VisualStudio.Shell.Interop.15.3.DesignTime.dll"),
            Path.Combine("Microsoft.VisualStudio.Shell.Interop.15.6.DesignTime", "15.6.27415", "lib\\net20", "Microsoft.VisualStudio.Shell.Interop.15.6.DesignTime.dll"),
            Path.Combine("Microsoft.VisualStudio.Shell.15.0", "15.6.27415", "lib\\net45", "Microsoft.VisualStudio.Shell.15.0.dll"),
            Path.Combine("Microsoft.VisualStudio.Shell.Framework", "15.6.27415", "lib\\net45", "Microsoft.VisualStudio.Shell.Framework.dll"),
            Path.Combine("Microsoft.VisualStudio.Threading", "16.3.52", "lib\\net472", "Microsoft.VisualStudio.Threading.dll"),
            Path.Combine("Microsoft.VisualStudio.Validation", "15.3.15", "lib\\net45", "Microsoft.VisualStudio.Validation.dll"),
        });

        public Test()
        {
            this.TestState.AdditionalReferences.Add(PresentationFrameworkReference);
            this.TestState.AdditionalReferences.Add(MPFReference);

            this.SolutionTransforms.Add((solution, projectId) =>
            {
                if (this.IncludeVisualStudioSdk)
                {
                    string nugetPackagesFolder = Environment.GetEnvironmentVariable("NUGET_PACKAGES") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
                    foreach (var reference in VSSDKPackageReferences)
                    {
                        solution = solution.AddMetadataReference(projectId, MetadataReference.CreateFromFile(Path.Combine(nugetPackagesFolder, reference)));
                    }
                }

                return solution;
            });
        }

        public bool IncludeVisualStudioSdk { get; set; } = true;
    }
}
