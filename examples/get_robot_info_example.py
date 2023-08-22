import socket
import json
import re

IP_ADDR = '192.168.121.156'
PORT = 3333

var_info = {
    "Robot model":"$TRAFONAME[]",
    "System":"$RCV_INFO[]",
    "Operation mode":"$MODE_OP",    
    "Stop message":"$STOPMESS",
    "User level":"$USER_LEVEL",
    "On path":"$ON_PATH"
}

def request(command, client):
    if not command.endswith('\n'):
        command += '\n'
    command = command.encode()
    client.send(command)
    resp = b''
    while not resp.endswith(b'\n'):
        resp += client.recv(32)
    return json.loads(resp)

def show_rob_info(client):
    for key, val in var_info.items():
        try:
            resp = request("{'method':'Var_ShowVar','params':['%s'],'id':1}\n" % val, client)['result']
            print(key, ":", resp)
        except:
            print(key, "not available")

def show_prog_info(client):
    prog_raw = request("{'method':'Var_ShowVar','params':['$PROG_INFO[]'],'id':1}\n", client)['result']
    progs = re.findall(r'\{PROG_INFO:(.+?)\}', prog_raw)
    progs = [dict([n.strip().replace('" "','""').split(' ') for n in p.strip().split(',')]) for p in progs]
    print("Active interpreters:")
    for prog in progs:
        if prog['P_STATE'] != '#P_FREE':
            print('\t%s\t[%s]\t(%s)' % (prog['SEL_NAME[]'], prog['P_STATE'], prog['PRO_IP_SNR']))

client = socket.socket()
client.connect((IP_ADDR, PORT))

show_rob_info(client)
show_prog_info(client)

resp = request("{'method':'listMethods','params':[],'id':1}\n", client)
methods = ['\t'+m.replace('System.', '') for m in resp['result']]

print('\nRPC methods:')
print ('\n'.join(methods))

client.close()