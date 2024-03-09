#!/bin/bash

dotnet restore Core_SK_Patch.sln

nuget restore Core_SK_Patch.sln
nuget update Core_SK_Patch.sln

MSBuild.exe Core_SK_Patch.sln