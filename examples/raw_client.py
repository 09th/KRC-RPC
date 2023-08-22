import socket
import time

client = socket.socket()
client.connect(('192.168.121.156', 3333))


start_time = time.time()
message = "{'method':'auth','params':['My_example_KEY'],'id':1}\n".encode()
print(">>>\t", message)
client.send(message)
print("<<<\t", client.recv(1024))
print("--- auth (first query) %s seconds ---" % (time.time() - start_time))


start_time = time.time()
message = "{'method':'Var_ShowVar','params':['$TRAFONAME[]'],'id':2}\n".encode()
print(">>>\t", message)
client.send(message)
print("<<<\t", client.recv(1024))
print("--- single query %s seconds ---" % (time.time() - start_time))

start_time = time.time()
message = "{'method':'Var_ShowVar','params':['$AXIS_ACT'],'id':2}\n".encode()
print(">>>\t", message)
client.send(message)
print("<<<\t", client.recv(1024))
print("--- single query %s seconds ---" % (time.time() - start_time))

client.close()

input("Any key to close...")