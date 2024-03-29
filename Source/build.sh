#!/bin/bash
dirname=$(realpath $(dirname $0))

config="1.4-debug"
clean=""
while (( "$#" )); do
	case $1 in
		-R )
			config="1.4"
			shift
			;;
		-C )
			clean="Clean\;Build"
			shift
			;;
		* )
			shift
			;;
	esac
done

echo "Current Configuration: -p:Configuration=$config $(if [ -n "$clean" ]; then echo "-t:$clean"; fi)"
echo "Continue? (y/n)"
read ans
if [ "$ans" != "y" ]; then
	echo "Aborted"
	exit 1
fi

dotnet restore $dirname/Core_SK_Patch.sln && \
nuget restore $dirname/Core_SK_Patch.sln && \
nuget update $dirname/Core_SK_Patch.sln && \
MSBuild.exe $dirname/Core_SK_Patch.sln "-p:Configuration=$config" "$clean"