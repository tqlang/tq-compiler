
class <module>.RuntimeHelpers extends System.Object {

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

	method private static SubSaturatedI8 (System.SByte A_0, System.SByte A_1) System.SByte {
		IL_0000: ldarg.0
		IL_0000: ldarg.1
		IL_0000: sub
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

	method private static SubSaturatedU8 (System.Byte A_0, System.Byte A_1) System.Byte {
		IL_0000: ldarg.0
		IL_0000: ldarg.1
		IL_0000: sub
		IL_0000: dup
		IL_0000: ldc.i4 0
		IL_0000: blt IL_0000
		IL_0000: conv.u1
		IL_0000: ret
		IL_0000: pop
		IL_0000: ldc.i4.s 0
		IL_0000: ret
	}

	method private static _start () System.Void {
		IL_0000: call System.Void Program.Program::main()
		IL_0000: leave IL_0000
		IL_0000: pop
		IL_0000: ldstr "!!! Panic !!!\n"
		IL_0000: call System.Void System.Console::Write(System.String)
		IL_0000: ldstr "Error: FIXME\n"
		IL_0000: call System.Void System.Console::Write(System.String)
		IL_0000: ldc.i4.1
		IL_0000: call System.Void System.Environment::Exit(System.Int32)
		IL_0000: leave IL_0000
		IL_0000: ret
	}
}

class Program.Program extends System.Object {

	method public static main () System.Void {
		IL_0000: call System.Void Program.Program::testBuiltins()
		IL_0005: call System.Void Program.Program::testArithmeticU8()
		IL_000A: call System.Void Program.Program::testArithmeticI8()
		IL_000F: ret
	}

	method private static testArithmeticU8 () System.Void {
		.locals init (System.Byte V_0)
		.locals init (System.Byte V_1)
		.locals init (System.Byte V_2)
		.locals init (System.Byte V_3)
		.locals init (System.Byte V_4)
		.locals init (System.Byte V_5)
		.locals init (System.Byte V_6)
		.locals init (System.Byte V_7)
		.locals init (System.Byte V_8)
		IL_0000: ldc.i4.0
		IL_0001: stloc V_0
		IL_0005: ldc.i4.1
		IL_0006: stloc V_1
		IL_000A: ldc.i4.2
		IL_000B: stloc V_2
		IL_000F: ldc.i4.3
		IL_0010: stloc V_3
		IL_0014: ldc.i4.s 100
		IL_0016: stloc V_4
		IL_001A: ldc.i4.s 101
		IL_001C: stloc V_5
		IL_0020: ldc.i4 255
		IL_0025: stloc V_6
		IL_0029: ldc.i4.0
		IL_002A: stloc V_7
		IL_002E: ldc.i4 255
		IL_0033: stloc V_8
		IL_0037: ldstr "u8 arithmetic - checked add"
		IL_003C: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_0041: ldloc V_1
		IL_0045: ldloc V_1
		IL_0049: add
		IL_004A: conv.ovf.u1
		IL_004B: ldc.i4.2
		IL_004C: conv.ovf.u1.un
		IL_004D: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_0052: ldloc V_2
		IL_0056: ldloc V_2
		IL_005A: add
		IL_005B: conv.ovf.u1
		IL_005C: ldc.i4.4
		IL_005D: conv.ovf.u1.un
		IL_005E: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_0063: ldloc V_8
		IL_0067: ldc.i4.0
		IL_0068: add
		IL_0069: conv.ovf.u1
		IL_006A: ldloc V_8
		IL_006E: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_0073: ldstr "u8 arithmetic - add with overflow"
		IL_0078: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_007D: ldloc V_1
		IL_0081: ldloc V_1
		IL_0085: add
		IL_0086: conv.u1
		IL_0087: ldc.i4.2
		IL_0088: conv.ovf.u1.un
		IL_0089: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_008E: ldloc V_2
		IL_0092: ldloc V_2
		IL_0096: add
		IL_0097: conv.u1
		IL_0098: ldc.i4.4
		IL_0099: conv.ovf.u1.un
		IL_009A: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_009F: ldloc V_8
		IL_00A3: ldloc V_0
		IL_00A7: add
		IL_00A8: conv.u1
		IL_00A9: ldloc V_8
		IL_00AD: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_00B2: ldloc V_8
		IL_00B6: ldloc V_1
		IL_00BA: add
		IL_00BB: conv.u1
		IL_00BC: ldloc V_7
		IL_00C0: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_00C5: ldloc V_8
		IL_00C9: ldloc V_4
		IL_00CD: add
		IL_00CE: conv.u1
		IL_00CF: ldc.i4.s 99
		IL_00D1: conv.ovf.u1.un
		IL_00D2: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_00D7: ldstr "u8 arithmetic - add saturated"
		IL_00DC: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_00E1: ldloc V_1
		IL_00E5: ldloc V_1
		IL_00E9: call System.Byte <module>.RuntimeHelpers::AddSaturatedU8(System.Byte, System.Byte)
		IL_00EE: ldc.i4.2
		IL_00EF: conv.ovf.u1.un
		IL_00F0: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_00F5: ldloc V_2
		IL_00F9: ldloc V_2
		IL_00FD: call System.Byte <module>.RuntimeHelpers::AddSaturatedU8(System.Byte, System.Byte)
		IL_0102: ldc.i4.4
		IL_0103: conv.ovf.u1.un
		IL_0104: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_0109: ldloc V_8
		IL_010D: ldloc V_0
		IL_0111: call System.Byte <module>.RuntimeHelpers::AddSaturatedU8(System.Byte, System.Byte)
		IL_0116: ldloc V_8
		IL_011A: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_011F: ldloc V_8
		IL_0123: ldloc V_1
		IL_0127: call System.Byte <module>.RuntimeHelpers::AddSaturatedU8(System.Byte, System.Byte)
		IL_012C: ldloc V_8
		IL_0130: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_0135: ldloc V_8
		IL_0139: ldloc V_4
		IL_013D: call System.Byte <module>.RuntimeHelpers::AddSaturatedU8(System.Byte, System.Byte)
		IL_0142: ldloc V_8
		IL_0146: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_014B: ldstr "u8 arithmetic - checked sub"
		IL_0150: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_0155: ldloc V_1
		IL_0159: ldloc V_1
		IL_015D: sub
		IL_015E: conv.ovf.u1
		IL_015F: ldc.i4.0
		IL_0160: conv.ovf.u1.un
		IL_0161: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_0166: ldloc V_2
		IL_016A: ldloc V_1
		IL_016E: sub
		IL_016F: conv.ovf.u1
		IL_0170: ldc.i4.1
		IL_0171: conv.ovf.u1.un
		IL_0172: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_0177: ldc.i4.0
		IL_0178: conv.ovf.u1.un
		IL_0179: ldc.i4.0
		IL_017A: conv.ovf.u1.un
		IL_017B: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_0180: ldstr "u8 arithmetic - sub with overflow"
		IL_0185: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_018A: ldloc V_1
		IL_018E: ldloc V_1
		IL_0192: sub
		IL_0193: conv.u1
		IL_0194: ldc.i4.0
		IL_0195: conv.ovf.u1.un
		IL_0196: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_019B: ldloc V_2
		IL_019F: ldloc V_1
		IL_01A3: sub
		IL_01A4: conv.u1
		IL_01A5: ldc.i4.1
		IL_01A6: conv.ovf.u1.un
		IL_01A7: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_01AC: ldc.i4.0
		IL_01AD: conv.ovf.u1.un
		IL_01AE: ldc.i4.0
		IL_01AF: conv.ovf.u1.un
		IL_01B0: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_01B5: ldloc V_1
		IL_01B9: ldloc V_2
		IL_01BD: sub
		IL_01BE: conv.u1
		IL_01BF: ldc.i4 255
		IL_01C4: conv.ovf.u1.un
		IL_01C5: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_01CA: ldloc V_0
		IL_01CE: ldc.i4.s 100
		IL_01D0: sub
		IL_01D1: conv.u1
		IL_01D2: ldc.i4 156
		IL_01D7: conv.ovf.u1.un
		IL_01D8: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_01DD: ldstr "u8 arithmetic - sub saturated"
		IL_01E2: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_01E7: ldloc V_1
		IL_01EB: ldloc V_1
		IL_01EF: call System.Byte <module>.RuntimeHelpers::SubSaturatedU8(System.Byte, System.Byte)
		IL_01F4: ldc.i4.0
		IL_01F5: conv.ovf.u1.un
		IL_01F6: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_01FB: ldloc V_2
		IL_01FF: ldloc V_1
		IL_0203: call System.Byte <module>.RuntimeHelpers::SubSaturatedU8(System.Byte, System.Byte)
		IL_0208: ldc.i4.1
		IL_0209: conv.ovf.u1.un
		IL_020A: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_020F: ldc.i4.0
		IL_0210: conv.ovf.u1.un
		IL_0211: ldc.i4.0
		IL_0212: conv.ovf.u1.un
		IL_0213: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_0218: ldloc V_1
		IL_021C: ldloc V_2
		IL_0220: call System.Byte <module>.RuntimeHelpers::SubSaturatedU8(System.Byte, System.Byte)
		IL_0225: ldc.i4.0
		IL_0226: conv.ovf.u1.un
		IL_0227: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_022C: ldloc V_0
		IL_0230: ldc.i4.s 100
		IL_0232: call System.Byte <module>.RuntimeHelpers::SubSaturatedU8(System.Byte, System.Byte)
		IL_0237: ldc.i4.0
		IL_0238: conv.ovf.u1.un
		IL_0239: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_023E: ldstr "u8 arithmetic - mul checked"
		IL_0243: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_0248: ldloc V_1
		IL_024C: ldloc V_1
		IL_0250: mul.ovf.un
		IL_0251: ldc.i4.1
		IL_0252: conv.ovf.u1.un
		IL_0253: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_0258: ldloc V_2
		IL_025C: ldloc V_2
		IL_0260: mul.ovf.un
		IL_0261: ldc.i4.4
		IL_0262: conv.ovf.u1.un
		IL_0263: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_0268: ldloc V_2
		IL_026C: ldloc V_4
		IL_0270: mul.ovf.un
		IL_0271: ldc.i4 200
		IL_0276: conv.ovf.u1.un
		IL_0277: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_027C: ldloc V_0
		IL_0280: ldloc V_4
		IL_0284: mul.ovf.un
		IL_0285: ldc.i4.0
		IL_0286: conv.ovf.u1.un
		IL_0287: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_028C: ldstr "u8 arithmetic - mul with overflow - SKIPPED"
		IL_0291: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_0296: ldstr "u8 arithmetic - mul saturated - SKIPPED"
		IL_029B: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_02A0: ldstr "u8 arithmetic - div checked"
		IL_02A5: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_02AA: ldloc V_2
		IL_02AE: ldloc V_1
		IL_02B2: div.un
		IL_02B3: ldc.i4.2
		IL_02B4: conv.ovf.u1.un
		IL_02B5: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_02BA: ldloc V_4
		IL_02BE: ldloc V_2
		IL_02C2: div.un
		IL_02C3: ldc.i4.s 50
		IL_02C5: conv.ovf.u1.un
		IL_02C6: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_02CB: ldloc V_0
		IL_02CF: ldloc V_4
		IL_02D3: div.un
		IL_02D4: ldc.i4.0
		IL_02D5: conv.ovf.u1.un
		IL_02D6: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_02DB: ldstr "u8 arithmetic - div floor - SKIPPED"
		IL_02E0: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_02E5: ldstr "u8 arithmetic - div ceil - SKIPPED"
		IL_02EA: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_02EF: ldstr "u8 arithmetic - reminder"
		IL_02F4: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_02F9: ldloc V_2
		IL_02FD: ldloc V_1
		IL_0301: rem.un
		IL_0302: ldc.i4.0
		IL_0303: conv.ovf.u1.un
		IL_0304: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_0309: ldloc V_4
		IL_030D: ldloc V_2
		IL_0311: rem.un
		IL_0312: ldc.i4.0
		IL_0313: conv.ovf.u1.un
		IL_0314: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_0319: ldloc V_5
		IL_031D: ldloc V_2
		IL_0321: rem.un
		IL_0322: ldc.i4.1
		IL_0323: conv.ovf.u1.un
		IL_0324: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_0329: ldloc V_6
		IL_032D: ldloc V_2
		IL_0331: rem.un
		IL_0332: ldc.i4.1
		IL_0333: conv.ovf.u1.un
		IL_0334: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_0339: ldloc V_0
		IL_033D: ldloc V_4
		IL_0341: rem.un
		IL_0342: ldc.i4.0
		IL_0343: conv.ovf.u1.un
		IL_0344: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_0349: ldstr "u8 arithmetic - pow checked - SKIPPED"
		IL_034E: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_0353: ldstr "u8 arithmetic - pow with overflow - SKIPPED"
		IL_0358: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_035D: ldstr "u8 arithmetic - pow saturated - SKIPPED"
		IL_0362: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_0367: ret
	}

	method private static testArithmeticI8 () System.Void {
		.locals init (System.SByte V_0)
		.locals init (System.SByte V_1)
		.locals init (System.SByte V_2)
		.locals init (System.SByte V_3)
		.locals init (System.SByte V_4)
		.locals init (System.SByte V_5)
		.locals init (System.SByte V_6)
		.locals init (System.SByte V_7)
		.locals init (System.SByte V_8)
		IL_0000: ldc.i4.0
		IL_0001: stloc V_0
		IL_0005: ldc.i4.1
		IL_0006: stloc V_1
		IL_000A: ldc.i4.2
		IL_000B: stloc V_2
		IL_000F: ldc.i4.3
		IL_0010: stloc V_3
		IL_0014: ldc.i4.s 100
		IL_0016: stloc V_4
		IL_001A: ldc.i4.s 101
		IL_001C: stloc V_5
		IL_0020: ldc.i4.s 127
		IL_0022: stloc V_6
		IL_0026: ldc.i4.s -128
		IL_0028: stloc V_7
		IL_002C: ldc.i4.s 127
		IL_002E: stloc V_8
		IL_0032: ldstr "i8 arithmetic - checked add"
		IL_0037: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_003C: ldloc V_1
		IL_0040: ldloc V_1
		IL_0044: add
		IL_0045: conv.ovf.i1
		IL_0046: ldc.i4.2
		IL_0047: conv.ovf.i1
		IL_0048: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_004D: ldloc V_2
		IL_0051: ldloc V_2
		IL_0055: add
		IL_0056: conv.ovf.i1
		IL_0057: ldc.i4.4
		IL_0058: conv.ovf.i1
		IL_0059: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_005E: ldloc V_8
		IL_0062: ldc.i4.0
		IL_0063: add
		IL_0064: conv.ovf.i1
		IL_0065: ldloc V_8
		IL_0069: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_006E: ldstr "i8 arithmetic - add with overflow"
		IL_0073: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_0078: ldloc V_1
		IL_007C: ldloc V_1
		IL_0080: add
		IL_0081: conv.i1
		IL_0082: ldc.i4.2
		IL_0083: conv.ovf.i1
		IL_0084: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_0089: ldloc V_2
		IL_008D: ldloc V_2
		IL_0091: add
		IL_0092: conv.i1
		IL_0093: ldc.i4.4
		IL_0094: conv.ovf.i1
		IL_0095: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_009A: ldloc V_8
		IL_009E: ldloc V_0
		IL_00A2: add
		IL_00A3: conv.i1
		IL_00A4: ldloc V_8
		IL_00A8: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_00AD: ldloc V_8
		IL_00B1: ldloc V_1
		IL_00B5: add
		IL_00B6: conv.i1
		IL_00B7: ldloc V_7
		IL_00BB: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_00C0: ldloc V_8
		IL_00C4: ldloc V_4
		IL_00C8: add
		IL_00C9: conv.i1
		IL_00CA: ldc.i4.s -29
		IL_00CC: conv.ovf.i1
		IL_00CD: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_00D2: ldstr "i8 arithmetic - add saturated"
		IL_00D7: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_00DC: ldloc V_1
		IL_00E0: ldloc V_1
		IL_00E4: call System.SByte <module>.RuntimeHelpers::AddSaturatedI8(System.SByte, System.SByte)
		IL_00E9: ldc.i4.2
		IL_00EA: conv.ovf.i1
		IL_00EB: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_00F0: ldloc V_2
		IL_00F4: ldloc V_2
		IL_00F8: call System.SByte <module>.RuntimeHelpers::AddSaturatedI8(System.SByte, System.SByte)
		IL_00FD: ldc.i4.4
		IL_00FE: conv.ovf.i1
		IL_00FF: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_0104: ldloc V_8
		IL_0108: ldloc V_0
		IL_010C: call System.SByte <module>.RuntimeHelpers::AddSaturatedI8(System.SByte, System.SByte)
		IL_0111: ldloc V_8
		IL_0115: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_011A: ldloc V_8
		IL_011E: ldloc V_1
		IL_0122: call System.SByte <module>.RuntimeHelpers::AddSaturatedI8(System.SByte, System.SByte)
		IL_0127: ldloc V_8
		IL_012B: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_0130: ldloc V_8
		IL_0134: ldloc V_4
		IL_0138: call System.SByte <module>.RuntimeHelpers::AddSaturatedI8(System.SByte, System.SByte)
		IL_013D: ldloc V_8
		IL_0141: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_0146: ldstr "i8 arithmetic - checked sub"
		IL_014B: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_0150: ldloc V_1
		IL_0154: ldloc V_1
		IL_0158: sub
		IL_0159: conv.ovf.i1
		IL_015A: ldc.i4.0
		IL_015B: conv.ovf.i1
		IL_015C: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_0161: ldloc V_2
		IL_0165: ldloc V_1
		IL_0169: sub
		IL_016A: conv.ovf.i1
		IL_016B: ldc.i4.1
		IL_016C: conv.ovf.i1
		IL_016D: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_0172: ldc.i4.0
		IL_0173: conv.ovf.u1.un
		IL_0174: ldc.i4.0
		IL_0175: conv.ovf.u1.un
		IL_0176: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_017B: ldstr "i8 arithmetic - sub with overflow"
		IL_0180: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_0185: ldloc V_1
		IL_0189: ldloc V_1
		IL_018D: sub
		IL_018E: conv.i1
		IL_018F: ldc.i4.0
		IL_0190: conv.ovf.i1
		IL_0191: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_0196: ldloc V_2
		IL_019A: ldloc V_1
		IL_019E: sub
		IL_019F: conv.i1
		IL_01A0: ldc.i4.1
		IL_01A1: conv.ovf.i1
		IL_01A2: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_01A7: ldc.i4.0
		IL_01A8: conv.ovf.u1.un
		IL_01A9: ldc.i4.0
		IL_01AA: conv.ovf.u1.un
		IL_01AB: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_01B0: ldloc V_1
		IL_01B4: ldloc V_2
		IL_01B8: sub
		IL_01B9: conv.i1
		IL_01BA: ldc.i4.m1
		IL_01BB: conv.ovf.i1
		IL_01BC: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_01C1: ldloc V_0
		IL_01C5: ldc.i4.s 100
		IL_01C7: sub
		IL_01C8: conv.i1
		IL_01C9: ldc.i4.s -100
		IL_01CB: conv.ovf.i1
		IL_01CC: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_01D1: ldstr "i8 arithmetic - sub saturated"
		IL_01D6: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_01DB: ldloc V_1
		IL_01DF: ldloc V_1
		IL_01E3: call System.SByte <module>.RuntimeHelpers::SubSaturatedI8(System.SByte, System.SByte)
		IL_01E8: ldc.i4.0
		IL_01E9: conv.ovf.i1
		IL_01EA: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_01EF: ldloc V_2
		IL_01F3: ldloc V_1
		IL_01F7: call System.SByte <module>.RuntimeHelpers::SubSaturatedI8(System.SByte, System.SByte)
		IL_01FC: ldc.i4.1
		IL_01FD: conv.ovf.i1
		IL_01FE: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_0203: ldc.i4.0
		IL_0204: conv.ovf.u1.un
		IL_0205: ldc.i4.0
		IL_0206: conv.ovf.u1.un
		IL_0207: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_020C: ldloc V_1
		IL_0210: ldloc V_2
		IL_0214: call System.SByte <module>.RuntimeHelpers::SubSaturatedI8(System.SByte, System.SByte)
		IL_0219: ldc.i4.m1
		IL_021A: conv.ovf.i1
		IL_021B: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_0220: ldloc V_0
		IL_0224: ldc.i4.s 100
		IL_0226: call System.SByte <module>.RuntimeHelpers::SubSaturatedI8(System.SByte, System.SByte)
		IL_022B: ldc.i4.s -100
		IL_022D: conv.ovf.i1
		IL_022E: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_0233: ldstr "i8 arithmetic - mul checked"
		IL_0238: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_023D: ldloc V_1
		IL_0241: ldloc V_1
		IL_0245: mul.ovf
		IL_0246: ldc.i4.1
		IL_0247: conv.ovf.i1
		IL_0248: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_024D: ldloc V_2
		IL_0251: ldloc V_2
		IL_0255: mul.ovf
		IL_0256: ldc.i4.4
		IL_0257: conv.ovf.i1
		IL_0258: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_025D: ldloc V_0
		IL_0261: ldloc V_4
		IL_0265: mul.ovf
		IL_0266: ldc.i4.0
		IL_0267: conv.ovf.i1
		IL_0268: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_026D: ldstr "i8 arithmetic - mul with overflow - SKIPPED"
		IL_0272: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_0277: ldstr "i8 arithmetic - mul saturated - SKIPPED"
		IL_027C: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_0281: ldstr "i8 arithmetic - div checked"
		IL_0286: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_028B: ldloc V_2
		IL_028F: ldloc V_1
		IL_0293: div
		IL_0294: ldc.i4.2
		IL_0295: conv.ovf.i1
		IL_0296: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_029B: ldloc V_4
		IL_029F: ldloc V_2
		IL_02A3: div
		IL_02A4: ldc.i4.s 50
		IL_02A6: conv.ovf.i1
		IL_02A7: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_02AC: ldloc V_0
		IL_02B0: ldloc V_4
		IL_02B4: div
		IL_02B5: ldc.i4.0
		IL_02B6: conv.ovf.i1
		IL_02B7: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_02BC: ldstr "i8 arithmetic - div floor - SKIPPED"
		IL_02C1: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_02C6: ldstr "i8 arithmetic - div ceil - SKIPPED"
		IL_02CB: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_02D0: ldstr "i8 arithmetic - reminder"
		IL_02D5: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_02DA: ldloc V_2
		IL_02DE: ldloc V_1
		IL_02E2: rem
		IL_02E3: ldc.i4.0
		IL_02E4: conv.ovf.i1
		IL_02E5: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_02EA: ldloc V_4
		IL_02EE: ldloc V_2
		IL_02F2: rem
		IL_02F3: ldc.i4.0
		IL_02F4: conv.ovf.i1
		IL_02F5: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_02FA: ldloc V_5
		IL_02FE: ldloc V_2
		IL_0302: rem
		IL_0303: ldc.i4.1
		IL_0304: conv.ovf.i1
		IL_0305: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_030A: ldloc V_6
		IL_030E: ldloc V_2
		IL_0312: rem
		IL_0313: ldc.i4.1
		IL_0314: conv.ovf.i1
		IL_0315: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_031A: ldloc V_0
		IL_031E: ldloc V_4
		IL_0322: rem
		IL_0323: ldc.i4.0
		IL_0324: conv.ovf.i1
		IL_0325: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_032A: ldstr "i8 arithmetic - pow checked - SKIPPED"
		IL_032F: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_0334: ldstr "i8 arithmetic - pow with overflow - SKIPPED"
		IL_0339: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_033E: ldstr "i8 arithmetic - pow saturated - SKIPPED"
		IL_0343: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_0348: ret
	}

	method private static testBuiltins () System.Void {
		IL_0000: ldstr "builtins"
		IL_0005: call System.Void Std.Std+Test::newTestUnit(System.String)
		IL_000A: ldc.i4.0
		IL_000B: conv.ovf.u1.un
		IL_000C: ldc.i4.0
		IL_000D: conv.ovf.u1.un
		IL_000E: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_0013: ldc.i4 255
		IL_0018: conv.ovf.u1.un
		IL_0019: ldc.i4 255
		IL_001E: conv.ovf.u1.un
		IL_001F: call System.Void Std.Std+Test::assert(System.Byte, System.Byte)
		IL_0024: ldc.i4.s -128
		IL_0026: conv.ovf.i1
		IL_0027: ldc.i4.s -128
		IL_0029: conv.ovf.i1
		IL_002A: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_002F: ldc.i4.s 127
		IL_0031: conv.ovf.i1
		IL_0032: ldc.i4.s 127
		IL_0034: conv.ovf.i1
		IL_0035: call System.Void Std.Std+Test::assert(System.SByte, System.SByte)
		IL_003A: ret
	}
}

class Std.Std extends System.Object {
}

struct <module>.RuntimeHelpers+Error extends System.ValueType {
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
		IL_0011: call System.String <module>.RuntimeHelpers::AsString<!!0>(!!0[])
		IL_0016: call System.Void Std.Std+Test::__assert__impl__(System.Boolean, System.String)
		IL_001B: ret
	}

	method public static newTestUnit (System.String title) System.Void {
		IL_0000: ldc.i8 0
		IL_0009: stsfld System.UInt64 Std.Std+Test::testCount
		IL_000E: ldarg System.String title
		IL_0012: call System.Void Std.Std+Console::writeln(System.String)
		IL_0017: ret
	}

	method private static __assert__impl__ (System.Boolean status, System.String value) System.Void {
		IL_0000: ldsfld System.UInt64 Std.Std+Test::testCount
		IL_0005: ldc.i8 1
		IL_000E: add
		IL_000F: stsfld System.UInt64 Std.Std+Test::testCount
		IL_0014: ldarg System.Boolean status
		IL_0018: ldc.i4.0
		IL_0019: ceq
		IL_001B: brfalse IL_007F
		IL_0020: nop
		IL_0021: ldstr "\t- TEST "
		IL_0026: ldsfld System.UInt64 Std.Std+Test::testCount
		IL_002B: box System.UInt64
		IL_0030: callvirt System.String System.Object::ToString()
		IL_0035: ldstr " "
		IL_003A: ldsfld System.String Std.Std+Console::color_red
		IL_003F: ldstr "[FAILED] ("
		IL_0044: ldarg System.String value
		IL_0048: ldstr ")"
		IL_004D: ldsfld System.String Std.Std+Console::style_reset
		IL_0052: call System.String System.String::Concat(System.String, System.String)
		IL_0057: call System.String System.String::Concat(System.String, System.String)
		IL_005C: call System.String System.String::Concat(System.String, System.String)
		IL_0061: call System.String System.String::Concat(System.String, System.String)
		IL_0066: call System.String System.String::Concat(System.String, System.String)
		IL_006B: call System.String System.String::Concat(System.String, System.String)
		IL_0070: call System.String System.String::Concat(System.String, System.String)
		IL_0075: call System.Void Std.Std+Console::writeln(System.String)
		IL_007A: br IL_007F
		IL_007F: nop
		IL_0080: ret
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
		IL_0004: call System.String <module>.RuntimeHelpers::AsString<System.Object>(!!0[])
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
		IL_0004: call System.String <module>.RuntimeHelpers::AsString<System.Object>(!!0[])
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
