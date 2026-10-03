@echo off
REM Builds the template and every sample in this pack, so you can
REM check your toolchain before writing anything of your own.
REM Needs MSBuild on the PATH, or run it from a Developer Command
REM Prompt.

setlocal
set FAILED=0

for %%P in (
  "template\MyNexusPlugin\MyNexusPlugin.csproj"
  "samples\Sample\Nexus.Plugin.Sample.csproj"
  "samples\ExtraLaunchers\Nexus.Plugin.ExtraLaunchers.csproj"
  "samples\LibraryExport\Nexus.Plugin.LibraryExport.csproj"
  "samples\SaveBackup\Nexus.Plugin.SaveBackup.csproj"
  "samples\DiscordPresence\Nexus.Plugin.DiscordPresence.csproj"
) do (
  echo.
  echo === %%~P
  msbuild %%P -t:Rebuild -p:Configuration=Release -v:minimal -nologo || set FAILED=1
)

echo.
if "%FAILED%"=="1" (echo One or more builds failed.) else (echo All built.)
endlocal
