
class <Module> extends  {
}

class Bootstrap.Static extends System.Object {

	method public static main () System.Void {
		.locals init (System.String V_0)
		.locals init (System.String[] V_1)
		.locals init (System.String V_2)
		IL_0000: ldstr "Hello, World!"
		IL_0005: stloc V_0
		IL_0009: ldstr "Hello, World!"
		IL_000E: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_0013: call System.String[] Std.ProcessStatic::getProgramArguments()
		IL_0018: stloc V_1
		IL_001C: ldloc V_1
		IL_0020: ldlen
		IL_0021: ldc.i8 2
		IL_002A: clt.un
		IL_002C: brfalse IL_0050
		IL_0031: nop
		IL_0032: ldstr "Expected 1 argument!"
		IL_0037: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_003C: ldc.i8 1
		IL_0045: conv.ovf.u8.un
		IL_0046: call System.Void Std.ProcessStatic::exit(System.UInt64)
		IL_004B: br IL_0050
		IL_0050: nop
		IL_0051: ldstr "test.txt"
		IL_0056: call System.String Std.FsStatic::readAllText(System.String)
		IL_005B: stloc V_2
		IL_005F: ldstr "Old content was: "
		IL_0064: call System.Void Std.ConsoleStatic::write(System.String)
		IL_0069: ldloc V_2
		IL_006D: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_0072: ldstr "test.txt"
		IL_0077: ldloc V_1
		IL_007B: ldc.i8 1
		IL_0084: conv.i4
		IL_0085: ldelem System.String
		IL_008A: call System.Void Std.FsStatic::writeAllText(System.String, System.String)
		IL_008F: ret
	}
}

class Std.Static extends System.Object {
}

class Std.IoStatic extends System.Object {
}

class Std.ProcessStatic extends System.Object {

	method public static getProgramArguments () System.String[] {
		IL_0000: call System.String[] System.Environment::GetCommandLineArgs()
		IL_0005: ret
	}

	method public static exit (System.UInt64 status) System.Void {
		IL_0000: ldarg System.UInt64 status
		IL_0004: conv.i4
		IL_0005: conv.ovf.i4
		IL_0006: call System.Void System.Environment::Exit(System.Int32)
		IL_000B: ret
	}
}

class Std.MemStatic extends System.Object {

	method public static copy () System.Void {
		IL_0000: ret
	}

	method public static compare () System.Boolean {
		IL_0000: ldc.i4.0
		IL_0001: ret
	}

	method public static alloc <T> (System.UInt64 length, Std.Mem.Alignment alignment) !!0[] {
		IL_0000: ldarg System.UInt64 length
		IL_0004: conv.i4
		IL_0005: conv.ovf.i4
		IL_0006: ldc.i4.0
		IL_0007: call !!0[] System.GC::AllocateArray<!!0>(System.Int32, System.Boolean)
		IL_000C: ret
	}
}

class Std.TestStatic extends System.Object {

	method public static testValue (System.Byte value, System.Byte reference) System.Void {
		IL_0000: ldarg System.Byte value
		IL_0004: ldarg System.Byte reference
		IL_0008: ceq
		IL_000A: call System.Void Std.TestStatic::printStatusMessage(System.Boolean)
		IL_000F: ret
	}

	method private static printStatusMessage (System.Boolean status) System.Void {
		IL_0000: ldarg System.Boolean status
		IL_0004: brfalse IL_0019
		IL_0009: nop
		IL_000A: ldstr "TEST PASSED"
		IL_000F: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_0014: br IL_0024
		IL_0019: nop
		IL_001A: ldstr "TEST FAILED"
		IL_001F: call System.Void Std.ConsoleStatic::write(System.String)
		IL_0024: nop
		IL_0025: ret
	}
}

class Std.ConsoleStatic extends System.Object {

	method private static write (System.Int64 value) System.Void {
		IL_0000: ldarg System.Int64 value
		IL_0004: box System.Int64
		IL_0009: callvirt System.String System.Object::ToString()
		IL_000E: call System.Void System.Console::Write(System.String)
		IL_0013: ret
	}

	method private static write (System.UInt64 value) System.Void {
		IL_0000: ldarg System.UInt64 value
		IL_0004: box System.UInt64
		IL_0009: callvirt System.String System.Object::ToString()
		IL_000E: call System.Void System.Console::Write(System.String)
		IL_0013: ret
	}

	method private static write (System.Int128 value) System.Void {
		IL_0000: ldarg System.Int128 value
		IL_0004: box System.Int128
		IL_0009: callvirt System.String System.Object::ToString()
		IL_000E: call System.Void System.Console::Write(System.String)
		IL_0013: ret
	}

	method private static write (System.UInt128 value) System.Void {
		IL_0000: ldarg System.UInt128 value
		IL_0004: box System.UInt128
		IL_0009: callvirt System.String System.Object::ToString()
		IL_000E: call System.Void System.Console::Write(System.String)
		IL_0013: ret
	}

	method private static write (System.String value) System.Void {
		IL_0000: ldarg System.String value
		IL_0004: call System.Void System.Console::Write(System.String)
		IL_0009: ret
	}

	method private static writeln (System.Int64 value) System.Void {
		IL_0000: ldarg System.Int64 value
		IL_0004: box System.Int64
		IL_0009: callvirt System.String System.Object::ToString()
		IL_000E: call System.Void System.Console::WriteLine(System.String)
		IL_0013: ret
	}

	method private static writeln (System.UInt64 value) System.Void {
		IL_0000: ldarg System.UInt64 value
		IL_0004: box System.UInt64
		IL_0009: callvirt System.String System.Object::ToString()
		IL_000E: call System.Void System.Console::WriteLine(System.String)
		IL_0013: ret
	}

	method private static writeln (System.Int128 value) System.Void {
		IL_0000: ldarg System.Int128 value
		IL_0004: box System.Int128
		IL_0009: callvirt System.String System.Object::ToString()
		IL_000E: call System.Void System.Console::WriteLine(System.String)
		IL_0013: ret
	}

	method private static writeln (System.UInt128 value) System.Void {
		IL_0000: ldarg System.UInt128 value
		IL_0004: box System.UInt128
		IL_0009: callvirt System.String System.Object::ToString()
		IL_000E: call System.Void System.Console::WriteLine(System.String)
		IL_0013: ret
	}

	method private static writeln (System.String value) System.Void {
		IL_0000: ldarg System.String value
		IL_0004: call System.Void System.Console::WriteLine(System.String)
		IL_0009: ret
	}
}

class Std.FsStatic extends System.Object {

	method public static open (System.String path) Std.Fs.File {
		.locals init (System.IO.FileStream V_0)
		.locals init (Std.Fs.File V_1)
		IL_0000: ldarg System.String path
		IL_0004: ldc.i4.4
		IL_0005: ldc.i4.1
		IL_0006: newobj System.Void System.IO.FileStream::.ctor(System.String, System.IO.FileMode, System.IO.FileAccess)
		IL_000B: stloc V_0
		IL_000F: ldloca V_1
		IL_0013: ldloc V_0
		IL_0017: call System.Void Std.Fs.File::.ctor(System.IO.FileStream)
		IL_001C: ldloc V_1
		IL_0020: ret
	}

	method public static readAllText (System.String path) System.String {
		IL_0000: ldarg System.String path
		IL_0004: call System.String System.IO.File::ReadAllText(System.String)
		IL_0009: ret
	}

	method public static writeAllText (System.String path, System.String content) System.Void {
		IL_0000: ldarg System.String path
		IL_0004: ldarg System.String content
		IL_0008: call System.Void System.IO.File::WriteAllText(System.String, System.String)
		IL_000D: ret
	}
}

struct Std.Io.Reader extends System.ValueType {

	method private instance .ctor () System.Void {
		IL_0000: ret
	}
}

struct Std.Io.Writer extends System.ValueType {

	method private instance .ctor () System.Void {
		IL_0000: ret
	}
}

struct Std.Fs.File extends System.ValueType {
	field private instance System.IO.FileStream _filestream

	method private instance .ctor (System.IO.FileStream stream) System.Void {
		IL_0000: ldarg.0
		IL_0001: ldarg System.IO.FileStream stream
		IL_0005: stfld System.IO.FileStream Std.Fs.File::_filestream
		IL_000A: ret
	}

	method private instance close () System.Void {
		IL_0000: ldarg.0
		IL_0001: ldflda System.IO.FileStream Std.Fs.File::_filestream
		IL_0006: ldobj System.IO.FileStream
		IL_000B: call System.Void System.IO.Stream::Close()
		IL_0010: ret
	}
}

struct Std.Fs.FileReader extends System.ValueType {
	field public instance Std.Io.Reader base

	method private instance .ctor () System.Void {
		IL_0000: ret
	}
}

struct Std.Fs.FileWriter extends System.ValueType {
	field public instance Std.Io.Writer base

	method private instance .ctor () System.Void {
		IL_0000: ret
	}
}

struct Std.Fs.FileInfo extends System.ValueType {
	field private instance System.IO.FileInfo _fileInfo
	field public instance System.String name
	field public instance System.String directory
	field public instance System.UInt64 length

	method private instance .ctor (System.IO.FileInfo fInfo) System.Void {
		IL_0000: ldarg.0
		IL_0001: ldarg System.IO.FileInfo fInfo
		IL_0005: stfld System.IO.FileInfo Std.Fs.FileInfo::_fileInfo
		IL_000A: ldarg.0
		IL_000B: ldarg System.IO.FileInfo fInfo
		IL_000F: call System.String System.IO.FileInfo::get_Name()
		IL_0014: stfld System.String Std.Fs.FileInfo::name
		IL_0019: ldarg.0
		IL_001A: ldarg System.IO.FileInfo fInfo
		IL_001E: call System.String System.IO.FileInfo::get_DirectoryName()
		IL_0023: stfld System.String Std.Fs.FileInfo::directory
		IL_0028: ldarg.0
		IL_0029: ldarg System.IO.FileInfo fInfo
		IL_002D: call System.Int64 System.IO.FileInfo::get_Length()
		IL_0032: stfld System.UInt64 Std.Fs.FileInfo::length
		IL_0037: ret
	}
}

struct Std.Mem.Alignment extends System.Enum {
	field public instance System.Int64 value__
	field public static Std.Mem.Alignment _1
	field public static Std.Mem.Alignment _2
	field public static Std.Mem.Alignment _4
	field public static Std.Mem.Alignment _8
	field public static Std.Mem.Alignment _16
	field public static Std.Mem.Alignment _32
}

struct Std.Fs.SeekOrigin extends System.Enum {
	field public instance System.Int32 value__
	field public static Std.Fs.SeekOrigin begin
	field public static Std.Fs.SeekOrigin current
	field public static Std.Fs.SeekOrigin end
}

struct Std.Fs.FileMode extends System.Enum {
	field public instance System.Int32 value__
	field public static Std.Fs.FileMode createNew
	field public static Std.Fs.FileMode create
	field public static Std.Fs.FileMode open
	field public static Std.Fs.FileMode openOrCreate
	field public static Std.Fs.FileMode truncate
	field public static Std.Fs.FileMode append
}

struct Std.Fs.FileAccess extends System.Enum {
	field public instance System.Int32 value__
	field public static Std.Fs.FileAccess RO
	field public static Std.Fs.FileAccess WO
	field public static Std.Fs.FileAccess RW
}
