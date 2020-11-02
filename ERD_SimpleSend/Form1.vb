Imports System.Globalization
Imports System.Numerics
Imports System.Text
Imports ERDLib

Public Class Form1
    Dim a As New Elrond

    Public Shared Function StringToByteArray(s As String) As Byte()
        ' remove any spaces from, e.g. "A0 20 34 34"
        s = s.Replace(" "c, "")
        ' make sure we have an even number of digits
        If (s.Length And 1) = 1 Then
            Throw New FormatException("Odd string length when even string length is required.")
        End If

        ' calculate the length of the byte array and dim an array to that
        Dim nBytes = s.Length \ 2
        Dim a(nBytes - 1) As Byte

        ' pick out every two bytes and convert them from hex representation
        For i = 0 To nBytes - 1
            a(i) = Convert.ToByte(s.Substring(i * 2, 2), 16)
        Next

        Return a

    End Function
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim ofd1 As New OpenFileDialog
        If RadioButton1.Checked Then
            ofd1.Filter = "JSON|*.json"
            Dim password As String = InputBox("please type json password", "JSON password", "")
            If ofd1.ShowDialog = DialogResult.OK Then
                a.FromJSONKeystore(ofd1.FileName, password)
                TextBox1.Text = a.PubKeyBech32
                a.UpdateBalanceAndNonce()
                Label6.Text = CDbl((BigInteger.Parse(a._balance) * BigInteger.Pow(10, 18))).ToString.Substring(0, 15)
                Label9.Text = a._nonce
            End If
        Else
            ofd1.Filter = "pem|*.PEM"
            If ofd1.ShowDialog = DialogResult.OK Then
                a.FromPEM(ofd1.FileName)
                TextBox1.Text = a.PubKeyBech32
                a.UpdateBalanceAndNonce()
                Label6.Text = CDbl((BigInteger.Parse(a._balance) * BigInteger.Pow(10, 18))).ToString.Substring(0, 15)
                Label9.Text = a._nonce
            End If
        End If


    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Dim nonce As Integer = a._nonce
        Dim gasPrice As Integer = 1000000000
        Dim gasLimit As Integer = 70000
        Dim data As String = "" 'not required 
        Dim chainID As String = "1"
        Dim version As Integer = 1
        a.SendTX(a.SignTX(a._nonce, TextBox2.Text, TextBox3.Text, gasPrice, gasLimit, data, chainID, version))
    End Sub
End Class
