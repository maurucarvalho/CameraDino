$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) { $csc = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe" }
Write-Host "Compiling C# Launcher..."
& $csc /nologo /target:winexe /out:CameraDino.exe /win32icon:dino.ico /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:Microsoft.CSharp.dll CameraDino.cs

$iscc = "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) {
    Write-Host "Inno Setup Compiler not found." -ForegroundColor Red
    exit
}
Write-Host "Compiling Installer..."
& $iscc "CameraDino.iss"
if ($LASTEXITCODE -eq 0) {
    Write-Host "CameraDino_Setup.exe installer generated successfully!" -ForegroundColor Green
} else {
    Write-Host "Failed to generate installer." -ForegroundColor Red
}
