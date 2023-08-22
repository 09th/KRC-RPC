import socket
import ssl
import os

context = ssl.create_default_context()
context.check_hostname = False
cert_path = os.path.dirname(os.path.abspath(__file__))
cert_path = os.path.join(cert_path, 'shared.cer')
context.load_verify_locations(cert_path)
client = socket.socket()
client.connect(('192.168.121.156', 3333))
sclient = context.wrap_socket(client)
print('---', sclient.version(), '---')
message = "{'method':'Var_ShowVar','params':['$TRAFONAME[]'],'id':2}\n".encode()
print(">>>\t", message)
sclient.send(message)
print("<<<\t", sclient.recv(1024))
sclient.close()
input("Any key to close...")