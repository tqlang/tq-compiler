
class <Module> extends  {
}

class Program.Static extends System.Object {
	field private static System.UInt64 src
	field private static System.UInt64 debug
	field private static System.Byte[] symbols
	field private static System.Byte[] text
	field private static System.Byte[] data
	field private static System.Byte[] stack
	field private static System.UInt64 srcPointer
	field private static System.UInt64 currToken
	field private static System.UInt64[] currIdentifier

	method private static main () System.Void {
		.locals init (System.String[] V_0)
		.locals init (System.Int64 V_1)
		.locals init (System.Int64 V_2)
		.locals init (Std.Fs.File V_3)
		.locals init (System.UInt64 V_4)
		.locals init (System.String V_5)
		IL_0000: call System.String[] Std.ProcessStatic::getProgramArguments()
		IL_0005: stloc V_0
		IL_0009: ldloc V_0
		IL_000D: ldlen
		IL_000E: conv.i8
		IL_000F: stloc V_1
		IL_0013: ldc.i8 1
		IL_001C: stloc V_2
		IL_0020: ldloc V_1
		IL_0024: ldloc V_2
		IL_0028: sub
		IL_0029: ldc.i8 0
		IL_0032: cgt
		IL_0034: brfalse IL_009A
		IL_0039: ldloc V_0
		IL_003D: ldloc V_2
		IL_0041: conv.i4
		IL_0042: ldelem System.String
		IL_0047: ldc.i8 0
		IL_0050: conv.i4
		IL_0051: call System.Char System.String::get_Chars(System.Int32)
		IL_0056: ldc.i4.s 45
		IL_0058: ceq
		IL_005A: brfalse IL_009A
		IL_005F: ldloc V_0
		IL_0063: ldloc V_2
		IL_0067: conv.i4
		IL_0068: ldelem System.String
		IL_006D: ldstr "-s"
		IL_0072: call System.Boolean System.String::Equals(System.String, System.String)
		IL_0077: brfalse IL_009A
		IL_007C: nop
		IL_007D: ldloc V_2
		IL_0081: dup
		IL_0082: ldc.i8 1
		IL_008B: add
		IL_008C: stloc V_2
		IL_0090: stsfld System.UInt64 Program.Static::src
		IL_0095: br IL_009A
		IL_009A: nop
		IL_009B: ldloc V_1
		IL_009F: ldloc V_2
		IL_00A3: sub
		IL_00A4: ldc.i8 0
		IL_00AD: cgt
		IL_00AF: brfalse IL_0115
		IL_00B4: ldloc V_0
		IL_00B8: ldloc V_2
		IL_00BC: conv.i4
		IL_00BD: ldelem System.String
		IL_00C2: ldc.i8 0
		IL_00CB: conv.i4
		IL_00CC: call System.Char System.String::get_Chars(System.Int32)
		IL_00D1: ldc.i4.s 45
		IL_00D3: ceq
		IL_00D5: brfalse IL_0115
		IL_00DA: ldloc V_0
		IL_00DE: ldloc V_2
		IL_00E2: conv.i4
		IL_00E3: ldelem System.String
		IL_00E8: ldstr "-d"
		IL_00ED: call System.Boolean System.String::Equals(System.String, System.String)
		IL_00F2: brfalse IL_0115
		IL_00F7: nop
		IL_00F8: ldloc V_2
		IL_00FC: dup
		IL_00FD: ldc.i8 1
		IL_0106: add
		IL_0107: stloc V_2
		IL_010B: stsfld System.UInt64 Program.Static::debug
		IL_0110: br IL_0115
		IL_0115: nop
		IL_0116: ldloc V_1
		IL_011A: ldloc V_2
		IL_011E: sub
		IL_011F: ldc.i8 1
		IL_0128: clt
		IL_012A: brfalse IL_014D
		IL_012F: nop
		IL_0130: ldstr "usage: c4 [-s] [-d] file ..."
		IL_0135: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_013A: ldc.i8 1
		IL_0143: call System.Void Std.ProcessStatic::exit(System.UInt64)
		IL_0148: br IL_014D
		IL_014D: nop
		IL_014E: ldloc V_0
		IL_0152: ldloc V_2
		IL_0156: conv.i4
		IL_0157: ldelem System.String
		IL_015C: call Std.Fs.File Std.FsStatic::open(System.String)
		IL_0161: stloc V_3
		IL_0165: ldstr "ok..."
		IL_016A: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_016F: ldc.i8 262144
		IL_0178: stloc V_4
		IL_017C: ldloc V_4
		IL_0180: ldc.i8 0
		IL_0189: call System.Byte[] Std.MemStatic::alloc__impl(System.UInt64, Std.Mem.Alignment)
		IL_018E: stsfld System.Byte[] Program.Static::symbols
		IL_0193: ldloc V_4
		IL_0197: ldc.i8 0
		IL_01A0: call System.Byte[] Std.MemStatic::alloc__impl(System.UInt64, Std.Mem.Alignment)
		IL_01A5: stsfld System.Byte[] Program.Static::text
		IL_01AA: ldloc V_4
		IL_01AE: ldc.i8 0
		IL_01B7: call System.Byte[] Std.MemStatic::alloc__impl(System.UInt64, Std.Mem.Alignment)
		IL_01BC: stsfld System.Byte[] Program.Static::data
		IL_01C1: ldloc V_4
		IL_01C5: ldc.i8 0
		IL_01CE: call System.Byte[] Std.MemStatic::alloc__impl(System.UInt64, Std.Mem.Alignment)
		IL_01D3: stsfld System.Byte[] Program.Static::stack
		IL_01D8: ldstr "char else enum if int return sizeof while open read close printf malloc free memset memcmp exit void main"
		IL_01DD: stloc V_5
		IL_01E1: ldstr "Not bad untill here"
		IL_01E6: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_01EB: ret
	}

	method private static next () System.Void {
		.locals init (System.UInt64 V_0)
		IL_0000: ldc.i8 0
		IL_0009: stloc V_0
		IL_000D: ret
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

	method public static exit (System.UInt64 A_0) System.Void {
		IL_0000: ldarg System.UInt64 A_0
		IL_0004: conv.i4
		IL_0005: call System.Void System.Environment::Exit(System.Int32)
		IL_000A: ret
	}
}

class Std.MemStatic extends System.Object {

	method public static alloc__impl (System.UInt64 alloc__impl, Std.Mem.Alignment A_1) System.Byte[] {
		IL_0000: ldarg System.UInt64 alloc__impl
		IL_0004: conv.i4
		IL_0005: ldc.i4.0
		IL_0006: call !!0[] System.GC::AllocateArray<System.Byte>(System.Int32, System.Boolean)
		IL_000B: ret
	}

	method public static alloc__impl (System.UInt64 alloc__impl, Std.Mem.Alignment A_1) System.Byte[] {
		IL_0000: ldarg System.UInt64 alloc__impl
		IL_0004: conv.i4
		IL_0005: ldc.i4.0
		IL_0006: call !!0[] System.GC::AllocateArray<System.Byte>(System.Int32, System.Boolean)
		IL_000B: ret
	}

	method public static alloc__impl (System.UInt64 alloc__impl, Std.Mem.Alignment A_1) System.Byte[] {
		IL_0000: ldarg System.UInt64 alloc__impl
		IL_0004: conv.i4
		IL_0005: ldc.i4.0
		IL_0006: call !!0[] System.GC::AllocateArray<System.Byte>(System.Int32, System.Boolean)
		IL_000B: ret
	}

	method public static alloc__impl (System.UInt64 alloc__impl, Std.Mem.Alignment A_1) System.Byte[] {
		IL_0000: ldarg System.UInt64 alloc__impl
		IL_0004: conv.i4
		IL_0005: ldc.i4.0
		IL_0006: call !!0[] System.GC::AllocateArray<System.Byte>(System.Int32, System.Boolean)
		IL_000B: ret
	}
}

class Std.TestStatic extends System.Object {

	method public static testValue (System.Byte testValue, System.Byte A_1) System.Void {
		IL_0000: ldarg System.Byte testValue
		IL_0004: ldarg System.Byte A_1
		IL_0008: ceq
		IL_000A: call System.Void Std.TestStatic::printStatusMessage(System.Boolean)
		IL_000F: ret
	}

	method private static printStatusMessage (System.Boolean A_0) System.Void {
		IL_0000: ldarg System.Boolean A_0
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

	method private static write (System.Int64 A_0) System.Void {
		IL_0000: ldarg System.Int64 A_0
		IL_0004: box System.Int64
		IL_0009: callvirt System.String System.Object::ToString()
		IL_000E: call System.Void System.Console::Write(System.String)
		IL_0013: ret
	}

	method private static write (System.UInt64 A_0) System.Void {
		IL_0000: ldarg System.UInt64 A_0
		IL_0004: box System.UInt64
		IL_0009: callvirt System.String System.Object::ToString()
		IL_000E: call System.Void System.Console::Write(System.String)
		IL_0013: ret
	}

	method private static write (System.Int128 A_0) System.Void {
		IL_0000: ldarg System.Int128 A_0
		IL_0004: box System.Int128
		IL_0009: callvirt System.String System.Object::ToString()
		IL_000E: call System.Void System.Console::Write(System.String)
		IL_0013: ret
	}

	method private static write (System.UInt128 A_0) System.Void {
		IL_0000: ldarg System.UInt128 A_0
		IL_0004: box System.UInt128
		IL_0009: callvirt System.String System.Object::ToString()
		IL_000E: call System.Void System.Console::Write(System.String)
		IL_0013: ret
	}

	method private static write (System.String A_0) System.Void {
		IL_0000: ldarg System.String A_0
		IL_0004: call System.Void System.Console::Write(System.String)
		IL_0009: ret
	}

	method private static writeln (System.Int64 A_0) System.Void {
		IL_0000: ldarg System.Int64 A_0
		IL_0004: box System.Int64
		IL_0009: callvirt System.String System.Object::ToString()
		IL_000E: call System.Void System.Console::WriteLine(System.String)
		IL_0013: ret
	}

	method private static writeln (System.UInt64 A_0) System.Void {
		IL_0000: ldarg System.UInt64 A_0
		IL_0004: box System.UInt64
		IL_0009: callvirt System.String System.Object::ToString()
		IL_000E: call System.Void System.Console::WriteLine(System.String)
		IL_0013: ret
	}

	method private static writeln (System.Int128 A_0) System.Void {
		IL_0000: ldarg System.Int128 A_0
		IL_0004: box System.Int128
		IL_0009: callvirt System.String System.Object::ToString()
		IL_000E: call System.Void System.Console::WriteLine(System.String)
		IL_0013: ret
	}

	method private static writeln (System.UInt128 A_0) System.Void {
		IL_0000: ldarg System.UInt128 A_0
		IL_0004: box System.UInt128
		IL_0009: callvirt System.String System.Object::ToString()
		IL_000E: call System.Void System.Console::WriteLine(System.String)
		IL_0013: ret
	}

	method private static writeln (System.String A_0) System.Void {
		IL_0000: ldarg System.String A_0
		IL_0004: call System.Void System.Console::WriteLine(System.String)
		IL_0009: ret
	}
}

class Std.FsStatic extends System.Object {

	method public static open (System.String A_0) Std.Fs.File {
		.locals init (System.IO.FileStream V_0)
		.locals init (Std.Fs.File V_1)
		IL_0000: ldarg System.String A_0
		IL_0004: ldc.i8 3
		IL_000D: ldc.i8 1
		IL_0016: newobj System.Void System.IO.FileStream::.ctor(System.String, System.IO.FileMode, System.IO.FileAccess)
		IL_001B: stloc V_0
		IL_001F: ldloca V_1
		IL_0023: ldloc V_0
		IL_0027: call System.Void Std.Fs.File::.ctor(System.IO.FileStream)
		IL_002C: ldloc V_1
		IL_0030: ret
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

struct Program.Tokens extends System.Enum {
	field public instance System.UInt64 value__
	field public static Program.Tokens num
	field public static Program.Tokens fun
	field public static Program.Tokens sys
	field public static Program.Tokens glo
	field public static Program.Tokens loc
	field public static Program.Tokens id
	field public static Program.Tokens _char
	field public static Program.Tokens _else
	field public static Program.Tokens enum
	field public static Program.Tokens _if
	field public static Program.Tokens _int
	field public static Program.Tokens ret
	field public static Program.Tokens sizeof
	field public static Program.Tokens _while
	field public static Program.Tokens assign
	field public static Program.Tokens cond
	field public static Program.Tokens opLor
	field public static Program.Tokens opLan
	field public static Program.Tokens opOr
	field public static Program.Tokens opXor
	field public static Program.Tokens opAnd
	field public static Program.Tokens opEq
	field public static Program.Tokens opNe
	field public static Program.Tokens poLt
	field public static Program.Tokens opGt
	field public static Program.Tokens opLe
	field public static Program.Tokens opGe
	field public static Program.Tokens opShl
	field public static Program.Tokens opShr
	field public static Program.Tokens opAdd
	field public static Program.Tokens opSub
	field public static Program.Tokens opMul
	field public static Program.Tokens opDiv
	field public static Program.Tokens opMod
	field public static Program.Tokens opInc
	field public static Program.Tokens opDec
	field public static Program.Tokens _break
}

struct Program.OpCodes extends System.Enum {
	field public instance System.UInt64 value__
	field public static Program.OpCodes lea
	field public static Program.OpCodes imm
	field public static Program.OpCodes jmp
	field public static Program.OpCodes jsr
	field public static Program.OpCodes bz
	field public static Program.OpCodes bnz
	field public static Program.OpCodes ent
	field public static Program.OpCodes adj
	field public static Program.OpCodes lev
	field public static Program.OpCodes li
	field public static Program.OpCodes lc
	field public static Program.OpCodes si
	field public static Program.OpCodes sc
	field public static Program.OpCodes psh
	field public static Program.OpCodes _or
	field public static Program.OpCodes xor
	field public static Program.OpCodes _and
	field public static Program.OpCodes eq
	field public static Program.OpCodes ne
	field public static Program.OpCodes lt
	field public static Program.OpCodes gt
	field public static Program.OpCodes le
	field public static Program.OpCodes ge
	field public static Program.OpCodes shl
	field public static Program.OpCodes shr
	field public static Program.OpCodes add
	field public static Program.OpCodes sub
	field public static Program.OpCodes mul
	field public static Program.OpCodes div
	field public static Program.OpCodes mod
	field public static Program.OpCodes open
	field public static Program.OpCodes read
	field public static Program.OpCodes close
	field public static Program.OpCodes printf
	field public static Program.OpCodes malloc
	field public static Program.OpCodes free
	field public static Program.OpCodes memset
	field public static Program.OpCodes memcmp
	field public static Program.OpCodes exit
}

struct Program.CTypes extends System.Enum {
	field public instance System.UInt64 value__
	field public static Program.CTypes _char
	field public static Program.CTypes _int
	field public static Program.CTypes ptr
}

struct Program.IdentifierOffsets extends System.Enum {
	field public instance System.UInt64 value__
	field public static Program.IdentifierOffsets tk
	field public static Program.IdentifierOffsets hash
	field public static Program.IdentifierOffsets name
	field public static Program.IdentifierOffsets class
	field public static Program.IdentifierOffsets _type
	field public static Program.IdentifierOffsets val
	field public static Program.IdentifierOffsets hClass
	field public static Program.IdentifierOffsets hType
	field public static Program.IdentifierOffsets hVal
	field public static Program.IdentifierOffsets idsz
}

struct Std.Mem.Alignment extends System.Enum {
	field public instance System.UInt64 value__
	field public static Std.Mem.Alignment _1
	field public static Std.Mem.Alignment _2
	field public static Std.Mem.Alignment _4
	field public static Std.Mem.Alignment _8
	field public static Std.Mem.Alignment _16
	field public static Std.Mem.Alignment _32
}

struct Std.Fs.SeekOrigin extends System.Enum {
	field public instance System.UInt64 value__
	field public static Std.Fs.SeekOrigin begin
	field public static Std.Fs.SeekOrigin current
	field public static Std.Fs.SeekOrigin end
}

struct Std.Fs.FileMode extends System.Enum {
	field public instance System.UInt64 value__
	field public static Std.Fs.FileMode createNew
	field public static Std.Fs.FileMode create
	field public static Std.Fs.FileMode open
	field public static Std.Fs.FileMode openOrCreate
	field public static Std.Fs.FileMode truncate
	field public static Std.Fs.FileMode append
}

struct Std.Fs.FileAccess extends System.Enum {
	field public instance System.UInt64 value__
	field public static Std.Fs.FileAccess _fuck
	field public static Std.Fs.FileAccess RO
	field public static Std.Fs.FileAccess WO
	field public static Std.Fs.FileAccess RW
}
