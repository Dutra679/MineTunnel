$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dist = Join-Path $root 'dist'
$icon = Join-Path $root 'MineTunnel.ico'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$framework = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$wpf = Join-Path $framework 'WPF'

New-Item -ItemType Directory -Force -Path $dist | Out-Null

if (-not (Test-Path -LiteralPath $icon)) {
    Add-Type -AssemblyName System.Drawing
    $bitmap = [System.Drawing.Bitmap]::new(64, 64)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([System.Drawing.Color]::FromArgb(16, 20, 18))
    $green = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(67, 209, 122))
    $dark = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(7, 19, 12))
    $graphics.FillRectangle($green, 8, 8, 48, 48)
    $graphics.FillPie($dark, 17, 15, 30, 30, 180, 180)
    $graphics.FillRectangle($dark, 17, 29, 30, 20)
    $graphics.FillRectangle($green, 27, 29, 10, 20)
    $handle = $bitmap.GetHicon()
    $appIcon = [System.Drawing.Icon]::FromHandle($handle)
    $stream = [System.IO.File]::Create($icon)
    $appIcon.Save($stream)
    $stream.Dispose()
    $appIcon.Dispose()
    $green.Dispose()
    $dark.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
}

$arguments = @(
    '/nologo',
    '/target:winexe',
    '/platform:x64',
    '/optimize+',
    ('/out:' + (Join-Path $dist 'MineTunnel.exe')),
    ('/win32icon:' + $icon),
    ('/win32manifest:' + (Join-Path $root 'MineTunnel.exe.manifest')),
    ('/r:' + (Join-Path $framework 'System.dll')),
    ('/r:' + (Join-Path $framework 'System.Core.dll')),
    ('/r:' + (Join-Path $framework 'System.Web.Extensions.dll')),
    ('/r:' + (Join-Path $framework 'System.Xaml.dll')),
    ('/r:' + (Join-Path $wpf 'WindowsBase.dll')),
    ('/r:' + (Join-Path $wpf 'PresentationCore.dll')),
    ('/r:' + (Join-Path $wpf 'PresentationFramework.dll')),
    (Join-Path $root 'MineTunnel.cs')
)

& $compiler $arguments
if ($LASTEXITCODE -ne 0) { throw "Compilation failed with exit code $LASTEXITCODE" }

$config = Join-Path $dist 'minetunnel.json'
if (-not (Test-Path -LiteralPath $config)) {
    Copy-Item -LiteralPath (Join-Path $root 'minetunnel.json.example') -Destination $config
}
Copy-Item -LiteralPath (Join-Path $root 'LEIA-ME.txt') -Destination (Join-Path $dist 'LEIA-ME.txt') -Force

Get-ChildItem -LiteralPath $dist | Select-Object Name, Length, LastWriteTime
