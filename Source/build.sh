#!/bin/bash

dotnet restore gCore_SK_Patch.sln

nuget restore gCore_SK_Patch.sln
nuget update gCore_SK_Patch.sln

MSBuild.exe gCore_SK_Patch.sln