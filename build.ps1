param(
    [Parameter(Mandatory=$true)][string]$ActExe,
    [Parameter(Mandatory=$true)][string]$OverlayDirectory
)
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$core = Join-Path $OverlayDirectory 'libs/OverlayPlugin.Core.dll'
$common = Join-Path $OverlayDirectory 'libs/OverlayPlugin.Common.dll'
foreach ($path in @($compiler, $ActExe, $core, $common)) {
    if (!(Test-Path -LiteralPath $path)) { throw "Missing: $path" }
}
$destination = Join-Path $PSScriptRoot 'FfxivAutoEnd.dll'
$source = Join-Path $PSScriptRoot 'AutoEnd.cs'
$detector = Join-Path $PSScriptRoot 'EndDetector.cs'
& $compiler /nologo /target:library /optimize+ /platform:x64 "/out:$destination" "/reference:$ActExe" "/reference:$core" "/reference:$common" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll $source $detector
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
