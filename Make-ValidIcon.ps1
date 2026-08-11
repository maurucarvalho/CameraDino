Add-Type -AssemblyName System.Drawing
$img = [System.Drawing.Image]::FromFile("dino.png")

$sizes = @(256, 128, 64, 48, 32, 16)
$pngData = @()

foreach ($size in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.DrawImage($img, 0, 0, $size, $size)
    $g.Dispose()
    
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngData += ,$ms.ToArray()
    $bmp.Dispose()
}

$img.Dispose()

$bw = New-Object System.IO.BinaryWriter([System.IO.File]::Open("dino_valid.ico", [System.IO.FileMode]::Create))
# Header
$bw.Write([uint16]0)
$bw.Write([uint16]1)
$bw.Write([uint16]$sizes.Length)

$offset = 6 + (16 * $sizes.Length)

# Directory entries
for ($i = 0; $i -lt $sizes.Length; $i++) {
    $size = $sizes[$i]
    if ($size -eq 256) { $sizeByte = [byte]0 } else { $sizeByte = [byte]$size }
    $bw.Write($sizeByte) # Width
    $bw.Write($sizeByte) # Height
    $bw.Write([byte]0)   # Colors
    $bw.Write([byte]0)   # Reserved
    $bw.Write([uint16]1) # Color planes
    $bw.Write([uint16]32) # BPP
    $bw.Write([uint32]$pngData[$i].Length) # Size
    $bw.Write([uint32]$offset) # Offset
    $offset += $pngData[$i].Length
}

# Image data
for ($i = 0; $i -lt $sizes.Length; $i++) {
    $bw.Write($pngData[$i])
}

$bw.Close()
