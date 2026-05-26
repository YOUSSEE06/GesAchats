@echo off
title GesAchats v2.0 - Lancement Rapide
echo ==========================================
echo   LANCEMENT DE L'APPLICATION GESACHATS
echo ==========================================
echo.

echo [1/2] Fermeture des instances existantes...
taskkill /F /IM GesAchats.WPF.exe /T >nul 2>&1

echo [2/2] Lancement de l'application WPF...
dotnet run --project GesAchats.WPF/GesAchats.WPF.csproj

if %ERRORLEVEL% neq 0 (
    echo.
    echo [ERREUR] Impossible de lancer l'application.
    echo Verifiez que PostgreSQL est bien demarre.
    pause
)
