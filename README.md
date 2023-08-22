## KRC-RPC

Implementation of the JSON-RPC TCP socket server for the KUKA robot Cross3Krc COM service. Allows to remote acces and control of some robot variables and functions.
This project is inspired by [KukavarProxy](https://github.com/ImtsSrl/KUKAVARPROXY)

## Features (WIP)
#### Common
* Standart JSON-RPC protocol for the interacting with the robot
* Authorization and control of authorized/unauthorized access to methods
* SSL support
#### Robot
* Read and write global variables
* Read and write variables from the specific .dat file
* Robot interpreter control
* Submit interpreter control

## Build
Install NET core SDK 6 or greater. Find PrimaryInterOp.Cross3Krc.dll on the robot controller (e.g. in the C:\KRC\SERVICES\WorkVisualServiceHost) and copy it to the "server" directory of the project. Run build.bat from the "server" directory. 

At the first build run, an internet connection should be available to download missing dependency packages.

If changing build commands, you must take into account that target architecture should stay X86 in according to Cross COM service.

## Install
1. Make the necessary changes to the appsettings.json (tcp port, auth key, available methods, SSL e.t.c.)
2. Сonfigure robot NAT to connect to the port selected in the appsettings.json. Startup->Network configuration->Advanced->NAT->Add port. Reboot "with reload files" to apply changes. 
3. Minimize HMI to access the host Windows on the KRC cabinet Startup->Services->Minimize HMI (Administrator account required). Сopy the binaries somewhere on the system, e.g. D:\KRC_RPC
4. Launch KRC_RPC.EXE 

## Usage
#### RAW socket
You can use PuTTY in raw mode to test. For example, to get a list of implemented methods, you can send 
```
{'method':'listMethods','params':[],'id':1}
``` 
or 
```
{'method':'auth','params':['My_example_KEY'],'id':1}
```
to try authorize on the server. More examples can be found in `examples/command_examples.txt`

`examples/raw_client.py` - an example of using python as a raw socket client

`examples/raw_client.ps1` - same for the windows power shell

#### SSL
`examples/ssl/ssl_client.py` - using python to connect via SSL and retrieve some data. Also the `examples/ssl` directory contains examples of server and client certificates + appsettings.json with the SSL option enabled.

## Links
* [KukavarProxy](https://github.com/ImtsSrl/)
* [JSON-RPC.net](https://github.com/Astn/JSON-RPC.NET)
