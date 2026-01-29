ilverify ./.abs-out/Program.dll \
    --verbose \
    --sanity-checks \
    -s System.Runtime \
    -r /usr/lib/dotnet/shared/Microsoft.NETCore.App/10.0.0/System.Runtime.dll \
    -r /usr/lib/dotnet/shared/Microsoft.NETCore.App/10.0.0/System.Console.dll \
    -r /usr/lib/dotnet/shared/Microsoft.NETCore.App/10.0.0/System.Private.CoreLib.dll \
    > .abs-out/a.txt