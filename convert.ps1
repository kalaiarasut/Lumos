Add-Type -AssemblyName System.Drawing

$imgPath = "D:\ON-AIR\Lumos\logo.png"
$icoPath = "D:\ON-AIR\Lumos\src\Lumos\Resources\logo.ico"

# Create Resources folder if not exists
$resFolder = "D:\ON-AIR\Lumos\src\Lumos\Resources"
if (-not (Test-Path $resFolder)) {
    New-Item -ItemType Directory -Force -Path $resFolder | Out-Null
}

$img = [System.Drawing.Image]::FromFile($imgPath)
$bmp = New-Object System.Drawing.Bitmap 256, 256
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.DrawImage($img, 0, 0, 256, 256)
$g.Dispose()

$ms = New-Object System.IO.MemoryStream
$bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
$pngBytes = $ms.ToArray()

$icoStream = New-Object System.IO.FileStream $icoPath, Create
$bw = New-Object System.IO.BinaryWriter $icoStream

# ICO Header
$bw.Write([int16]0) # Reserved
$bw.Write([int16]1) # Type (1 = ICO)
$bw.Write([int16]1) # Image count

# Image Entry
$bw.Write([byte]0)  # Width (0 = 256)
$bw.Write([byte]0)  # Height (0 = 256)
$bw.Write([byte]0)  # Color palette
$bw.Write([byte]0)  # Reserved
$bw.Write([int16]1) # Color planes
$bw.Write([int16]32) # Bits per pixel
$bw.Write([int]$pngBytes.Length) # Image size
$bw.Write([int]22)  # Offset to image data

# Image Data
$bw.Write($pngBytes)

$bw.Close()
$ms.Close()
$img.Dispose()
$bmp.Dispose()

Write-Host "Created $icoPath successfully."
