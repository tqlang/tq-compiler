
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
		IL_0203: br IL_0242
		IL_0208: ldloc V_5
		IL_020C: conv.ovf.u8.un
		IL_020D: call System.Void Std.ConsoleStatic::writeln(System.UInt64)
		IL_0212: call System.Void Program.Static::next()
		IL_0217: ldsfld System.String[] Program.Static::id
		IL_021C: ldc.i8 0
		IL_0225: conv.u8
		IL_0226: conv.ovf.i4.un
		IL_0227: ldloc V_5
		IL_022B: stelem System.String
		IL_0230: ldloc V_5
		IL_0234: ldc.i8 1
		IL_023D: add
		IL_023E: stloc V_5
		IL_0242: ldloc V_5
		IL_0246: ldc.i8 0
		IL_024F: conv.i8
		IL_0250: cgt.un
		IL_0252: ldc.i4.0
		IL_0253: ceq
		IL_0255: brtrue IL_0208
		IL_025A: nop
		IL_025B: ldc.i8 0
		IL_0264: conv.i8
		IL_0265: stloc V_6
		IL_0269: br IL_02A8
		IL_026E: ldloc V_6
		IL_0272: conv.ovf.u8.un
		IL_0273: call System.Void Std.ConsoleStatic::writeln(System.UInt64)
		IL_0278: call System.Void Program.Static::next()
		IL_027D: ldsfld System.String[] Program.Static::id
		IL_0282: ldc.i8 3
		IL_028B: conv.u8
		IL_028C: conv.ovf.i4.un
		IL_028D: ldloc V_6
		IL_0291: stelem System.String
		IL_0296: ldloc V_6
		IL_029A: ldc.i8 1
		IL_02A3: add
		IL_02A4: stloc V_6
		IL_02A8: ldloc V_6
		IL_02AC: ldc.i8 0
		IL_02B5: conv.i8
		IL_02B6: cgt.un
		IL_02B8: ldc.i4.0
		IL_02B9: ceq
		IL_02BB: brtrue IL_026E
		IL_02C0: nop
		IL_02C1: ldstr "Not bad until here"
		IL_02C6: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_02CB: ret
	}

	method private static next () System.Void {
		.locals init (System.UInt64 V_0)
		.locals init (System.Int64 V_1)
		.locals init (System.Char V_2)
		.locals init (System.String[] V_3)
		IL_0000: ldc.i8 0
		IL_0009: stloc V_0
		IL_000D: ldsfld System.UInt64 Program.Static::p
		IL_0012: stsfld System.UInt64 Program.Static::tk
		IL_0017: br IL_066A
		IL_001C: ldsfld System.UInt64 Program.Static::p
		IL_0021: ldc.i8 1
		IL_002A: add
		IL_002B: stsfld System.UInt64 Program.Static::p
		IL_0030: ldc.i8 0
		IL_0039: stloc V_1
		IL_003D: ldstr "p: "
		IL_0042: call System.Void Std.ConsoleStatic::write(System.String)
		IL_0047: ldsfld System.UInt64 Program.Static::p
		IL_004C: conv.ovf.u8.un
		IL_004D: call System.Void Std.ConsoleStatic::write(System.UInt64)
		IL_0052: ldstr " pval: "
		IL_0057: call System.Void Std.ConsoleStatic::write(System.String)
		IL_005C: ldsfld System.String Program.Static::pval
		IL_0061: ldloc V_1
		IL_0065: conv.ovf.i4.un
		IL_0066: ldsfld System.UInt64 Program.Static::p
		IL_006B: ldloc V_1
		IL_006F: sub.ovf
		IL_0070: conv.ovf.i4.un
		IL_0071: call System.String System.String::Substring(System.Int32, System.Int32)
		IL_0076: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_007B: ldsfld System.String Program.Static::pval
		IL_0080: ldsfld System.UInt64 Program.Static::p
		IL_0085: conv.i4
		IL_0086: call System.Char System.String::get_Chars(System.Int32)
		IL_008B: stloc V_2
		IL_008F: ldsfld System.String Program.Static::pval
		IL_0094: ldsfld System.UInt64 Program.Static::tk
		IL_0099: conv.i4
		IL_009A: call System.Char System.String::get_Chars(System.Int32)
		IL_009F: ldc.i4.s 92
		IL_00A1: ceq
		IL_00A3: brfalse IL_039C
		IL_00A8: nop
		IL_00A9: ldsfld System.Int64 Program.Static::src
		IL_00AE: ldc.i8 0
		IL_00B7: ceq
		IL_00B9: ldc.i4.0
		IL_00BA: ceq
		IL_00BC: brfalse IL_0382
		IL_00C1: nop
		IL_00C2: ldsfld System.UInt64 Program.Static::line
		IL_00C7: conv.ovf.u8.un
		IL_00C8: call System.Void Std.ConsoleStatic::write(System.UInt64)
		IL_00CD: ldstr ": "
		IL_00D2: call System.Void Std.ConsoleStatic::write(System.String)
		IL_00D7: ldsfld System.String Program.Static::pval
		IL_00DC: ldsfld System.UInt64 Program.Static::lp
		IL_00E1: conv.ovf.i4.un
		IL_00E2: ldsfld System.UInt64 Program.Static::p
		IL_00E7: ldsfld System.UInt64 Program.Static::lp
		IL_00EC: sub.ovf
		IL_00ED: conv.ovf.i4.un
		IL_00EE: call System.String System.String::Substring(System.Int32, System.Int32)
		IL_00F3: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_00F8: ldsfld System.UInt64 Program.Static::p
		IL_00FD: stsfld System.UInt64 Program.Static::lp
		IL_0102: br IL_036B
		IL_0107: ldc.i4.s 39
		IL_0109: newarr System.String
		IL_010E: dup
		IL_010F: ldc.i4.0
		IL_0110: ldstr "LEA"
		IL_0115: stelem System.String
		IL_011A: dup
		IL_011B: ldc.i4.1
		IL_011C: ldstr "IMM"
		IL_0121: stelem System.String
		IL_0126: dup
		IL_0127: ldc.i4.2
		IL_0128: ldstr "JMP"
		IL_012D: stelem System.String
		IL_0132: dup
		IL_0133: ldc.i4.3
		IL_0134: ldstr "JSR"
		IL_0139: stelem System.String
		IL_013E: dup
		IL_013F: ldc.i4.4
		IL_0140: ldstr "BZ"
		IL_0145: stelem System.String
		IL_014A: dup
		IL_014B: ldc.i4.5
		IL_014C: ldstr "BNZ"
		IL_0151: stelem System.String
		IL_0156: dup
		IL_0157: ldc.i4.6
		IL_0158: ldstr "ENT"
		IL_015D: stelem System.String
		IL_0162: dup
		IL_0163: ldc.i4.7
		IL_0164: ldstr "ADJ"
		IL_0169: stelem System.String
		IL_016E: dup
		IL_016F: ldc.i4.8
		IL_0170: ldstr "LEV"
		IL_0175: stelem System.String
		IL_017A: dup
		IL_017B: ldc.i4.s 9
		IL_017D: ldstr "LI"
		IL_0182: stelem System.String
		IL_0187: dup
		IL_0188: ldc.i4.s 10
		IL_018A: ldstr "LC"
		IL_018F: stelem System.String
		IL_0194: dup
		IL_0195: ldc.i4.s 11
		IL_0197: ldstr "SI"
		IL_019C: stelem System.String
		IL_01A1: dup
		IL_01A2: ldc.i4.s 12
		IL_01A4: ldstr "SC"
		IL_01A9: stelem System.String
		IL_01AE: dup
		IL_01AF: ldc.i4.s 13
		IL_01B1: ldstr "PSH"
		IL_01B6: stelem System.String
		IL_01BB: dup
		IL_01BC: ldc.i4.s 14
		IL_01BE: ldstr "OR"
		IL_01C3: stelem System.String
		IL_01C8: dup
		IL_01C9: ldc.i4.s 15
		IL_01CB: ldstr "XOR"
		IL_01D0: stelem System.String
		IL_01D5: dup
		IL_01D6: ldc.i4.s 16
		IL_01D8: ldstr "AND"
		IL_01DD: stelem System.String
		IL_01E2: dup
		IL_01E3: ldc.i4.s 17
		IL_01E5: ldstr "EQ"
		IL_01EA: stelem System.String
		IL_01EF: dup
		IL_01F0: ldc.i4.s 18
		IL_01F2: ldstr "NE"
		IL_01F7: stelem System.String
		IL_01FC: dup
		IL_01FD: ldc.i4.s 19
		IL_01FF: ldstr "LT"
		IL_0204: stelem System.String
		IL_0209: dup
		IL_020A: ldc.i4.s 20
		IL_020C: ldstr "GT"
		IL_0211: stelem System.String
		IL_0216: dup
		IL_0217: ldc.i4.s 21
		IL_0219: ldstr "LE"
		IL_021E: stelem System.String
		IL_0223: dup
		IL_0224: ldc.i4.s 22
		IL_0226: ldstr "GE"
		IL_022B: stelem System.String
		IL_0230: dup
		IL_0231: ldc.i4.s 23
		IL_0233: ldstr "SHL"
		IL_0238: stelem System.String
		IL_023D: dup
		IL_023E: ldc.i4.s 24
		IL_0240: ldstr "SHR"
		IL_0245: stelem System.String
		IL_024A: dup
		IL_024B: ldc.i4.s 25
		IL_024D: ldstr "ADD"
		IL_0252: stelem System.String
		IL_0257: dup
		IL_0258: ldc.i4.s 26
		IL_025A: ldstr "SUB"
		IL_025F: stelem System.String
		IL_0264: dup
		IL_0265: ldc.i4.s 27
		IL_0267: ldstr "MUL"
		IL_026C: stelem System.String
		IL_0271: dup
		IL_0272: ldc.i4.s 28
		IL_0274: ldstr "DIV"
		IL_0279: stelem System.String
		IL_027E: dup
		IL_027F: ldc.i4.s 29
		IL_0281: ldstr "MOD"
		IL_0286: stelem System.String
		IL_028B: dup
		IL_028C: ldc.i4.s 30
		IL_028E: ldstr "OPEN"
		IL_0293: stelem System.String
		IL_0298: dup
		IL_0299: ldc.i4.s 31
		IL_029B: ldstr "READ"
		IL_02A0: stelem System.String
		IL_02A5: dup
		IL_02A6: ldc.i4.s 32
		IL_02A8: ldstr "CLOS"
		IL_02AD: stelem System.String
		IL_02B2: dup
		IL_02B3: ldc.i4.s 33
		IL_02B5: ldstr "PRTF"
		IL_02BA: stelem System.String
		IL_02BF: dup
		IL_02C0: ldc.i4.s 34
		IL_02C2: ldstr "MALC"
		IL_02C7: stelem System.String
		IL_02CC: dup
		IL_02CD: ldc.i4.s 35
		IL_02CF: ldstr "FREE"
		IL_02D4: stelem System.String
		IL_02D9: dup
		IL_02DA: ldc.i4.s 36
		IL_02DC: ldstr "MSET"
		IL_02E1: stelem System.String
		IL_02E6: dup
		IL_02E7: ldc.i4.s 37
		IL_02E9: ldstr "MCMP"
		IL_02EE: stelem System.String
		IL_02F3: dup
		IL_02F4: ldc.i4.s 38
		IL_02F6: ldstr "EXIT"
		IL_02FB: stelem System.String
		IL_0300: stloc V_3
		IL_0304: ldloc V_3
		IL_0308: ldsfld System.UInt64 Program.Static::lePtr
		IL_030D: ldc.i8 1
		IL_0316: add
		IL_0317: dup
		IL_0318: stsfld System.UInt64 Program.Static::lePtr
		IL_031D: conv.i4
		IL_031E: ldelem System.String
		IL_0323: call System.Void Std.ConsoleStatic::write(System.String)
		IL_0328: ldsfld System.UInt64 Program.Static::lePtr
		IL_032D: ldc.i8 0
		IL_0336: conv.i8
		IL_0337: clt.un
		IL_0339: brfalse IL_035F
		IL_033E: nop
		IL_033F: ldsfld System.UInt64 Program.Static::lePtr
		IL_0344: ldc.i8 1
		IL_034D: add
		IL_034E: dup
		IL_034F: stsfld System.UInt64 Program.Static::lePtr
		IL_0354: conv.ovf.u8.un
		IL_0355: call System.Void Std.ConsoleStatic::writeln(System.UInt64)
		IL_035A: br IL_036A
		IL_035F: nop
		IL_0360: ldstr ""
		IL_0365: call System.Void Std.ConsoleStatic::writeln(System.String)
		IL_036A: nop
		IL_036B: ldsfld System.UInt64 Program.Static::lePtr
		IL_0370: ldsfld System.UInt64 Program.Static::e
		IL_0375: clt.un
		IL_0377: brtrue IL_0107
		IL_037C: nop
		IL_037D: br IL_0382
		IL_0382: nop
		IL_0383: ldsfld System.UInt64 Program.Static::line
		IL_0388: ldc.i8 1
		IL_0391: add
		IL_0392: stsfld System.UInt64 Program.Static::line
		IL_0397: br IL_065F
		IL_039C: nop
		IL_039D: ldsfld System.UInt64 Program.Static::tk
		IL_03A2: ldc.i4.s 35
		IL_03A4: ceq
		IL_03A6: brfalse IL_0404
		IL_03AB: nop
		IL_03AC: br IL_03C5
		IL_03B1: ldsfld System.UInt64 Program.Static::p
		IL_03B6: ldc.i8 1
		IL_03BF: add
		IL_03C0: stsfld System.UInt64 Program.Static::p
		IL_03C5: ldsfld System.UInt64 Program.Static::p
		IL_03CA: ldsfld System.String Program.Static::pval
		IL_03CF: ldlen
		IL_03D0: clt.un
		IL_03D2: brfalse IL_03FE
		IL_03D7: nop
		IL_03D8: ldsfld System.String Program.Static::pval
		IL_03DD: ldsfld System.UInt64 Program.Static::p
		IL_03E2: conv.i4
		IL_03E3: call System.Char System.String::get_Chars(System.Int32)
		IL_03E8: ldc.i4.s 92
		IL_03EA: ceq
		IL_03EC: ldc.i4.0
		IL_03ED: ceq
		IL_03EF: brfalse IL_03FE
		IL_03F4: br IL_03B1
		IL_03F9: br IL_03B1
		IL_03FE: nop
		IL_03FF: br IL_065F
		IL_0404: nop
		IL_0405: ldloc V_2
		IL_0409: ldc.i4.s 97
		IL_040B: clt.un
		IL_040D: ldc.i4.0
		IL_040E: ceq
		IL_0410: brfalse IL_042B
		IL_0415: nop
		IL_0416: ldloc V_2
		IL_041A: ldc.i4.s 122
		IL_041C: cgt.un
		IL_041E: ldc.i4.0
		IL_041F: ceq
		IL_0421: brfalse IL_042B
		IL_0426: br IL_0465
		IL_042B: nop
		IL_042C: ldloc V_2
		IL_0430: ldc.i4.s 65
		IL_0432: clt.un
		IL_0434: ldc.i4.0
		IL_0435: ceq
		IL_0437: brfalse IL_0452
		IL_043C: nop
		IL_043D: ldloc V_2
		IL_0441: ldc.i4.s 90
		IL_0443: cgt.un
		IL_0445: ldc.i4.0
		IL_0446: ceq
		IL_0448: brfalse IL_0452
		IL_044D: br IL_0465
		IL_0452: nop
		IL_0453: ldloc V_2
		IL_0457: ldc.i4.s 95
		IL_0459: ceq
		IL_045B: brtrue IL_0465
		IL_0460: br IL_065F
		IL_0465: nop
		IL_0466: ldsfld System.UInt64 Program.Static::p
		IL_046B: ldc.i8 1
		IL_0474: sub
		IL_0475: stloc V_0
		IL_0479: br IL_04B4
		IL_047E: ldsfld System.UInt64 Program.Static::tk
		IL_0483: ldc.i8 147
		IL_048C: mul.ovf.un
		IL_048D: ldsfld System.String Program.Static::pval
		IL_0492: ldsfld System.UInt64 Program.Static::p
		IL_0497: dup
		IL_0498: ldc.i8 1
		IL_04A1: add
		IL_04A2: stsfld System.UInt64 Program.Static::p
		IL_04A7: conv.i4
		IL_04A8: call System.Char System.String::get_Chars(System.Int32)
		IL_04AD: conv.u8
		IL_04AE: add
		IL_04AF: stsfld System.UInt64 Program.Static::tk
		IL_04B4: ldsfld System.String Program.Static::pval
		IL_04B9: ldsfld System.UInt64 Program.Static::p
		IL_04BE: conv.i4
		IL_04BF: call System.Char System.String::get_Chars(System.Int32)
		IL_04C4: ldc.i4.s 97
		IL_04C6: clt.un
		IL_04C8: ldc.i4.0
		IL_04C9: ceq
		IL_04CB: brfalse IL_04F2
		IL_04D0: nop
		IL_04D1: ldsfld System.String Program.Static::pval
		IL_04D6: ldsfld System.UInt64 Program.Static::p
		IL_04DB: conv.i4
		IL_04DC: call System.Char System.String::get_Chars(System.Int32)
		IL_04E1: ldc.i4.s 122
		IL_04E3: cgt.un
		IL_04E5: ldc.i4.0
		IL_04E6: ceq
		IL_04E8: brfalse IL_04F2
		IL_04ED: br IL_047E
		IL_04F2: nop
		IL_04F3: ldsfld System.String Program.Static::pval
		IL_04F8: ldsfld System.UInt64 Program.Static::p
		IL_04FD: conv.i4
		IL_04FE: call System.Char System.String::get_Chars(System.Int32)
		IL_0503: ldc.i4.s 65
		IL_0505: clt.un
		IL_0507: ldc.i4.0
		IL_0508: ceq
		IL_050A: brfalse IL_0531
		IL_050F: nop
		IL_0510: ldsfld System.String Program.Static::pval
		IL_0515: ldsfld System.UInt64 Program.Static::p
		IL_051A: conv.i4
		IL_051B: call System.Char System.String::get_Chars(System.Int32)
		IL_0520: ldc.i4.s 90
		IL_0522: cgt.un
		IL_0524: ldc.i4.0
		IL_0525: ceq
		IL_0527: brfalse IL_0531
		IL_052C: br IL_047E
		IL_0531: nop
		IL_0532: ldsfld System.String Program.Static::pval
		IL_0537: ldsfld System.UInt64 Program.Static::p
		IL_053C: conv.i4
		IL_053D: call System.Char System.String::get_Chars(System.Int32)
		IL_0542: ldc.i4.s 48
		IL_0544: clt.un
		IL_0546: ldc.i4.0
		IL_0547: ceq
		IL_0549: brfalse IL_0570
		IL_054E: nop
		IL_054F: ldsfld System.String Program.Static::pval
		IL_0554: ldsfld System.UInt64 Program.Static::p
		IL_0559: conv.i4
		IL_055A: call System.Char System.String::get_Chars(System.Int32)
		IL_055F: ldc.i4.s 57
		IL_0561: cgt.un
		IL_0563: ldc.i4.0
		IL_0564: ceq
		IL_0566: brfalse IL_0570
		IL_056B: br IL_047E
		IL_0570: nop
		IL_0571: ldsfld System.String Program.Static::pval
		IL_0576: ldsfld System.UInt64 Program.Static::p
		IL_057B: conv.i4
		IL_057C: call System.Char System.String::get_Chars(System.Int32)
		IL_0581: ldc.i4.s 95
		IL_0583: ceq
		IL_0585: brtrue IL_047E
		IL_058A: br IL_0594
		IL_058F: br IL_047E
		IL_0594: nop
		IL_0595: ldsfld System.UInt64 Program.Static::tk
		IL_059A: ldc.i8 64
		IL_05A3: mul.ovf.un
		IL_05A4: ldsfld System.UInt64 Program.Static::p
		IL_05A9: ldloc V_0
		IL_05AD: sub
		IL_05AE: add
		IL_05AF: stsfld System.UInt64 Program.Static::tk
		IL_05B4: ldsfld System.Byte[] Program.Static::sym
		IL_05B9: stsfld System.String[] Program.Static::id
		IL_05BE: br IL_0643
		IL_05C3: ldsfld System.UInt64 Program.Static::tk
		IL_05C8: ldsfld System.String[] Program.Static::id
		IL_05CD: ldc.i8 1
		IL_05D6: conv.i4
		IL_05D7: ldelem System.String
		IL_05DC: ceq
		IL_05DE: brfalse IL_0642
		IL_05E3: nop
		IL_05E4: ldsfld System.String[] Program.Static::id
		IL_05E9: ldc.i8 2
		IL_05F2: conv.i4
		IL_05F3: ldelem System.String
		IL_05F8: ldsfld System.String Program.Static::pval
		IL_05FD: ldsfld System.UInt64 Program.Static::p
		IL_0602: conv.ovf.i4.un
		IL_0603: ldloc V_0
		IL_0607: ldsfld System.UInt64 Program.Static::p
		IL_060C: sub.ovf
		IL_060D: conv.ovf.i4.un
		IL_060E: call System.String System.String::Substring(System.Int32, System.Int32)
		IL_0613: call System.Boolean System.String::Equals(System.String, System.String)
		IL_0618: brfalse IL_0642
		IL_061D: br IL_0622
		IL_0622: nop
		IL_0623: ldsfld System.String[] Program.Static::id
		IL_0628: ldc.i8 0
		IL_0631: conv.i4
		IL_0632: ldelem System.String
		IL_0637: stsfld System.UInt64 Program.Static::tk
		IL_063C: ret
		IL_063D: br IL_0642
		IL_0642: nop
		IL_0643: ldsfld System.String[] Program.Static::id
		IL_0648: ldlen
		IL_0649: ldc.i8 0
		IL_0652: clt.un
		IL_0654: brtrue IL_05C3
		IL_0659: nop
		IL_065A: br IL_065F
		IL_065F: nop
		IL_0660: ldsfld System.UInt64 Program.Static::p
		IL_0665: stsfld System.UInt64 Program.Static::tk
		IL_066A: ldsfld System.UInt64 Program.Static::p
		IL_066F: ldsfld System.String Program.Static::pval
		IL_0674: ldlen
		IL_0675: clt.un
		IL_0677: brtrue IL_001C
		IL_067C: nop
		IL_067D: ret
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
