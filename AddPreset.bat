@echo off
title Cursor Preset Tool [Add Preset]
echo 現在のマウスカーソル設定をプリセットに登録します
set /p NAME=登録するプリセット名を入力してください（規定値：preset）：
if "%NAME%"=="" set NAME="preset"
echo.
CursorPresetTool.exe export %NAME%
pause > nul