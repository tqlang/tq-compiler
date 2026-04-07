
class <Module>.RuntimeHelpers extends System.Object {

	method private static AsString <T> (!!0[] A_0) System.String {
		.locals init (System.Text.StringBuilder V_0)
		.locals init (System.Int32 V_1)
		.locals init (System.Int32 V_2)
		IL_0000: newobj System.Void System.Text.StringBuilder::.ctor()
		IL_0000: stloc V_0
		IL_0000: ldloc V_0
		IL_0000: ldc.i4.s 91
		IL_0000: call System.Text.StringBuilder System.Text.StringBuilder::Append(System.Char)
		IL_0000: pop
		IL_0000: ldc.i4.0
		IL_0000: stloc V_1
		IL_0000: ldarg.0
		IL_0000: ldlen
		IL_0000: dup
		IL_0000: stloc V_2
		IL_0000: brfalse IL_0000
		IL_0000: br IL_0000
		IL_0000: ldloc V_0
		IL_0000: ldarg.0
		IL_0000: ldloc V_1
		IL_0000: ldelem !!0
		IL_0000: box !!0
		IL_0000: callvirt System.String System.Object::ToString()
		IL_0000: call System.Text.StringBuilder System.Text.StringBuilder::Append(System.String)
		IL_0000: ldstr ", "
		IL_0000: call System.Text.StringBuilder System.Text.StringBuilder::Append(System.String)
		IL_0000: pop
		IL_0000: ldloc V_1
		IL_0000: ldc.i4.1
		IL_0000: add
		IL_0000: stloc V_1
		IL_0000: ldloc V_1
		IL_0000: ldloc V_2
		IL_0000: blt IL_0000
		IL_0000: ldloc V_0
		IL_0000: dup
		IL_0000: call System.Int32 System.Text.StringBuilder::get_Length()
		IL_0000: ldc.i4.2
		IL_0000: sub
		IL_0000: call System.Void System.Text.StringBuilder::set_Length(System.Int32)
		IL_0000: ldloc V_0
		IL_0000: ldc.i4.s 93
		IL_0000: call System.Text.StringBuilder System.Text.StringBuilder::Append(System.Char)
		IL_0000: call System.String System.Text.StringBuilder::ToString()
		IL_0000: ret
	}

	method private static AddSaturatedI8 (System.SByte A_0, System.SByte A_1) System.SByte {
		IL_0000: ldarg.0
		IL_0000: ldarg.1
		IL_0000: add
		IL_0000: dup
		IL_0000: ldc.i4 127
		IL_0000: bgt IL_0000
		IL_0000: dup
		IL_0000: ldc.i4.s -128
		IL_0000: blt IL_0000
		IL_0000: conv.i1
		IL_0000: ret
		IL_0000: pop
		IL_0000: ldc.i4.s 127
		IL_0000: conv.i1
		IL_0000: ret
		IL_0000: pop
		IL_0000: ldc.i4.s -128
		IL_0000: ret
	}

	method private static AddSaturatedU8 (System.Byte A_0, System.Byte A_1) System.Byte {
		IL_0000: ldarg.0
		IL_0000: ldarg.1
		IL_0000: add
		IL_0000: dup
		IL_0000: ldc.i4 255
		IL_0000: bgt.un IL_0000
		IL_0000: conv.u1
		IL_0000: ret
		IL_0000: pop
		IL_0000: ldc.i4.s -1
		IL_0000: ret
	}

	method private static AddSaturatedI16 (System.Int16 A_0, System.Int16 A_1) System.Int16 {
		IL_0000: ldarg.0
		IL_0000: ldarg.1
		IL_0000: add
		IL_0000: dup
		IL_0000: ldc.i4 32767
		IL_0000: bgt IL_0000
		IL_0000: dup
		IL_0000: ldc.i4 -32768
		IL_0000: blt IL_0000
		IL_0000: conv.i2
		IL_0000: ret
		IL_0000: pop
		IL_0000: ldc.i4 32767
		IL_0000: conv.i2
		IL_0000: ret
		IL_0000: pop
		IL_0000: ldc.i4 -32768
		IL_0000: ret
	}

	method private static AddSaturatedU16 (System.UInt16 A_0, System.UInt16 A_1) System.UInt16 {
		IL_0000: ldarg.0
		IL_0000: ldarg.1
		IL_0000: add
		IL_0000: dup
		IL_0000: ldc.i4 65535
		IL_0000: bgt.un IL_0000
		IL_0000: conv.u2
		IL_0000: ret
		IL_0000: pop
		IL_0000: ldc.i4 65535
		IL_0000: ret
	}
}

class Program.Program extends System.Object {

	method public static main () System.Void {
		IL_0000: call System.Void Program.Program::testArithmeticU8()
		IL_0005: call System.Void Program.Program::testSlices()
		IL_000A: ret
	}

	method private static testArithmeticU8 () System.Void {
		.locals init (System.Byte V_0)
		.locals init (System.Byte V_1)
		.locals init (System.Byte V_2)
		.locals init (System.Byte V_3)
		.locals init (System.Byte V_4)
		.locals init (System.Byte V_5)
		IL_0000: ldstr "Testing u8 aritmetic..."
		IL_0005: call System.Void Std.Std+Console::writeln(System.String)
		IL_000A: ldc.i4.0
		IL_000B: stloc V_0
		IL_000F: ldc.i4.1
		IL_0010: stloc V_1
		IL_0014: ldc.i4.2
		IL_0015: stloc V_2
		IL_0019: ldc.i4.s 100
		IL_001B: stloc V_3
		IL_001F: ldc.i4.0
		IL_0020: stloc V_4
		IL_0024: ldc.i4 255
		IL_0029: stloc V_5
		IL_002D: ldstr "u8 arithmetic - checked add"
		IL_0032: call System.Void Std.Std+Console::writeln(System.String)
		IL_0037: ldloc V_1
		IL_003B: ldloc V_1
		IL_003F: add
		IL_0040: conv.ovf.u1
		IL_0041: ldc.i4.2
		IL_0042: conv.ovf.u1.un
		IL_0043: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_0048: ldloc V_2
		IL_004C: ldloc V_2
		IL_0050: add
		IL_0051: conv.ovf.u1
		IL_0052: ldc.i4.4
		IL_0053: conv.ovf.u1.un
		IL_0054: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_0059: ldloc V_5
		IL_005D: ldc.i4.0
		IL_005E: add
		IL_005F: conv.ovf.u1
		IL_0060: ldloc V_5
		IL_0064: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_0069: ldstr "u8 arithmetic - add with overflow"
		IL_006E: call System.Void Std.Std+Console::writeln(System.String)
		IL_0073: ldloc V_1
		IL_0077: ldloc V_1
		IL_007B: add
		IL_007C: conv.u1
		IL_007D: ldc.i4.2
		IL_007E: conv.ovf.u1.un
		IL_007F: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_0084: ldloc V_2
		IL_0088: ldloc V_2
		IL_008C: add
		IL_008D: conv.u1
		IL_008E: ldc.i4.4
		IL_008F: conv.ovf.u1.un
		IL_0090: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_0095: ldloc V_5
		IL_0099: ldloc V_0
		IL_009D: add
		IL_009E: conv.u1
		IL_009F: ldloc V_5
		IL_00A3: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_00A8: ldloc V_5
		IL_00AC: ldloc V_1
		IL_00B0: add
		IL_00B1: conv.u1
		IL_00B2: ldloc V_4
		IL_00B6: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_00BB: ldloc V_5
		IL_00BF: ldloc V_3
		IL_00C3: add
		IL_00C4: conv.u1
		IL_00C5: ldc.i4.s 99
		IL_00C7: conv.ovf.u1.un
		IL_00C8: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_00CD: ldstr "u8 arithmetic - add on bounds"
		IL_00D2: call System.Void Std.Std+Console::writeln(System.String)
		IL_00D7: ldloc V_1
		IL_00DB: ldloc V_1
		IL_00DF: call System.Byte <Module>.RuntimeHelpers::AddSaturatedU8(System.Byte, System.Byte)
		IL_00E4: ldc.i4.2
		IL_00E5: conv.ovf.u1.un
		IL_00E6: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_00EB: ldloc V_2
		IL_00EF: ldloc V_2
		IL_00F3: call System.Byte <Module>.RuntimeHelpers::AddSaturatedU8(System.Byte, System.Byte)
		IL_00F8: ldc.i4.4
		IL_00F9: conv.ovf.u1.un
		IL_00FA: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_00FF: ldloc V_5
		IL_0103: ldloc V_0
		IL_0107: call System.Byte <Module>.RuntimeHelpers::AddSaturatedU8(System.Byte, System.Byte)
		IL_010C: ldloc V_5
		IL_0110: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_0115: ldloc V_5
		IL_0119: ldloc V_1
		IL_011D: call System.Byte <Module>.RuntimeHelpers::AddSaturatedU8(System.Byte, System.Byte)
		IL_0122: ldloc V_5
		IL_0126: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_012B: ldloc V_5
		IL_012F: ldloc V_3
		IL_0133: call System.Byte <Module>.RuntimeHelpers::AddSaturatedU8(System.Byte, System.Byte)
		IL_0138: ldloc V_5
		IL_013C: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_0141: ret
	}

	method private static testSlices () System.Void {
		.locals init (System.Byte[] V_0)
		.locals init (System.Byte[] V_1)
		.locals init (System.Byte[] V_2)
		.locals init (System.Byte[] V_3)
		.locals init (System.Byte[] V_4)
		.locals init (System.Byte[] V_5)
		.locals init (System.Byte[] V_6)
		IL_0000: ldstr "Testing slices..."
		IL_0005: call System.Void Std.Std+Console::writeln(System.String)
		IL_000A: ldc.i4.5
		IL_000B: newarr System.Byte
		IL_0010: dup
		IL_0011: ldc.i4.0
		IL_0012: ldc.i4.s 10
		IL_0014: stelem.i1
		IL_0015: dup
		IL_0016: ldc.i4.1
		IL_0017: ldc.i4.s 20
		IL_0019: stelem.i1
		IL_001A: dup
		IL_001B: ldc.i4.2
		IL_001C: ldc.i4.s 30
		IL_001E: stelem.i1
		IL_001F: dup
		IL_0020: ldc.i4.3
		IL_0021: ldc.i4.s 40
		IL_0023: stelem.i1
		IL_0024: dup
		IL_0025: ldc.i4.4
		IL_0026: ldc.i4.s 50
		IL_0028: stelem.i1
		IL_0029: stloc V_0
		IL_002D: ldc.i4.5
		IL_002E: newarr System.Byte
		IL_0033: dup
		IL_0034: ldc.i4.0
		IL_0035: ldc.i4.0
		IL_0036: stelem.i1
		IL_0037: dup
		IL_0038: ldc.i4.1
		IL_0039: ldc.i4.0
		IL_003A: stelem.i1
		IL_003B: dup
		IL_003C: ldc.i4.2
		IL_003D: ldc.i4.0
		IL_003E: stelem.i1
		IL_003F: dup
		IL_0040: ldc.i4.3
		IL_0041: ldc.i4.0
		IL_0042: stelem.i1
		IL_0043: dup
		IL_0044: ldc.i4.4
		IL_0045: ldc.i4.0
		IL_0046: stelem.i1
		IL_0047: stloc V_1
		IL_004B: ldc.i4.5
		IL_004C: newarr System.Byte
		IL_0051: dup
		IL_0052: ldc.i4.0
		IL_0053: ldc.i4.s 10
		IL_0055: stelem.i1
		IL_0056: dup
		IL_0057: ldc.i4.1
		IL_0058: ldc.i4.s 20
		IL_005A: stelem.i1
		IL_005B: dup
		IL_005C: ldc.i4.2
		IL_005D: ldc.i4.s 30
		IL_005F: stelem.i1
		IL_0060: dup
		IL_0061: ldc.i4.3
		IL_0062: ldc.i4.s 40
		IL_0064: stelem.i1
		IL_0065: dup
		IL_0066: ldc.i4.4
		IL_0067: ldc.i4.s 50
		IL_0069: stelem.i1
		IL_006A: stloc V_2
		IL_006E: ldc.i4.5
		IL_006F: newarr System.Byte
		IL_0074: dup
		IL_0075: ldc.i4.0
		IL_0076: ldc.i4.0
		IL_0077: stelem.i1
		IL_0078: dup
		IL_0079: ldc.i4.1
		IL_007A: ldc.i4.0
		IL_007B: stelem.i1
		IL_007C: dup
		IL_007D: ldc.i4.2
		IL_007E: ldc.i4.0
		IL_007F: stelem.i1
		IL_0080: dup
		IL_0081: ldc.i4.3
		IL_0082: ldc.i4.0
		IL_0083: stelem.i1
		IL_0084: dup
		IL_0085: ldc.i4.4
		IL_0086: ldc.i4.0
		IL_0087: stelem.i1
		IL_0088: stloc V_3
		IL_008C: ldc.i4.5
		IL_008D: newarr System.Byte
		IL_0092: dup
		IL_0093: ldc.i4.0
		IL_0094: ldc.i4.5
		IL_0095: stelem.i1
		IL_0096: dup
		IL_0097: ldc.i4.1
		IL_0098: ldc.i4.5
		IL_0099: stelem.i1
		IL_009A: dup
		IL_009B: ldc.i4.2
		IL_009C: ldc.i4.5
		IL_009D: stelem.i1
		IL_009E: dup
		IL_009F: ldc.i4.3
		IL_00A0: ldc.i4.5
		IL_00A1: stelem.i1
		IL_00A2: dup
		IL_00A3: ldc.i4.4
		IL_00A4: ldc.i4.5
		IL_00A5: stelem.i1
		IL_00A6: stloc V_4
		IL_00AA: ldc.i4.5
		IL_00AB: newarr System.Byte
		IL_00B0: dup
		IL_00B1: ldc.i4.0
		IL_00B2: ldc.i4.s 10
		IL_00B4: stelem.i1
		IL_00B5: dup
		IL_00B6: ldc.i4.1
		IL_00B7: ldc.i4.s 10
		IL_00B9: stelem.i1
		IL_00BA: dup
		IL_00BB: ldc.i4.2
		IL_00BC: ldc.i4.s 10
		IL_00BE: stelem.i1
		IL_00BF: dup
		IL_00C0: ldc.i4.3
		IL_00C1: ldc.i4.s 10
		IL_00C3: stelem.i1
		IL_00C4: dup
		IL_00C5: ldc.i4.4
		IL_00C6: ldc.i4.s 10
		IL_00C8: stelem.i1
		IL_00C9: stloc V_5
		IL_00CD: ldc.i4.5
		IL_00CE: newarr System.Byte
		IL_00D3: dup
		IL_00D4: ldc.i4.0
		IL_00D5: ldc.i4.s 15
		IL_00D7: stelem.i1
		IL_00D8: dup
		IL_00D9: ldc.i4.1
		IL_00DA: ldc.i4.s 15
		IL_00DC: stelem.i1
		IL_00DD: dup
		IL_00DE: ldc.i4.2
		IL_00DF: ldc.i4.s 15
		IL_00E1: stelem.i1
		IL_00E2: dup
		IL_00E3: ldc.i4.3
		IL_00E4: ldc.i4.s 15
		IL_00E6: stelem.i1
		IL_00E7: dup
		IL_00E8: ldc.i4.4
		IL_00E9: ldc.i4.s 15
		IL_00EB: stelem.i1
		IL_00EC: stloc V_6
		IL_00F0: ldloc V_0
		IL_00F4: ldloc V_2
		IL_00F8: call System.Void Std.Std+Test::assert<System.Byte>(!!0[], !!0[])
		IL_00FD: ldloc V_0
		IL_0101: call System.String <Module>.RuntimeHelpers::AsString<System.Byte>(!!0[])
		IL_0106: ldstr "[10, 20, 30, 40, 50]"
		IL_010B: call System.Void Std.Std+Test::assert(System.String, System.String)
		IL_0110: ldloc V_1
		IL_0114: ldloc V_3
		IL_0118: call System.Void Std.Std+Test::assert<System.Byte>(!!0[], !!0[])
		IL_011D: ldloc V_1
		IL_0121: call System.String <Module>.RuntimeHelpers::AsString<System.Byte>(!!0[])
		IL_0126: ldstr "[0, 0, 0, 0, 0]"
		IL_012B: call System.Void Std.Std+Test::assert(System.String, System.String)
		IL_0130: ldloc V_0
		IL_0134: ldloc V_1
		IL_0138: call System.Boolean Std.Std+Mem::compare(System.Byte[], System.Byte[])
		IL_013D: ldc.i4.0
		IL_013E: call System.Void Std.Std+Test::assert(System.Boolean, System.Boolean)
		IL_0143: ldloc V_0
		IL_0147: ldloc V_1
		IL_014B: call System.Void Std.Std+Mem::copy(System.Byte[], System.Byte[])
		IL_0150: ldloc V_0
		IL_0154: ldloc V_2
		IL_0158: call System.Void Std.Std+Test::assert<System.Byte>(!!0[], !!0[])
		IL_015D: ldloc V_0
		IL_0161: call System.String <Module>.RuntimeHelpers::AsString<System.Byte>(!!0[])
		IL_0166: ldstr "[10, 20, 30, 40, 50]"
		IL_016B: call System.Void Std.Std+Test::assert(System.String, System.String)
		IL_0170: ldloc V_1
		IL_0174: ldloc V_2
		IL_0178: call System.Void Std.Std+Test::assert<System.Byte>(!!0[], !!0[])
		IL_017D: ldloc V_1
		IL_0181: call System.String <Module>.RuntimeHelpers::AsString<System.Byte>(!!0[])
		IL_0186: ldstr "[10, 20, 30, 40, 50]"
		IL_018B: call System.Void Std.Std+Test::assert(System.String, System.String)
		IL_0190: ldloc V_0
		IL_0194: ldloc V_1
		IL_0198: call System.Boolean Std.Std+Mem::compare(System.Byte[], System.Byte[])
		IL_019D: ldc.i4.1
		IL_019E: call System.Void Std.Std+Test::assert(System.Boolean, System.Boolean)
		IL_01A3: ldloc V_0
		IL_01A7: ldc.i4.5
		IL_01A8: conv.ovf.u1.un
		IL_01A9: call System.Void Std.Std+Mem::fill(System.Byte[], System.Byte)
		IL_01AE: ldloc V_1
		IL_01B2: ldloc V_4
		IL_01B6: call System.Void Std.Std+Test::assert<System.Byte>(!!0[], !!0[])
		IL_01BB: ldloc V_0
		IL_01BF: ldc.i4.s 10
		IL_01C1: conv.ovf.u1.un
		IL_01C2: call System.Void Std.Std+Mem::fill(System.Byte[], System.Byte)
		IL_01C7: ldloc V_1
		IL_01CB: ldloc V_5
		IL_01CF: call System.Void Std.Std+Test::assert<System.Byte>(!!0[], !!0[])
		IL_01D4: ldloc V_0
		IL_01D8: ldc.i4.s 15
		IL_01DA: conv.ovf.u1.un
		IL_01DB: call System.Void Std.Std+Mem::fill(System.Byte[], System.Byte)
		IL_01E0: ldloc V_1
		IL_01E4: ldloc V_6
		IL_01E8: call System.Void Std.Std+Test::assert<System.Byte>(!!0[], !!0[])
		IL_01ED: ret
	}
}

class Std.Std extends System.Object {
}

class Std.Std+Io extends System.Object {
}

class Std.Std+Process extends System.Object {

	method public static getProgramArguments () System.String[] {
		IL_0000: call System.String[] System.Environment::GetCommandLineArgs()
		IL_0005: ret
	}

	method public static exit (System.UInt64 status) System.Void {
		IL_0000: ldarg System.UInt64 status
		IL_0004: conv.i4
		IL_0005: call System.Void System.Environment::Exit(System.Int32)
		IL_000A: ret
	}
}

class Std.Std+Mem extends System.Object {

	method public static copy (System.SByte[] source, System.SByte[] target) System.Void {
		IL_0000: ldarg System.SByte[] source
		IL_0004: ldarg System.SByte[] target
		IL_0008: call System.Void Std.Std+Mem::__copy_impl__<System.SByte>(!!0[], !!0[])
		IL_000D: ret
	}

	method public static copy (System.Byte[] source, System.Byte[] target) System.Void {
		IL_0000: ldarg System.Byte[] source
		IL_0004: ldarg System.Byte[] target
		IL_0008: call System.Void Std.Std+Mem::__copy_impl__<System.Byte>(!!0[], !!0[])
		IL_000D: ret
	}

	method public static copy (System.Int16[] source, System.Int16[] target) System.Void {
		IL_0000: ldarg System.Int16[] source
		IL_0004: ldarg System.Int16[] target
		IL_0008: call System.Void Std.Std+Mem::__copy_impl__<System.Int16>(!!0[], !!0[])
		IL_000D: ret
	}

	method public static copy (System.UInt16[] source, System.UInt16[] target) System.Void {
		IL_0000: ldarg System.UInt16[] source
		IL_0004: ldarg System.UInt16[] target
		IL_0008: call System.Void Std.Std+Mem::__copy_impl__<System.UInt16>(!!0[], !!0[])
		IL_000D: ret
	}

	method public static copy (System.Int32[] source, System.Int32[] target) System.Void {
		IL_0000: ldarg System.Int32[] source
		IL_0004: ldarg System.Int32[] target
		IL_0008: call System.Void Std.Std+Mem::__copy_impl__<System.Int32>(!!0[], !!0[])
		IL_000D: ret
	}

	method public static copy (System.UInt32[] source, System.UInt32[] target) System.Void {
		IL_0000: ldarg System.UInt32[] source
		IL_0004: ldarg System.UInt32[] target
		IL_0008: call System.Void Std.Std+Mem::__copy_impl__<System.UInt32>(!!0[], !!0[])
		IL_000D: ret
	}

	method public static copy (System.Int64[] source, System.Int64[] target) System.Void {
		IL_0000: ldarg System.Int64[] source
		IL_0004: ldarg System.Int64[] target
		IL_0008: call System.Void Std.Std+Mem::__copy_impl__<System.Int64>(!!0[], !!0[])
		IL_000D: ret
	}

	method public static copy (System.UInt64[] source, System.UInt64[] target) System.Void {
		IL_0000: ldarg System.UInt64[] source
		IL_0004: ldarg System.UInt64[] target
		IL_0008: call System.Void Std.Std+Mem::__copy_impl__<System.UInt64>(!!0[], !!0[])
		IL_000D: ret
	}

	method public static copy (System.Int128[] source, System.Int128[] target) System.Void {
		IL_0000: ldarg System.Int128[] source
		IL_0004: ldarg System.Int128[] target
		IL_0008: call System.Void Std.Std+Mem::__copy_impl__<System.Int128>(!!0[], !!0[])
		IL_000D: ret
	}

	method public static copy (System.UInt128[] source, System.UInt128[] target) System.Void {
		IL_0000: ldarg System.UInt128[] source
		IL_0004: ldarg System.UInt128[] target
		IL_0008: call System.Void Std.Std+Mem::__copy_impl__<System.UInt128>(!!0[], !!0[])
		IL_000D: ret
	}

	method public static copy (System.Boolean[] source, System.Boolean[] target) System.Void {
		IL_0000: ldarg System.Boolean[] source
		IL_0004: ldarg System.Boolean[] target
		IL_0008: call System.Void Std.Std+Mem::__copy_impl__<System.Boolean>(!!0[], !!0[])
		IL_000D: ret
	}

	method public static copy (System.Object[] source, System.Object[] target) System.Void {
		IL_0000: ldarg System.Object[] source
		IL_0004: ldarg System.Object[] target
		IL_0008: call System.Void Std.Std+Mem::__copy_impl__<System.Object>(!!0[], !!0[])
		IL_000D: ret
	}

	method private static __copy_impl__ <T> (!!0[] source, !!0[] target) System.Void {
		.locals init (System.Int64 V_0)
		IL_0000: ldarg !!0[] source
		IL_0004: ldlen
		IL_0005: conv.u8
		IL_0006: ldarg !!0[] target
		IL_000A: ldlen
		IL_000B: conv.u8
		IL_000C: ceq
		IL_000E: ldc.i4.0
		IL_000F: ceq
		IL_0011: brfalse IL_001D
		IL_0016: nop
		IL_0017: ret
		IL_0018: br IL_001D
		IL_001D: nop
		IL_001E: ldc.i8 0
		IL_0027: stloc V_0
		IL_002B: br IL_005E
		IL_0030: ldarg !!0[] target
		IL_0034: ldloc V_0
		IL_0038: conv.ovf.i4
		IL_0039: ldarg !!0[] source
		IL_003D: ldloc V_0
		IL_0041: conv.i4
		IL_0042: ldelem !!0
		IL_0047: stelem !!0
		IL_004C: ldloc V_0
		IL_0050: ldc.i8 1
		IL_0059: add
		IL_005A: stloc V_0
		IL_005E: ldloc V_0
		IL_0062: ldarg !!0[] source
		IL_0066: ldlen
		IL_0067: conv.u8
		IL_0068: clt
		IL_006A: brtrue IL_0030
		IL_006F: nop
		IL_0070: ret
	}

	method public static compare (System.SByte[] a, System.SByte[] b) System.Boolean {
		IL_0000: ldarg System.SByte[] a
		IL_0004: ldarg System.SByte[] b
		IL_0008: call System.Boolean Std.Std+Mem::__compare_impl__<System.SByte>(!!0[], !!0[])
		IL_000D: ret
	}

	method public static compare (System.Byte[] a, System.Byte[] b) System.Boolean {
		IL_0000: ldarg System.Byte[] a
		IL_0004: ldarg System.Byte[] b
		IL_0008: call System.Boolean Std.Std+Mem::__compare_impl__<System.Byte>(!!0[], !!0[])
		IL_000D: ret
	}

	method public static compare (System.Int16[] a, System.Int16[] b) System.Boolean {
		IL_0000: ldarg System.Int16[] a
		IL_0004: ldarg System.Int16[] b
		IL_0008: call System.Boolean Std.Std+Mem::__compare_impl__<System.Int16>(!!0[], !!0[])
		IL_000D: ret
	}

	method public static compare (System.UInt16[] a, System.UInt16[] b) System.Boolean {
		IL_0000: ldarg System.UInt16[] a
		IL_0004: ldarg System.UInt16[] b
		IL_0008: call System.Boolean Std.Std+Mem::__compare_impl__<System.UInt16>(!!0[], !!0[])
		IL_000D: ret
	}

	method public static compare (System.Int32[] a, System.Int32[] b) System.Boolean {
		IL_0000: ldarg System.Int32[] a
		IL_0004: ldarg System.Int32[] b
		IL_0008: call System.Boolean Std.Std+Mem::__compare_impl__<System.Int32>(!!0[], !!0[])
		IL_000D: ret
	}

	method public static compare (System.UInt32[] a, System.UInt32[] b) System.Boolean {
		IL_0000: ldarg System.UInt32[] a
		IL_0004: ldarg System.UInt32[] b
		IL_0008: call System.Boolean Std.Std+Mem::__compare_impl__<System.UInt32>(!!0[], !!0[])
		IL_000D: ret
	}

	method public static compare (System.Int64[] a, System.Int64[] b) System.Boolean {
		IL_0000: ldarg System.Int64[] a
		IL_0004: ldarg System.Int64[] b
		IL_0008: call System.Boolean Std.Std+Mem::__compare_impl__<System.Int64>(!!0[], !!0[])
		IL_000D: ret
	}

	method public static compare (System.UInt64[] a, System.UInt64[] b) System.Boolean {
		IL_0000: ldarg System.UInt64[] a
		IL_0004: ldarg System.UInt64[] b
		IL_0008: call System.Boolean Std.Std+Mem::__compare_impl__<System.UInt64>(!!0[], !!0[])
		IL_000D: ret
	}

	method public static compare (System.Int128[] a, System.Int128[] b) System.Boolean {
		IL_0000: ldarg System.Int128[] a
		IL_0004: ldarg System.Int128[] b
		IL_0008: call System.Boolean Std.Std+Mem::__compare_impl__<System.Int128>(!!0[], !!0[])
		IL_000D: ret
	}

	method public static compare (System.UInt128[] a, System.UInt128[] b) System.Boolean {
		IL_0000: ldarg System.UInt128[] a
		IL_0004: ldarg System.UInt128[] b
		IL_0008: call System.Boolean Std.Std+Mem::__compare_impl__<System.UInt128>(!!0[], !!0[])
		IL_000D: ret
	}

	method public static compare (System.Boolean[] a, System.Boolean[] b) System.Boolean {
		IL_0000: ldarg System.Boolean[] a
		IL_0004: ldarg System.Boolean[] b
		IL_0008: call System.Boolean Std.Std+Mem::__compare_impl__<System.Boolean>(!!0[], !!0[])
		IL_000D: ret
	}

	method public static compare (System.Object[] a, System.Object[] b) System.Boolean {
		IL_0000: ldarg System.Object[] a
		IL_0004: ldarg System.Object[] b
		IL_0008: call System.Boolean Std.Std+Mem::__compare_impl__<System.Object>(!!0[], !!0[])
		IL_000D: ret
	}

	method private static __compare_impl__ <T> (!!0[] a, !!0[] b) System.Boolean {
		.locals init (System.Int64 V_0)
		IL_0000: ldarg !!0[] a
		IL_0004: ldlen
		IL_0005: conv.u8
		IL_0006: ldarg !!0[] b
		IL_000A: ldlen
		IL_000B: conv.u8
		IL_000C: ceq
		IL_000E: ldc.i4.0
		IL_000F: ceq
		IL_0011: brfalse IL_001E
		IL_0016: nop
		IL_0017: ldc.i4.0
		IL_0018: ret
		IL_0019: br IL_001E
		IL_001E: nop
		IL_001F: ldc.i8 0
		IL_0028: stloc V_0
		IL_002C: br IL_007F
		IL_0031: ldarg !!0[] a
		IL_0035: ldloc V_0
		IL_0039: conv.i4
		IL_003A: ldelem !!0
		IL_003F: box !!0
		IL_0044: ldarg !!0[] b
		IL_0048: ldloc V_0
		IL_004C: conv.i4
		IL_004D: ldelem !!0
		IL_0052: box !!0
		IL_0057: callvirt System.Boolean System.Object::Equals(System.Object)
		IL_005C: ldc.i4.0
		IL_005D: ceq
		IL_005F: brfalse IL_006C
		IL_0064: nop
		IL_0065: ldc.i4.0
		IL_0066: ret
		IL_0067: br IL_006C
		IL_006C: nop
		IL_006D: ldloc V_0
		IL_0071: ldc.i8 1
		IL_007A: add
		IL_007B: stloc V_0
		IL_007F: ldloc V_0
		IL_0083: ldarg !!0[] a
		IL_0087: ldlen
		IL_0088: conv.u8
		IL_0089: clt
		IL_008B: brtrue IL_0031
		IL_0090: nop
		IL_0091: ldc.i4.1
		IL_0092: ret
	}

	method public static fill (System.SByte[] slice, System.SByte value) System.Void {
		IL_0000: ldarg System.SByte[] slice
		IL_0004: ldarg System.SByte value
		IL_0008: call System.Void Std.Std+Mem::__fill_impl__<System.SByte>(!!0[], !!0)
		IL_000D: ret
	}

	method public static fill (System.Byte[] slice, System.Byte value) System.Void {
		IL_0000: ldarg System.Byte[] slice
		IL_0004: ldarg System.Byte value
		IL_0008: call System.Void Std.Std+Mem::__fill_impl__<System.Byte>(!!0[], !!0)
		IL_000D: ret
	}

	method public static fill (System.Int16[] slice, System.Int16 value) System.Void {
		IL_0000: ldarg System.Int16[] slice
		IL_0004: ldarg System.Int16 value
		IL_0008: call System.Void Std.Std+Mem::__fill_impl__<System.Int16>(!!0[], !!0)
		IL_000D: ret
	}

	method public static fill (System.UInt16[] slice, System.UInt16 value) System.Void {
		IL_0000: ldarg System.UInt16[] slice
		IL_0004: ldarg System.UInt16 value
		IL_0008: call System.Void Std.Std+Mem::__fill_impl__<System.UInt16>(!!0[], !!0)
		IL_000D: ret
	}

	method public static fill (System.Int32[] slice, System.Int32 value) System.Void {
		IL_0000: ldarg System.Int32[] slice
		IL_0004: ldarg System.Int32 value
		IL_0008: call System.Void Std.Std+Mem::__fill_impl__<System.Int32>(!!0[], !!0)
		IL_000D: ret
	}

	method public static fill (System.UInt32[] slice, System.UInt32 value) System.Void {
		IL_0000: ldarg System.UInt32[] slice
		IL_0004: ldarg System.UInt32 value
		IL_0008: call System.Void Std.Std+Mem::__fill_impl__<System.UInt32>(!!0[], !!0)
		IL_000D: ret
	}

	method public static fill (System.Int64[] slice, System.Int64 value) System.Void {
		IL_0000: ldarg System.Int64[] slice
		IL_0004: ldarg System.Int64 value
		IL_0008: call System.Void Std.Std+Mem::__fill_impl__<System.Int64>(!!0[], !!0)
		IL_000D: ret
	}

	method public static fill (System.UInt64[] slice, System.UInt64 value) System.Void {
		IL_0000: ldarg System.UInt64[] slice
		IL_0004: ldarg System.UInt64 value
		IL_0008: call System.Void Std.Std+Mem::__fill_impl__<System.UInt64>(!!0[], !!0)
		IL_000D: ret
	}

	method public static fill (System.Int128[] slice, System.Int128 value) System.Void {
		IL_0000: ldarg System.Int128[] slice
		IL_0004: ldarg System.Int128 value
		IL_0008: call System.Void Std.Std+Mem::__fill_impl__<System.Int128>(!!0[], !!0)
		IL_000D: ret
	}

	method public static fill (System.UInt128[] slice, System.UInt128 value) System.Void {
		IL_0000: ldarg System.UInt128[] slice
		IL_0004: ldarg System.UInt128 value
		IL_0008: call System.Void Std.Std+Mem::__fill_impl__<System.UInt128>(!!0[], !!0)
		IL_000D: ret
	}

	method public static fill (System.Boolean[] slice, System.Boolean value) System.Void {
		IL_0000: ldarg System.Boolean[] slice
		IL_0004: ldarg System.Boolean value
		IL_0008: call System.Void Std.Std+Mem::__fill_impl__<System.Boolean>(!!0[], !!0)
		IL_000D: ret
	}

	method public static fill (System.Object[] slice, System.Object value) System.Void {
		IL_0000: ldarg System.Object[] slice
		IL_0004: ldarg System.Object value
		IL_0008: call System.Void Std.Std+Mem::__fill_impl__<System.Object>(!!0[], !!0)
		IL_000D: ret
	}

	method private static __fill_impl__ <T> (!!0[] slice, !!0 value) System.Void {
		.locals init (System.Int64 V_0)
		IL_0000: ldc.i8 0
		IL_0009: stloc V_0
		IL_000D: br IL_0036
		IL_0012: ldarg !!0[] slice
		IL_0016: ldloc V_0
		IL_001A: conv.ovf.i4
		IL_001B: ldarg !!0 value
		IL_001F: stelem !!0
		IL_0024: ldloc V_0
		IL_0028: ldc.i8 1
		IL_0031: add
		IL_0032: stloc V_0
		IL_0036: ldloc V_0
		IL_003A: ldarg !!0[] slice
		IL_003E: ldlen
		IL_003F: conv.u8
		IL_0040: clt
		IL_0042: brtrue IL_0012
		IL_0047: nop
		IL_0048: ret
	}

	method public static alloc <T> (System.UInt64 length, Std.Std+Mem+Alignment alignment) !!0[] {
		IL_0000: ldarg System.UInt64 length
		IL_0004: conv.i4
		IL_0005: ldc.i4.0
		IL_0006: call !!0[] System.GC::AllocateArray<!!0>(System.Int32, System.Boolean)
		IL_000B: ret
	}
}

class Std.Std+Test extends System.Object {
	field private static System.UInt64 testCount

	method public static assert <T> (!!0 value, !!0 reference) System.Void {
		IL_0000: ldarg !!0 value
		IL_0004: box !!0
		IL_0009: ldarg !!0 reference
		IL_000D: box !!0
		IL_0012: callvirt System.Boolean System.Object::Equals(System.Object)
		IL_0017: ldarg !!0 value
		IL_001B: box !!0
		IL_0020: callvirt System.String System.Object::ToString()
		IL_0025: call System.Void Std.Std+Test::__assert__impl__(System.Boolean, System.String)
		IL_002A: ret
	}

	method public static assert (System.Byte value, System.Byte reference) System.Void {
		IL_0000: ldarg System.Byte value
		IL_0004: ldarg System.Byte reference
		IL_0008: ceq
		IL_000A: ldarg System.Byte value
		IL_000E: box System.Byte
		IL_0013: callvirt System.String System.Object::ToString()
		IL_0018: call System.Void Std.Std+Test::__assert__impl__(System.Boolean, System.String)
		IL_001D: ret
	}

	method public static assert (System.SByte value, System.SByte reference) System.Void {
		IL_0000: ldarg System.SByte value
		IL_0004: ldarg System.SByte reference
		IL_0008: ceq
		IL_000A: ldarg System.SByte value
		IL_000E: box System.SByte
		IL_0013: callvirt System.String System.Object::ToString()
		IL_0018: call System.Void Std.Std+Test::__assert__impl__(System.Boolean, System.String)
		IL_001D: ret
	}

	method public static assert (System.UInt16 value, System.UInt16 reference) System.Void {
		IL_0000: ldarg System.UInt16 value
		IL_0004: ldarg System.UInt16 reference
		IL_0008: ceq
		IL_000A: ldarg System.UInt16 value
		IL_000E: box System.UInt16
		IL_0013: callvirt System.String System.Object::ToString()
		IL_0018: call System.Void Std.Std+Test::__assert__impl__(System.Boolean, System.String)
		IL_001D: ret
	}

	method public static assert (System.Int16 value, System.Int16 reference) System.Void {
		IL_0000: ldarg System.Int16 value
		IL_0004: ldarg System.Int16 reference
		IL_0008: ceq
		IL_000A: ldarg System.Int16 value
		IL_000E: box System.Int16
		IL_0013: callvirt System.String System.Object::ToString()
		IL_0018: call System.Void Std.Std+Test::__assert__impl__(System.Boolean, System.String)
		IL_001D: ret
	}

	method public static assert (System.UInt32 value, System.UInt32 reference) System.Void {
		IL_0000: ldarg System.UInt32 value
		IL_0004: ldarg System.UInt32 reference
		IL_0008: ceq
		IL_000A: ldarg System.UInt32 value
		IL_000E: box System.UInt32
		IL_0013: callvirt System.String System.Object::ToString()
		IL_0018: call System.Void Std.Std+Test::__assert__impl__(System.Boolean, System.String)
		IL_001D: ret
	}

	method public static assert (System.Int32 value, System.Int32 reference) System.Void {
		IL_0000: ldarg System.Int32 value
		IL_0004: ldarg System.Int32 reference
		IL_0008: ceq
		IL_000A: ldarg System.Int32 value
		IL_000E: box System.Int32
		IL_0013: callvirt System.String System.Object::ToString()
		IL_0018: call System.Void Std.Std+Test::__assert__impl__(System.Boolean, System.String)
		IL_001D: ret
	}

	method public static assert (System.UInt64 value, System.UInt64 reference) System.Void {
		IL_0000: ldarg System.UInt64 value
		IL_0004: ldarg System.UInt64 reference
		IL_0008: ceq
		IL_000A: ldarg System.UInt64 value
		IL_000E: box System.UInt64
		IL_0013: callvirt System.String System.Object::ToString()
		IL_0018: call System.Void Std.Std+Test::__assert__impl__(System.Boolean, System.String)
		IL_001D: ret
	}

	method public static assert (System.Int64 value, System.Int64 reference) System.Void {
		IL_0000: ldarg System.Int64 value
		IL_0004: ldarg System.Int64 reference
		IL_0008: ceq
		IL_000A: ldarg System.Int64 value
		IL_000E: box System.Int64
		IL_0013: callvirt System.String System.Object::ToString()
		IL_0018: call System.Void Std.Std+Test::__assert__impl__(System.Boolean, System.String)
		IL_001D: ret
	}

	method public static assert (System.UInt128 value, System.UInt128 reference) System.Void {
		IL_0000: ldarg System.UInt128 value
		IL_0004: ldarg System.UInt128 reference
		IL_0008: ceq
		IL_000A: ldarg System.UInt128 value
		IL_000E: box System.UInt128
		IL_0013: callvirt System.String System.Object::ToString()
		IL_0018: call System.Void Std.Std+Test::__assert__impl__(System.Boolean, System.String)
		IL_001D: ret
	}

	method public static assert (System.Int128 value, System.Int128 reference) System.Void {
		IL_0000: ldarg System.Int128 value
		IL_0004: ldarg System.Int128 reference
		IL_0008: ceq
		IL_000A: ldarg System.Int128 value
		IL_000E: box System.Int128
		IL_0013: callvirt System.String System.Object::ToString()
		IL_0018: call System.Void Std.Std+Test::__assert__impl__(System.Boolean, System.String)
		IL_001D: ret
	}

	method public static assert (System.Boolean value, System.Boolean reference) System.Void {
		IL_0000: ldarg System.Boolean value
		IL_0004: ldarg System.Boolean reference
		IL_0008: ceq
		IL_000A: ldarg System.Boolean value
		IL_000E: box System.Boolean
		IL_0013: callvirt System.String System.Object::ToString()
		IL_0018: call System.Void Std.Std+Test::__assert__impl__(System.Boolean, System.String)
		IL_001D: ret
	}

	method public static assert (System.Char value, System.Char reference) System.Void {
		IL_0000: ldarg System.Char value
		IL_0004: ldarg System.Char reference
		IL_0008: ceq
		IL_000A: ldarg System.Char value
		IL_000E: box System.Char
		IL_0013: callvirt System.String System.Object::ToString()
		IL_0018: call System.Void Std.Std+Test::__assert__impl__(System.Boolean, System.String)
		IL_001D: ret
	}

	method public static assert (System.String value, System.String reference) System.Void {
		IL_0000: ldarg System.String value
		IL_0004: ldarg System.String reference
		IL_0008: call System.Boolean System.String::Equals(System.String, System.String)
		IL_000D: ldarg System.String value
		IL_0011: box System.String
		IL_0016: callvirt System.String System.Object::ToString()
		IL_001B: call System.Void Std.Std+Test::__assert__impl__(System.Boolean, System.String)
		IL_0020: ret
	}

	method public static assert <T> (!!0[] value, !!0[] reference) System.Void {
		IL_0000: ldarg !!0[] value
		IL_0004: ldarg !!0[] reference
		IL_0008: call System.Boolean Std.Std+Mem::compare(System.Object[], System.Object[])
		IL_000D: ldarg !!0[] value
		IL_0011: call System.String <Module>.RuntimeHelpers::AsString<!!0>(!!0[])
		IL_0016: call System.Void Std.Std+Test::__assert__impl__(System.Boolean, System.String)
		IL_001B: ret
	}

	method private static __assert__impl__ (System.Boolean status, System.String value) System.Void {
		IL_0000: ldsfld System.UInt64 Std.Std+Test::testCount
		IL_0005: ldc.i8 1
		IL_000E: add
		IL_000F: stsfld System.UInt64 Std.Std+Test::testCount
		IL_0014: ldarg System.Boolean status
		IL_0018: brfalse IL_0069
		IL_001D: nop
		IL_001E: ldstr "\t- TEST "
		IL_0023: ldsfld System.UInt64 Std.Std+Test::testCount
		IL_0028: box System.UInt64
		IL_002D: callvirt System.String System.Object::ToString()
		IL_0032: ldstr " "
		IL_0037: ldsfld System.String Std.Std+Console::color_green
		IL_003C: ldstr "[PASSED]"
		IL_0041: ldsfld System.String Std.Std+Console::style_reset
		IL_0046: call System.String System.String::Concat(System.String, System.String)
		IL_004B: call System.String System.String::Concat(System.String, System.String)
		IL_0050: call System.String System.String::Concat(System.String, System.String)
		IL_0055: call System.String System.String::Concat(System.String, System.String)
		IL_005A: call System.String System.String::Concat(System.String, System.String)
		IL_005F: call System.Void Std.Std+Console::writeln(System.String)
		IL_0064: br IL_00C3
		IL_0069: nop
		IL_006A: ldstr "\t- TEST "
		IL_006F: ldsfld System.UInt64 Std.Std+Test::testCount
		IL_0074: box System.UInt64
		IL_0079: callvirt System.String System.Object::ToString()
		IL_007E: ldstr " "
		IL_0083: ldsfld System.String Std.Std+Console::color_red
		IL_0088: ldstr "[FAILED] ("
		IL_008D: ldarg System.String value
		IL_0091: ldstr ")"
		IL_0096: ldsfld System.String Std.Std+Console::style_reset
		IL_009B: call System.String System.String::Concat(System.String, System.String)
		IL_00A0: call System.String System.String::Concat(System.String, System.String)
		IL_00A5: call System.String System.String::Concat(System.String, System.String)
		IL_00AA: call System.String System.String::Concat(System.String, System.String)
		IL_00AF: call System.String System.String::Concat(System.String, System.String)
		IL_00B4: call System.String System.String::Concat(System.String, System.String)
		IL_00B9: call System.String System.String::Concat(System.String, System.String)
		IL_00BE: call System.Void Std.Std+Console::writeln(System.String)
		IL_00C3: nop
		IL_00C4: ret
	}

	method private static .cctor () System.Void {
		IL_0000: ldc.i8 0
		IL_0000: stsfld System.UInt64 Std.Std+Test::testCount
		IL_0000: ret
	}
}

class Std.Std+Console extends System.Object {
	field private static System.String style_reset
	field private static System.String color_red
	field private static System.String color_green

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

	method private static write (System.Object[] value) System.Void {
		IL_0000: ldarg System.Object[] value
		IL_0004: call System.String <Module>.RuntimeHelpers::AsString<System.Object>(!!0[])
		IL_0009: call System.Void System.Console::Write(System.String)
		IL_000E: ret
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

	method private static writeln (System.Object[] value) System.Void {
		IL_0000: ldarg System.Object[] value
		IL_0004: call System.String <Module>.RuntimeHelpers::AsString<System.Object>(!!0[])
		IL_0009: call System.Void System.Console::WriteLine(System.String)
		IL_000E: ret
	}

	method private static writeln (System.String value) System.Void {
		IL_0000: ldarg System.String value
		IL_0004: call System.Void System.Console::WriteLine(System.String)
		IL_0009: ret
	}

	method private static .cctor () System.Void {
		IL_0000: ldstr "[0m"
		IL_0000: stsfld System.String Std.Std+Console::style_reset
		IL_0000: ldstr "[31m"
		IL_0000: stsfld System.String Std.Std+Console::color_red
		IL_0000: ldstr "[32m"
		IL_0000: stsfld System.String Std.Std+Console::color_green
		IL_0000: ret
	}
}

class Std.Std+Fs extends System.Object {

	method public static open (System.String path) Std.Std+Fs+File {
		.locals init (System.IO.FileStream V_0)
		.locals init (Std.Std+Fs+File V_1)
		IL_0000: ldarg System.String path
		IL_0004: ldc.i4.4
		IL_0005: ldc.i4.1
		IL_0006: newobj System.Void System.IO.FileStream::.ctor(System.String, System.IO.FileMode, System.IO.FileAccess)
		IL_000B: stloc V_0
		IL_000F: ldloca V_1
		IL_0013: ldloc V_0
		IL_0017: call System.Void Std.Std+Fs+File::.ctor(System.IO.FileStream)
		IL_001C: ldloc V_1
		IL_0020: ret
	}

	method public static fileExists (System.String path) System.Boolean {
		IL_0000: ldarg System.String path
		IL_0004: call System.Boolean System.IO.File::Exists(System.String)
		IL_0009: ret
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

struct Std.Std+Io+Reader extends System.ValueType {

	method private instance .ctor () System.Void {
		IL_0000: ret
	}
}

struct Std.Std+Io+Writer extends System.ValueType {

	method private instance .ctor () System.Void {
		IL_0000: ret
	}
}

struct Std.Std+Mem+Alignment extends System.Enum {
	field public instance System.Int64 value__
	field public static Std.Std+Mem+Alignment _1
	field public static Std.Std+Mem+Alignment _2
	field public static Std.Std+Mem+Alignment _4
	field public static Std.Std+Mem+Alignment _8
	field public static Std.Std+Mem+Alignment _16
	field public static Std.Std+Mem+Alignment _32
}

struct Std.Std+Fs+File extends System.ValueType {
	field private instance System.IO.FileStream _filestream

	method private instance .ctor (System.IO.FileStream stream) System.Void {
		IL_0000: ldarg.0
		IL_0001: ldarg System.IO.FileStream stream
		IL_0005: stfld System.IO.FileStream Std.Std+Fs+File::_filestream
		IL_000A: ret
	}

	method private instance close () System.Void {
		IL_0000: ldarg.0
		IL_0001: ldflda System.IO.FileStream Std.Std+Fs+File::_filestream
		IL_0006: ldobj System.IO.FileStream
		IL_000B: callvirt System.Void System.IO.Stream::Close()
		IL_0010: ret
	}
}

struct Std.Std+Fs+FileReader extends System.ValueType {
	field public instance Std.Std+Io+Reader base

	method private instance .ctor () System.Void {
		IL_0000: ret
	}
}

struct Std.Std+Fs+FileWriter extends System.ValueType {
	field public instance Std.Std+Io+Writer base

	method private instance .ctor () System.Void {
		IL_0000: ret
	}
}

struct Std.Std+Fs+FileInfo extends System.ValueType {
	field private instance System.IO.FileInfo _fileInfo
	field public instance System.String name
	field public instance System.String directory
	field public instance System.UInt64 length

	method private instance .ctor (System.IO.FileInfo fInfo) System.Void {
		IL_0000: ldarg.0
		IL_0001: ldarg System.IO.FileInfo fInfo
		IL_0005: stfld System.IO.FileInfo Std.Std+Fs+FileInfo::_fileInfo
		IL_000A: ldarg.0
		IL_000B: ldarg System.IO.FileInfo fInfo
		IL_000F: callvirt System.String System.IO.FileInfo::get_Name()
		IL_0014: stfld System.String Std.Std+Fs+FileInfo::name
		IL_0019: ldarg.0
		IL_001A: ldarg System.IO.FileInfo fInfo
		IL_001E: call System.String System.IO.FileInfo::get_DirectoryName()
		IL_0023: stfld System.String Std.Std+Fs+FileInfo::directory
		IL_0028: ldarg.0
		IL_0029: ldarg System.IO.FileInfo fInfo
		IL_002D: call System.Int64 System.IO.FileInfo::get_Length()
		IL_0032: stfld System.UInt64 Std.Std+Fs+FileInfo::length
		IL_0037: ret
	}
}

struct Std.Std+Fs+SeekOrigin extends System.Enum {
	field public instance System.Int32 value__
	field public static Std.Std+Fs+SeekOrigin begin
	field public static Std.Std+Fs+SeekOrigin current
	field public static Std.Std+Fs+SeekOrigin end
}

struct Std.Std+Fs+FileMode extends System.Enum {
	field public instance System.Int32 value__
	field public static Std.Std+Fs+FileMode createNew
	field public static Std.Std+Fs+FileMode create
	field public static Std.Std+Fs+FileMode open
	field public static Std.Std+Fs+FileMode openOrCreate
	field public static Std.Std+Fs+FileMode truncate
	field public static Std.Std+Fs+FileMode append
}

struct Std.Std+Fs+FileAccess extends System.Enum {
	field public instance System.Int32 value__
	field public static Std.Std+Fs+FileAccess RO
	field public static Std.Std+Fs+FileAccess WO
	field public static Std.Std+Fs+FileAccess RW
}
