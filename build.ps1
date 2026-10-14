# Build PeekShot.exe with the in-box .NET Framework C# compiler (C# 5).
# No .NET SDK required. ASCII only for PowerShell 5.1 compatibility.
$ErrorActionPreference = 'Stop'

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $csc)) {
    throw "csc.exe not found at: $csc"
}

$root = $PSScriptRoot
$out = Join-Path $root 'PeekShot.exe'
$src = Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' |
    ForEach-Object { $_.FullName }

if (-not $src -or $src.Count -eq 0) {
    throw "No .cs files found under src\"
}

# /codepage:65001 - source files are UTF-8 (Chinese comments)
# /noconfig + explicit references - deterministic build
& $csc /nologo /noconfig /codepage:65001 `
    /target:winexe /platform:anycpu /optimize+ /warn:4 `
    /out:"$out" `
    /r:System.dll /r:System.Core.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:Accessibility.dll `
    $src

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

Write-Host "Build OK: $out"
