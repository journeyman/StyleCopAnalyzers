﻿// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace StyleCop.Analyzers.ReadabilityRules
{
    using System;
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using StyleCop.Analyzers.Helpers;

    /// <summary>
    /// The parameters to a C# method or indexer call or declaration are not all on the same line or each on a separate
    /// line.
    /// </summary>
    /// <remarks>
    /// <para>A violation of this rule occurs when the parameters to a method or indexer are not all on the same line or
    /// each on its own line. For example:</para>
    /// <code language="csharp">
    /// public string JoinName(string first, string middle,
    ///     string last)
    /// {
    /// }
    /// </code>
    /// <para>The parameters can all be placed on the same line:</para>
    /// <code language="csharp">
    /// public string JoinName(string first, string middle, string last)
    /// {
    /// }
    ///
    /// public string JoinName(
    ///     string first, string middle, string last)
    /// {
    /// }
    /// </code>
    /// <para>Alternatively, each parameter can be placed on its own line:</para>
    /// <code language="csharp">
    /// public string JoinName(
    ///     string first,
    ///     string middle,
    ///     string last)
    /// {
    /// }
    /// </code>
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class SA1117ParametersMustBeOnSameLineOrSeparateLines : DiagnosticAnalyzer
    {
        /// <summary>
        /// The ID for diagnostics produced by the <see cref="SA1117ParametersMustBeOnSameLineOrSeparateLines"/>
        /// analyzer.
        /// </summary>
        public const string DiagnosticId = "SA1117";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(ReadabilityResources.SA1117Title), ReadabilityResources.ResourceManager, typeof(ReadabilityResources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(ReadabilityResources.SA1117MessageFormat), ReadabilityResources.ResourceManager, typeof(ReadabilityResources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(ReadabilityResources.SA1117Description), ReadabilityResources.ResourceManager, typeof(ReadabilityResources));
        private static readonly string HelpLink = "https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1117.md";

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
            context.RegisterSyntaxNodeActionHonorExclusions(
                HandleBaseMethodDeclaration,
                new[] { SyntaxKind.ConstructorDeclaration, SyntaxKind.MethodDeclaration });

            context.RegisterSyntaxNodeActionHonorExclusions(
                HandleConstructorInitializer,
                new[] { SyntaxKind.BaseConstructorInitializer, SyntaxKind.ThisConstructorInitializer });

            context.RegisterSyntaxNodeActionHonorExclusions(HandleDelegateDeclaration, SyntaxKind.DelegateDeclaration);
            context.RegisterSyntaxNodeActionHonorExclusions(HandleIndexerDeclaration, SyntaxKind.IndexerDeclaration);
            context.RegisterSyntaxNodeActionHonorExclusions(HandleInvocation, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeActionHonorExclusions(HandleObjectCreation, SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeActionHonorExclusions(HandleElementAccess, SyntaxKind.ElementAccessExpression);
            context.RegisterSyntaxNodeActionHonorExclusions(HandleElementBinding, SyntaxKind.ElementBindingExpression);
            context.RegisterSyntaxNodeActionHonorExclusions(HandleArrayCreation, SyntaxKind.ArrayCreationExpression);
            context.RegisterSyntaxNodeActionHonorExclusions(HandleAttribute, SyntaxKind.Attribute);
            context.RegisterSyntaxNodeActionHonorExclusions(HandleAnonymousMethod, SyntaxKind.AnonymousMethodExpression);
            context.RegisterSyntaxNodeActionHonorExclusions(HandleLambda, SyntaxKind.ParenthesizedLambdaExpression);
        }

        private static void HandleBaseMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            var declaration = (BaseMethodDeclarationSyntax)context.Node;
            HandleParameterListSyntax(context, declaration.ParameterList);
        }

        private static void HandleInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            HandleArgumentListSyntax(context, invocation.ArgumentList);
        }

        private static void HandleObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;
            HandleArgumentListSyntax(context, objectCreation.ArgumentList);
        }

        private static void HandleIndexerDeclaration(SyntaxNodeAnalysisContext context)
        {
            var indexerDeclaration = (IndexerDeclarationSyntax)context.Node;
            BracketedParameterListSyntax argumentListSyntax = indexerDeclaration.ParameterList;
            SeparatedSyntaxList<ParameterSyntax> arguments = argumentListSyntax.Parameters;

            if (arguments.Count > 2)
            {
                Analyze(context, argumentListSyntax.Parameters);
            }
        }

        private static void HandleElementAccess(SyntaxNodeAnalysisContext context)
        {
            var elementAccess = (ElementAccessExpressionSyntax)context.Node;
            HandleBracketedArgumentListSyntax(context, elementAccess.ArgumentList);
        }

        private static void HandleArrayCreation(SyntaxNodeAnalysisContext context)
        {
            var arrayCreation = (ArrayCreationExpressionSyntax)context.Node;

            foreach (var rankSpecifier in arrayCreation.Type.RankSpecifiers)
            {
                SeparatedSyntaxList<ExpressionSyntax> sizes = rankSpecifier.Sizes;
                if (sizes.Count < 3)
                {
                    continue;
                }

                ExpressionSyntax previousParameter = sizes[1];
                int firstParameterLine = sizes[0].GetLine();
                int previousLine = previousParameter.GetLine();
                Func<int, int, bool> lineCondition;

                if (firstParameterLine == previousLine)
                {
                    // arguments must be on same line
                    lineCondition = (param1Line, param2Line) => param1Line == param2Line;
                }
                else
                {
                    // each argument must be on its own line
                    lineCondition = (param1Line, param2Line) => param1Line != param2Line;
                }

                for (int i = 2; i < sizes.Count; ++i)
                {
                    ExpressionSyntax currentParameter = sizes[i];
                    int currentLine = currentParameter.GetLine();

                    if (lineCondition(previousLine, currentLine))
                    {
                        previousLine = currentLine;
                        continue;
                    }

                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, currentParameter.GetLocation()));
                    return;
                }
            }
        }

        private static void HandleAttribute(SyntaxNodeAnalysisContext context)
        {
            var attribute = (AttributeSyntax)context.Node;
            AttributeArgumentListSyntax argumentListSyntax = attribute.ArgumentList;
            if (argumentListSyntax == null)
            {
                return;
            }

            SeparatedSyntaxList<AttributeArgumentSyntax> arguments = argumentListSyntax.Arguments;
            if (arguments.Count < 3)
            {
                return;
            }

            AttributeArgumentSyntax previousParameter = arguments[1];
            int firstParameterLine = arguments[0].GetLine();
            int previousLine = previousParameter.GetLine();
            Func<int, int, bool> lineCondition;

            if (firstParameterLine == previousLine)
            {
                // arguments must be on same line
                lineCondition = (param1Line, param2Line) => param1Line == param2Line;
            }
            else
            {
                // each argument must be on its own line
                lineCondition = (param1Line, param2Line) => param1Line != param2Line;
            }

            for (int i = 2; i < arguments.Count; ++i)
            {
                AttributeArgumentSyntax currentParameter = arguments[i];
                int currentLine = currentParameter.GetLine();

                if (lineCondition(previousLine, currentLine))
                {
                    previousLine = currentLine;
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(Descriptor, currentParameter.GetLocation()));
                return;
            }
        }

        private static void HandleAnonymousMethod(SyntaxNodeAnalysisContext context)
        {
            var anonymousMethod = (AnonymousMethodExpressionSyntax)context.Node;
            HandleParameterListSyntax(context, anonymousMethod.ParameterList);
        }

        private static void HandleLambda(SyntaxNodeAnalysisContext context)
        {
            var parenthesizedLambda = (ParenthesizedLambdaExpressionSyntax)context.Node;
            HandleParameterListSyntax(context, parenthesizedLambda.ParameterList);
        }

        private static void HandleDelegateDeclaration(SyntaxNodeAnalysisContext context)
        {
            var delegateDeclaration = (DelegateDeclarationSyntax)context.Node;
            HandleParameterListSyntax(context, delegateDeclaration.ParameterList);
        }

        private static void HandleConstructorInitializer(SyntaxNodeAnalysisContext context)
        {
            var constructorInitializer = (ConstructorInitializerSyntax)context.Node;
            HandleArgumentListSyntax(context, constructorInitializer.ArgumentList);
        }

        private static void HandleElementBinding(SyntaxNodeAnalysisContext context)
        {
            var elementBinding = (ElementBindingExpressionSyntax)context.Node;
            HandleBracketedArgumentListSyntax(context, elementBinding.ArgumentList);
        }

        private static void HandleArgumentListSyntax(SyntaxNodeAnalysisContext context, ArgumentListSyntax argumentList)
        {
            if (argumentList == null)
            {
                return;
            }

            SeparatedSyntaxList<ArgumentSyntax> arguments = argumentList.Arguments;
            if (arguments.Count > 2)
            {
                Analyze(context, arguments);
            }
        }

        private static void HandleParameterListSyntax(SyntaxNodeAnalysisContext context, ParameterListSyntax parameterList)
        {
            if (parameterList == null)
            {
                return;
            }

            SeparatedSyntaxList<ParameterSyntax> parameters = parameterList.Parameters;
            if (parameters.Count > 2)
            {
                Analyze(context, parameters);
            }
        }

        private static void HandleBracketedArgumentListSyntax(SyntaxNodeAnalysisContext context, BracketedArgumentListSyntax bracketedArgumentList)
        {
            SeparatedSyntaxList<ArgumentSyntax> arguments = bracketedArgumentList.Arguments;
            if (arguments.Count > 2)
            {
                Analyze(context, arguments);
            }
        }

        private static void Analyze(SyntaxNodeAnalysisContext context, SeparatedSyntaxList<ParameterSyntax> parameters)
        {
            ParameterSyntax previousParameter = parameters[1];
            int firstParameterLine = parameters[0].GetLine();
            int previousLine = previousParameter.GetLine();
            Func<int, int, bool> lineCondition;

            if (firstParameterLine == previousLine)
            {
                // parameters must be on same line
                lineCondition = (param1Line, param2Line) => param1Line == param2Line;
            }
            else
            {
                // each parameter must be on its own line
                lineCondition = (param1Line, param2Line) => param1Line != param2Line;
            }

            for (int i = 2; i < parameters.Count; ++i)
            {
                ParameterSyntax currentParameter = parameters[i];
                int currentLine = currentParameter.GetLine();

                if (lineCondition(previousLine, currentLine))
                {
                    previousLine = currentLine;
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(Descriptor, currentParameter.GetLocation()));
                return;
            }
        }

        private static void Analyze(SyntaxNodeAnalysisContext context, SeparatedSyntaxList<ArgumentSyntax> arguments)
        {
            ArgumentSyntax previousParameter = arguments[1];
            int firstParameterLine = arguments[0].GetLine();
            int previousLine = previousParameter.GetLine();
            Func<int, int, bool> lineCondition;

            if (firstParameterLine == previousLine)
            {
                // arguments must be on same line
                lineCondition = (param1Line, param2Line) => param1Line == param2Line;
            }
            else
            {
                // each argument must be on its own line
                lineCondition = (param1Line, param2Line) => param1Line != param2Line;
            }

            for (int i = 2; i < arguments.Count; ++i)
            {
                ArgumentSyntax currentParameter = arguments[i];
                int currentLine = currentParameter.GetLine();

                if (lineCondition(previousLine, currentLine))
                {
                    previousLine = currentLine;
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(Descriptor, currentParameter.GetLocation()));
                return;
            }
        }
    }
}
