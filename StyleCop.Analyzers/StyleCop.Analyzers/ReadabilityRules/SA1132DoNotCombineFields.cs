﻿// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace StyleCop.Analyzers.ReadabilityRules
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    /// <summary>
    /// Two or more fields were declared in the same field declaration syntax.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class SA1132DoNotCombineFields : DiagnosticAnalyzer
    {
        /// <summary>
        /// The ID for diagnostics produced by the <see cref="SA1132DoNotCombineFields"/> analyzer.
        /// </summary>
        public const string DiagnosticId = "SA1132";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(ReadabilityResources.SA1132Title), ReadabilityResources.ResourceManager, typeof(ReadabilityResources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(ReadabilityResources.SA1132MessageFormat), ReadabilityResources.ResourceManager, typeof(ReadabilityResources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(ReadabilityResources.SA1132Description), ReadabilityResources.ResourceManager, typeof(ReadabilityResources));
        private static readonly string HelpLink = "https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1132.md";

        private static readonly DiagnosticDescriptor Descriptor =
            new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, AnalyzerCategory.ReadabilityRules, DiagnosticSeverity.Warning, AnalyzerConstants.EnabledByDefault, Description, HelpLink);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
            = ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(HandleCompilationStart);
        }

        private static void HandleCompilationStart(CompilationStartAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionHonorExclusions(HandleDeclaration, SyntaxKind.FieldDeclaration, SyntaxKind.EventFieldDeclaration);
        }

        private static void HandleDeclaration(SyntaxNodeAnalysisContext context)
        {
            var fieldDeclaration = (BaseFieldDeclarationSyntax)context.Node;
            if (fieldDeclaration.Declaration.Variables.Count > 1)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, fieldDeclaration.GetLocation()));
            }
        }
    }
}
