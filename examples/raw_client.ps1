$RPCServer = "192.168.121.156"
$RPCPort = 3333
Write-Output ( "Connect to {0}:{1}" -f ($RPCServer, $RPCPort))
$tcpConnection = New-Object System.Net.Sockets.TcpClient($RPCServer, $RPCPort)
$tcpStream = $tcpConnection.GetStream()
$reader = New-Object System.IO.StreamReader($tcpStream)
$writer = New-Object System.IO.StreamWriter($tcpStream)
$writer.AutoFlush = $true

$encoding = new-object System.Text.UTF8Encoding 

$command = "{'method':'Var_ShowVar','params':['`$TRAFONAME[]'],'id':1}"

$writer.WriteLine($command)
Write-Output ( "Send {0}" -f ($command))

$response = ""
while ($tcpStream.DataAvailable -or $response -eq ""){
    $response = $reader.ReadLine()
}
Write-Output ( "Recive {0}" -f ($response))

$reader.Close()
$writer.Close()
$tcpConnection.Close()

Write-Host -NoNewLine "Any key to close...";
$Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown');