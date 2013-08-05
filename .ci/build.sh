#!/bin/bash -x

function ExitIfNonZero {
	if [ $1 -ne 0 ]; then
		exit $1
	fi
}

wget -P .ci https://nuget.org/nuget.exe 
ExitIfNonZero $?

mv .ci/nuget.exe .ci/NuGet.exe
ExitIfNonZero $?

xbuild /p:NoWarn=1584 /property:Configuration=Debug /property:Platform="Any CPU" SteamBot.sln 
ExitIfNonZero $?
