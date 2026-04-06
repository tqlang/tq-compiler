
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
}

class Bootstrap.Bootstrap extends System.Object {

	method public static main () System.Void {
		.locals init (System.Byte[] V_0)
		.locals init (System.Byte[] V_1)
		IL_0000: ldc.i4.5
		IL_0001: newarr System.Byte
		IL_0006: dup
		IL_0007: ldc.i4.0
		IL_0008: ldc.i4.s 10
		IL_000A: stelem.i1
		IL_000B: dup
		IL_000C: ldc.i4.1
		IL_000D: ldc.i4.s 20
		IL_000F: stelem.i1
		IL_0010: dup
		IL_0011: ldc.i4.2
		IL_0012: ldc.i4.s 30
		IL_0014: stelem.i1
		IL_0015: dup
		IL_0016: ldc.i4.3
		IL_0017: ldc.i4.s 40
		IL_0019: stelem.i1
		IL_001A: dup
		IL_001B: ldc.i4.4
		IL_001C: ldc.i4.s 50
		IL_001E: stelem.i1
		IL_001F: stloc V_0
		IL_0023: ldc.i4.5
		IL_0024: newarr System.Byte
		IL_0029: dup
		IL_002A: ldc.i4.0
		IL_002B: ldc.i4.0
		IL_002C: stelem.i1
		IL_002D: dup
		IL_002E: ldc.i4.1
		IL_002F: ldc.i4.0
		IL_0030: stelem.i1
		IL_0031: dup
		IL_0032: ldc.i4.2
		IL_0033: ldc.i4.0
		IL_0034: stelem.i1
		IL_0035: dup
		IL_0036: ldc.i4.3
		IL_0037: ldc.i4.0
		IL_0038: stelem.i1
		IL_0039: dup
		IL_003A: ldc.i4.4
		IL_003B: ldc.i4.0
		IL_003C: stelem.i1
		IL_003D: stloc V_1
		IL_0041: ldstr "Before:"
		IL_0046: call System.Void Std.Std+Console::writeln(System.String)
		IL_004B: ldstr "arr0 "
		IL_0050: ldloc V_0
		IL_0054: call System.String <Module>.RuntimeHelpers::AsString<System.Byte>(!!0[])
		IL_0059: ldstr " (len "
		IL_005E: ldloc V_0
		IL_0062: ldlen
		IL_0063: conv.u8
		IL_0064: box System.UInt64
		IL_0069: callvirt System.String System.Object::ToString()
		IL_006E: ldstr ")"
		IL_0073: call System.String System.String::Concat(System.String, System.String)
		IL_0078: call System.String System.String::Concat(System.String, System.String)
		IL_007D: call System.String System.String::Concat(System.String, System.String)
		IL_0082: call System.String System.String::Concat(System.String, System.String)
		IL_0087: call System.Void Std.Std+Console::writeln(System.String)
		IL_008C: ldstr "arr1 "
		IL_0091: ldloc V_1
		IL_0095: call System.String <Module>.RuntimeHelpers::AsString<System.Byte>(!!0[])
		IL_009A: ldstr " (len "
		IL_009F: ldloc V_1
		IL_00A3: ldlen
		IL_00A4: conv.u8
		IL_00A5: box System.UInt64
		IL_00AA: callvirt System.String System.Object::ToString()
		IL_00AF: ldstr ")"
		IL_00B4: call System.String System.String::Concat(System.String, System.String)
		IL_00B9: call System.String System.String::Concat(System.String, System.String)
		IL_00BE: call System.String System.String::Concat(System.String, System.String)
		IL_00C3: call System.String System.String::Concat(System.String, System.String)
		IL_00C8: call System.Void Std.Std+Console::writeln(System.String)
		IL_00CD: ldloc V_0
		IL_00D1: ldloc V_1
		IL_00D5: call System.Void Std.Std+Mem::copy(System.Byte[], System.Byte[])
		IL_00DA: ldstr "After:"
		IL_00DF: call System.Void Std.Std+Console::writeln(System.String)
		IL_00E4: ldstr "arr0 "
		IL_00E9: ldloc V_0
		IL_00ED: call System.String <Module>.RuntimeHelpers::AsString<System.Byte>(!!0[])
		IL_00F2: ldstr " (len "
		IL_00F7: ldloc V_0
		IL_00FB: ldlen
		IL_00FC: conv.u8
		IL_00FD: box System.UInt64
		IL_0102: callvirt System.String System.Object::ToString()
		IL_0107: ldstr ")"
		IL_010C: call System.String System.String::Concat(System.String, System.String)
		IL_0111: call System.String System.String::Concat(System.String, System.String)
		IL_0116: call System.String System.String::Concat(System.String, System.String)
		IL_011B: call System.String System.String::Concat(System.String, System.String)
		IL_0120: call System.Void Std.Std+Console::writeln(System.String)
		IL_0125: ldstr "arr1 "
		IL_012A: ldloc V_1
		IL_012E: call System.String <Module>.RuntimeHelpers::AsString<System.Byte>(!!0[])
		IL_0133: ldstr " (len "
		IL_0138: ldloc V_1
		IL_013C: ldlen
		IL_013D: conv.u8
		IL_013E: box System.UInt64
		IL_0143: callvirt System.String System.Object::ToString()
		IL_0148: ldstr ")"
		IL_014D: call System.String System.String::Concat(System.String, System.String)
		IL_0152: call System.String System.String::Concat(System.String, System.String)
		IL_0157: call System.String System.String::Concat(System.String, System.String)
		IL_015C: call System.String System.String::Concat(System.String, System.String)
		IL_0161: call System.Void Std.Std+Console::writeln(System.String)
		IL_0166: ldloc V_0
		IL_016A: ldloc V_1
		IL_016E: call System.Boolean Std.Std+Mem::compare(System.Byte[], System.Byte[])
		IL_0173: brfalse IL_0188
		IL_0178: nop
		IL_0179: ldstr "Arrays are equal!"
		IL_017E: call System.Void Std.Std+Console::writeln(System.String)
		IL_0183: br IL_0193
		IL_0188: nop
		IL_0189: ldstr "Error! arrays are not equal!"
		IL_018E: call System.Void Std.Std+Console::writeln(System.String)
		IL_0193: nop
		IL_0194: ret
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
		IL_0005: conv.ovf.i4
		IL_0006: call System.Void System.Environment::Exit(System.Int32)
		IL_000B: ret
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
		IL_0008: conv.ovf.i1
		IL_0009: call System.Void Std.Std+Mem::__fill_impl__<System.SByte>(!!0[], !!0)
		IL_000E: ret
	}

	method public static fill (System.Byte[] slice, System.Byte value) System.Void {
		IL_0000: ldarg System.Byte[] slice
		IL_0004: ldarg System.Byte value
		IL_0008: conv.ovf.u1.un
		IL_0009: call System.Void Std.Std+Mem::__fill_impl__<System.Byte>(!!0[], !!0)
		IL_000E: ret
	}

	method public static fill (System.Int16[] slice, System.Int16 value) System.Void {
		IL_0000: ldarg System.Int16[] slice
		IL_0004: ldarg System.Int16 value
		IL_0008: conv.ovf.i2
		IL_0009: call System.Void Std.Std+Mem::__fill_impl__<System.Int16>(!!0[], !!0)
		IL_000E: ret
	}

	method public static fill (System.UInt16[] slice, System.UInt16 value) System.Void {
		IL_0000: ldarg System.UInt16[] slice
		IL_0004: ldarg System.UInt16 value
		IL_0008: conv.ovf.u2.un
		IL_0009: call System.Void Std.Std+Mem::__fill_impl__<System.UInt16>(!!0[], !!0)
		IL_000E: ret
	}

	method public static fill (System.Int32[] slice, System.Int32 value) System.Void {
		IL_0000: ldarg System.Int32[] slice
		IL_0004: ldarg System.Int32 value
		IL_0008: conv.ovf.i4
		IL_0009: call System.Void Std.Std+Mem::__fill_impl__<System.Int32>(!!0[], !!0)
		IL_000E: ret
	}

	method public static fill (System.UInt32[] slice, System.UInt32 value) System.Void {
		IL_0000: ldarg System.UInt32[] slice
		IL_0004: ldarg System.UInt32 value
		IL_0008: conv.ovf.u4.un
		IL_0009: call System.Void Std.Std+Mem::__fill_impl__<System.UInt32>(!!0[], !!0)
		IL_000E: ret
	}

	method public static fill (System.Int64[] slice, System.Int64 value) System.Void {
		IL_0000: ldarg System.Int64[] slice
		IL_0004: ldarg System.Int64 value
		IL_0008: conv.ovf.i8
		IL_0009: call System.Void Std.Std+Mem::__fill_impl__<System.Int64>(!!0[], !!0)
		IL_000E: ret
	}

	method public static fill (System.UInt64[] slice, System.UInt64 value) System.Void {
		IL_0000: ldarg System.UInt64[] slice
		IL_0004: ldarg System.UInt64 value
		IL_0008: conv.ovf.u8.un
		IL_0009: call System.Void Std.Std+Mem::__fill_impl__<System.UInt64>(!!0[], !!0)
		IL_000E: ret
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
		IL_0005: conv.ovf.i4
		IL_0006: ldc.i4.0
		IL_0007: call !!0[] System.GC::AllocateArray<!!0>(System.Int32, System.Boolean)
		IL_000C: ret
	}
}

class Std.Std+Test extends System.Object {

	method public static testValue (System.Byte value, System.Byte reference) System.Void {
		IL_0000: ldarg System.Byte value
		IL_0004: ldarg System.Byte reference
		IL_0008: ceq
		IL_000A: call System.Void Std.Std+Test::printStatusMessage(System.Boolean)
		IL_000F: ret
	}

	method private static printStatusMessage (System.Boolean status) System.Void {
		IL_0000: ldarg System.Boolean status
		IL_0004: brfalse IL_0019
		IL_0009: nop
		IL_000A: ldstr "TEST PASSED"
		IL_000F: call System.Void Std.Std+Console::writeln(System.String)
		IL_0014: br IL_0024
		IL_0019: nop
		IL_001A: ldstr "TEST FAILED"
		IL_001F: call System.Void Std.Std+Console::write(System.String)
		IL_0024: nop
		IL_0025: ret
	}
}

class Std.Std+Console extends System.Object {

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
