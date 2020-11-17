# Elrond-SDK
 .net sdk for elrond with example projects


- [ ] change transaction amount handling to big decimal or similar
- [ ] better function responses e.g return transaction information after send
- [ ] allow user defined fee/get recommended fee from api
- [ ] convert to C with .NET bindings (speed improvement)




![Alt text](https://raw.githubusercontent.com/jamieS95/Elrond-SDK/main/screenshots/simplesend.png )
![Alt text](https://raw.githubusercontent.com/jamieS95/Elrond-SDK/main/screenshots/walletconverter.png)



 
    using ERDLib;
	
    Elrond a = new Elrond;
    
    a.FromPEM(String filelocation);
    a.FromJSONKeystore(String filelocation,String password);
	
    a.ToPEM(String filelocation);
	
	a.SendTransaction(String receiver,String value,String data = "");
	
	
	
    Imports ERDLib
	
	Dim a As New Elrond
	
    a.FromPEM(filelocation)
    a.FromJSONKeystore(filelocation, password)
	
    a.ToPEM(filelocation)
	
	a.SendTransaction(ByVal receiver As String, ByVal value As String, Optional data As String = "") 	