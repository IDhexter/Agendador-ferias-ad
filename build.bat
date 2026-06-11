@echo off
chcp 65001 >nul
echo.
echo ╔═══════════════════════════════════════════════════════╗
echo ║         AD User Manager - Build Script               ║
echo ╚═══════════════════════════════════════════════════════╝
echo.
echo Compilando como executavel unico (self-contained)...
echo Isso pode levar alguns minutos na primeira vez.
echo.

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true

echo.
if %ERRORLEVEL% EQU 0 (
    echo ══════════════════════════════════════════════════════
    echo   BUILD CONCLUIDO COM SUCESSO!
    echo ══════════════════════════════════════════════════════
    echo.
    echo   O arquivo .exe esta em:
    echo   bin\Release\net8.0-windows\win-x64\publish\ADUserManager.exe
    echo.
    echo   Copie o ADUserManager.exe para o servidor ou
    echo   estacao de trabalho desejada.
    echo.
    echo   REQUISITOS para execucao:
    echo   - Windows 10/11 ou Windows Server 2016+
    echo   - RSAT (ActiveDirectory PowerShell Module)
    echo   - Executar como Administrador
    echo ══════════════════════════════════════════════════════
) else (
    echo ══════════════════════════════════════════════════════
    echo   ERRO NA COMPILACAO!
    echo ══════════════════════════════════════════════════════
    echo.
    echo   Verifique se o .NET 8 SDK esta instalado:
    echo   https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
)
echo.
pause
