Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Collections.Generic
Imports Org.BouncyCastle.Crypto
Imports Org.BouncyCastle.Crypto.Generators
Imports Org.BouncyCastle.Crypto.Parameters
Imports Org.BouncyCastle.Security
#Const BOUNCY_CASTLE_CRYPTO = True
Public Class Elrond
    Private _PrivKeyRaw(32 - 1), _PubKeyRaw(32 - 1) As Byte
    Private _PrivKeyBech32, _PubKeyBech32, _BalanceRaw As String
    'nonce
    Public _Nonce As UInt64

    Private Function GenKeysAndEncode()
#If BOUNCY_CASTLE_CRYPTO Then
        _PubKeyRaw = ((New Ed25519PrivateKeyParameters(_PrivKeyRaw, 0)).GeneratePublicKey()).GetEncoded()
#Else
        _PubKeyRaw = Ed25519.PublicKey(PrivKeyRaw)
#End If
        _PubKeyBech32 = Bech32.Bech32Engine2.Encode("erd", _PubKeyRaw)
        _PrivKeyBech32 = Bech32.Bech32Engine2.Encode("erd", _PrivKeyRaw)
    End Function
    Function showMatch(ByVal text As String, ByVal expr As String) As String
        Dim mc As MatchCollection = Regex.Matches(text, expr)
        Dim m As Match
        Return mc(0).Groups(1).Value
    End Function
    Public Sub SetPrivateKey(ByVal p() As Byte)
        Buffer.BlockCopy(p, 0, _PrivKeyRaw, 0, 32)
        GenKeysAndEncode()
    End Sub
    Public Function GetPrivateKey() As Byte()
        Dim tmpPrivKeyRaw(32 - 1) As Byte
        Buffer.BlockCopy(_PrivKeyRaw, 0, tmpPrivKeyRaw, 0, 32)
        Return tmpPrivKeyRaw
    End Function
    Public Function GetPrivateKeyBech32() As String
        Return _PrivKeyBech32
    End Function
    Public Function GetPublicKey() As Byte()
        Dim tmpPubKeyRaw(32 - 1) As Byte
        Buffer.BlockCopy(_PubKeyRaw, 0, tmpPubKeyRaw, 0, 32)
        Return tmpPubKeyRaw
    End Function
    Public Function GetPublicKeyBech32() As String
        Return _PubKeyBech32
    End Function
    Public Function GetNonce()
        Return _Nonce
    End Function
    Public Function GetBalanceRaw() As String
        Return _BalanceRaw
    End Function
    Public Function GetBalance() As String
        Return _BalanceRaw.Substring(0, _BalanceRaw.Length - 18) & "." & _BalanceRaw.Substring(_BalanceRaw.Length - 18, 18)
    End Function

    Public Sub FromJSONKeystore(ByVal f As String, ByVal password As String)
        Dim jsonstr As String = File.ReadAllText(f)

        If jsonstr.Contains("""cipher"":""aes-128-ctr""") <> True OrElse jsonstr.Contains("""kdf"":""scrypt""") <> True Then
            Throw New Exception("error keystore")
        End If

        Dim salt As String = showMatch(jsonstr, """salt"":""([a-fA-f0-9 ]*)""")
        Dim dklen As Integer = showMatch(jsonstr, """dklen"":([0-9]*)")
        Dim n As Integer = showMatch(jsonstr, """n"":([0-9]*)")
        Dim p As Integer = showMatch(jsonstr, """p"":([0-9]*)")
        Dim r As Integer = showMatch(jsonstr, """r"":([0-9]*)")
#If BOUNCY_CASTLE_CRYPTO Then
        Dim key() As Byte = SCrypt.Generate(System.Text.Encoding.ASCII.GetBytes(password), StringToByteArray(salt), n, r, p, dklen)
#Else

#End If
        Dim decryption_key(16 - 1) As Byte
        Buffer.BlockCopy(key, 0, decryption_key, 0, 16)
        Dim iv() As Byte = StringToByteArray(showMatch(jsonstr, """iv"":""([a-fA-f0-9]*)"""))
        Dim ciphertext() As Byte = StringToByteArray(showMatch(jsonstr, """ciphertext"":""([a-fA-f0-9]*)"""))
        ' create AES cipher
#If BOUNCY_CASTLE_CRYPTO Then
        Dim cipher As IBufferedCipher = CipherUtilities.GetCipher("AES/CTR/NoPadding")
        cipher.Init(False, New ParametersWithIV(ParameterUtilities.CreateKeyParameter("AES", decryption_key), iv))
        Dim plainBytes As Byte() = cipher.DoFinal(ciphertext)
#Else

#End If
        Buffer.BlockCopy(plainBytes, 0, _PrivKeyRaw, 0, 32)
        GenKeysAndEncode()
    End Sub
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
    Private Function Wraptext(ByVal str As String, ByVal len As Integer) As List(Of String)
        Dim strings As New List(Of String)
        For i As Integer = 0 To str.Length Step len
            strings.Add(str.Substring(i, System.Math.Min(str.Length - i, 64)))
        Next
        Return strings
    End Function
    Public Sub ToPEM(ByVal f As String, Optional name As String = "")
        name = If(String.IsNullOrEmpty(name), _PubKeyBech32, name)
        Using sw As New StreamWriter(f)
            Dim pubkeynew = System.Text.Encoding.ASCII.GetBytes(Bytes_To_String2(_PrivKeyRaw).ToLower & Bytes_To_String2(_PubKeyRaw).ToLower)
            Dim wrap As String() = Wraptext(Convert.ToBase64String(pubkeynew, 0, pubkeynew.Length), 64).ToArray
            sw.WriteLine(String.Format("-----BEGIN PRIVATE KEY for {0}-----", name))
            sw.WriteLine(String.Join(vbNewLine, wrap))
            sw.WriteLine(String.Format("-----END PRIVATE KEY for {0}-----", name))
        End Using
    End Sub
    Public Sub FromPEM(ByVal f As String)
        Dim strings As New List(Of String)
        Using sr As New StreamReader(f)
            If sr.ReadLine().StartsWith("-----BEGIN PRIVATE KEY") = False Then
                Throw New Exception("invalid pem")
            End If
            Dim cont As Boolean = True
            While cont
                Dim currentline As String = sr.ReadLine.Trim
                If currentline.StartsWith("-----END PRIVATE KEY") Then
                    cont = False
                Else
                    strings.Add(currentline)
                End If
            End While
        End Using
        FromPEMbase64(String.Join("", strings.ToArray))
    End Sub
    Private Sub FromPEMbase64(ByVal base64EncodedSeed As String)
        Dim vOut As String = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(base64EncodedSeed))
        Dim newprivkey() As Byte = StringToByteArray(vOut.Substring(0, 64))
        Dim newpubkey() As Byte = StringToByteArray(vOut.Substring(64, 64))
        _PrivKeyRaw = newprivkey


        GenKeysAndEncode()
        If Byte.ReferenceEquals(_PubKeyRaw, newpubkey) Then
            Throw New Exception("pem error")
        End If
    End Sub
    Public Function UpdateBalanceAndNonce()
        Dim jsonstr = getnonceandbalance()

        _BalanceRaw = showMatch(jsonstr, """balance"":""([0-9]*)""")
        _Nonce = showMatch(jsonstr, """nonce"":([0-9]*)")
    End Function
    Private Function getnonceandbalance()
        Dim Req As HttpWebRequest
        'create a web request to the URL  
        Try
            Req = HttpWebRequest.Create("https://api.elrond.com/address/" & _PubKeyBech32)
            ' Req.
            Req.ContentType = "application/json-rpc"
            Req.Method = "GET"
            Dim webResponlse As WebResponse = Req.GetResponse()
            Dim loResponseStream As StreamReader = New StreamReader(webResponlse.GetResponseStream())
            Return loResponseStream.ReadToEnd()
        Catch ex As WebException When DirectCast(ex.Response, HttpWebResponse).StatusCode = HttpStatusCode.BadRequest

            Dim ddfdfd As StreamReader = New StreamReader(ex.Response.GetResponseStream())
            Return ddfdfd.ReadToEnd()
        End Try
    End Function
    Public Function SendTX(ByVal contnt As String)
        Dim gdf As Byte() = Encoding.UTF8.GetBytes(contnt)
        Dim Req As HttpWebRequest
        'create a web request to the URL  
        Try
            Req = HttpWebRequest.Create("https://api.elrond.com/transaction/send")
            ' Req.
            Req.ContentType = "application/json-rpc"
            Req.Method = "POST"
            Dim dataStream As Stream = Req.GetRequestStream()
            dataStream.Write(gdf, 0, gdf.Length)
            dataStream.Close()
            Dim webResponlse As WebResponse = Req.GetResponse()
            Dim loResponseStream As StreamReader = New StreamReader(webResponlse.GetResponseStream())
            Return loResponseStream.ReadToEnd()
        Catch ex As WebException When DirectCast(ex.Response, HttpWebResponse).StatusCode = HttpStatusCode.BadRequest
            Dim ddfdfd As StreamReader = New StreamReader(ex.Response.GetResponseStream())
            Return ddfdfd.ReadToEnd()
        End Try

    End Function

    Public Function SignTX(_nonce, _value, _receiver, _gasPrice, _gasLimit, _data, _chainID, _version) As String
        Dim txtosign As String = CreateTXstring(_nonce, _value, _receiver, _PubKeyBech32, _gasPrice, _gasLimit, _data, _chainID, _version, "")
        Dim messagebytestosign = System.Text.UTF8Encoding.ASCII.GetBytes(txtosign)


#If BOUNCY_CASTLE_CRYPTO Then
        Dim parameters As Ed25519PrivateKeyParameters = New Ed25519PrivateKeyParameters(_PrivKeyRaw, 0)
        Dim df As New Org.BouncyCastle.Crypto.Signers.Ed25519Signer
        df.Init(True, parameters)
        df.BlockUpdate(messagebytestosign, 0, messagebytestosign.Length)
        Dim signature As Byte() = df.GenerateSignature()
#Else
        Dim signature As Byte() = Ed25519.Signature(messagebytestosign, PrivKeyRaw, PubKeyRaw)
#End If


        Dim signedtx As String = CreateTXstring(_nonce, _value, _receiver, _PubKeyBech32, _gasPrice, _gasLimit, _data, _chainID, _version, Bytes_To_String2(signature))
        Return signedtx
    End Function
    Private Function Bytes_To_String2(ByVal bytes_Input As Byte()) As String
        Dim strTemp As New StringBuilder(bytes_Input.Length * 2)
        For Each b As Byte In bytes_Input
            strTemp.Append(b.ToString("X02"))
        Next
        Return strTemp.ToString()
    End Function
    Private Function CreateTXstring(nonce As Integer, value As String, receiver As String, sender As String, gasPrice As Integer, gasLimit As Integer, data As String, chainID As String, version As Integer, Optional signature As String = "") As String
        Dim sb As New StringBuilder
        sb.Append("{")
        sb.Append(String.Format("""nonce"":{0},", nonce))
        sb.Append(String.Format("""value"":""{0}"",", value))
        sb.Append(String.Format("""receiver"":""{0}"",", receiver))
        sb.Append(String.Format("""sender"":""{0}"",", sender))
        sb.Append(String.Format("""gasPrice"":{0},", gasPrice))
        sb.Append(String.Format("""gasLimit"":{0},", gasLimit))
        If String.IsNullOrEmpty(data) = False Then 'don't add a data field if we dont supply data
            Dim databytes() As Byte = System.Text.Encoding.ASCII.GetBytes(data)
            sb.Append(String.Format("""data"":""{0}"",", Convert.ToBase64String(databytes, 0, databytes.Length)))
        End If
        sb.Append(String.Format("""chainID"":""{0}"",", chainID))
        If String.IsNullOrEmpty(signature) Then 'only allow signature field if we have signed the data
            sb.Append(String.Format("""version"":{0}", version))
        Else
            sb.Append(String.Format("""version"":{0},", version))
            sb.Append(String.Format("""signature"":""{0}""", signature))
        End If
        sb.Append("}")
        Return sb.ToString
    End Function
    'Public Function ClaimRewards() As String

    'End Function
    Public Function SendTransaction(ByVal receiver As String, ByVal value As String, Optional data As String = "") As String
        Dim gasPrice As Integer = 1000000000
        Dim gasLimit As Integer = 70000
        Dim chainID As String = "1"
        Dim version As Integer = 1

        Dim txtosign As String = CreateTXstring(_Nonce, value, receiver, _PubKeyBech32, gasPrice, gasLimit, data, chainID, version, "")
        Dim messagebytestosign = System.Text.UTF8Encoding.ASCII.GetBytes(txtosign)
#If BOUNCY_CASTLE_CRYPTO Then
        Dim parameters As Ed25519PrivateKeyParameters = New Ed25519PrivateKeyParameters(_PrivKeyRaw, 0)
        Dim df As New Org.BouncyCastle.Crypto.Signers.Ed25519Signer
        df.Init(True, parameters)
        df.BlockUpdate(messagebytestosign, 0, messagebytestosign.Length)
        Dim signature As Byte() = df.GenerateSignature()
#Else
        Dim signature As Byte() = Ed25519.Signature(messagebytestosign, PrivKeyRaw, PubKeyRaw)
#End If
        Dim signedtx As String = CreateTXstring(_Nonce, value, receiver, _PubKeyBech32, gasPrice, gasLimit, data, chainID, version, Bytes_To_String2(signature))
        _Nonce += 1
        Return SendTX(signedtx)
    End Function
End Class