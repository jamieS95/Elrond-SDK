@echo off
set NETVER=v4.0.30319
c:\windows\microsoft.net\framework\%NETVER%\vbc -target:library -reference:Microsoft.VisualBasic.dll -reference:BouncyCastle.Crypto.dll -reference:System.dll  -reference:System.Data.dll  -out:ERDLib.dll -nowarn -nologo -debug *.vb