# Elrond-SDK
 .net sdk for elrond with example projects


![Alt text](https://raw.githubusercontent.com/jamieS95/Elrond-SDK/main/screenshots/simplesend.png )
![Alt text](https://raw.githubusercontent.com/jamieS95/Elrond-SDK/main/screenshots/walletconverter.png)




    Imports ERDLib
    using ERDLib;
	
	
    Dim a As New Elrond
    Elrond a = new Elrond;


    a.FromPEM(filelocation)
    a.FromJSONKeystore(filelocation, password)
	
    a.ToPEM(filelocation)
	
	
    
	a.SendTransaction(ByVal receiver As String, ByVal value As String, Optional data As String = "") 
