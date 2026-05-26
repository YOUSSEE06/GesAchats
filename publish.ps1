# Script de publication GesAchats v2.0
# Ce script génère les binaires prêts pour le déploiement

$publishDir = ".\Publish"
$projectName = "GesAchats.WPF\GesAchats.WPF.csproj"

Write-Host "🚀 Début de la publication de GesAchats..." -ForegroundColor Cyan

# Nettoyage du dossier précédent
if (Test-Path $publishDir) {
    Remove-Item -Path $publishDir -Recurse -Force
}

# Publication pour Windows 64-bit (Single File et Trimmed pour plus de légèreté)
dotnet publish $projectName `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDir

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Publication réussie ! Les fichiers se trouvent dans le dossier : $publishDir" -ForegroundColor Green
    Write-Host "Note: N'oubliez pas de configurer le fichier appsettings.json dans le dossier de destination." -ForegroundColor Yellow
} else {
    Write-Host "`n❌ Échec de la publication." -ForegroundColor Red
}
