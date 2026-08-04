ilverify ./Tests/.abs-out/Program.dll \
    --sanity-checks \
    -s System.Runtime \
    -r /usr/lib/dotnet/shared/Microsoft.NETCore.App/10.0.4/System.Runtime.dll \
    -r /usr/lib/dotnet/shared/Microsoft.NETCore.App/10.0.4/System.Console.dll \
    -r /usr/lib/dotnet/shared/Microsoft.NETCore.App/10.0.4/System.Private.CoreLib.dll \
    > verify-out.txt
