Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq

Namespace Bech32
	Module Bech32Engine2
		Public Function PolyMod(values As Byte()) As UInteger
			Dim chk As UInteger = 1UI
			For Each value As Byte In values
				Dim top As UInteger = chk >> 25
				chk = ((chk And 33554431UI) << 5 Xor CUInt(value))
				For i As Integer = 0 To 5 - 1
					If (top >> i And 1UI) = 1UI Then
						chk = chk Xor generator(i)
					End If
				Next
			Next
			Return chk
		End Function
		Public Sub Decode(encoded As String, <System.Runtime.InteropServices.OutAttribute()> ByRef hrp As String, <System.Runtime.InteropServices.OutAttribute()> ByRef data As Byte())
			Dim squashed As Byte()
#Disable Warning BC42030 ' Variable is passed by reference before it has been assigned a value
			DecodeSquashed(encoded, hrp, squashed)
#Enable Warning BC42030 ' Variable is passed by reference before it has been assigned a value
			If squashed Is Nothing Then
				data = Nothing
			Else
				data = Bytes5to8(squashed)
			End If
		End Sub
		Private Sub DecodeSquashed(adr As String, <System.Runtime.InteropServices.OutAttribute()> ByRef hrp As String, <System.Runtime.InteropServices.OutAttribute()> ByRef data As Byte())
			adr = CheckAndFormat(adr)
			If adr = Nothing Then
				data = Nothing
				hrp = Nothing
			Else
				Dim splitLoc As Integer = adr.LastIndexOf("1")
				Dim flag2 As Boolean = splitLoc = -1
				If flag2 Then
					Debug.WriteLine("1 separator not present in address")
					data = Nothing
					hrp = Nothing
				Else
					hrp = adr.Substring(0, splitLoc)
					Dim squashed As Byte() = StringToSquashedBytes(adr.Substring(splitLoc + 1))
					If squashed Is Nothing Then
						data = Nothing
					Else
						Dim flag4 As Boolean = Not VerifyChecksum(hrp, squashed)
						If flag4 Then
							Debug.WriteLine("Checksum invalid")
							data = Nothing
						Else
							Dim length As Integer = squashed.Length - 6
							data = New Byte(length - 1) {}
							Array.Copy(squashed, 0, data, 0, length)
						End If
					End If
				End If
			End If
		End Sub
		Private Function CheckAndFormat(adr As String) As String
			Dim lowAdr As String = adr.ToLower()
			Dim highAdr As String = adr.ToUpper()
			Dim result As String
			If adr <> lowAdr AndAlso adr <> highAdr Then
				Debug.WriteLine("mixed case address")
				result = Nothing
			Else
				result = lowAdr
			End If
			Return result
		End Function
		Private Function VerifyChecksum(hrp As String, data As Byte()) As Boolean
			Dim values As Byte() = HRPExpand(hrp).Concat(data).ToArray()
			Dim checksum As UInteger = Bech32Engine2.PolyMod(values)
			Return checksum = 1UI
		End Function
		Private Function StringToSquashedBytes(input As String) As Byte()
			Dim squashed As Byte() = New Byte(input.Length - 1) {}
			For i As Integer = 0 To input.Length - 1
				Dim c As Char = input(i)
				Dim buffer As Short = icharset(Convert.ToInt32((c)))
				Dim flag As Boolean = buffer = -1S
				If flag Then
					Debug.WriteLine("contains invalid character " + c.ToString())
					Return Nothing
				End If
				squashed(i) = CByte(buffer)
			Next
			Return squashed
		End Function
		Public Function Encode(hrp As String, data As Byte()) As String
			Dim base5 As Byte() = Bytes8to5(data)
			Dim flag As Boolean = base5 Is Nothing
			Dim result As String
			'If flag Then
			'result = " String.Empty"
			'Else
			result = EncodeSquashed(hrp, base5)
			'End If
			Return result
		End Function
		Private Function EncodeSquashed(hrp As String, data As Byte()) As String
			Dim checksum As Byte() = CreateChecksum(hrp, data)
			Dim combined As Byte() = data.Concat(checksum).ToArray()
			Dim encoded As String = SquashedBytesToString(combined)
			Dim flag As Boolean = encoded = Nothing
			Dim result As String
			If flag Then
				result = Nothing
			Else
				result = hrp + "1" + encoded
			End If
			Return result
		End Function
		Private Function CreateChecksum(hrp As String, data As Byte()) As Byte()
			Dim values As Byte() = HRPExpand(hrp).Concat(data).ToArray()
			values = values.Concat(New Byte(5) {}).ToArray()
			Dim checksum As UInteger = Bech32Engine2.PolyMod(values) Xor 1UI
			Dim ret As Byte() = New Byte(5) {}
			For i As Integer = 0 To 6 - 1
				ret(i) = CByte((checksum >> 5 * (5 - i) And 31UI))
			Next
			Return ret
		End Function
		Private Function HRPExpand(input As String) As Byte()
			Dim output As Byte() = New Byte(input.Length * 2 + 1 - 1) {}
			For i As Integer = 0 To input.Length - 1
				Dim c As Char = input(i)
				output(i) = CByte(Convert.ToInt32(c) >> 5)
			Next
			For j As Integer = 0 To input.Length - 1
				Dim c2 As Char = input(j)
				output(j + input.Length + 1) = CByte(Convert.ToInt32(c2) And Convert.ToInt32((31))) 'looks wrong
			Next
			Return output
		End Function
		Private Function SquashedBytesToString(input As Byte()) As String
			Dim s As String = String.Empty
			For i As Integer = 0 To input.Length - 1
				Dim c As Byte = input(i)
				Dim flag As Boolean = (c And 224) > 0
				If flag Then
					Debug.WriteLine("high bits set at position {0}: {1}", New Object() {i, c})
					Return Nothing
				End If
				s += charset(CInt(c)).ToString()
			Next
			Return s
		End Function
		Private Function Bytes8to5(data As Byte()) As Byte()
			Return ByteSquasher(data, 8, 5)
		End Function
		Private Function Bytes5to8(data As Byte()) As Byte()
			Return ByteSquasher(data, 5, 8)
		End Function
		Private Function ByteSquasher(input As Byte(), inputWidth As Integer, outputWidth As Integer) As Byte()
			Dim bitstash As Integer = 0
			Dim accumulator As Integer = 0
			Dim output As List(Of Byte) = New List(Of Byte)()
			Dim maxOutputValue As Integer = (1 << outputWidth) - 1
			For i As Integer = 0 To input.Length - 1
				Dim c As Byte = input(i)
				Dim flag As Boolean = CInt(c) >> inputWidth <> 0
				If flag Then
					Console.WriteLine("byte {0} ({1}) high bits set", New Object() {i, c})
					Return Nothing
				End If
				accumulator = (accumulator << inputWidth Or CInt(c))
				bitstash += inputWidth
				While bitstash >= outputWidth
					bitstash -= outputWidth
					output.Add(CByte((accumulator >> bitstash And maxOutputValue)))
				End While
			Next
			If inputWidth = 8 AndAlso outputWidth = 5 Then
				Dim flag3 As Boolean = bitstash <> 0
				If flag3 Then
					output.Add(CByte((accumulator << outputWidth - bitstash And maxOutputValue)))
				Else
				End If
			Else
				Dim flag4 As Boolean = bitstash >= inputWidth OrElse (accumulator << outputWidth - bitstash And maxOutputValue) <> 0
				If flag4 Then
					Console.WriteLine("invalid padding from {0} to {1} bits", New Object() {inputWidth, outputWidth})
					Return Nothing
				End If
			End If
			Return output.ToArray()
		End Function


		Private generator As UInteger() = New UInteger() {996825010UI, 642813549UI, 513874426UI, 1027748829UI, 705979059UI}
		Private Const charset As String = "qpzry9x8gf2tvdw0s3jn54khce6mua7l"
		Private icharset As Short() = New Short() {-1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, 15S, -1S, 10S, 17S, 21S, 20S, 26S, 30S, 7S, 5S, -1S, -1S, -1S, -1S, -1S, -1S, -1S, 29S, -1S, 24S, 13S, 25S, 9S, 8S, 23S, -1S, 18S, 22S, 31S, 27S, 19S, -1S, 1S, 0S, 3S, 16S, 11S, 28S, 12S, 14S, 6S, 4S, 2S, -1S, -1S, -1S, -1S, -1S, -1S, 29S, -1S, 24S, 13S, 25S, 9S, 8S, 23S, -1S, 18S, 22S, 31S, 27S, 19S, -1S, 1S, 0S, 3S, 16S, 11S, 28S, 12S, 14S, 6S, 4S, 2S, -1S, -1S, -1S, -1S, -1S}
	End Module
End Namespace
