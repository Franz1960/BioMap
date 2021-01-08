echo off
rem Zur Ausführung des Skripts muss die Ausführungsrichtlinie für PowerShell 
rem auf "Unrestricted" gesetzt werden.
rem Befehl in der PowerShell: Set-ExecutionPolicy Unrestricted -Scope CurrentUser
rem
rem Außerdem muss die Inkscape.exe im Pfad liegen (Umgebungsvariable PATH).
rem
echo on
powershell "& "".\CreateBitmaps.ps1""" > CreateBitmaps.out.txt 2>&1
