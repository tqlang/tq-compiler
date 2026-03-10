
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
		IL_0090: stsfld System.Int64 Program.Static::src
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
		IL_010B: stsfld System.Int64 Program.Static::debug
		IL_0110: br IL_0115
		IL_0115: nop
		IL_0116: ldloc V_1
		IL_011A: ldloc V_2
		IL_011E: sub
		IL_011F: ldc.i8 1
		IL_0128: clt
		IL_012A: brfalse IL_014E
		IL_012F: nop
		IL_0130: ldstr "usage: c4 [-s] [-d] file ..."
		IL_0135: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_013A: ldc.i8 1
		IL_0143: conv.ovf.u8.un
		IL_0144: call System.Void Std.ProcessStatic::exit(System.UInt64)
		IL_0149: br IL_014E
		IL_014E: nop
		IL_014F: ldloc V_0
		IL_0153: ldloc V_2
		IL_0157: conv.i4
		IL_0158: ldelem System.String
		IL_015D: call Std.Fs.File Std.FsStatic::open(System.String)
		IL_0162: stloc V_3
		IL_0166: ldc.i8 262144
		IL_016F: stloc V_4
		IL_0173: ldloc V_4
		IL_0177: conv.ovf.u8.un
		IL_0178: ldc.i8 0
		IL_0181: call !!0[] Std.MemStatic::alloc<System.Byte>(System.UInt64, Std.Mem.Alignment)
		IL_0186: stsfld System.Byte[] Program.Static::sym
		IL_018B: ldloc V_4
		IL_018F: conv.ovf.u8.un
		IL_0190: ldc.i8 0
		IL_0199: call !!0[] Std.MemStatic::alloc<System.Byte>(System.UInt64, Std.Mem.Alignment)
		IL_019E: stsfld System.Byte[] Program.Static::le
		IL_01A3: ldloc V_4
		IL_01A7: conv.ovf.u8.un
		IL_01A8: ldc.i8 0
		IL_01B1: call !!0[] Std.MemStatic::alloc<System.Byte>(System.UInt64, Std.Mem.Alignment)
		IL_01B6: stsfld System.Byte[] Program.Static::data
		IL_01BB: ldloc V_4
		IL_01BF: conv.ovf.u8.un
		IL_01C0: ldc.i8 0
		IL_01C9: call !!0[] Std.MemStatic::alloc<System.Byte>(System.UInt64, Std.Mem.Alignment)
		IL_01CE: stsfld System.Byte[] Program.Static::sp
		IL_01D3: ldstr "char else enum if int return sizeof while open read close printf malloc free memset memcmp exit void main"
		IL_01D8: stsfld System.String Program.Static::pval
		IL_01DD: ldstr "Everything fine until here"
		IL_01E2: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_01E7: ldc.i8 0
		IL_01F0: conv.i8
		IL_01F1: stloc V_5
		IL_01F5: br IL_022A
		IL_01FA: call System.Void Program.Static::next()
		IL_01FF: ldsfld System.String[] Program.Static::id
		IL_0204: ldc.i8 0
		IL_020D: conv.u8
		IL_020E: conv.ovf.i4.un
		IL_020F: ldloc V_5
		IL_0213: stelem System.String
		IL_0218: ldloc V_5
		IL_021C: ldc.i8 1
		IL_0225: add
		IL_0226: stloc V_5
		IL_022A: ldloc V_5
		IL_022E: ldc.i8 0
		IL_0237: conv.i8
		IL_0238: cgt.un
		IL_023A: ldc.i4.0
		IL_023B: ceq
		IL_023D: brtrue IL_01FA
		IL_0242: nop
		IL_0243: ldstr "Ok until here"
		IL_0248: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_024D: ldc.i8 0
		IL_0256: conv.i8
		IL_0257: stloc V_6
		IL_025B: br IL_0290
		IL_0260: call System.Void Program.Static::next()
		IL_0265: ldsfld System.String[] Program.Static::id
		IL_026A: ldc.i8 3
		IL_0273: conv.u8
		IL_0274: conv.ovf.i4.un
		IL_0275: ldloc V_6
		IL_0279: stelem System.String
		IL_027E: ldloc V_6
		IL_0282: ldc.i8 1
		IL_028B: add
		IL_028C: stloc V_6
		IL_0290: ldloc V_6
		IL_0294: ldc.i8 0
		IL_029D: conv.i8
		IL_029E: cgt.un
		IL_02A0: ldc.i4.0
		IL_02A1: ceq
		IL_02A3: brtrue IL_0260
		IL_02A8: nop
		IL_02A9: ldstr "Not bad until here"
		IL_02AE: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_02B3: ret
	}

	method private static next () System.Void {
		.locals init (System.UInt64 V_0)
		.locals init (System.String[] V_1)
		IL_0000: ldc.i8 0
		IL_0009: stloc V_0
		IL_000D: ldsfld System.UInt64 Program.Static::p
		IL_0012: stsfld System.UInt64 Program.Static::tk
		IL_0017: br IL_060E
		IL_001C: ldsfld System.UInt64 Program.Static::p
		IL_0021: ldc.i8 1
		IL_002A: add
		IL_002B: stsfld System.UInt64 Program.Static::p
		IL_0030: ldsfld System.String Program.Static::pval
		IL_0035: ldsfld System.UInt64 Program.Static::tk
		IL_003A: conv.i4
		IL_003B: call System.Char System.String::get_Chars(System.Int32)
		IL_0040: ldc.i4.s 92
		IL_0042: ceq
		IL_0044: brfalse IL_033D
		IL_0049: nop
		IL_004A: ldsfld System.Int64 Program.Static::src
		IL_004F: ldc.i8 0
		IL_0058: ceq
		IL_005A: ldc.i4.0
		IL_005B: ceq
		IL_005D: brfalse IL_0323
		IL_0062: nop
		IL_0063: ldsfld System.UInt64 Program.Static::line
		IL_0068: conv.ovf.u8.un
		IL_0069: call System.Void Std.ConsoleStatic::write(System.UInt64)
		IL_006E: ldstr ": "
		IL_0073: call System.Void Std.ConsoleStatic::write(System.String)
		IL_0078: ldsfld System.String Program.Static::pval
		IL_007D: ldsfld System.UInt64 Program.Static::lp
		IL_0082: conv.ovf.i4.un
		IL_0083: ldsfld System.UInt64 Program.Static::p
		IL_0088: ldsfld System.UInt64 Program.Static::lp
		IL_008D: sub.ovf
		IL_008E: conv.ovf.i4.un
		IL_008F: call System.String System.String::Substring(System.Int32, System.Int32)
		IL_0094: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_0099: ldsfld System.UInt64 Program.Static::p
		IL_009E: stsfld System.UInt64 Program.Static::lp
		IL_00A3: br IL_030C
		IL_00A8: ldc.i4.s 39
		IL_00AA: newarr System.String
		IL_00AF: dup
		IL_00B0: ldc.i4.0
		IL_00B1: ldstr "LEA"
		IL_00B6: stelem System.String
		IL_00BB: dup
		IL_00BC: ldc.i4.1
		IL_00BD: ldstr "IMM"
		IL_00C2: stelem System.String
		IL_00C7: dup
		IL_00C8: ldc.i4.2
		IL_00C9: ldstr "JMP"
		IL_00CE: stelem System.String
		IL_00D3: dup
		IL_00D4: ldc.i4.3
		IL_00D5: ldstr "JSR"
		IL_00DA: stelem System.String
		IL_00DF: dup
		IL_00E0: ldc.i4.4
		IL_00E1: ldstr "BZ"
		IL_00E6: stelem System.String
		IL_00EB: dup
		IL_00EC: ldc.i4.5
		IL_00ED: ldstr "BNZ"
		IL_00F2: stelem System.String
		IL_00F7: dup
		IL_00F8: ldc.i4.6
		IL_00F9: ldstr "ENT"
		IL_00FE: stelem System.String
		IL_0103: dup
		IL_0104: ldc.i4.7
		IL_0105: ldstr "ADJ"
		IL_010A: stelem System.String
		IL_010F: dup
		IL_0110: ldc.i4.8
		IL_0111: ldstr "LEV"
		IL_0116: stelem System.String
		IL_011B: dup
		IL_011C: ldc.i4.s 9
		IL_011E: ldstr "LI"
		IL_0123: stelem System.String
		IL_0128: dup
		IL_0129: ldc.i4.s 10
		IL_012B: ldstr "LC"
		IL_0130: stelem System.String
		IL_0135: dup
		IL_0136: ldc.i4.s 11
		IL_0138: ldstr "SI"
		IL_013D: stelem System.String
		IL_0142: dup
		IL_0143: ldc.i4.s 12
		IL_0145: ldstr "SC"
		IL_014A: stelem System.String
		IL_014F: dup
		IL_0150: ldc.i4.s 13
		IL_0152: ldstr "PSH"
		IL_0157: stelem System.String
		IL_015C: dup
		IL_015D: ldc.i4.s 14
		IL_015F: ldstr "OR"
		IL_0164: stelem System.String
		IL_0169: dup
		IL_016A: ldc.i4.s 15
		IL_016C: ldstr "XOR"
		IL_0171: stelem System.String
		IL_0176: dup
		IL_0177: ldc.i4.s 16
		IL_0179: ldstr "AND"
		IL_017E: stelem System.String
		IL_0183: dup
		IL_0184: ldc.i4.s 17
		IL_0186: ldstr "EQ"
		IL_018B: stelem System.String
		IL_0190: dup
		IL_0191: ldc.i4.s 18
		IL_0193: ldstr "NE"
		IL_0198: stelem System.String
		IL_019D: dup
		IL_019E: ldc.i4.s 19
		IL_01A0: ldstr "LT"
		IL_01A5: stelem System.String
		IL_01AA: dup
		IL_01AB: ldc.i4.s 20
		IL_01AD: ldstr "GT"
		IL_01B2: stelem System.String
		IL_01B7: dup
		IL_01B8: ldc.i4.s 21
		IL_01BA: ldstr "LE"
		IL_01BF: stelem System.String
		IL_01C4: dup
		IL_01C5: ldc.i4.s 22
		IL_01C7: ldstr "GE"
		IL_01CC: stelem System.String
		IL_01D1: dup
		IL_01D2: ldc.i4.s 23
		IL_01D4: ldstr "SHL"
		IL_01D9: stelem System.String
		IL_01DE: dup
		IL_01DF: ldc.i4.s 24
		IL_01E1: ldstr "SHR"
		IL_01E6: stelem System.String
		IL_01EB: dup
		IL_01EC: ldc.i4.s 25
		IL_01EE: ldstr "ADD"
		IL_01F3: stelem System.String
		IL_01F8: dup
		IL_01F9: ldc.i4.s 26
		IL_01FB: ldstr "SUB"
		IL_0200: stelem System.String
		IL_0205: dup
		IL_0206: ldc.i4.s 27
		IL_0208: ldstr "MUL"
		IL_020D: stelem System.String
		IL_0212: dup
		IL_0213: ldc.i4.s 28
		IL_0215: ldstr "DIV"
		IL_021A: stelem System.String
		IL_021F: dup
		IL_0220: ldc.i4.s 29
		IL_0222: ldstr "MOD"
		IL_0227: stelem System.String
		IL_022C: dup
		IL_022D: ldc.i4.s 30
		IL_022F: ldstr "OPEN"
		IL_0234: stelem System.String
		IL_0239: dup
		IL_023A: ldc.i4.s 31
		IL_023C: ldstr "READ"
		IL_0241: stelem System.String
		IL_0246: dup
		IL_0247: ldc.i4.s 32
		IL_0249: ldstr "CLOS"
		IL_024E: stelem System.String
		IL_0253: dup
		IL_0254: ldc.i4.s 33
		IL_0256: ldstr "PRTF"
		IL_025B: stelem System.String
		IL_0260: dup
		IL_0261: ldc.i4.s 34
		IL_0263: ldstr "MALC"
		IL_0268: stelem System.String
		IL_026D: dup
		IL_026E: ldc.i4.s 35
		IL_0270: ldstr "FREE"
		IL_0275: stelem System.String
		IL_027A: dup
		IL_027B: ldc.i4.s 36
		IL_027D: ldstr "MSET"
		IL_0282: stelem System.String
		IL_0287: dup
		IL_0288: ldc.i4.s 37
		IL_028A: ldstr "MCMP"
		IL_028F: stelem System.String
		IL_0294: dup
		IL_0295: ldc.i4.s 38
		IL_0297: ldstr "EXIT"
		IL_029C: stelem System.String
		IL_02A1: stloc V_1
		IL_02A5: ldloc V_1
		IL_02A9: ldsfld System.UInt64 Program.Static::lePtr
		IL_02AE: ldc.i8 1
		IL_02B7: add
		IL_02B8: dup
		IL_02B9: stsfld System.UInt64 Program.Static::lePtr
		IL_02BE: conv.i4
		IL_02BF: ldelem System.String
		IL_02C4: call System.Void Std.ConsoleStatic::write(System.String)
		IL_02C9: ldsfld System.UInt64 Program.Static::lePtr
		IL_02CE: ldc.i8 0
		IL_02D7: conv.i8
		IL_02D8: clt.un
		IL_02DA: brfalse IL_0300
		IL_02DF: nop
		IL_02E0: ldsfld System.UInt64 Program.Static::lePtr
		IL_02E5: ldc.i8 1
		IL_02EE: add
		IL_02EF: dup
		IL_02F0: stsfld System.UInt64 Program.Static::lePtr
		IL_02F5: conv.ovf.u8.un
		IL_02F6: call System.Void Std.ConsoleStatic::writeln(System.UInt64)
		IL_02FB: br IL_030B
		IL_0300: nop
		IL_0301: ldstr ""
		IL_0306: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_030B: nop
		IL_030C: ldsfld System.UInt64 Program.Static::lePtr
		IL_0311: ldsfld System.UInt64 Program.Static::e
		IL_0316: clt.un
		IL_0318: brtrue IL_00A8
		IL_031D: nop
		IL_031E: br IL_0323
		IL_0323: nop
		IL_0324: ldsfld System.UInt64 Program.Static::line
		IL_0329: ldc.i8 1
		IL_0332: add
		IL_0333: stsfld System.UInt64 Program.Static::line
		IL_0338: br IL_0603
		IL_033D: nop
		IL_033E: ldsfld System.UInt64 Program.Static::tk
		IL_0343: ldc.i4.s 35
		IL_0345: ceq
		IL_0347: brfalse IL_039F
		IL_034C: nop
		IL_034D: br IL_0366
		IL_0352: ldsfld System.UInt64 Program.Static::p
		IL_0357: ldc.i8 1
		IL_0360: add
		IL_0361: stsfld System.UInt64 Program.Static::p
		IL_0366: ldsfld System.UInt64 Program.Static::p
		IL_036B: ldsfld System.String Program.Static::pval
		IL_0370: ldlen
		IL_0371: clt.un
		IL_0373: brfalse IL_0399
		IL_0378: ldsfld System.String Program.Static::pval
		IL_037D: ldsfld System.UInt64 Program.Static::p
		IL_0382: conv.i4
		IL_0383: call System.Char System.String::get_Chars(System.Int32)
		IL_0388: ldc.i4.s 92
		IL_038A: ceq
		IL_038C: ldc.i4.0
		IL_038D: ceq
		IL_038F: brfalse IL_0399
		IL_0394: br IL_0352
		IL_0399: nop
		IL_039A: br IL_0603
		IL_039F: nop
		IL_03A0: ldsfld System.String Program.Static::pval
		IL_03A5: ldsfld System.UInt64 Program.Static::p
		IL_03AA: conv.i4
		IL_03AB: call System.Char System.String::get_Chars(System.Int32)
		IL_03B0: ldc.i4.s 97
		IL_03B2: cgt.un
		IL_03B4: ldc.i4.0
		IL_03B5: ceq
		IL_03B7: brfalse IL_0603
		IL_03BC: ldsfld System.String Program.Static::pval
		IL_03C1: ldsfld System.UInt64 Program.Static::p
		IL_03C6: conv.i4
		IL_03C7: call System.Char System.String::get_Chars(System.Int32)
		IL_03CC: ldc.i4.s 122
		IL_03CE: cgt.un
		IL_03D0: ldc.i4.0
		IL_03D1: ceq
		IL_03D3: brfalse IL_0603
		IL_03D8: ldsfld System.String Program.Static::pval
		IL_03DD: ldsfld System.UInt64 Program.Static::p
		IL_03E2: conv.i4
		IL_03E3: call System.Char System.String::get_Chars(System.Int32)
		IL_03E8: ldc.i4.s 65
		IL_03EA: cgt.un
		IL_03EC: ldc.i4.0
		IL_03ED: ceq
		IL_03EF: brfalse IL_0603
		IL_03F4: ldsfld System.String Program.Static::pval
		IL_03F9: ldsfld System.UInt64 Program.Static::p
		IL_03FE: conv.i4
		IL_03FF: call System.Char System.String::get_Chars(System.Int32)
		IL_0404: ldc.i4.s 90
		IL_0406: cgt.un
		IL_0408: ldc.i4.0
		IL_0409: ceq
		IL_040B: brfalse IL_0603
		IL_0410: ldsfld System.String Program.Static::pval
		IL_0415: ldsfld System.UInt64 Program.Static::p
		IL_041A: conv.i4
		IL_041B: call System.Char System.String::get_Chars(System.Int32)
		IL_0420: ldc.i4.s 95
		IL_0422: ceq
		IL_0424: brtrue IL_0429
		IL_0429: nop
		IL_042A: ldsfld System.UInt64 Program.Static::p
		IL_042F: ldc.i8 1
		IL_0438: sub
		IL_0439: stloc V_0
		IL_043D: br IL_0478
		IL_0442: ldsfld System.UInt64 Program.Static::tk
		IL_0447: ldc.i8 147
		IL_0450: mul.ovf.un
		IL_0451: ldsfld System.String Program.Static::pval
		IL_0456: ldsfld System.UInt64 Program.Static::p
		IL_045B: dup
		IL_045C: ldc.i8 1
		IL_0465: add
		IL_0466: stsfld System.UInt64 Program.Static::p
		IL_046B: conv.i4
		IL_046C: call System.Char System.String::get_Chars(System.Int32)
		IL_0471: conv.u8
		IL_0472: add
		IL_0473: stsfld System.UInt64 Program.Static::tk
		IL_0478: ldsfld System.String Program.Static::pval
		IL_047D: ldsfld System.UInt64 Program.Static::p
		IL_0482: conv.i4
		IL_0483: call System.Char System.String::get_Chars(System.Int32)
		IL_0488: ldc.i4.s 97
		IL_048A: cgt.un
		IL_048C: ldc.i4.0
		IL_048D: ceq
		IL_048F: brfalse IL_053E
		IL_0494: ldsfld System.String Program.Static::pval
		IL_0499: ldsfld System.UInt64 Program.Static::p
		IL_049E: conv.i4
		IL_049F: call System.Char System.String::get_Chars(System.Int32)
		IL_04A4: ldc.i4.s 122
		IL_04A6: cgt.un
		IL_04A8: ldc.i4.0
		IL_04A9: ceq
		IL_04AB: brfalse IL_053E
		IL_04B0: ldsfld System.String Program.Static::pval
		IL_04B5: ldsfld System.UInt64 Program.Static::p
		IL_04BA: conv.i4
		IL_04BB: call System.Char System.String::get_Chars(System.Int32)
		IL_04C0: ldc.i4.s 65
		IL_04C2: cgt.un
		IL_04C4: ldc.i4.0
		IL_04C5: ceq
		IL_04C7: brfalse IL_053E
		IL_04CC: ldsfld System.String Program.Static::pval
		IL_04D1: ldsfld System.UInt64 Program.Static::p
		IL_04D6: conv.i4
		IL_04D7: call System.Char System.String::get_Chars(System.Int32)
		IL_04DC: ldc.i4.s 90
		IL_04DE: cgt.un
		IL_04E0: ldc.i4.0
		IL_04E1: ceq
		IL_04E3: brfalse IL_053E
		IL_04E8: ldsfld System.String Program.Static::pval
		IL_04ED: ldsfld System.UInt64 Program.Static::p
		IL_04F2: conv.i4
		IL_04F3: call System.Char System.String::get_Chars(System.Int32)
		IL_04F8: ldc.i4.s 48
		IL_04FA: cgt.un
		IL_04FC: ldc.i4.0
		IL_04FD: ceq
		IL_04FF: brfalse IL_053E
		IL_0504: ldsfld System.String Program.Static::pval
		IL_0509: ldsfld System.UInt64 Program.Static::p
		IL_050E: conv.i4
		IL_050F: call System.Char System.String::get_Chars(System.Int32)
		IL_0514: ldc.i4.s 57
		IL_0516: cgt.un
		IL_0518: ldc.i4.0
		IL_0519: ceq
		IL_051B: brfalse IL_053E
		IL_0520: ldsfld System.String Program.Static::pval
		IL_0525: ldsfld System.UInt64 Program.Static::p
		IL_052A: conv.i4
		IL_052B: call System.Char System.String::get_Chars(System.Int32)
		IL_0530: ldc.i4.s 95
		IL_0532: ceq
		IL_0534: brtrue IL_0478
		IL_0539: br IL_0442
		IL_053E: nop
		IL_053F: ldsfld System.UInt64 Program.Static::tk
		IL_0544: ldc.i8 64
		IL_054D: mul.ovf.un
		IL_054E: ldsfld System.UInt64 Program.Static::p
		IL_0553: ldloc V_0
		IL_0557: sub
		IL_0558: add
		IL_0559: stsfld System.UInt64 Program.Static::tk
		IL_055E: ldsfld System.Byte[] Program.Static::sym
		IL_0563: stsfld System.String[] Program.Static::id
		IL_0568: br IL_05E7
		IL_056D: ldsfld System.UInt64 Program.Static::tk
		IL_0572: ldsfld System.String[] Program.Static::id
		IL_0577: ldc.i8 1
		IL_0580: conv.i4
		IL_0581: ldelem System.String
		IL_0586: ceq
		IL_0588: brfalse IL_05E6
		IL_058D: ldsfld System.String[] Program.Static::id
		IL_0592: ldc.i8 2
		IL_059B: conv.i4
		IL_059C: ldelem System.String
		IL_05A1: ldsfld System.String Program.Static::pval
		IL_05A6: ldsfld System.UInt64 Program.Static::p
		IL_05AB: conv.ovf.i4.un
		IL_05AC: ldloc V_0
		IL_05B0: ldsfld System.UInt64 Program.Static::p
		IL_05B5: sub.ovf
		IL_05B6: conv.ovf.i4.un
		IL_05B7: call System.String System.String::Substring(System.Int32, System.Int32)
		IL_05BC: call System.Boolean System.String::Equals(System.String, System.String)
		IL_05C1: brfalse IL_05E6
		IL_05C6: nop
		IL_05C7: ldsfld System.String[] Program.Static::id
		IL_05CC: ldc.i8 0
		IL_05D5: conv.i4
		IL_05D6: ldelem System.String
		IL_05DB: stsfld System.UInt64 Program.Static::tk
		IL_05E0: ret
		IL_05E1: br IL_05E6
		IL_05E6: nop
		IL_05E7: ldsfld System.String[] Program.Static::id
		IL_05EC: ldlen
		IL_05ED: ldc.i8 0
		IL_05F6: clt.un
		IL_05F8: brtrue IL_056D
		IL_05FD: nop
		IL_05FE: br IL_0603
		IL_0603: nop
		IL_0604: ldsfld System.UInt64 Program.Static::p
		IL_0609: stsfld System.UInt64 Program.Static::tk
		IL_060E: ldsfld System.String Program.Static::pval
		IL_0613: ldsfld System.UInt64 Program.Static::p
		IL_0618: conv.i4
		IL_0619: call System.Char System.String::get_Chars(System.Int32)
		IL_061E: ldsfld System.String Program.Static::pval
		IL_0623: ldlen
		IL_0624: clt.un
		IL_0626: brtrue IL_001C
		IL_062B: nop
		IL_062C: ret
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
		IL_0000: ret
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
