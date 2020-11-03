# Elrond-SDK
 .net sdk for elrond with example projects


![Alt text](https://raw.githubusercontent.com/jamieS95/Elrond-SDK/main/screenshots/simplesend.png )
![Alt text](https://raw.githubusercontent.com/jamieS95/Elrond-SDK/main/screenshots/walletconverter.png)


    Dim a As New Elrond
    Elrond a = new Elrond;


    a.FromPEM(filelocation)
    a.FromJSONKeystore(filelocation, password)

    a.ToPEM(filelocation)

    a.PrivKeyRaw
    a.PrivKeyBech32
    a.PubKeyRaw
    a.PubKeyBech32

    a._nonce
    a._balance

    a.SignTX(a._nonce, TextBox2.Text, TextBox3.Text, gasPrice, gasLimit, data, chainID, version)
    a.SendTX()
