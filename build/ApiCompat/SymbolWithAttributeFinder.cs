// Copyright (c) 2022 Matthias Wolf, Mawosoft.

/*
    Usage in PowerShell:

    Add-Type -LiteralPath "SymbolWithAttributeFinder.cs" -CompilerOptions '-nowarn:CS1701' -ReferencedAssemblies @(
        [System.Collections.Generic.CollectionExtensions].Assembly.FullName
        [System.Collections.Immutable.ImmutableArray].Assembly.FullName
        [Microsoft.CodeAnalysis.SymbolVisitor].Assembly.FullName
    )

*/

#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ApiCompatHelper
{
    public class SymbolWithAttributeFinder : SymbolVisitor
    {
        private readonly Dictionary<string, List<ISymbol>> _symbolsWithAttribute;
        public bool IncludeTypeForwards { get; set; }

        private void VisitAttributes(ISymbol symbol, IEnumerable<AttributeData> attributes)
        {
            foreach (AttributeData attribute in attributes)
            {
                if (attribute.AttributeClass is not null
                    && _symbolsWithAttribute.TryGetValue(attribute.AttributeClass.Name, out List<ISymbol>? symbolsWithAttribute))
                {
                    symbolsWithAttribute.Add(symbol);
                }
            }
        }

        private void VisitChildren<T>(params T[] children) where T : ISymbol => VisitChildren((IEnumerable<T>)children);

        private void VisitChildren<T>(IEnumerable<T> children) where T : ISymbol
        {
            foreach (T item in children)
            {
                item.Accept(this);
            }
        }

        public SymbolWithAttributeFinder(params string[] attributeNames) : base()
        {
            _symbolsWithAttribute = new Dictionary<string, List<ISymbol>>(attributeNames.Length);
            foreach (string attribute in attributeNames)
            {
                _symbolsWithAttribute[attribute] = new List<ISymbol>();
            }
        }

        public ISymbol[] GetSymbolsWithAttribute(string attributeName)
        {
            if (_symbolsWithAttribute.TryGetValue(attributeName, out List<ISymbol>? symbolsWithAttribute))
            {
                return symbolsWithAttribute.ToArray();
            }
            return Array.Empty<ISymbol>();
        }

        public Dictionary<string, ISymbol> GetSymbolsWithAttributeAsDocIdDictionary(string attributeName)
        {
            if (_symbolsWithAttribute.TryGetValue(attributeName, out List<ISymbol>? symbolsWithAttribute))
            {
                Dictionary<string, ISymbol> docids = new(symbolsWithAttribute.Count);
                foreach (ISymbol symbol in symbolsWithAttribute)
                {
                    string? docid = symbol.GetDocumentationCommentId();
                    if (string.IsNullOrEmpty(docid))
                    {
                        docid = symbol.ContainingSymbol?.GetDocumentationCommentId();
                        docid = string.IsNullOrEmpty(docid) ? "-:" : docid + " <- ";
                        docid += symbol switch
                        {
                            IParameterSymbol parameterSymbol => $"Parameter[{parameterSymbol.Ordinal}]",
                            ITypeParameterSymbol typeParameterSymbol => $"TypeParameter[{typeParameterSymbol.Ordinal}]",
                            _ => $"{symbol.Kind}/{symbol}/{symbol.Name}",
                        };
                    }
                    // Dupes are ignored. Use GetSymbolsWithAttribute() to get all symbols
                    docids.TryAdd(docid, symbol);
                }
                return docids;
            }
            return new Dictionary<string, ISymbol>();
        }

        public override void DefaultVisit(ISymbol symbol)
        {
            base.DefaultVisit(symbol);
            VisitAttributes(symbol, symbol.GetAttributes());
            if (symbol.Kind == SymbolKind.Method)
            {
                VisitAttributes(symbol, ((IMethodSymbol)symbol).GetReturnTypeAttributes());
            }
        }
        public override void Visit(ISymbol? symbol)
        {
            base.Visit(symbol);
            throw new InvalidOperationException("Unexpected call to Visit(ISymbol?)");
        }

        public override void VisitAssembly(IAssemblySymbol symbol)
        {
            base.VisitAssembly(symbol);
            VisitChildren(symbol.Modules);
            if (IncludeTypeForwards)
            {
                VisitChildren(symbol.GetForwardedTypes());
            }
        }

        public override void VisitMethod(IMethodSymbol symbol)
        {
            base.VisitMethod(symbol);
            VisitChildren(symbol.TypeParameters);
            VisitChildren(symbol.Parameters);
        }

        public override void VisitModule(IModuleSymbol symbol)
        {
            base.VisitModule(symbol);
            VisitChildren(symbol.GlobalNamespace);
        }

        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            base.VisitNamedType(symbol);
            VisitChildren(symbol.TypeParameters);
            VisitChildren(symbol.GetMembers());
        }

        public override void VisitNamespace(INamespaceSymbol symbol)
        {
            base.VisitNamespace(symbol);
            VisitChildren(symbol.GetMembers());
        }

        public override void VisitProperty(IPropertySymbol symbol)
        {
            base.VisitProperty(symbol);
            VisitChildren(symbol.Parameters);
        }
    }
}
