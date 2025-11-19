@echo off
title Cursor Preset Tool [Apply Preset]
CursorPresetTool.exe list
echo.
set /p PRESET=適用するプリセット名または番号を入力してください（規定値：0）：
if "%PRESET%"=="" set PRESET=0
echo.
CursorPresetTool.exe apply %PRESET%
pause > nul