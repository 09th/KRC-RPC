#import os
import tkinter as tk
import tkinter.ttk as ttk
import socket
import json

ROOT = r'KRC:\R1'
ADDR = '192.168.121.156'
PORT = 3333
AUTH_KEY = 'My_example_KEY'

# Class for data exchange with the KRC-RPC service 
class RPC_Conn():
    def __init__(self, addr, port, a_key):
        self._message = ''
        self._responce = ''
        self.client = socket.socket()
        self.client.connect((addr, port))
        self.on_message_listeners = []
        self.on_responce_listeners = []
        self.message_silent = False
        # in the default appsettings.json all methods except ShowVar requres authenticate
        self.message = "{'method':'auth','params':['%s'],'id':1}" % a_key

        self.f_data = {} # file data recived from robot

    @property
    def responce(self):
        return self._responce

    @property
    def message(self):
        return self._message
    
    @message.setter
    def message(self,val):
        self._message = val
        if not self.message_silent:
            for f in self.on_message_listeners:
                f(self._message)
        self._responce = self._do_request(self._message, self.client)
        if not self.message_silent:
            for f in self.on_responce_listeners:
                f(self._responce)


    # Send json command and read result
    def _do_request(self, command, client):
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
    def add_to_tree(self, root, f_path):
        el = f_path[0]
        if len(el) > 0:
            if el not in root.keys():
                root[el] = {}
            if len(f_path) > 1:
                self.add_to_tree(root[el], f_path[1:])

    def request_and_parse_tree(self, root, dict_tree):
        cmd = {'method':'File_NameList','params':[root, 511, 127],'id':1}
        self.message = json.dumps(cmd)
        #self.responce = self.do_request(self.message, self.client)
        try:
            self.f_data = self.responce['result']
            # Convert retrived data from robot to the tree
            for k, v in self.f_data.items():
                self.add_to_tree(dict_tree, k.split('\\'))
        except:
            print('Error on file tree request')
            print(self.responce)

    def request_status(self):
        vlist = ['$PRO_NAME[]', '$PRO_STATE', '$MODE_OP']
        msglist = []
        for i,v in enumerate(vlist):
            msglist.append({'method':'Var_ShowVar','params':[v],'id':i})
        self.message = json.dumps(msglist)
        #self.responce = self.do_request(self.message, self.client)
        #print(self.responce)
        return [m['result'] for m in self.responce]

# UI class
class App(object):
    def __init__(self, master, args):
        self.master = master
        self.args = args

        self.statusColors = {
            '#P_FREE':'#EEEEEE',
            '#P_ACTIVE':"green",
            '#P_RESET':"yellow",
            '#P_STOP':'red',
            '#P_END':'#777777'
        }
        self.strStatus=tk.StringVar() 
        self.lblStatus=tk.Label(master, bd=1, relief=tk.SUNKEN, anchor=tk.W,
                           textvariable=self.strStatus,
                           font=('arial',10,'bold'),
                           padx = 5)
        self.lblStatus.pack(fill=tk.X)  

        self.create_toolbar(
            {
                'Select':self.cmd_select,
                'Satrt':self.cmd_start,
                'Stop':self.cmd_stop,
                'Reset':self.cmd_reset,
                'Cancel':self.cmd_cancel
            }
        )
        
        self.nodes = {}
        self.tree = ttk.Treeview(master)
        master.geometry('500x600')
        self.tree['columns'] = ('#props')
        self.tree.pack(fill=tk.BOTH, expand=tk.YES)
        
        text_box = tk.Text(
            master,
            height=12,
            width=40
        )
        text_box.pack(fill=tk.X)
        text_box.config(state='disabled')
        self.txtLog = text_box

        self.tree.heading('#0', text='Tree')
        self.tree.heading('#props', text='Props')

        
        self.RPC = RPC_Conn(self.args.address, self.args.port, self.args.key)
        self.f_tree = {}
        self.RPC.request_and_parse_tree(self.args.root, self.f_tree)
        for k in self.f_tree.keys():
            self.insert_node('', k, k)
        
        # Lazy tree open
        self.tree.bind('<<TreeviewOpen>>', self.open_node)

        self.RPC.on_message_listeners.append(self.on_message)
        self.RPC.on_responce_listeners.append(self.on_responce)
        self.update_status()

    def cmd_select(self):
        for node in self.tree.selection():
            abspath = self.args.root + '\\' + self.nodes[node]
            if abspath.endswith(('.dat','.src')):
                #print(abspath)
                if abspath.endswith('.dat'):
                    abspath = abspath[:-4]+'.src'
                cmd = {'method':'Select_Select','params':[abspath],'id':1}
                self.RPC.message = json.dumps(cmd)
                break
    
    def cmd_cancel(self):
        cmd = {'method':'Select_Cancel','params':[],'id':1}
        self.RPC.message = json.dumps(cmd)

    def cmd_start(self):
        cmd = {'method':'Select_Start','params':[],'id':1}
        self.RPC.message = json.dumps(cmd)

    def cmd_stop(self):
        cmd = {'method':'Select_Stop','params':[1],'id':1}
        self.RPC.message = json.dumps(cmd)

    def cmd_reset(self):
        cmd = {'method':'Select_Reset','params':[1],'id':1}
        self.RPC.message = json.dumps(cmd)

    def update_status(self):
        self.RPC.message_silent = True
        status = self.RPC.request_status()
        for k,v in self.statusColors.items():
            if k in status:
                self.lblStatus.config(bg=v)
                break
        status_str = '%s:\t%s' % (self.args.address, ' '.join(status))
        self.strStatus.set(status_str)
        self.RPC.message_silent = False
        self.master.after(1000, self.update_status)

    def create_toolbar(self, buttons):
        self.toolbarFrame = tk.Frame(self.master, bg='gray')
        self.toolbarButtons = []
        for k,v in buttons.items():
            b = tk.Button(self.toolbarFrame, text=k, command=v)
            b.pack(side=tk.TOP, fill=tk.X, padx=2, pady=2)
            self.toolbarButtons.append(b)
        self.toolbarFrame.pack(side=tk.LEFT, fill=tk.Y)
    
    def on_message(self, msg):
        self.txtLog.config(state='normal')
        self.txtLog.insert('end', '>>> %s\n' % msg)
        self.txtLog.see('end')
        self.txtLog.config(state='disabled')

    def on_responce(self, resp):
        self.txtLog.config(state='normal')
        self.txtLog.insert('end', '<<< %s\n' % str(resp))
        self.txtLog.see('end')
        self.txtLog.config(state='disabled')

    def insert_node(self, parent, text, abspath):
        node = self.tree.insert(parent, 'end', text=text, open=False)
        # add file proiperties to the second collumn
        self.nodes[node] = abspath
        if abspath in self.RPC.f_data.keys():
            self.tree.set(node, '#props', self.RPC.f_data[abspath])
        if len(self.listdir(abspath))>0:
            #self.nodes[node] = abspath
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
    # Refreshing node content. May be extended to get actual data from robot
    def open_node(self, event):
        node = self.tree.focus()
        abspath = self.nodes.pop(node, None)
        if abspath:
            self.tree.delete(self.tree.get_children(node))
            for p in self.listdir(abspath):
                self.insert_node(node, p, abspath + '\\' + p)



if __name__ == '__main__':
    from argparse import ArgumentParser

    parser = ArgumentParser()
    parser.add_argument("-a", "--address",default=ADDR, type=str,
                        help="IP address of KRC-RPC service (default %s)" % ADDR)
    parser.add_argument("-p", "--port", default=PORT, type=int,
                        help="TCP port of KRC-RPC service (default %s)" % PORT)
    parser.add_argument("-r", "--root", default=ROOT, type=str,
                        help="Root directory (default %s)" % ROOT)
    parser.add_argument("-k", "--key", default=AUTH_KEY, type=str,
                        help="Authentication key (default %s)" % AUTH_KEY)
    
    args = parser.parse_args()
    #print(args)

    tk_root = tk.Tk()
    app = App(tk_root, args)
    tk_root.mainloop()