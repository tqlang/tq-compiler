clear
/home/leo/Documentos/repos/tq-compiler/tq-compiler-vibes/Src/Tqc.CLI/bin/Debug/net10.0/Tqc.CLI build_toml

if [ $? -ne 0 ]; then
    echo "Build failed, aborted";
    exit 1;
fi

clear
dotnet .tq-out/SilkTest.dll Testing ...;
