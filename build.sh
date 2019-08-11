#!/bin/bash
cd "$(dirname "$0")"
msbuild  /nologo /verbosity:quiet /consoleloggerparameters:summary /restore roboot.csproj
