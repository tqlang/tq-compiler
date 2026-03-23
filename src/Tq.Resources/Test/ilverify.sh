ilverify ./.abs-out/Program.dll \
    --sanity-checks \
    -s System.Runtime \
    -r /usr/lib/dotnet/shared/Microsoft.NETCore.App/10.0.3/System.Runtime.dll \
    -r /usr/lib/dotnet/shared/Microsoft.NETCore.App/10.0.3/System.Console.dll \
    -r /usr/lib/dotnet/shared/Microsoft.NETCore.App/10.0.3/System.Private.CoreLib.dll \
    > .abs-out/a.txt
