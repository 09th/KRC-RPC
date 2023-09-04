#import os
import tkinter as tk
import tkinter.ttk as ttk
import socket
import json

ROOT = 'KRC:\\\\R1'
ADDR = '192.168.121.156'
PORT = 3333
AUTH_KEY = 'My_example_KEY'

# Send json command and read result
def request(command, client):
    if not command.endswith('\n'):
        command += '\n'
    command = command.encode()
    client.send(command)
    resp = b''
    while not resp.endswith(b'\n'):
        resp += client.recv(256)
    return json.loads(resp)

# Recursive addition of path parts to the tree
# f_path is list of names splitted from path like 'My\\path\\to\\file.res'.split('\\')
def add_to_tree(root, f_path):
    el = f_path[0]
    if len(el) > 0:
        if el not in root.keys():
            root[el] = {}
        if len(f_path) > 1:
            add_to_tree(root[el], f_path[1:])


class App(object):
    def __init__(self, master, path, f_data):
        self.nodes = {}
        self.tree = ttk.Treeview(master)
        master.geometry('500x400')
        self.tree['columns'] = ('#props')
        self.tree.pack(fill=tk.BOTH, expand=tk.YES)
        self.f_data = f_data
        self.f_tree = {}
        self.tree.heading('#0', text='Tree')
        self.tree.heading('#props', text='Props')

        # Convert retrived data from robot to the tree
        for k, v in self.f_data.items():
            add_to_tree(self.f_tree, k.split('\\'))

        # Root nodes
        for k in self.f_tree.keys():
            self.insert_node('', k, k)
        
        # Lazy tree open
        self.tree.bind('<<TreeviewOpen>>', self.open_node)

    def insert_node(self, parent, text, abspath):
        node = self.tree.insert(parent, 'end', text=text, open=False)
        # add file proiperties to the second collumn
        if abspath in self.f_data.keys():
            self.tree.set(node, '#props', self.f_data[abspath])
        if len(self.listdir(abspath))>0:
            self.nodes[node] = abspath
            self.tree.insert(node, 'end')
    
    # Traverse the tree to the last node of given path and get its child nodes    
    def listdir(self, path):
        p_spl = path.split('\\')
        res = self.f_tree[p_spl[0]]
        if len(p_spl)>1:
            for el in p_spl[1:]:
                if len(el)>0:
                    res = res[el]
        return res.keys()
                
    # Callback for the <<TreeviewOpen>>
    # Refreshing node content. May be extended to retrive actual data from robot
    def open_node(self, event):
        node = self.tree.focus()
        abspath = self.nodes.pop(node, None)
        if abspath:
            self.tree.delete(self.tree.get_children(node))
            for p in self.listdir(abspath):
                self.insert_node(node, p, abspath + '\\' + p)



if __name__ == '__main__':
    client = socket.socket()
    client.connect((ADDR, PORT))
    # in the default appsettings.json all methods except ShowVar requres authenticate
    message = "{'method':'auth','params':['%s'],'id':1}" % AUTH_KEY
    request(message, client)
    message = "{'method':'File_NameList','params':['%s', 511, 127],'id':1}" % ROOT
    res = request(message, client)['result']
    root = tk.Tk()
    app = App(root, '', res)
    root.mainloop()