# Copyright (c) 2022 Matthias Wolf, Mawosoft.

<#
.SYNOPSIS
    Helpers for using Arcade ApiCompat, Arcade AsmDiff, and the [SymbolWithAttributeFinder] class.
#>

#Requires -Version 7

# Don't rely on these usings for OutputType and parameter type declarations!
using namespace System
using namespace System.Collections.Generic
using namespace System.Reflection
using namespace Microsoft.CodeAnalysis
using namespace Microsoft.CodeAnalysis.CSharp
using namespace System.Xml

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot/../startNativeExecution.ps1"

# These will be initialized to the full assembly path and can be invoked via 'dotnet <asmpath>'.
[string]$script:AsmDiffDll = $null
[string]$script:ApiCompatDll = $null

[string]$restoreProjectFilePath = Join-Path $PSScriptRoot "ToolRestore/ToolRestore.proj"
[string]$restoreNugetFilePath = Join-Path $PSScriptRoot "ToolRestore/obj/ToolRestore.proj.nuget.g.props"
[string]$symbolWithAttributeFinderPath = Join-Path $PSScriptRoot "SymbolWithAttributeFinder.cs"

# .SYNOPSIS
#   Installs Arcade ApiCompat, Arcade AsmDiff, and adds the [SymbolWithAttributeFinder] class.
# .NOTES
#   The ApiCompatRestore.proj is used with 'dotnet restore' to install ApiCompat and AsmDiff.
#   The -Force switch can re-install the tools, but the class will only be added on the first call.
function Install-ApiCompatTools {
    [CmdletBinding()]
    param(
        [switch]$Force
    )

    if ($Force.IsPresent) {
        $script:AsmDiffDll = $null
        $script:ApiCompatDll = $null
        Remove-Item $restoreNugetFilePath -ErrorAction Ignore
    }
    if (-not $script:AsmDiffDll -or -not $script:ApiCompatDll) {
        if (-not (Test-Path $restoreNugetFilePath -PathType Leaf)) {
            Start-NativeExecution { dotnet restore $restoreProjectFilePath } -VerboseOutputOnError
        }
        [XmlNode]$docElem = (Select-Xml -Path $restoreNugetFilePath -XPath '/*').Node
        [hashtable]$xmlns = @{ ns = $docElem.NamespaceURI }
        [string]$AsmDiffPkg = (Select-Xml -xml $docElem -XPath '//ns:PkgMicrosoft_DotNet_AsmDiff' -Namespace $xmlns).Node.InnerText
        [string]$ApiCompatPkg = (Select-Xml -xml $docElem -XPath '//ns:PkgMicrosoft_DotNet_ApiCompat' -Namespace $xmlns).Node.InnerText
        if (-not $AsmDiffPkg -or -not $ApiCompatPkg) {
            throw 'Unable to restore ApiCompat tools.'
        }
        $docElem = (Select-Xml -Path $restoreProjectFilePath -XPath '/*').Node
        $xmlns = @{ ns = $docElem.NamespaceURI }
        $script:AsmDiffDll = Join-Path $AsmDiffPkg -ChildPath (
            Select-Xml -xml $docElem -XPath '//ns:PackageReference[@Include="Microsoft.DotNet.AsmDiff"]' -Namespace $xmlns
        ).Node.RelativeToolPath
        $script:ApiCompatDll = Join-Path $ApiCompatPkg -ChildPath (
            Select-Xml -xml $docElem -XPath '//ns:PackageReference[@Include="Microsoft.DotNet.ApiCompat"]' -Namespace $xmlns
        ).Node.RelativeToolPath
    }
    # Notes:
    # - If the type source is unchanged, Add-Type would not trow any error if adding it again.
    # - Cannot use [Type]::GetType() here, it needs the AssemblyQualifiedName.
    try { $null = [ApiCompatHelper.SymbolWithAttributeFinder] }
    catch {
        Add-Type -LiteralPath $symbolWithAttributeFinderPath -CompilerOptions '-nowarn:CS1701' -ReferencedAssemblies @(
            [System.Collections.Generic.CollectionExtensions].Assembly.FullName
            [System.Collections.Immutable.ImmutableArray].Assembly.FullName
            [Microsoft.CodeAnalysis.SymbolVisitor].Assembly.FullName
        )
    }
}

# .SYNOPSIS
#   Invokes ApiCompat with the given ArgumentList
# .NOTES
#   This is a wrapper for Start-NativeExecution { dotnet $ApiCompatDll $ArgumentList }
# .OUTPUTS
#   Any tool output if not supressed by switches passed to Start-NativeExecution.
function Invoke-ApiCompat {
    [CmdletBinding()]
    param(
        # Passed to Start-NativeExecution
        [switch]$IgnoreExitcode,
        # Passed to Start-NativeExecution
        [switch]$VerboseOutputOnError,
        # Passed to ApiCompat
        [Alias('Args')]
        [string[]]$ArgumentList
    )
    if (-not $script:ApiCompatDll) { Install-ApiCompatTools }
    Start-NativeExecution { dotnet $script:ApiCompatDll $ArgumentList } -IgnoreExitCode:$IgnoreExitcode -VerboseOutputOnError:$VerboseOutputOnError
}

# .SYNOPSIS
#   Invokes AsmDiff with the given ArgumentList
# .NOTES
#   This is a wrapper for Start-NativeExecution { dotnet $AsmDiffDll $ArgumentList }
# .OUTPUTS
#   Any tool output if not supressed by switches passed to Start-NativeExecution.
function Invoke-AsmDiff {
    [CmdletBinding()]
    param(
        # Passed to Start-NativeExecution
        [switch]$IgnoreExitcode,
        # Passed to Start-NativeExecution
        [switch]$VerboseOutputOnError,
        # Passed to AsmDiff
        [Alias('Args')]
        [string[]]$ArgumentList
    )
    if (-not $script:AsmDiffDll) { Install-ApiCompatTools }
    Start-NativeExecution { dotnet $script:AsmDiffDll $ArgumentList } -IgnoreExitCode:$IgnoreExitcode -VerboseOutputOnError:$VerboseOutputOnError
}

# .SYNOPSIS
#   Formats symbol from ApiCompat message for better readability.
# .OUTPUTS
#   [String] The formatted symbol.
function Format-ApiCompatSymbol {
    [CmdletBinding()]
    [OutputType([string])]
    param(
        # The symbol from the message, without the single quotes
        [Parameter(ValueFromPipeline)]
        [Alias('s')]
        [string]$Symbol,

        # How to handle return type (and visibility)
        [ValidateSet('Include', 'Trimmed', 'Exclude')]
        [Alias('r')]
        [string]$ReturnType,

        # Optional tags to highlight the method name, e.g. '`**`' or '<b>','</b>'
        [ValidateCount(1, 2)]
        [Alias('mt')]
        [string[]]$MethodTags,

        # Html-encode the symbol. Note that the method tags will not be encoded.
        [Alias('h')]
        [switch]$HtmlEncode
    )

    process {
        if (-not $Symbol) { return }
        # Attributes are mostly left alone
        if ($Symbol.StartsWith('[')) {
            if ($HtmlEncode.IsPresent) {
                return [System.Net.WebUtility]::HtmlEncode($Symbol)
            }
            else {
                return $Symbol
            }
        }

        # Separate the parameter list (bracket for this[index])
        [string]$paramList = ''
        [int]$pos = $Symbol.IndexOfAny('([')
        if ($pos -ge 0) {
            $paramList = $Symbol.Substring($pos)
            $Symbol = $Symbol.Substring(0, $pos)
        }
        # Separate visibility and return type
        [List[string]]$parts = $Symbol -csplit '(?<!,) '
        [string]$mainPart = $parts[-1]
        $parts.RemoveAt($parts.Count - 1)

        if (-not $ReturnType -or $ReturnType -eq 'Exclude') {
            $parts.Clear()
        }
        else {
            # ApiCompat lists the return value of fields and enum names twice. Not sure why
            # We could probably use 'Select-Object -Unique' here as it is unlikely to have dupes that
            # are not besides each other.
            for ([int]$i = $parts.Count - 1; $i -gt 0; $i--) {
                if ($parts[$i] -ceq $parts[$i - 1]) { $parts.RemoveAt($i) }
            }

            if ($ReturnType -eq 'Trimmed') {
                # This is intended for enum names, but there is no way to distinguish them from fields.
                if ($parts.Count -gt 0 -and $mainPart.StartsWith($parts[-1] + '.')) {
                    $parts.RemoveAt($parts.Count - 1)
                }
            }
        }

        # The main part may contain generics which have to be shortened as well
        [string]$generics = ''
        $pos = $mainPart.IndexOf('<')
        if ($pos -ge 0) {
            $generics = $mainPart.Substring($pos)
            $mainPart = $mainPart.Substring(0, $pos)
        }

        # Shorten all before-main parts, generics, and params
        $parts = $parts -creplace '\b[\w_]+\.', ''
        $generics = $generics -creplace '\b[\w_]+\.', ''
        $paramList = $paramList -creplace '\b[\w_]+\.', ''

        # Apply optional Html encoding
        if ($HtmlEncode) {
            for ([int]$i = 0; $i -lt $parts.Count; $i++) {
                $parts[$i] = [System.Net.WebUtility]::HtmlEncode($parts[$i])
            }
            $mainPart = [System.Net.WebUtility]::HtmlEncode($mainPart)
            $generics = [System.Net.WebUtility]::HtmlEncode($generics)
            $paramList = [System.Net.WebUtility]::HtmlEncode($paramList)
        }

        # Apply optional method tags if this is a method
        if ($MethodTags -and $paramList.Length -gt 0) {
            [string[]]$mainParts = $mainPart -csplit '\.'
            [int]$nameIndex = $mainParts.Count - 1
            if ($nameIndex -gt 0 -and ($mainParts[$nameIndex] -ceq 'get' -or $mainParts[$nameIndex] -ceq 'set')) {
                $nameIndex--
            }
            $mainParts[$nameIndex] = $MethodTags[0] + $mainParts[$nameIndex] + $MethodTags[-1]
            $mainPart = $mainParts -join '.'
        }

        return (($parts + $mainPart) -join ' ') + $generics + $paramList
    }
}

# .SYNOPSIS
#   Splits an ApiCompat rule message into parts.
# .INPUTS
#   You can pipe the message strings to the cmdlet.
# .OUTPUTS
#   A PSCustomObject for each message line, depending on type.
#
#   [PSCustomObject]@{
#       PSTypeName = 'ApiCompatMessage'
#       Original = [string]  # Original message if -Preserve is specified or $null
#       Unknown  = [string]  # Unknown message or $null
#       Issue    = [PSCustomObject]@{ # Issue message object (rule violation) or $null
#           Rule              = [string] # Rule name, e.g. TypeMustExist
#           IsSimple          = [bool]   # $true if simple rule with one symbol and discardable message text
#           TargetSymbolIndex = [int]    # 0-based index of target symbol in Parts, e.g. for sorting messages
#           Parts             = @('msgpart1 ', 'symbol1', ' msgpart2 ', 'symbol2', ' msgpart3')
#           # The even-indexed elements of Parts are always message text.
#           # The odd-indexed elements of Parts are always symbols with the surrounding single quotes stripped.
#           # In the unlikely case the message starts with a symbol, Parts[0] will be an empty string.
#       }
#       Trace    = [PSCustomObject]@{ # Trace message object or $null
#           Kind       = [string]       # Error, Warning, etc.
#           Code       = [string]       # This seems to be always just '0'
#           Message    = [string]       # Message with all prefixes (tool name, Kind, Code) stripped
#           IsResolver = [bool]         # $true if it is an 'Unable to resolve assembly' message
#       }
#       Header   = [string]           # Compat issue header or $null
#       Total    = [int]              # Total number of issues (can be 0) or $null
#   }
#
function Split-ApiCompatMessage {
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param(
        # Message line from ApiCompat
        [Parameter(ValueFromPipeline)]
        [string]$Message,

        # Preserve original message as part of the output
        [Alias('Keep')]
        [switch]$Preserve
    )

    begin {
        [string]$headerPattern = 'Compat issues '
        [string]$totalPattern = 'Total Issues: '
        [string]$tracePattern = 'Microsoft.DotNet.ApiCompat '
        [string]$resolverPattern = 'Unable to resolve assembly '
    }

    process {
        if (-not $Message) { return }
        $retVal = [PSCustomObject]@{
            PSTypeName = 'ApiCompatMessage'
            Original   = $Preserve.IsPresent ? $Message : $null
            Unknown    = $null
            Issue      = $null
            Trace      = $null
            Header     = $null
            Total      = $null
        }
        if ($Message.StartsWith($headerPattern)) {
            $retVal.Header = $Message
        }
        elseif ($Message.StartsWith($totalPattern)) {
            $retVal.Total = [int] $Message.Substring($totalPattern.Length).Trim()
        }
        elseif ($Message.StartsWith($tracePattern) -and
                ($parts = $Message.Substring($tracePattern.Length).Split(':', 3, [StringSplitOptions]::TrimEntries)).Count -ge 3) {
            $retVal.Trace = [PSCustomObject]@{
                Kind       = $parts[0]
                Code       = $parts[1]
                Message    = $parts[2]
                IsResolver = $parts[2].StartsWith($resolverPattern)
            }
        }
        elseif (([int]$pos = $Message.IndexOf(' : ')) -gt 0 -and
                ([string]$rule = $Message.Substring(0, $pos)).IndexOf(' ') -lt 0) {
            [string]$msg = $Message.Substring($pos + 3)
            [List[string]]$parts = [List[string]]::new()
            [int]$lastpos = 0
            [string]$quote = ' '''
            if ($msg.StartsWith('''')) {
                $parts.Add('');
                $quote = ''' '
                $lastpos = 1
            }
            while ($lastpos -lt $msg.Length) {
                $pos = $msg.IndexOf($quote, $lastpos)
                if ($pos -lt 0) { $pos = $msg.Length }
                [int]$l = $pos - $lastpos
                if ($quote -ceq ''' ') {
                    $parts.Add($msg.Substring($lastpos, $l))
                    $lastpos = $pos + 1
                    $quote = ' '''
                }
                else {
                    if (($l + $lastpos) -lt $msg.Length) { $l++ }
                    $parts.Add($msg.Substring($lastpos, $l))
                    $lastpos = $pos + 2
                    $quote = ''' '
                }
            }
            [bool]$isSimple = $false
            [int]$symIndex = 0
            if ($rule -in @(
                    'CannotAddAbstractMembers'
                    'CannotMakeMemberAbstract'
                    'CannotMakeTypeAbstract'
                    'CannotMakeMemberNonVirtual'
                    'InterfacesShouldHaveSameMembers'
                    'MembersMustExist'
                    'TypesMustExist'
                )) {
                $isSimple = $true
            }
            elseif ($rule -in @('DelegateParamTypeMustMatch')) {
                $symIndex = 1
            }
            elseif ($rule -in @('ParameterModifiersCannotChange')) {
                if ($parts.Count -ne 0 -and $parts[0].Contains(' on parameter ')) {
                    $symIndex = 1
                }
            }
            elseif ($rule -in @(
                    'AddedAttribute'
                    'CannotChangeAttribute'
                    'CannotRemoveAttribute'
                )) {
                if ($parts.Count -gt 2) {
                    $symIndex = 1
                    if ($parts[2].Contains(' generic param ') -or $parts[2].Contains(' parameter ')) {
                        $symIndex = 2
                    }
                }
            }
            $retVal.Issue = [PSCustomObject]@{
                Rule              = $rule
                IsSimple          = $isSimple
                TargetSymbolIndex = $symIndex * 2 + 1
                Parts             = $parts.ToArray()
            }
        }
        else {
            $retVal.Unknown = $Message
        }
        return $retVal
    }
}

# .SYNOPSIS
#   Compares attributes in two sets of assembly files.
# .OUTPUTS
#   [hashtable] with $Attributes as keys and [string[]] array of diffs as values.
function Compare-AttributeUsage {
    [CmdletBinding()]
    [OutputType([hashtable])]
    param(
        # Full paths to the reference/contract assemblies
        [ValidateNotNullOrEmpty()]
        [string[]]$ReferenceAssemblies,

        # Full paths to the difference/implementation assemblies
        [ValidateNotNullOrEmpty()]
        [string[]]$DifferenceAssemblies,

        # Attributes to check for
        [ValidateNotNullOrEmpty()]
        [string[]]$Attributes,

        # Scope of symbols to check
        [Alias('Scope')]
        [Microsoft.CodeAnalysis.MetadataImportOptions]$MetadataImportOptions = [Microsoft.CodeAnalysis.MetadataImportOptions]::Public,

        # Should forwarde symbols be included?
        [switch]$IncludeTypeForwards
    )

    function Get-SymbolsWithAttributes {
        [OutputType([hashtable])]
        param(
            [ValidateNotNullOrEmpty()]
            [string[]]$Path,
            [ValidateNotNullOrEmpty()]
            [string[]]$Attributes,
            [Alias('Scope')]
            [Microsoft.CodeAnalysis.MetadataImportOptions]$MetadataImportOptions,
            [switch]$IncludeTypeForwards
        )

        [CSharpCompilationOptions]$options = [CSharpCompilationOptions]::new(
            [OutputKind]::DynamicallyLinkedLibrary,
            $false, $null, $null, $null, $null,
            [OptimizationLevel]::Debug,
            $false, $false, $null, $null,
            [System.Collections.Immutable.ImmutableArray[byte]]::new(),
            $null,
            [Platform]::AnyCpu, [ReportDiagnostic]::Default,
            4, <# warning level #>
            $null, $true, $false, $null, $null, $null, $null, $null, $false,
            $MetadataImportOptions, <# Public/Internal/All #>
            [NullableContextOptions]::Enable
        )
        [CSharpCompilation]$csc = [CSharpCompilation]::Create("AssemblyLoader_$([DateTime]::Now.Ticks)", $null, $null, $options)
        [List[MetadataReference]]$references = [List[MetadataReference]]::new()
        foreach ($_path in $Path) {
            $references.Add([MetadataReference]::CreateFromFile($_path));
        }
        $csc = $csc.AddReferences($references)
        [ApiCompatHelper.SymbolWithAttributeFinder]$finder = [ApiCompatHelper.SymbolWithAttributeFinder]::new($Attributes)
        if ($IncludeTypeForwards.IsPresent) { $finder.IncludeTypeForwards = $true }
        foreach ($meta in $csc.References) {
            [ISymbol]$asm = $csc.GetAssemblyOrModuleSymbol($meta)
            ([ISymbol]$asm).Accept($finder)
        }
        [hashtable]$retVal = @{}
        foreach ($attr in $Attributes) {
            $retVal.Add($attr, $finder.GetSymbolsWithAttributeAsDocIdDictionary($attr))
        }
        return $retVal
    }

    function Format-ISymbol {
        param(
            [ValidateNotNull()]
            [HashSet[string]]$DocIdKeys,
            [ValidateNotNull()]
            [Dictionary[string, ISymbol]]$SymbolDict,
            [ValidateSet('-', '+')]
            [char]$DiffPrefix,
            [List[string]]$Results
        )

        foreach ($key in $DocIdKeys) {
            [ISymbol]$symbol = $SymbolDict[$key]
            [string]$display = ([ISymbol]$symbol).ToDisplayString([SymbolDisplayFormat]::CSharpErrorMessageFormat)
            if ($display.IndexOfAny('([<')) { $display = Format-ApiCompatSymbol $display }
            $Results.Add($DiffPrefix + $key.Substring(0, 2) + $display)
        }
    }

    if (-not $script:ApiCompatDll) { Install-ApiCompatTools }
    [hashtable]$finderLeft = Get-SymbolsWithAttributes $ReferenceAssemblies $Attributes $MetadataImportOptions -IncludeTypeForwards:$IncludeTypeForwards
    [hashtable]$finderRight = Get-SymbolsWithAttributes $DifferenceAssemblies $Attributes $MetadataImportOptions -IncludeTypeForwards:$IncludeTypeForwards
    [hashtable]$retVal = @{}
    foreach ($attr in $Attributes) {
        # .$attr dot notation throws on missing
        [Dictionary[string, ISymbol]]$dictLeft = $finderLeft.$attr
        [Dictionary[string, ISymbol]]$dictRight = $finderRight.$attr
        [HashSet[string]]$leftOnly = [HashSet[string]]::new($dictLeft.Keys)
        $leftOnly.ExceptWith($dictRight.Keys)
        [HashSet[string]]$rightOnly = [HashSet[string]]::new($dictRight.Keys)
        $rightOnly.ExceptWith($dictLeft.Keys)
        [List[string]]$diff = [List[string]]::new($leftOnly.Count + $rightOnly.Count)
        Format-ISymbol $leftOnly $dictLeft '-' $diff
        Format-ISymbol $rightOnly $dictRight '+' $diff
        [string[]]$sorted = $diff | Sort-Object { $_.Substring(3) }, { $_.Substring(0, 1) } -CaseSensitive
        $retVal.Add($attr, $sorted)
    }
    return $retVal
}

Export-ModuleMember -Function @(
    'Install-ApiCompatTools'
    'Invoke-ApiCompat'
    'Invoke-AsmDiff'
    'Format-ApiCompatSymbol'
    'Split-ApiCompatMessage'
    'Compare-AttributeUsage'
) -Variable @(
    'AsmDiffDll'
    'ApiCompatDll'
)
