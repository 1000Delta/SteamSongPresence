param([string]$OutputPath = (Join-Path (Split-Path $PSScriptRoot -Parent) 'assets\icon.png'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($OutputPath)) -Force | Out-Null
$bitmap = [Drawing.Bitmap]::new(256,256)
$graphics = [Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
function Rounded-Rectangle([int]$x,[int]$y,[int]$width,[int]$height,[int]$radius) {
    $shape = [Drawing.Drawing2D.GraphicsPath]::new()
    $diameter = $radius * 2
    $shape.AddArc($x,$y,$diameter,$diameter,180,90)
    $shape.AddArc(($x+$width-$diameter),$y,$diameter,$diameter,270,90)
    $shape.AddArc(($x+$width-$diameter),($y+$height-$diameter),$diameter,$diameter,0,90)
    $shape.AddArc($x,($y+$height-$diameter),$diameter,$diameter,90,90)
    $shape.CloseFigure()
    return ,$shape
}
$navy = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#0b1220'))
$white = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#e2e8f0'))
$red = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#f43f5e'))
$blue = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#38bdf8'))
$green = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#22c55e'))
$outline = [Drawing.Pen]::new($white,6)
$stem = [Drawing.Pen]::new($white,8)
$background = Rounded-Rectangle 0 0 256 256 30
$bubble = Rounded-Rectangle 38 44 176 138 23
try {
    $graphics.Clear([Drawing.Color]::Transparent)
    $graphics.FillPath($navy,$background)
    $graphics.DrawPath($outline,$bubble)
    $graphics.DrawLines($outline,[Drawing.Point[]]@([Drawing.Point]::new(154,183),[Drawing.Point]::new(154,205),[Drawing.Point]::new(128,183)))
    $graphics.DrawLine($stem,114,82,114,142)
    $graphics.DrawLine($stem,161,70,161,130)
    $graphics.FillPolygon($white,[Drawing.Point[]]@([Drawing.Point]::new(110,80),[Drawing.Point]::new(165,66),[Drawing.Point]::new(165,80),[Drawing.Point]::new(110,94)))
    $graphics.FillEllipse($red,82,131,36,26)
    $graphics.FillEllipse($blue,129,119,36,26)
    $graphics.FillEllipse($navy,192,20,48,48)
    $graphics.FillEllipse($green,198,26,36,36)
    $bitmap.Save($OutputPath,[Drawing.Imaging.ImageFormat]::Png)
} finally {
    foreach($resource in @($background,$bubble,$stem,$outline,$navy,$white,$red,$blue,$green,$graphics,$bitmap)) { $resource.Dispose() }
}
Write-Output $OutputPath
