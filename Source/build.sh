#!/bin/bash
dirname=$(realpath $(dirname $0))

config="Debug"
case $1 in
	-R )
		config="Release"
		;;
	* )
		config="Debug"
		;;
esac



dotnet restore $dirname/Core_SK_Patch.sln

nuget restore $dirname/Core_SK_Patch.sln
nuget update $dirname/Core_SK_Patch.sln

MSBuild.exe $dirname/Core_SK_Patch.sln "-p:Configuration=$config" "-t:Clean;Rebuild"