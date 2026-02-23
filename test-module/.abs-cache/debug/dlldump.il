
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
	field private static System.UInt64[] id
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
		IL_01FF: ldsfld System.UInt64[] Program.Static::id
		IL_0204: ldc.i8 0
		IL_020D: conv.i8
		IL_020E: conv.ovf.i4.un
		IL_020F: ldloc V_5
		IL_0213: stelem System.UInt64
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
		IL_0243: ldc.i8 0
		IL_024C: conv.i8
		IL_024D: stloc V_6
		IL_0251: br IL_0286
		IL_0256: call System.Void Program.Static::next()
		IL_025B: ldsfld System.UInt64[] Program.Static::id
		IL_0260: ldc.i8 0
		IL_0269: conv.i8
		IL_026A: conv.ovf.i4.un
		IL_026B: ldloc V_6
		IL_026F: stelem System.UInt64
		IL_0274: ldloc V_6
		IL_0278: ldc.i8 1
		IL_0281: add
		IL_0282: stloc V_6
		IL_0286: ldloc V_6
		IL_028A: ldc.i8 0
		IL_0293: conv.i8
		IL_0294: cgt.un
		IL_0296: ldc.i4.0
		IL_0297: ceq
		IL_0299: brtrue IL_0256
		IL_029E: nop
		IL_029F: ldstr "Not bad until here"
		IL_02A4: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_02A9: ret
	}

	method private static next () System.Void {
		.locals init (System.UInt64 V_0)
		.locals init (System.String[] V_1)
		IL_0000: ldc.i8 0
		IL_0009: stloc V_0
		IL_000D: ldsfld System.UInt64 Program.Static::p
		IL_0012: stsfld System.UInt64 Program.Static::tk
		IL_0017: br IL_0542
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
		IL_0044: brfalse IL_0307
		IL_0049: nop
		IL_004A: ldsfld System.Int64 Program.Static::src
		IL_004F: ldc.i8 0
		IL_0058: ceq
		IL_005A: ldc.i4.0
		IL_005B: ceq
		IL_005D: brfalse IL_02ED
		IL_0062: nop
		IL_0063: ldsfld System.UInt64 Program.Static::p
		IL_0068: stsfld System.UInt64 Program.Static::lp
		IL_006D: br IL_02D6
		IL_0072: ldc.i4.s 39
		IL_0074: newarr System.String
		IL_0079: dup
		IL_007A: ldc.i4.0
		IL_007B: ldstr "LEA"
		IL_0080: stelem System.String
		IL_0085: dup
		IL_0086: ldc.i4.1
		IL_0087: ldstr "IMM"
		IL_008C: stelem System.String
		IL_0091: dup
		IL_0092: ldc.i4.2
		IL_0093: ldstr "JMP"
		IL_0098: stelem System.String
		IL_009D: dup
		IL_009E: ldc.i4.3
		IL_009F: ldstr "JSR"
		IL_00A4: stelem System.String
		IL_00A9: dup
		IL_00AA: ldc.i4.4
		IL_00AB: ldstr "BZ"
		IL_00B0: stelem System.String
		IL_00B5: dup
		IL_00B6: ldc.i4.5
		IL_00B7: ldstr "BNZ"
		IL_00BC: stelem System.String
		IL_00C1: dup
		IL_00C2: ldc.i4.6
		IL_00C3: ldstr "ENT"
		IL_00C8: stelem System.String
		IL_00CD: dup
		IL_00CE: ldc.i4.7
		IL_00CF: ldstr "ADJ"
		IL_00D4: stelem System.String
		IL_00D9: dup
		IL_00DA: ldc.i4.8
		IL_00DB: ldstr "LEV"
		IL_00E0: stelem System.String
		IL_00E5: dup
		IL_00E6: ldc.i4.s 9
		IL_00E8: ldstr "LI"
		IL_00ED: stelem System.String
		IL_00F2: dup
		IL_00F3: ldc.i4.s 10
		IL_00F5: ldstr "LC"
		IL_00FA: stelem System.String
		IL_00FF: dup
		IL_0100: ldc.i4.s 11
		IL_0102: ldstr "SI"
		IL_0107: stelem System.String
		IL_010C: dup
		IL_010D: ldc.i4.s 12
		IL_010F: ldstr "SC"
		IL_0114: stelem System.String
		IL_0119: dup
		IL_011A: ldc.i4.s 13
		IL_011C: ldstr "PSH"
		IL_0121: stelem System.String
		IL_0126: dup
		IL_0127: ldc.i4.s 14
		IL_0129: ldstr "OR"
		IL_012E: stelem System.String
		IL_0133: dup
		IL_0134: ldc.i4.s 15
		IL_0136: ldstr "XOR"
		IL_013B: stelem System.String
		IL_0140: dup
		IL_0141: ldc.i4.s 16
		IL_0143: ldstr "AND"
		IL_0148: stelem System.String
		IL_014D: dup
		IL_014E: ldc.i4.s 17
		IL_0150: ldstr "EQ"
		IL_0155: stelem System.String
		IL_015A: dup
		IL_015B: ldc.i4.s 18
		IL_015D: ldstr "NE"
		IL_0162: stelem System.String
		IL_0167: dup
		IL_0168: ldc.i4.s 19
		IL_016A: ldstr "LT"
		IL_016F: stelem System.String
		IL_0174: dup
		IL_0175: ldc.i4.s 20
		IL_0177: ldstr "GT"
		IL_017C: stelem System.String
		IL_0181: dup
		IL_0182: ldc.i4.s 21
		IL_0184: ldstr "LE"
		IL_0189: stelem System.String
		IL_018E: dup
		IL_018F: ldc.i4.s 22
		IL_0191: ldstr "GE"
		IL_0196: stelem System.String
		IL_019B: dup
		IL_019C: ldc.i4.s 23
		IL_019E: ldstr "SHL"
		IL_01A3: stelem System.String
		IL_01A8: dup
		IL_01A9: ldc.i4.s 24
		IL_01AB: ldstr "SHR"
		IL_01B0: stelem System.String
		IL_01B5: dup
		IL_01B6: ldc.i4.s 25
		IL_01B8: ldstr "ADD"
		IL_01BD: stelem System.String
		IL_01C2: dup
		IL_01C3: ldc.i4.s 26
		IL_01C5: ldstr "SUB"
		IL_01CA: stelem System.String
		IL_01CF: dup
		IL_01D0: ldc.i4.s 27
		IL_01D2: ldstr "MUL"
		IL_01D7: stelem System.String
		IL_01DC: dup
		IL_01DD: ldc.i4.s 28
		IL_01DF: ldstr "DIV"
		IL_01E4: stelem System.String
		IL_01E9: dup
		IL_01EA: ldc.i4.s 29
		IL_01EC: ldstr "MOD"
		IL_01F1: stelem System.String
		IL_01F6: dup
		IL_01F7: ldc.i4.s 30
		IL_01F9: ldstr "OPEN"
		IL_01FE: stelem System.String
		IL_0203: dup
		IL_0204: ldc.i4.s 31
		IL_0206: ldstr "READ"
		IL_020B: stelem System.String
		IL_0210: dup
		IL_0211: ldc.i4.s 32
		IL_0213: ldstr "CLOS"
		IL_0218: stelem System.String
		IL_021D: dup
		IL_021E: ldc.i4.s 33
		IL_0220: ldstr "PRTF"
		IL_0225: stelem System.String
		IL_022A: dup
		IL_022B: ldc.i4.s 34
		IL_022D: ldstr "MALC"
		IL_0232: stelem System.String
		IL_0237: dup
		IL_0238: ldc.i4.s 35
		IL_023A: ldstr "FREE"
		IL_023F: stelem System.String
		IL_0244: dup
		IL_0245: ldc.i4.s 36
		IL_0247: ldstr "MSET"
		IL_024C: stelem System.String
		IL_0251: dup
		IL_0252: ldc.i4.s 37
		IL_0254: ldstr "MCMP"
		IL_0259: stelem System.String
		IL_025E: dup
		IL_025F: ldc.i4.s 38
		IL_0261: ldstr "EXIT"
		IL_0266: stelem System.String
		IL_026B: stloc V_1
		IL_026F: ldloc V_1
		IL_0273: ldsfld System.UInt64 Program.Static::lePtr
		IL_0278: ldc.i8 1
		IL_0281: add
		IL_0282: dup
		IL_0283: stsfld System.UInt64 Program.Static::lePtr
		IL_0288: conv.i4
		IL_0289: ldelem System.String
		IL_028E: call System.Void Std.ConsoleStatic::write(System.String)
		IL_0293: ldsfld System.UInt64 Program.Static::lePtr
		IL_0298: ldc.i8 0
		IL_02A1: conv.i8
		IL_02A2: clt.un
		IL_02A4: brfalse IL_02CA
		IL_02A9: nop
		IL_02AA: ldsfld System.UInt64 Program.Static::lePtr
		IL_02AF: ldc.i8 1
		IL_02B8: add
		IL_02B9: dup
		IL_02BA: stsfld System.UInt64 Program.Static::lePtr
		IL_02BF: conv.ovf.u8.un
		IL_02C0: call System.Void Std.ConsoleStatic::writeln(System.UInt64)
		IL_02C5: br IL_02D5
		IL_02CA: nop
		IL_02CB: ldstr ""
		IL_02D0: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_02D5: nop
		IL_02D6: ldsfld System.UInt64 Program.Static::lePtr
		IL_02DB: ldsfld System.UInt64 Program.Static::e
		IL_02E0: clt.un
		IL_02E2: brtrue IL_0072
		IL_02E7: nop
		IL_02E8: br IL_02ED
		IL_02ED: nop
		IL_02EE: ldsfld System.UInt64 Program.Static::line
		IL_02F3: ldc.i8 1
		IL_02FC: add
		IL_02FD: stsfld System.UInt64 Program.Static::line
		IL_0302: br IL_0537
		IL_0307: nop
		IL_0308: ldsfld System.UInt64 Program.Static::tk
		IL_030D: ldc.i4.s 35
		IL_030F: ceq
		IL_0311: brfalse IL_0369
		IL_0316: nop
		IL_0317: br IL_0330
		IL_031C: ldsfld System.UInt64 Program.Static::p
		IL_0321: ldc.i8 1
		IL_032A: add
		IL_032B: stsfld System.UInt64 Program.Static::p
		IL_0330: ldsfld System.UInt64 Program.Static::p
		IL_0335: ldsfld System.String Program.Static::pval
		IL_033A: ldlen
		IL_033B: clt.un
		IL_033D: brfalse IL_0363
		IL_0342: ldsfld System.String Program.Static::pval
		IL_0347: ldsfld System.UInt64 Program.Static::p
		IL_034C: conv.i4
		IL_034D: call System.Char System.String::get_Chars(System.Int32)
		IL_0352: ldc.i4.s 92
		IL_0354: ceq
		IL_0356: ldc.i4.0
		IL_0357: ceq
		IL_0359: brfalse IL_0363
		IL_035E: br IL_031C
		IL_0363: nop
		IL_0364: br IL_0537
		IL_0369: nop
		IL_036A: ldsfld System.String Program.Static::pval
		IL_036F: ldsfld System.UInt64 Program.Static::p
		IL_0374: conv.i4
		IL_0375: call System.Char System.String::get_Chars(System.Int32)
		IL_037A: ldc.i4.s 97
		IL_037C: cgt.un
		IL_037E: ldc.i4.0
		IL_037F: ceq
		IL_0381: brfalse IL_0537
		IL_0386: ldsfld System.String Program.Static::pval
		IL_038B: ldsfld System.UInt64 Program.Static::p
		IL_0390: conv.i4
		IL_0391: call System.Char System.String::get_Chars(System.Int32)
		IL_0396: ldc.i4.s 122
		IL_0398: cgt.un
		IL_039A: ldc.i4.0
		IL_039B: ceq
		IL_039D: brfalse IL_0537
		IL_03A2: ldsfld System.String Program.Static::pval
		IL_03A7: ldsfld System.UInt64 Program.Static::p
		IL_03AC: conv.i4
		IL_03AD: call System.Char System.String::get_Chars(System.Int32)
		IL_03B2: ldc.i4.s 65
		IL_03B4: cgt.un
		IL_03B6: ldc.i4.0
		IL_03B7: ceq
		IL_03B9: brfalse IL_0537
		IL_03BE: ldsfld System.String Program.Static::pval
		IL_03C3: ldsfld System.UInt64 Program.Static::p
		IL_03C8: conv.i4
		IL_03C9: call System.Char System.String::get_Chars(System.Int32)
		IL_03CE: ldc.i4.s 90
		IL_03D0: cgt.un
		IL_03D2: ldc.i4.0
		IL_03D3: ceq
		IL_03D5: brfalse IL_0537
		IL_03DA: ldsfld System.String Program.Static::pval
		IL_03DF: ldsfld System.UInt64 Program.Static::p
		IL_03E4: conv.i4
		IL_03E5: call System.Char System.String::get_Chars(System.Int32)
		IL_03EA: ldc.i4.s 95
		IL_03EC: ceq
		IL_03EE: brtrue IL_03F3
		IL_03F3: nop
		IL_03F4: ldsfld System.UInt64 Program.Static::p
		IL_03F9: ldc.i8 1
		IL_0402: sub
		IL_0403: stloc V_0
		IL_0407: br IL_0442
		IL_040C: ldsfld System.UInt64 Program.Static::tk
		IL_0411: ldc.i8 147
		IL_041A: mul.ovf.un
		IL_041B: ldsfld System.String Program.Static::pval
		IL_0420: ldsfld System.UInt64 Program.Static::p
		IL_0425: dup
		IL_0426: ldc.i8 1
		IL_042F: add
		IL_0430: stsfld System.UInt64 Program.Static::p
		IL_0435: conv.i4
		IL_0436: call System.Char System.String::get_Chars(System.Int32)
		IL_043B: conv.u8
		IL_043C: add
		IL_043D: stsfld System.UInt64 Program.Static::tk
		IL_0442: ldsfld System.String Program.Static::pval
		IL_0447: ldsfld System.UInt64 Program.Static::p
		IL_044C: conv.i4
		IL_044D: call System.Char System.String::get_Chars(System.Int32)
		IL_0452: ldc.i4.s 97
		IL_0454: cgt.un
		IL_0456: ldc.i4.0
		IL_0457: ceq
		IL_0459: brfalse IL_0508
		IL_045E: ldsfld System.String Program.Static::pval
		IL_0463: ldsfld System.UInt64 Program.Static::p
		IL_0468: conv.i4
		IL_0469: call System.Char System.String::get_Chars(System.Int32)
		IL_046E: ldc.i4.s 122
		IL_0470: cgt.un
		IL_0472: ldc.i4.0
		IL_0473: ceq
		IL_0475: brfalse IL_0508
		IL_047A: ldsfld System.String Program.Static::pval
		IL_047F: ldsfld System.UInt64 Program.Static::p
		IL_0484: conv.i4
		IL_0485: call System.Char System.String::get_Chars(System.Int32)
		IL_048A: ldc.i4.s 65
		IL_048C: cgt.un
		IL_048E: ldc.i4.0
		IL_048F: ceq
		IL_0491: brfalse IL_0508
		IL_0496: ldsfld System.String Program.Static::pval
		IL_049B: ldsfld System.UInt64 Program.Static::p
		IL_04A0: conv.i4
		IL_04A1: call System.Char System.String::get_Chars(System.Int32)
		IL_04A6: ldc.i4.s 90
		IL_04A8: cgt.un
		IL_04AA: ldc.i4.0
		IL_04AB: ceq
		IL_04AD: brfalse IL_0508
		IL_04B2: ldsfld System.String Program.Static::pval
		IL_04B7: ldsfld System.UInt64 Program.Static::p
		IL_04BC: conv.i4
		IL_04BD: call System.Char System.String::get_Chars(System.Int32)
		IL_04C2: ldc.i4.s 48
		IL_04C4: cgt.un
		IL_04C6: ldc.i4.0
		IL_04C7: ceq
		IL_04C9: brfalse IL_0508
		IL_04CE: ldsfld System.String Program.Static::pval
		IL_04D3: ldsfld System.UInt64 Program.Static::p
		IL_04D8: conv.i4
		IL_04D9: call System.Char System.String::get_Chars(System.Int32)
		IL_04DE: ldc.i4.s 57
		IL_04E0: cgt.un
		IL_04E2: ldc.i4.0
		IL_04E3: ceq
		IL_04E5: brfalse IL_0508
		IL_04EA: ldsfld System.String Program.Static::pval
		IL_04EF: ldsfld System.UInt64 Program.Static::p
		IL_04F4: conv.i4
		IL_04F5: call System.Char System.String::get_Chars(System.Int32)
		IL_04FA: ldc.i4.s 95
		IL_04FC: ceq
		IL_04FE: brtrue IL_0442
		IL_0503: br IL_040C
		IL_0508: nop
		IL_0509: ldsfld System.UInt64 Program.Static::tk
		IL_050E: ldc.i8 64
		IL_0517: mul.ovf.un
		IL_0518: ldsfld System.UInt64 Program.Static::p
		IL_051D: ldloc V_0
		IL_0521: sub
		IL_0522: add
		IL_0523: stsfld System.UInt64 Program.Static::tk
		IL_0528: ldsfld System.Byte[] Program.Static::sym
		IL_052D: stsfld System.UInt64[] Program.Static::id
		IL_0532: br IL_0537
		IL_0537: nop
		IL_0538: ldsfld System.UInt64 Program.Static::p
		IL_053D: stsfld System.UInt64 Program.Static::tk
		IL_0542: ldsfld System.String Program.Static::pval
		IL_0547: ldsfld System.UInt64 Program.Static::p
		IL_054C: conv.i4
		IL_054D: call System.Char System.String::get_Chars(System.Int32)
		IL_0552: ldsfld System.String Program.Static::pval
		IL_0557: ldlen
		IL_0558: clt.un
		IL_055A: brtrue IL_001C
		IL_055F: nop
		IL_0560: ret
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
	field public instance System.Int64 value__
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
