using System.Net.Sockets;
using StreamJsonRpc;

namespace HelloWorld;


class Program
{
    static async Task Main()
    {
        string ip_addr = "192.168.242.128";
        int ip_port = 3333;
        Console.WriteLine($"Hello, Rob! Connect to {ip_addr}:{ip_port}");
        TcpClient client = new TcpClient(ip_addr, ip_port);
        NetworkStream stream = client.GetStream();
        using var message_handler = new NewLineDelimitedMessageHandler(stream, stream, new JsonMessageFormatter());
        using (JsonRpc jsonRpc = new JsonRpc(message_handler))
        {
            jsonRpc.StartListening();
            
            // {'method':'auth','params':['My_example_KEY'],'id':1}
            string answer = await jsonRpc.InvokeAsync<string>("auth", "My_example_KEY");
            Console.WriteLine($"Authorization:{answer}");
            
            // {'method':'Var_ShowVar','params':['$TRAFONAME[]'],'id':1}
            answer = await jsonRpc.InvokeAsync<string>("Var_ShowVar", "$TRAFONAME[]");
            Console.WriteLine($"Robot model:{answer}");

            // {'method':'File_NameList','params':['KRC:\\R1',511,127],'id':1}
            string root_path = "KRC:\\R1";
            Dictionary<string, string> flist = await jsonRpc.InvokeAsync<Dictionary<string, string>>("File_NameList", root_path, 511, 127);
            Console.WriteLine($"List of files in {root_path}:\n");
            foreach (KeyValuePair<string, string> kvp in flist)
            {
                Console.WriteLine($"{kvp.Key} \t: {kvp.Value}");
            }

            // {'method':'File_CopyFile2Mem','params':['/R1/test2.src'],'id':1}
            string f_name = "/R1/test.dat";
            answer = await jsonRpc.InvokeAsync<string>("File_CopyFile2Mem", f_name);
            Console.WriteLine($"File {f_name} content:\n------------\n{answer}\n-------------\n");
        }
        stream.Close();
        client.Close();
    }

}