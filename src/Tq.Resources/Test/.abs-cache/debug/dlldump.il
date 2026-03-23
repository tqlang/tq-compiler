
class <Module> extends  {
}

class Program.Static extends System.Object {
	field private static System.Int64 src
	field private static System.Int64 debug
	field private static System.Byte[] sym
	field private static System.Byte[] le
	field private static System.Byte[] data
	field private static System.Byte[] sp
	field private static System.UInt64 lePtr
	field private static System.UInt64 e
	field private static System.UInt64 p
	field private static System.UInt64 lp
	field private static System.UInt64 tk
	field private static System.String[] id
	field private static System.UInt64 line
	field private static System.String pval

	method private static main () System.Void {
		.locals init (System.String[] V_0)
		.locals init (System.Int64 V_1)
		.locals init (System.Int64 V_2)
		.locals init (Std.Fs.File V_3)
		.locals init (System.UInt64 V_4)
		.locals init (System.UInt64 V_5)
		.locals init (System.UInt64 V_6)
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
		IL_0034: brfalse IL_00A1
		IL_0039: nop
		IL_003A: ldloc V_0
		IL_003E: ldloc V_2
		IL_0042: conv.i4
		IL_0043: ldelem System.String
		IL_0048: ldc.i8 0
		IL_0051: conv.i4
		IL_0052: call System.Char System.String::get_Chars(System.Int32)
		IL_0057: ldc.i4.s 45
		IL_0059: ceq
		IL_005B: brfalse IL_00A1
		IL_0060: nop
		IL_0061: ldloc V_0
		IL_0065: ldloc V_2
		IL_0069: conv.i4
		IL_006A: ldelem System.String
		IL_006F: ldstr "-s"
		IL_0074: call System.Boolean System.String::Equals(System.String, System.String)
		IL_0079: brfalse IL_00A1
		IL_007E: br IL_0083
		IL_0083: nop
		IL_0084: ldloc V_2
		IL_0088: dup
		IL_0089: ldc.i8 1
		IL_0092: add
		IL_0093: stloc V_2
		IL_0097: stsfld System.Int64 Program.Static::src
		IL_009C: br IL_00A1
		IL_00A1: nop
		IL_00A2: ldloc V_1
		IL_00A6: ldloc V_2
		IL_00AA: sub
		IL_00AB: ldc.i8 0
		IL_00B4: cgt
		IL_00B6: brfalse IL_0123
		IL_00BB: nop
		IL_00BC: ldloc V_0
		IL_00C0: ldloc V_2
		IL_00C4: conv.i4
		IL_00C5: ldelem System.String
		IL_00CA: ldc.i8 0
		IL_00D3: conv.i4
		IL_00D4: call System.Char System.String::get_Chars(System.Int32)
		IL_00D9: ldc.i4.s 45
		IL_00DB: ceq
		IL_00DD: brfalse IL_0123
		IL_00E2: nop
		IL_00E3: ldloc V_0
		IL_00E7: ldloc V_2
		IL_00EB: conv.i4
		IL_00EC: ldelem System.String
		IL_00F1: ldstr "-d"
		IL_00F6: call System.Boolean System.String::Equals(System.String, System.String)
		IL_00FB: brfalse IL_0123
		IL_0100: br IL_0105
		IL_0105: nop
		IL_0106: ldloc V_2
		IL_010A: dup
		IL_010B: ldc.i8 1
		IL_0114: add
		IL_0115: stloc V_2
		IL_0119: stsfld System.Int64 Program.Static::debug
		IL_011E: br IL_0123
		IL_0123: nop
		IL_0124: ldloc V_1
		IL_0128: ldloc V_2
		IL_012C: sub
		IL_012D: ldc.i8 1
		IL_0136: clt
		IL_0138: brfalse IL_015C
		IL_013D: nop
		IL_013E: ldstr "usage: c4 [-s] [-d] file ..."
		IL_0143: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_0148: ldc.i8 1
		IL_0151: conv.ovf.u8.un
		IL_0152: call System.Void Std.ProcessStatic::exit(System.UInt64)
		IL_0157: br IL_015C
		IL_015C: nop
		IL_015D: ldloc V_0
		IL_0161: ldloc V_2
		IL_0165: conv.i4
		IL_0166: ldelem System.String
		IL_016B: call Std.Fs.File Std.FsStatic::open(System.String)
		IL_0170: stloc V_3
		IL_0174: ldc.i8 262144
		IL_017D: stloc V_4
		IL_0181: ldloc V_4
		IL_0185: conv.ovf.u8.un
		IL_0186: ldc.i8 0
		IL_018F: call !!0[] Std.MemStatic::alloc<System.Byte>(System.UInt64, Std.Mem.Alignment)
		IL_0194: stsfld System.Byte[] Program.Static::sym
		IL_0199: ldloc V_4
		IL_019D: conv.ovf.u8.un
		IL_019E: ldc.i8 0
		IL_01A7: call !!0[] Std.MemStatic::alloc<System.Byte>(System.UInt64, Std.Mem.Alignment)
		IL_01AC: stsfld System.Byte[] Program.Static::le
		IL_01B1: ldloc V_4
		IL_01B5: conv.ovf.u8.un
		IL_01B6: ldc.i8 0
		IL_01BF: call !!0[] Std.MemStatic::alloc<System.Byte>(System.UInt64, Std.Mem.Alignment)
		IL_01C4: stsfld System.Byte[] Program.Static::data
		IL_01C9: ldloc V_4
		IL_01CD: conv.ovf.u8.un
		IL_01CE: ldc.i8 0
		IL_01D7: call !!0[] Std.MemStatic::alloc<System.Byte>(System.UInt64, Std.Mem.Alignment)
		IL_01DC: stsfld System.Byte[] Program.Static::sp
		IL_01E1: ldstr "char else enum if int return sizeof while open read close printf malloc free memset memcmp exit void main"
		IL_01E6: stsfld System.String Program.Static::pval
		IL_01EB: ldstr "Everything fine until here"
		IL_01F0: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_01F5: ldc.i8 0
		IL_01FE: conv.i8
		IL_01FF: stloc V_5
		IL_0203: br IL_0238
		IL_0208: call System.Void Program.Static::next()
		IL_020D: ldsfld System.String[] Program.Static::id
		IL_0212: ldc.i8 0
		IL_021B: conv.u8
		IL_021C: conv.ovf.i4.un
		IL_021D: ldloc V_5
		IL_0221: stelem System.String
		IL_0226: ldloc V_5
		IL_022A: ldc.i8 1
		IL_0233: add
		IL_0234: stloc V_5
		IL_0238: ldloc V_5
		IL_023C: ldc.i8 0
		IL_0245: conv.i8
		IL_0246: cgt.un
		IL_0248: ldc.i4.0
		IL_0249: ceq
		IL_024B: brtrue IL_0208
		IL_0250: nop
		IL_0251: ldc.i8 0
		IL_025A: conv.i8
		IL_025B: stloc V_6
		IL_025F: br IL_0294
		IL_0264: call System.Void Program.Static::next()
		IL_0269: ldsfld System.String[] Program.Static::id
		IL_026E: ldc.i8 3
		IL_0277: conv.u8
		IL_0278: conv.ovf.i4.un
		IL_0279: ldloc V_6
		IL_027D: stelem System.String
		IL_0282: ldloc V_6
		IL_0286: ldc.i8 1
		IL_028F: add
		IL_0290: stloc V_6
		IL_0294: ldloc V_6
		IL_0298: ldc.i8 0
		IL_02A1: conv.i8
		IL_02A2: cgt.un
		IL_02A4: ldc.i4.0
		IL_02A5: ceq
		IL_02A7: brtrue IL_0264
		IL_02AC: nop
		IL_02AD: ldstr "Not bad until here"
		IL_02B2: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_02B7: ret
	}

	method private static next () System.Void {
		.locals init (System.UInt64 V_0)
		.locals init (System.Char V_1)
		.locals init (System.String[] V_2)
		IL_0000: ldc.i8 0
		IL_0009: stloc V_0
		IL_000D: ldsfld System.UInt64 Program.Static::p
		IL_0012: stsfld System.UInt64 Program.Static::tk
		IL_0017: br IL_061F
		IL_001C: ldsfld System.UInt64 Program.Static::p
		IL_0021: ldc.i8 1
		IL_002A: add
		IL_002B: stsfld System.UInt64 Program.Static::p
		IL_0030: ldsfld System.String Program.Static::pval
		IL_0035: ldsfld System.UInt64 Program.Static::p
		IL_003A: conv.i4
		IL_003B: call System.Char System.String::get_Chars(System.Int32)
		IL_0040: stloc V_1
		IL_0044: ldsfld System.String Program.Static::pval
		IL_0049: ldsfld System.UInt64 Program.Static::tk
		IL_004E: conv.i4
		IL_004F: call System.Char System.String::get_Chars(System.Int32)
		IL_0054: ldc.i4.s 92
		IL_0056: ceq
		IL_0058: brfalse IL_0351
		IL_005D: nop
		IL_005E: ldsfld System.Int64 Program.Static::src
		IL_0063: ldc.i8 0
		IL_006C: ceq
		IL_006E: ldc.i4.0
		IL_006F: ceq
		IL_0071: brfalse IL_0337
		IL_0076: nop
		IL_0077: ldsfld System.UInt64 Program.Static::line
		IL_007C: conv.ovf.u8.un
		IL_007D: call System.Void Std.ConsoleStatic::write(System.UInt64)
		IL_0082: ldstr ": "
		IL_0087: call System.Void Std.ConsoleStatic::write(System.String)
		IL_008C: ldsfld System.String Program.Static::pval
		IL_0091: ldsfld System.UInt64 Program.Static::lp
		IL_0096: conv.ovf.i4.un
		IL_0097: ldsfld System.UInt64 Program.Static::p
		IL_009C: ldsfld System.UInt64 Program.Static::lp
		IL_00A1: sub.ovf
		IL_00A2: conv.ovf.i4.un
		IL_00A3: call System.String System.String::Substring(System.Int32, System.Int32)
		IL_00A8: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_00AD: ldsfld System.UInt64 Program.Static::p
		IL_00B2: stsfld System.UInt64 Program.Static::lp
		IL_00B7: br IL_0320
		IL_00BC: ldc.i4.s 39
		IL_00BE: newarr System.String
		IL_00C3: dup
		IL_00C4: ldc.i4.0
		IL_00C5: ldstr "LEA"
		IL_00CA: stelem System.String
		IL_00CF: dup
		IL_00D0: ldc.i4.1
		IL_00D1: ldstr "IMM"
		IL_00D6: stelem System.String
		IL_00DB: dup
		IL_00DC: ldc.i4.2
		IL_00DD: ldstr "JMP"
		IL_00E2: stelem System.String
		IL_00E7: dup
		IL_00E8: ldc.i4.3
		IL_00E9: ldstr "JSR"
		IL_00EE: stelem System.String
		IL_00F3: dup
		IL_00F4: ldc.i4.4
		IL_00F5: ldstr "BZ"
		IL_00FA: stelem System.String
		IL_00FF: dup
		IL_0100: ldc.i4.5
		IL_0101: ldstr "BNZ"
		IL_0106: stelem System.String
		IL_010B: dup
		IL_010C: ldc.i4.6
		IL_010D: ldstr "ENT"
		IL_0112: stelem System.String
		IL_0117: dup
		IL_0118: ldc.i4.7
		IL_0119: ldstr "ADJ"
		IL_011E: stelem System.String
		IL_0123: dup
		IL_0124: ldc.i4.8
		IL_0125: ldstr "LEV"
		IL_012A: stelem System.String
		IL_012F: dup
		IL_0130: ldc.i4.s 9
		IL_0132: ldstr "LI"
		IL_0137: stelem System.String
		IL_013C: dup
		IL_013D: ldc.i4.s 10
		IL_013F: ldstr "LC"
		IL_0144: stelem System.String
		IL_0149: dup
		IL_014A: ldc.i4.s 11
		IL_014C: ldstr "SI"
		IL_0151: stelem System.String
		IL_0156: dup
		IL_0157: ldc.i4.s 12
		IL_0159: ldstr "SC"
		IL_015E: stelem System.String
		IL_0163: dup
		IL_0164: ldc.i4.s 13
		IL_0166: ldstr "PSH"
		IL_016B: stelem System.String
		IL_0170: dup
		IL_0171: ldc.i4.s 14
		IL_0173: ldstr "OR"
		IL_0178: stelem System.String
		IL_017D: dup
		IL_017E: ldc.i4.s 15
		IL_0180: ldstr "XOR"
		IL_0185: stelem System.String
		IL_018A: dup
		IL_018B: ldc.i4.s 16
		IL_018D: ldstr "AND"
		IL_0192: stelem System.String
		IL_0197: dup
		IL_0198: ldc.i4.s 17
		IL_019A: ldstr "EQ"
		IL_019F: stelem System.String
		IL_01A4: dup
		IL_01A5: ldc.i4.s 18
		IL_01A7: ldstr "NE"
		IL_01AC: stelem System.String
		IL_01B1: dup
		IL_01B2: ldc.i4.s 19
		IL_01B4: ldstr "LT"
		IL_01B9: stelem System.String
		IL_01BE: dup
		IL_01BF: ldc.i4.s 20
		IL_01C1: ldstr "GT"
		IL_01C6: stelem System.String
		IL_01CB: dup
		IL_01CC: ldc.i4.s 21
		IL_01CE: ldstr "LE"
		IL_01D3: stelem System.String
		IL_01D8: dup
		IL_01D9: ldc.i4.s 22
		IL_01DB: ldstr "GE"
		IL_01E0: stelem System.String
		IL_01E5: dup
		IL_01E6: ldc.i4.s 23
		IL_01E8: ldstr "SHL"
		IL_01ED: stelem System.String
		IL_01F2: dup
		IL_01F3: ldc.i4.s 24
		IL_01F5: ldstr "SHR"
		IL_01FA: stelem System.String
		IL_01FF: dup
		IL_0200: ldc.i4.s 25
		IL_0202: ldstr "ADD"
		IL_0207: stelem System.String
		IL_020C: dup
		IL_020D: ldc.i4.s 26
		IL_020F: ldstr "SUB"
		IL_0214: stelem System.String
		IL_0219: dup
		IL_021A: ldc.i4.s 27
		IL_021C: ldstr "MUL"
		IL_0221: stelem System.String
		IL_0226: dup
		IL_0227: ldc.i4.s 28
		IL_0229: ldstr "DIV"
		IL_022E: stelem System.String
		IL_0233: dup
		IL_0234: ldc.i4.s 29
		IL_0236: ldstr "MOD"
		IL_023B: stelem System.String
		IL_0240: dup
		IL_0241: ldc.i4.s 30
		IL_0243: ldstr "OPEN"
		IL_0248: stelem System.String
		IL_024D: dup
		IL_024E: ldc.i4.s 31
		IL_0250: ldstr "READ"
		IL_0255: stelem System.String
		IL_025A: dup
		IL_025B: ldc.i4.s 32
		IL_025D: ldstr "CLOS"
		IL_0262: stelem System.String
		IL_0267: dup
		IL_0268: ldc.i4.s 33
		IL_026A: ldstr "PRTF"
		IL_026F: stelem System.String
		IL_0274: dup
		IL_0275: ldc.i4.s 34
		IL_0277: ldstr "MALC"
		IL_027C: stelem System.String
		IL_0281: dup
		IL_0282: ldc.i4.s 35
		IL_0284: ldstr "FREE"
		IL_0289: stelem System.String
		IL_028E: dup
		IL_028F: ldc.i4.s 36
		IL_0291: ldstr "MSET"
		IL_0296: stelem System.String
		IL_029B: dup
		IL_029C: ldc.i4.s 37
		IL_029E: ldstr "MCMP"
		IL_02A3: stelem System.String
		IL_02A8: dup
		IL_02A9: ldc.i4.s 38
		IL_02AB: ldstr "EXIT"
		IL_02B0: stelem System.String
		IL_02B5: stloc V_2
		IL_02B9: ldloc V_2
		IL_02BD: ldsfld System.UInt64 Program.Static::lePtr
		IL_02C2: ldc.i8 1
		IL_02CB: add
		IL_02CC: dup
		IL_02CD: stsfld System.UInt64 Program.Static::lePtr
		IL_02D2: conv.i4
		IL_02D3: ldelem System.String
		IL_02D8: call System.Void Std.ConsoleStatic::write(System.String)
		IL_02DD: ldsfld System.UInt64 Program.Static::lePtr
		IL_02E2: ldc.i8 0
		IL_02EB: conv.i8
		IL_02EC: clt.un
		IL_02EE: brfalse IL_0314
		IL_02F3: nop
		IL_02F4: ldsfld System.UInt64 Program.Static::lePtr
		IL_02F9: ldc.i8 1
		IL_0302: add
		IL_0303: dup
		IL_0304: stsfld System.UInt64 Program.Static::lePtr
		IL_0309: conv.ovf.u8.un
		IL_030A: call System.Void Std.ConsoleStatic::writeln(System.UInt64)
		IL_030F: br IL_031F
		IL_0314: nop
		IL_0315: ldstr ""
		IL_031A: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_031F: nop
		IL_0320: ldsfld System.UInt64 Program.Static::lePtr
		IL_0325: ldsfld System.UInt64 Program.Static::e
		IL_032A: clt.un
		IL_032C: brtrue IL_00BC
		IL_0331: nop
		IL_0332: br IL_0337
		IL_0337: nop
		IL_0338: ldsfld System.UInt64 Program.Static::line
		IL_033D: ldc.i8 1
		IL_0346: add
		IL_0347: stsfld System.UInt64 Program.Static::line
		IL_034C: br IL_0614
		IL_0351: nop
		IL_0352: ldsfld System.UInt64 Program.Static::tk
		IL_0357: ldc.i4.s 35
		IL_0359: ceq
		IL_035B: brfalse IL_03B9
		IL_0360: nop
		IL_0361: br IL_037A
		IL_0366: ldsfld System.UInt64 Program.Static::p
		IL_036B: ldc.i8 1
		IL_0374: add
		IL_0375: stsfld System.UInt64 Program.Static::p
		IL_037A: ldsfld System.UInt64 Program.Static::p
		IL_037F: ldsfld System.String Program.Static::pval
		IL_0384: ldlen
		IL_0385: clt.un
		IL_0387: brfalse IL_03B3
		IL_038C: nop
		IL_038D: ldsfld System.String Program.Static::pval
		IL_0392: ldsfld System.UInt64 Program.Static::p
		IL_0397: conv.i4
		IL_0398: call System.Char System.String::get_Chars(System.Int32)
		IL_039D: ldc.i4.s 92
		IL_039F: ceq
		IL_03A1: ldc.i4.0
		IL_03A2: ceq
		IL_03A4: brfalse IL_03B3
		IL_03A9: br IL_0366
		IL_03AE: br IL_0366
		IL_03B3: nop
		IL_03B4: br IL_0614
		IL_03B9: nop
		IL_03BA: ldloc V_1
		IL_03BE: ldc.i4.s 97
		IL_03C0: clt.un
		IL_03C2: ldc.i4.0
		IL_03C3: ceq
		IL_03C5: brfalse IL_03E0
		IL_03CA: nop
		IL_03CB: ldloc V_1
		IL_03CF: ldc.i4.s 122
		IL_03D1: cgt.un
		IL_03D3: ldc.i4.0
		IL_03D4: ceq
		IL_03D6: brfalse IL_03E0
		IL_03DB: br IL_041A
		IL_03E0: nop
		IL_03E1: ldloc V_1
		IL_03E5: ldc.i4.s 65
		IL_03E7: clt.un
		IL_03E9: ldc.i4.0
		IL_03EA: ceq
		IL_03EC: brfalse IL_0407
		IL_03F1: nop
		IL_03F2: ldloc V_1
		IL_03F6: ldc.i4.s 90
		IL_03F8: cgt.un
		IL_03FA: ldc.i4.0
		IL_03FB: ceq
		IL_03FD: brfalse IL_0407
		IL_0402: br IL_041A
		IL_0407: nop
		IL_0408: ldloc V_1
		IL_040C: ldc.i4.s 95
		IL_040E: ceq
		IL_0410: brtrue IL_041A
		IL_0415: br IL_0614
		IL_041A: nop
		IL_041B: ldsfld System.UInt64 Program.Static::p
		IL_0420: ldc.i8 1
		IL_0429: sub
		IL_042A: stloc V_0
		IL_042E: br IL_0469
		IL_0433: ldsfld System.UInt64 Program.Static::tk
		IL_0438: ldc.i8 147
		IL_0441: mul.ovf.un
		IL_0442: ldsfld System.String Program.Static::pval
		IL_0447: ldsfld System.UInt64 Program.Static::p
		IL_044C: dup
		IL_044D: ldc.i8 1
		IL_0456: add
		IL_0457: stsfld System.UInt64 Program.Static::p
		IL_045C: conv.i4
		IL_045D: call System.Char System.String::get_Chars(System.Int32)
		IL_0462: conv.u8
		IL_0463: add
		IL_0464: stsfld System.UInt64 Program.Static::tk
		IL_0469: ldsfld System.String Program.Static::pval
		IL_046E: ldsfld System.UInt64 Program.Static::p
		IL_0473: conv.i4
		IL_0474: call System.Char System.String::get_Chars(System.Int32)
		IL_0479: ldc.i4.s 97
		IL_047B: clt.un
		IL_047D: ldc.i4.0
		IL_047E: ceq
		IL_0480: brfalse IL_04A7
		IL_0485: nop
		IL_0486: ldsfld System.String Program.Static::pval
		IL_048B: ldsfld System.UInt64 Program.Static::p
		IL_0490: conv.i4
		IL_0491: call System.Char System.String::get_Chars(System.Int32)
		IL_0496: ldc.i4.s 122
		IL_0498: cgt.un
		IL_049A: ldc.i4.0
		IL_049B: ceq
		IL_049D: brfalse IL_04A7
		IL_04A2: br IL_0433
		IL_04A7: nop
		IL_04A8: ldsfld System.String Program.Static::pval
		IL_04AD: ldsfld System.UInt64 Program.Static::p
		IL_04B2: conv.i4
		IL_04B3: call System.Char System.String::get_Chars(System.Int32)
		IL_04B8: ldc.i4.s 65
		IL_04BA: clt.un
		IL_04BC: ldc.i4.0
		IL_04BD: ceq
		IL_04BF: brfalse IL_04E6
		IL_04C4: nop
		IL_04C5: ldsfld System.String Program.Static::pval
		IL_04CA: ldsfld System.UInt64 Program.Static::p
		IL_04CF: conv.i4
		IL_04D0: call System.Char System.String::get_Chars(System.Int32)
		IL_04D5: ldc.i4.s 90
		IL_04D7: cgt.un
		IL_04D9: ldc.i4.0
		IL_04DA: ceq
		IL_04DC: brfalse IL_04E6
		IL_04E1: br IL_0433
		IL_04E6: nop
		IL_04E7: ldsfld System.String Program.Static::pval
		IL_04EC: ldsfld System.UInt64 Program.Static::p
		IL_04F1: conv.i4
		IL_04F2: call System.Char System.String::get_Chars(System.Int32)
		IL_04F7: ldc.i4.s 48
		IL_04F9: clt.un
		IL_04FB: ldc.i4.0
		IL_04FC: ceq
		IL_04FE: brfalse IL_0525
		IL_0503: nop
		IL_0504: ldsfld System.String Program.Static::pval
		IL_0509: ldsfld System.UInt64 Program.Static::p
		IL_050E: conv.i4
		IL_050F: call System.Char System.String::get_Chars(System.Int32)
		IL_0514: ldc.i4.s 57
		IL_0516: cgt.un
		IL_0518: ldc.i4.0
		IL_0519: ceq
		IL_051B: brfalse IL_0525
		IL_0520: br IL_0433
		IL_0525: nop
		IL_0526: ldsfld System.String Program.Static::pval
		IL_052B: ldsfld System.UInt64 Program.Static::p
		IL_0530: conv.i4
		IL_0531: call System.Char System.String::get_Chars(System.Int32)
		IL_0536: ldc.i4.s 95
		IL_0538: ceq
		IL_053A: brtrue IL_0433
		IL_053F: br IL_0549
		IL_0544: br IL_0433
		IL_0549: nop
		IL_054A: ldsfld System.UInt64 Program.Static::tk
		IL_054F: ldc.i8 64
		IL_0558: mul.ovf.un
		IL_0559: ldsfld System.UInt64 Program.Static::p
		IL_055E: ldloc V_0
		IL_0562: sub
		IL_0563: add
		IL_0564: stsfld System.UInt64 Program.Static::tk
		IL_0569: ldsfld System.Byte[] Program.Static::sym
		IL_056E: stsfld System.String[] Program.Static::id
		IL_0573: br IL_05F8
		IL_0578: ldsfld System.UInt64 Program.Static::tk
		IL_057D: ldsfld System.String[] Program.Static::id
		IL_0582: ldc.i8 1
		IL_058B: conv.i4
		IL_058C: ldelem System.String
		IL_0591: ceq
		IL_0593: brfalse IL_05F7
		IL_0598: nop
		IL_0599: ldsfld System.String[] Program.Static::id
		IL_059E: ldc.i8 2
		IL_05A7: conv.i4
		IL_05A8: ldelem System.String
		IL_05AD: ldsfld System.String Program.Static::pval
		IL_05B2: ldsfld System.UInt64 Program.Static::p
		IL_05B7: conv.ovf.i4.un
		IL_05B8: ldloc V_0
		IL_05BC: ldsfld System.UInt64 Program.Static::p
		IL_05C1: sub.ovf
		IL_05C2: conv.ovf.i4.un
		IL_05C3: call System.String System.String::Substring(System.Int32, System.Int32)
		IL_05C8: call System.Boolean System.String::Equals(System.String, System.String)
		IL_05CD: brfalse IL_05F7
		IL_05D2: br IL_05D7
		IL_05D7: nop
		IL_05D8: ldsfld System.String[] Program.Static::id
		IL_05DD: ldc.i8 0
		IL_05E6: conv.i4
		IL_05E7: ldelem System.String
		IL_05EC: stsfld System.UInt64 Program.Static::tk
		IL_05F1: ret
		IL_05F2: br IL_05F7
		IL_05F7: nop
		IL_05F8: ldsfld System.String[] Program.Static::id
		IL_05FD: ldlen
		IL_05FE: ldc.i8 0
		IL_0607: clt.un
		IL_0609: brtrue IL_0578
		IL_060E: nop
		IL_060F: br IL_0614
		IL_0614: nop
		IL_0615: ldsfld System.UInt64 Program.Static::p
		IL_061A: stsfld System.UInt64 Program.Static::tk
		IL_061F: ldsfld System.UInt64 Program.Static::p
		IL_0624: ldsfld System.String Program.Static::pval
		IL_0629: ldlen
		IL_062A: clt.un
		IL_062C: brtrue IL_001C
		IL_0631: nop
		IL_0632: ret
	}

	method private static .cctor () System.Void {
		IL_0000: ldc.i8 0
		IL_0000: stsfld System.Int64 Program.Static::src
		IL_0000: ldc.i8 0
		IL_0000: stsfld System.Int64 Program.Static::debug
		IL_0000: ldc.i8 0
		IL_0000: stsfld System.UInt64 Program.Static::lePtr
		IL_0000: ldc.i8 0
		IL_0000: stsfld System.UInt64 Program.Static::e
		IL_0000: ldc.i8 0
		IL_0000: stsfld System.UInt64 Program.Static::p
		IL_0000: ldc.i8 0
		IL_0000: stsfld System.UInt64 Program.Static::lp
		IL_0000: ldc.i8 0
		IL_0000: stsfld System.UInt64 Program.Static::tk
		IL_0000: ldc.i8 0
		IL_0000: stsfld System.UInt64 Program.Static::line
		IL_0000: ret
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

	method public static create <T> (Std.Mem.Alignment alignment) !!0 {
		IL_0000: ldtoken !!0
		IL_0005: call System.Type System.Type::GetTypeFromHandle(System.RuntimeTypeHandle)
		IL_000A: call System.Object System.Runtime.CompilerServices.RuntimeHelpers::GetUninitializedObject(System.Type)
		IL_000F: unbox.any !!0
		IL_0014: ret
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

struct Program.Tokens extends System.Enum {
	field public instance System.Int64 value__
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
	field public instance System.Int64 value__
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
	field public instance System.Int64 value__
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
