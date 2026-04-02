
class <Module> extends  {
}

class Bootstrap.Static extends System.Object {

	method public static main () System.Void {
		.locals init (System.Byte[] V_0)
		.locals init (System.Byte[] V_1)
		.locals init (System.Text.StringBuilder V_2)
		.locals init (System.Int32 V_3)
		.locals init (System.Int32 V_4)
		IL_0000: ldc.i4.5
		IL_0001: newarr System.Byte
		IL_0006: dup
		IL_0007: ldc.i4.0
		IL_0008: ldc.i4.s 10
		IL_000A: stelem System.Byte
		IL_000F: dup
		IL_0010: ldc.i4.1
		IL_0011: ldc.i4.s 20
		IL_0013: stelem System.Byte
		IL_0018: dup
		IL_0019: ldc.i4.2
		IL_001A: ldc.i4.s 30
		IL_001C: stelem System.Byte
		IL_0021: dup
		IL_0022: ldc.i4.3
		IL_0023: ldc.i4.s 40
		IL_0025: stelem System.Byte
		IL_002A: dup
		IL_002B: ldc.i4.4
		IL_002C: ldc.i4.s 50
		IL_002E: stelem System.Byte
		IL_0033: stloc V_0
		IL_0037: ldc.i4.5
		IL_0038: newarr System.Byte
		IL_003D: dup
		IL_003E: ldc.i4.0
		IL_003F: ldc.i4.0
		IL_0040: stelem System.Byte
		IL_0045: dup
		IL_0046: ldc.i4.1
		IL_0047: ldc.i4.0
		IL_0048: stelem System.Byte
		IL_004D: dup
		IL_004E: ldc.i4.2
		IL_004F: ldc.i4.0
		IL_0050: stelem System.Byte
		IL_0055: dup
		IL_0056: ldc.i4.3
		IL_0057: ldc.i4.0
		IL_0058: stelem System.Byte
		IL_005D: dup
		IL_005E: ldc.i4.4
		IL_005F: ldc.i4.0
		IL_0060: stelem System.Byte
		IL_0065: stloc V_1
		IL_0069: ldstr "Before:"
		IL_006E: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_0073: ldstr "arr0 "
		IL_0078: newobj System.Void System.Text.StringBuilder::.ctor()
		IL_007D: dup
		IL_007E: stloc V_2
		IL_0082: ldc.i4.s 91
		IL_0084: call System.Text.StringBuilder System.Text.StringBuilder::Append(System.Char)
		IL_0089: pop
		IL_008A: ldc.i4.0
		IL_008B: stloc V_3
		IL_008F: ldloc V_0
		IL_0093: ldlen
		IL_0094: dup
		IL_0095: stloc V_4
		IL_0099: brfalse IL_00F6
		IL_009E: br IL_00D8
		IL_00A3: ldloc V_2
		IL_00A7: ldloc V_0
		IL_00AB: ldloc V_3
		IL_00AF: ldelem System.Byte
		IL_00B4: box System.Byte
		IL_00B9: callvirt System.String System.Object::ToString()
		IL_00BE: call System.Text.StringBuilder System.Text.StringBuilder::Append(System.String)
		IL_00C3: ldstr ", "
		IL_00C8: call System.Text.StringBuilder System.Text.StringBuilder::Append(System.String)
		IL_00CD: pop
		IL_00CE: ldloc V_3
		IL_00D2: ldc.i4.1
		IL_00D3: add
		IL_00D4: stloc V_3
		IL_00D8: ldloc V_3
		IL_00DC: ldloc V_4
		IL_00E0: blt IL_00A3
		IL_00E5: ldloc V_2
		IL_00E9: dup
		IL_00EA: call System.Int32 System.Text.StringBuilder::get_Length()
		IL_00EF: ldc.i4.2
		IL_00F0: sub
		IL_00F1: call System.Void System.Text.StringBuilder::set_Length(System.Int32)
		IL_00F6: ldloc V_2
		IL_00FA: ldc.i4.s 93
		IL_00FC: call System.Text.StringBuilder System.Text.StringBuilder::Append(System.Char)
		IL_0101: call System.String System.Text.StringBuilder::ToString()
		IL_0106: ldstr " (len "
		IL_010B: ldloc V_0
		IL_010F: ldlen
		IL_0110: conv.u8
		IL_0111: box System.UInt64
		IL_0116: callvirt System.String System.Object::ToString()
		IL_011B: ldstr ")"
		IL_0120: call System.String System.String::Concat(System.String, System.String)
		IL_0125: call System.String System.String::Concat(System.String, System.String)
		IL_012A: call System.String System.String::Concat(System.String, System.String)
		IL_012F: call System.String System.String::Concat(System.String, System.String)
		IL_0134: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_0139: ldstr "arr1 "
		IL_013E: newobj System.Void System.Text.StringBuilder::.ctor()
		IL_0143: dup
		IL_0144: stloc V_2
		IL_0148: ldc.i4.s 91
		IL_014A: call System.Text.StringBuilder System.Text.StringBuilder::Append(System.Char)
		IL_014F: pop
		IL_0150: ldc.i4.0
		IL_0151: stloc V_4
		IL_0155: ldloc V_1
		IL_0159: ldlen
		IL_015A: dup
		IL_015B: stloc V_3
		IL_015F: brfalse IL_01BC
		IL_0164: br IL_019E
		IL_0169: ldloc V_2
		IL_016D: ldloc V_1
		IL_0171: ldloc V_4
		IL_0175: ldelem System.Byte
		IL_017A: box System.Byte
		IL_017F: callvirt System.String System.Object::ToString()
		IL_0184: call System.Text.StringBuilder System.Text.StringBuilder::Append(System.String)
		IL_0189: ldstr ", "
		IL_018E: call System.Text.StringBuilder System.Text.StringBuilder::Append(System.String)
		IL_0193: pop
		IL_0194: ldloc V_4
		IL_0198: ldc.i4.1
		IL_0199: add
		IL_019A: stloc V_4
		IL_019E: ldloc V_4
		IL_01A2: ldloc V_3
		IL_01A6: blt IL_0169
		IL_01AB: ldloc V_2
		IL_01AF: dup
		IL_01B0: call System.Int32 System.Text.StringBuilder::get_Length()
		IL_01B5: ldc.i4.2
		IL_01B6: sub
		IL_01B7: call System.Void System.Text.StringBuilder::set_Length(System.Int32)
		IL_01BC: ldloc V_2
		IL_01C0: ldc.i4.s 93
		IL_01C2: call System.Text.StringBuilder System.Text.StringBuilder::Append(System.Char)
		IL_01C7: call System.String System.Text.StringBuilder::ToString()
		IL_01CC: ldstr " (len "
		IL_01D1: ldloc V_1
		IL_01D5: ldlen
		IL_01D6: conv.u8
		IL_01D7: box System.UInt64
		IL_01DC: callvirt System.String System.Object::ToString()
		IL_01E1: ldstr ")"
		IL_01E6: call System.String System.String::Concat(System.String, System.String)
		IL_01EB: call System.String System.String::Concat(System.String, System.String)
		IL_01F0: call System.String System.String::Concat(System.String, System.String)
		IL_01F5: call System.String System.String::Concat(System.String, System.String)
		IL_01FA: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_01FF: ldloc V_0
		IL_0203: ldloc V_1
		IL_0207: call System.Void Std.MemStatic::copy(System.Byte[], System.Byte[])
		IL_020C: ldstr "After:"
		IL_0211: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_0216: ldstr "arr0 "
		IL_021B: newobj System.Void System.Text.StringBuilder::.ctor()
		IL_0220: dup
		IL_0221: stloc V_2
		IL_0225: ldc.i4.s 91
		IL_0227: call System.Text.StringBuilder System.Text.StringBuilder::Append(System.Char)
		IL_022C: pop
		IL_022D: ldc.i4.0
		IL_022E: stloc V_3
		IL_0232: ldloc V_0
		IL_0236: ldlen
		IL_0237: dup
		IL_0238: stloc V_4
		IL_023C: brfalse IL_0299
		IL_0241: br IL_027B
		IL_0246: ldloc V_2
		IL_024A: ldloc V_0
		IL_024E: ldloc V_3
		IL_0252: ldelem System.Byte
		IL_0257: box System.Byte
		IL_025C: callvirt System.String System.Object::ToString()
		IL_0261: call System.Text.StringBuilder System.Text.StringBuilder::Append(System.String)
		IL_0266: ldstr ", "
		IL_026B: call System.Text.StringBuilder System.Text.StringBuilder::Append(System.String)
		IL_0270: pop
		IL_0271: ldloc V_3
		IL_0275: ldc.i4.1
		IL_0276: add
		IL_0277: stloc V_3
		IL_027B: ldloc V_3
		IL_027F: ldloc V_4
		IL_0283: blt IL_0246
		IL_0288: ldloc V_2
		IL_028C: dup
		IL_028D: call System.Int32 System.Text.StringBuilder::get_Length()
		IL_0292: ldc.i4.2
		IL_0293: sub
		IL_0294: call System.Void System.Text.StringBuilder::set_Length(System.Int32)
		IL_0299: ldloc V_2
		IL_029D: ldc.i4.s 93
		IL_029F: call System.Text.StringBuilder System.Text.StringBuilder::Append(System.Char)
		IL_02A4: call System.String System.Text.StringBuilder::ToString()
		IL_02A9: ldstr " (len "
		IL_02AE: ldloc V_0
		IL_02B2: ldlen
		IL_02B3: conv.u8
		IL_02B4: box System.UInt64
		IL_02B9: callvirt System.String System.Object::ToString()
		IL_02BE: ldstr ")"
		IL_02C3: call System.String System.String::Concat(System.String, System.String)
		IL_02C8: call System.String System.String::Concat(System.String, System.String)
		IL_02CD: call System.String System.String::Concat(System.String, System.String)
		IL_02D2: call System.String System.String::Concat(System.String, System.String)
		IL_02D7: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_02DC: ldstr "arr1 "
		IL_02E1: newobj System.Void System.Text.StringBuilder::.ctor()
		IL_02E6: dup
		IL_02E7: stloc V_2
		IL_02EB: ldc.i4.s 91
		IL_02ED: call System.Text.StringBuilder System.Text.StringBuilder::Append(System.Char)
		IL_02F2: pop
		IL_02F3: ldc.i4.0
		IL_02F4: stloc V_4
		IL_02F8: ldloc V_1
		IL_02FC: ldlen
		IL_02FD: dup
		IL_02FE: stloc V_3
		IL_0302: brfalse IL_035F
		IL_0307: br IL_0341
		IL_030C: ldloc V_2
		IL_0310: ldloc V_1
		IL_0314: ldloc V_4
		IL_0318: ldelem System.Byte
		IL_031D: box System.Byte
		IL_0322: callvirt System.String System.Object::ToString()
		IL_0327: call System.Text.StringBuilder System.Text.StringBuilder::Append(System.String)
		IL_032C: ldstr ", "
		IL_0331: call System.Text.StringBuilder System.Text.StringBuilder::Append(System.String)
		IL_0336: pop
		IL_0337: ldloc V_4
		IL_033B: ldc.i4.1
		IL_033C: add
		IL_033D: stloc V_4
		IL_0341: ldloc V_4
		IL_0345: ldloc V_3
		IL_0349: blt IL_030C
		IL_034E: ldloc V_2
		IL_0352: dup
		IL_0353: call System.Int32 System.Text.StringBuilder::get_Length()
		IL_0358: ldc.i4.2
		IL_0359: sub
		IL_035A: call System.Void System.Text.StringBuilder::set_Length(System.Int32)
		IL_035F: ldloc V_2
		IL_0363: ldc.i4.s 93
		IL_0365: call System.Text.StringBuilder System.Text.StringBuilder::Append(System.Char)
		IL_036A: call System.String System.Text.StringBuilder::ToString()
		IL_036F: ldstr " (len "
		IL_0374: ldloc V_1
		IL_0378: ldlen
		IL_0379: conv.u8
		IL_037A: box System.UInt64
		IL_037F: callvirt System.String System.Object::ToString()
		IL_0384: ldstr ")"
		IL_0389: call System.String System.String::Concat(System.String, System.String)
		IL_038E: call System.String System.String::Concat(System.String, System.String)
		IL_0393: call System.String System.String::Concat(System.String, System.String)
		IL_0398: call System.String System.String::Concat(System.String, System.String)
		IL_039D: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_03A2: ldloc V_0
		IL_03A6: ldloc V_1
		IL_03AA: call System.Boolean Std.MemStatic::compare(System.Byte[], System.Byte[])
		IL_03AF: brfalse IL_03C4
		IL_03B4: nop
		IL_03B5: ldstr "Arrays are equal!"
		IL_03BA: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_03BF: br IL_03CF
		IL_03C4: nop
		IL_03C5: ldstr "Error! arrays are not equal!"
		IL_03CA: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_03CF: nop
		IL_03D0: ret
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

	method public static copy (System.Byte[] source, System.Byte[] target) System.Void {
		.locals init (System.Int64 V_0)
		IL_0000: ldc.i8 0
		IL_0009: stloc V_0
		IL_000D: br IL_0040
		IL_0012: ldarg System.Byte[] target
		IL_0016: ldloc V_0
		IL_001A: conv.ovf.i4
		IL_001B: ldarg System.Byte[] source
		IL_001F: ldloc V_0
		IL_0023: conv.i4
		IL_0024: ldelem System.Byte
		IL_0029: stelem System.Byte
		IL_002E: ldloc V_0
		IL_0032: ldc.i8 1
		IL_003B: add
		IL_003C: stloc V_0
		IL_0040: ldloc V_0
		IL_0044: ldarg System.Byte[] source
		IL_0048: ldlen
		IL_0049: conv.u8
		IL_004A: clt
		IL_004C: brtrue IL_0012
		IL_0051: nop
		IL_0052: ret
	}

	method public static copy (System.SByte[] source, System.SByte[] target) System.Void {
		.locals init (System.Int64 V_0)
		IL_0000: ldc.i8 0
		IL_0009: stloc V_0
		IL_000D: br IL_0040
		IL_0012: ldarg System.SByte[] target
		IL_0016: ldloc V_0
		IL_001A: conv.ovf.i4
		IL_001B: ldarg System.SByte[] source
		IL_001F: ldloc V_0
		IL_0023: conv.i4
		IL_0024: ldelem System.SByte
		IL_0029: stelem System.SByte
		IL_002E: ldloc V_0
		IL_0032: ldc.i8 1
		IL_003B: add
		IL_003C: stloc V_0
		IL_0040: ldloc V_0
		IL_0044: ldarg System.SByte[] source
		IL_0048: ldlen
		IL_0049: conv.u8
		IL_004A: clt
		IL_004C: brtrue IL_0012
		IL_0051: nop
		IL_0052: ret
	}

	method public static copy (System.UInt16[] source, System.UInt16[] target) System.Void {
		.locals init (System.Int64 V_0)
		IL_0000: ldc.i8 0
		IL_0009: stloc V_0
		IL_000D: br IL_0040
		IL_0012: ldarg System.UInt16[] target
		IL_0016: ldloc V_0
		IL_001A: conv.ovf.i4
		IL_001B: ldarg System.UInt16[] source
		IL_001F: ldloc V_0
		IL_0023: conv.i4
		IL_0024: ldelem System.UInt16
		IL_0029: stelem System.UInt16
		IL_002E: ldloc V_0
		IL_0032: ldc.i8 1
		IL_003B: add
		IL_003C: stloc V_0
		IL_0040: ldloc V_0
		IL_0044: ldarg System.UInt16[] source
		IL_0048: ldlen
		IL_0049: conv.u8
		IL_004A: clt
		IL_004C: brtrue IL_0012
		IL_0051: nop
		IL_0052: ret
	}

	method public static copy (System.Int16[] source, System.Int16[] target) System.Void {
		.locals init (System.Int64 V_0)
		IL_0000: ldc.i8 0
		IL_0009: stloc V_0
		IL_000D: br IL_0040
		IL_0012: ldarg System.Int16[] target
		IL_0016: ldloc V_0
		IL_001A: conv.ovf.i4
		IL_001B: ldarg System.Int16[] source
		IL_001F: ldloc V_0
		IL_0023: conv.i4
		IL_0024: ldelem System.Int16
		IL_0029: stelem System.Int16
		IL_002E: ldloc V_0
		IL_0032: ldc.i8 1
		IL_003B: add
		IL_003C: stloc V_0
		IL_0040: ldloc V_0
		IL_0044: ldarg System.Int16[] source
		IL_0048: ldlen
		IL_0049: conv.u8
		IL_004A: clt
		IL_004C: brtrue IL_0012
		IL_0051: nop
		IL_0052: ret
	}

	method public static copy (System.UInt32[] source, System.UInt32[] target) System.Void {
		.locals init (System.Int64 V_0)
		IL_0000: ldc.i8 0
		IL_0009: stloc V_0
		IL_000D: br IL_0040
		IL_0012: ldarg System.UInt32[] target
		IL_0016: ldloc V_0
		IL_001A: conv.ovf.i4
		IL_001B: ldarg System.UInt32[] source
		IL_001F: ldloc V_0
		IL_0023: conv.i4
		IL_0024: ldelem System.UInt32
		IL_0029: stelem System.UInt32
		IL_002E: ldloc V_0
		IL_0032: ldc.i8 1
		IL_003B: add
		IL_003C: stloc V_0
		IL_0040: ldloc V_0
		IL_0044: ldarg System.UInt32[] source
		IL_0048: ldlen
		IL_0049: conv.u8
		IL_004A: clt
		IL_004C: brtrue IL_0012
		IL_0051: nop
		IL_0052: ret
	}

	method public static copy (System.Int32[] source, System.Int32[] target) System.Void {
		.locals init (System.Int64 V_0)
		IL_0000: ldc.i8 0
		IL_0009: stloc V_0
		IL_000D: br IL_0040
		IL_0012: ldarg System.Int32[] target
		IL_0016: ldloc V_0
		IL_001A: conv.ovf.i4
		IL_001B: ldarg System.Int32[] source
		IL_001F: ldloc V_0
		IL_0023: conv.i4
		IL_0024: ldelem System.Int32
		IL_0029: stelem System.Int32
		IL_002E: ldloc V_0
		IL_0032: ldc.i8 1
		IL_003B: add
		IL_003C: stloc V_0
		IL_0040: ldloc V_0
		IL_0044: ldarg System.Int32[] source
		IL_0048: ldlen
		IL_0049: conv.u8
		IL_004A: clt
		IL_004C: brtrue IL_0012
		IL_0051: nop
		IL_0052: ret
	}

	method public static copy (System.UInt64[] source, System.UInt64[] target) System.Void {
		.locals init (System.Int64 V_0)
		IL_0000: ldc.i8 0
		IL_0009: stloc V_0
		IL_000D: br IL_0040
		IL_0012: ldarg System.UInt64[] target
		IL_0016: ldloc V_0
		IL_001A: conv.ovf.i4
		IL_001B: ldarg System.UInt64[] source
		IL_001F: ldloc V_0
		IL_0023: conv.i4
		IL_0024: ldelem System.UInt64
		IL_0029: stelem System.UInt64
		IL_002E: ldloc V_0
		IL_0032: ldc.i8 1
		IL_003B: add
		IL_003C: stloc V_0
		IL_0040: ldloc V_0
		IL_0044: ldarg System.UInt64[] source
		IL_0048: ldlen
		IL_0049: conv.u8
		IL_004A: clt
		IL_004C: brtrue IL_0012
		IL_0051: nop
		IL_0052: ret
	}

	method public static copy (System.Int64[] source, System.Int64[] target) System.Void {
		.locals init (System.Int64 V_0)
		IL_0000: ldc.i8 0
		IL_0009: stloc V_0
		IL_000D: br IL_0040
		IL_0012: ldarg System.Int64[] target
		IL_0016: ldloc V_0
		IL_001A: conv.ovf.i4
		IL_001B: ldarg System.Int64[] source
		IL_001F: ldloc V_0
		IL_0023: conv.i4
		IL_0024: ldelem System.Int64
		IL_0029: stelem System.Int64
		IL_002E: ldloc V_0
		IL_0032: ldc.i8 1
		IL_003B: add
		IL_003C: stloc V_0
		IL_0040: ldloc V_0
		IL_0044: ldarg System.Int64[] source
		IL_0048: ldlen
		IL_0049: conv.u8
		IL_004A: clt
		IL_004C: brtrue IL_0012
		IL_0051: nop
		IL_0052: ret
	}

	method public static copy (System.UInt128[] source, System.UInt128[] target) System.Void {
		.locals init (System.Int64 V_0)
		IL_0000: ldc.i8 0
		IL_0009: stloc V_0
		IL_000D: br IL_0040
		IL_0012: ldarg System.UInt128[] target
		IL_0016: ldloc V_0
		IL_001A: conv.ovf.i4
		IL_001B: ldarg System.UInt128[] source
		IL_001F: ldloc V_0
		IL_0023: conv.i4
		IL_0024: ldelem System.UInt128
		IL_0029: stelem System.UInt128
		IL_002E: ldloc V_0
		IL_0032: ldc.i8 1
		IL_003B: add
		IL_003C: stloc V_0
		IL_0040: ldloc V_0
		IL_0044: ldarg System.UInt128[] source
		IL_0048: ldlen
		IL_0049: conv.u8
		IL_004A: clt
		IL_004C: brtrue IL_0012
		IL_0051: nop
		IL_0052: ret
	}

	method public static copy (System.Boolean[] source, System.Boolean[] target) System.Void {
		.locals init (System.Int64 V_0)
		IL_0000: ldc.i8 0
		IL_0009: stloc V_0
		IL_000D: br IL_0040
		IL_0012: ldarg System.Boolean[] target
		IL_0016: ldloc V_0
		IL_001A: conv.ovf.i4
		IL_001B: ldarg System.Boolean[] source
		IL_001F: ldloc V_0
		IL_0023: conv.i4
		IL_0024: ldelem System.Boolean
		IL_0029: stelem System.Boolean
		IL_002E: ldloc V_0
		IL_0032: ldc.i8 1
		IL_003B: add
		IL_003C: stloc V_0
		IL_0040: ldloc V_0
		IL_0044: ldarg System.Boolean[] source
		IL_0048: ldlen
		IL_0049: conv.u8
		IL_004A: clt
		IL_004C: brtrue IL_0012
		IL_0051: nop
		IL_0052: ret
	}

	method public static copy (System.Object[] source, System.Object[] target) System.Void {
		.locals init (System.Int64 V_0)
		IL_0000: ldc.i8 0
		IL_0009: stloc V_0
		IL_000D: br IL_0040
		IL_0012: ldarg System.Object[] target
		IL_0016: ldloc V_0
		IL_001A: conv.ovf.i4
		IL_001B: ldarg System.Object[] source
		IL_001F: ldloc V_0
		IL_0023: conv.i4
		IL_0024: ldelem System.Object
		IL_0029: stelem System.Object
		IL_002E: ldloc V_0
		IL_0032: ldc.i8 1
		IL_003B: add
		IL_003C: stloc V_0
		IL_0040: ldloc V_0
		IL_0044: ldarg System.Object[] source
		IL_0048: ldlen
		IL_0049: conv.u8
		IL_004A: clt
		IL_004C: brtrue IL_0012
		IL_0051: nop
		IL_0052: ret
	}

	method public static compare (System.Byte[] a, System.Byte[] b) System.Boolean {
		.locals init (System.Int64 V_0)
		IL_0000: ldarg System.Byte[] a
		IL_0004: ldlen
		IL_0005: conv.u8
		IL_0006: ldarg System.Byte[] b
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
		IL_002C: br IL_0072
		IL_0031: ldarg System.Byte[] a
		IL_0035: ldloc V_0
		IL_0039: conv.i4
		IL_003A: ldelem System.Byte
		IL_003F: ldarg System.Byte[] b
		IL_0043: ldloc V_0
		IL_0047: conv.i4
		IL_0048: ldelem System.Byte
		IL_004D: ceq
		IL_004F: ldc.i4.0
		IL_0050: ceq
		IL_0052: brfalse IL_005F
		IL_0057: nop
		IL_0058: ldc.i4.0
		IL_0059: ret
		IL_005A: br IL_005F
		IL_005F: nop
		IL_0060: ldloc V_0
		IL_0064: ldc.i8 1
		IL_006D: add
		IL_006E: stloc V_0
		IL_0072: ldloc V_0
		IL_0076: ldarg System.Byte[] a
		IL_007A: ldlen
		IL_007B: conv.u8
		IL_007C: clt
		IL_007E: brtrue IL_0031
		IL_0083: nop
		IL_0084: ldc.i4.1
		IL_0085: ret
	}

	method public static compare (System.Object[] a, System.Object[] b) System.Boolean {
		.locals init (System.Int64 V_0)
		IL_0000: ldarg System.Object[] a
		IL_0004: ldlen
		IL_0005: conv.u8
		IL_0006: ldarg System.Object[] b
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
		IL_002C: br IL_0077
		IL_0031: ldarg System.Object[] a
		IL_0035: ldloc V_0
		IL_0039: conv.i4
		IL_003A: ldelem System.Object
		IL_003F: ldarg System.Object[] b
		IL_0043: ldloc V_0
		IL_0047: conv.i4
		IL_0048: ldelem System.Object
		IL_004D: box System.Object
		IL_0052: callvirt System.Boolean System.Object::Equals(System.String)
		IL_0057: brfalse IL_0064
		IL_005C: nop
		IL_005D: ldc.i4.0
		IL_005E: ret
		IL_005F: br IL_0064
		IL_0064: nop
		IL_0065: ldloc V_0
		IL_0069: ldc.i8 1
		IL_0072: add
		IL_0073: stloc V_0
		IL_0077: ldloc V_0
		IL_007B: ldarg System.Object[] a
		IL_007F: ldlen
		IL_0080: conv.u8
		IL_0081: clt
		IL_0083: brtrue IL_0031
		IL_0088: nop
		IL_0089: ldc.i4.1
		IL_008A: ret
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
		IL_000B: callvirt System.Void System.IO.Stream::Close()
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
		IL_000F: callvirt System.String System.IO.FileInfo::get_Name()
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
