﻿// See https://aka.ms/new-console-template for more information

using AustinHarris.JsonRpc;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using PrimaryInterOp.Cross3Krc;
using System.Reflection;
//using System.Configuration.ConfigurationManager;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security.Authentication;

namespace KrcRpc_socket_server
{
    public class ClientContext
    {
        public bool auth;
        public string key ;
        public TcpClient client;
        public int failed_auth_count;

        public ClientContext()
        {
            this.client = null;
            this.auth = false;
            this.key = "";
            this.failed_auth_count = 0;
        }
    }


    class Program
    {
        private static KrcRpcService _svc;
        static void Main(string[] args)
        {
            // must new up an instance of the service so it can be registered to handle requests.
            _svc = new KrcRpcService();

            var rpcResultHandler = new AsyncCallback(
                state =>
                {
                    var async = ((JsonRpcStateAsync)state);
                    var result = async.Result;
                    var writer = ((StreamWriter)async.AsyncState);
                    if (_svc.cfgVerboseLvl > 1)
                        Console.WriteLine("RESPONSE: {0}", result);
                    writer.WriteLine(result);
                    writer.FlushAsync();
                });

            SocketListener listener = new();
            listener.Start((writer, line, context) =>
            {
                var async = new JsonRpcStateAsync(rpcResultHandler, writer) { JsonRpc = line };
                //JsonRpcProcessor.Process(async, writer);
                JsonRpcProcessor.Process(async, context);
            });
        }
    }


    class KrcRpcService : JsonRpcService
    {
        private readonly KrcServiceFactoryClass objServiceFactory;
        private readonly ICKSyncVar2 itfSyncvar;
        private readonly ICKSyncSelect3 itfSyncselect;
        private readonly ICKSyncKcpKey itfSynckcpkey;
        private readonly ICKSyncMotion itfSyncmotion;
        private readonly ICKSyncFile itfSyncfile;
        private readonly bool cfgAuthRequired;
        private readonly string[] cfgUnauthMethods;
        private readonly string[] cfgForbiddenMethods;
        private readonly string cfgAuthKey;
        public readonly int cfgVerboseLvl;

        public KrcRpcService(){
            objServiceFactory = new KrcServiceFactoryClass();
            itfSyncvar = (ICKSyncVar2)objServiceFactory.GetService("WBC_KrcLib.SyncVar", "CrossCom");
            itfSyncselect = (ICKSyncSelect3)objServiceFactory.GetService("WBC_KrcLib.SyncSelect", "CrossCom");
            itfSynckcpkey = (ICKSyncKcpKey)objServiceFactory.GetService("WBC_KrcLib.SyncKcpKey", "CrossCom");
            itfSyncmotion = (ICKSyncMotion)objServiceFactory.GetService("WBC_KrcLib.SyncMotion", "CrossCom");
            itfSyncfile = (ICKSyncFile)objServiceFactory.GetService("WBC_KrcLib.SyncFile", "CrossCom");
            IConfiguration appconfig = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();
            cfgAuthRequired = appconfig.GetSection("authRequired").Get<bool>();
            cfgAuthKey = appconfig["authKey"];
            cfgUnauthMethods = appconfig.GetSection("unauthMethods").Get<string[]>();
            cfgForbiddenMethods = appconfig.GetSection("forbiddenMethods").Get<string[]>();
            cfgVerboseLvl = appconfig.GetSection("verboseLvl").Get<int>();
            Config.SetPreProcessHandler(new PreProcessHandler(PreProcess));
            Config.SetParseErrorHandler(ParseErrorHandler);
        }

        private JsonRpcException PreProcess(JsonRequest rpc, object context)
        {
            ClientContext clientContext = (ClientContext) context;
            //Console.WriteLine(clientContext.auth);
            if (cfgForbiddenMethods is not null)
                if (cfgForbiddenMethods.Contains(rpc.Method, StringComparer.OrdinalIgnoreCase)){
                    return new JsonRpcException(-3, "This method is forbidden by config.", null);
                }
            if (cfgUnauthMethods is not null)
                if (cfgAuthRequired 
                    & !clientContext.auth
                    & !cfgUnauthMethods.Contains(rpc.Method, StringComparer.OrdinalIgnoreCase)){
                        return new JsonRpcException(-2, "This method requires authentication.", null);
                }
            return null;
        }

        private JsonRpcException ParseErrorHandler(string line, JsonRpcException ex){
            return new JsonRpcException(-10, "Json format error", null);
        }


        // -------------- generic try-catch for Cross call [begin] -----------------------
        // void call(<T>)
        private string TryCrossComCall<T>(T p, Action<T> a){
            try
            {
                a(p);
                return "Ok";
            }
            catch (Exception e)
            {
                if (cfgVerboseLvl > 1)
                    Console.WriteLine("exception " + e);
                JsonRpcContext.SetException(new JsonRpcException(-27000, e.Message, null));
                return "Error";
            }
        }

        // string call(<T>)
        private string TryCrossComCall<T>(T p, Func<T, string> f){
            try
            {
                return f(p);
            }
            catch (Exception e)
            {
                if (cfgVerboseLvl > 1)
                    Console.WriteLine("exception " + e);
                JsonRpcContext.SetException(new JsonRpcException(-27000, e.Message, null));
                return "Error";
            }
        }

        // void call(<T1>, <T2>)
        private string TryCrossComCall<T1, T2>(T1 p1, T2 p2, Action<T1, T2> a){
            try
            {
                a(p1, p2);
                return "Ok";
            }
            catch (Exception e)
            {
                if (cfgVerboseLvl > 1)
                    Console.WriteLine("exception " + e);
                JsonRpcContext.SetException(new JsonRpcException(-27000, e.Message, null));
                return "Error";
            }
        }

        // string call(<T1>, <T2>)
        private string TryCrossComCall<T1, T2>(T1 p1, T2 p2, Func<T1, T2, string> f){
            try
            {
                return f(p1, p2);
            }
            catch (Exception e)
            {
                if (cfgVerboseLvl > 1)
                    Console.WriteLine("exception " + e);
                JsonRpcContext.SetException(new JsonRpcException(-27000, e.Message, null));
                return "Error";
            }
        }

        // void call(<T1>, <T2>, <T3>)
        private string TryCrossComCall<T1, T2, T3>(T1 p1, T2 p2, T3 p3, Action<T1, T2, T3> a){
            try
            {
                a(p1, p2, p3);
                return "Ok";
            }
            catch (Exception e)
            {
                if (cfgVerboseLvl > 1)
                    Console.WriteLine("exception " + e);
                JsonRpcContext.SetException(new JsonRpcException(-27000, e.Message, null));
                return "Error";
            }
        }

        // string call(<T1>, <T2>, <T3>)
        private string TryCrossComCall<T1, T2, T3>(T1 p1, T2 p2, T3 p3, Func<T1, T2, T3, string> f){
            try
            {
                return f(p1, p2, p3);
            }
            catch (Exception e)
            {
                if (cfgVerboseLvl > 1)
                    Console.WriteLine("exception " + e);
                JsonRpcContext.SetException(new JsonRpcException(-27000, e.Message, null));
                return "Error";
            }
        }

        // void call(<T1>, <T2>, <T3>, <T4>)
        private string TryCrossComCall<T1, T2, T3, T4>(T1 p1, T2 p2, T3 p3, T4 p4, Action<T1, T2, T3, T4> a){
            try
            {
                a(p1, p2, p3, p4);
                return "Ok";
            }
            catch (Exception e)
            {
                if (cfgVerboseLvl > 1)
                    Console.WriteLine("exception " + e);
                JsonRpcContext.SetException(new JsonRpcException(-27000, e.Message, null));
                return "Error";
            }
        }

        delegate void ActionRef45<T1, T2, T3, T4, T5>(T1 p1, T2 p2, T3 p3, ref T4 p4, ref T5 p5);
        private string TryCrossComCall<T1, T2, T3, T4, T5>(T1 p1, T2 p2, T3 p3, ref T4 p4, ref T5 p5, ActionRef45<T1, T2, T3, T4, T5> a){
            try
            {
                a(p1, p2, p3, ref p4, ref p5);
                return "Ok";
            }
            catch (Exception e)
            {
                if (cfgVerboseLvl > 1)
                    Console.WriteLine("exception " + e);
                JsonRpcContext.SetException(new JsonRpcException(-27000, e.Message, null));
                return "Error";
            }
        }

        // -------------- generic try-catch for Cross call [end] -----------------------


        [JsonRpcMethodAttribute]
        private string auth(string authKey){
            ClientContext context = (ClientContext) JsonRpcContext.Current().Value;
            if (cfgAuthKey == authKey){
                context.key =  authKey;
                context.auth = true;
                return "Ok";
            } else {
                context.failed_auth_count++;
                JsonRpcContext.SetException(new JsonRpcException(-3, "Authentication failed. Wrong key.", null));
                return "Error";
            }
        }


        [JsonRpcMethodAttribute]
        private string[] listMethods(){
            var methods = this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(m=>m.GetCustomAttributes(typeof(JsonRpcMethodAttribute), false).Length > 0)
                        .ToArray();
            return methods.Select(x=>x.ToString()).ToArray();
        }

        [JsonRpcMethodAttribute]
        private string Kcpkey_Start(bool backward){
            return TryCrossComCall<EKInterpreter, bool, bool>(EKInterpreter.eInterpreterRobot, backward, false, itfSynckcpkey.Start);
        }

        [JsonRpcMethodAttribute]
        private string Motion_LinRel(string val){
            return TryCrossComCall<string>(val, itfSyncmotion.LinRel);
        }

        // ------- WBC_KrcLib.SyncSelect functions [begin] ------------
        [JsonRpcMethodAttribute]
        private string Select_Select(string fileName){
            return TryCrossComCall<string, string, bool>(fileName, "", true, itfSyncselect.Select);
        }

        [JsonRpcMethodAttribute]
        private string Select_Run(string fileName){
            return TryCrossComCall<string, string, bool>(fileName, "", true, itfSyncselect.Run);
        }

        [JsonRpcMethodAttribute]
        private string Select_SelectSubmit(string fileName, short idx){
            return TryCrossComCall<string, string, short, bool>(fileName, "", idx, true, itfSyncselect.SelectSubmit);
        }

        [JsonRpcMethodAttribute]
        private string Select_Cancel(){
            return TryCrossComCall<EKInterpreter>(EKInterpreter.eInterpreterRobot, itfSyncselect.Cancel);
        }

        [JsonRpcMethodAttribute]
        private string Select_Stop(short interpreter){
            return TryCrossComCall<EKInterpreter>((EKInterpreter) interpreter, itfSyncselect.Stop);
        }

        [JsonRpcMethodAttribute]
        private string Select_Start(){
            return TryCrossComCall<EKInterpreter>(EKInterpreter.eInterpreterRobot, itfSyncselect.Start);
        }

        [JsonRpcMethodAttribute]
        private string Select_Reset(short interpreter){
            return TryCrossComCall<EKInterpreter>((EKInterpreter) interpreter, itfSyncselect.Reset);
        }

        [JsonRpcMethodAttribute]
        private string Select_StopSubmit(short idx){
            return TryCrossComCall<short>(idx, itfSyncselect.StopSubmit);
        }

        [JsonRpcMethodAttribute]
        private string Select_CancelSubmit(short idx){
            return TryCrossComCall<short>(idx, itfSyncselect.CancelSubmit);
        }
        // ------- WBC_KrcLib.SyncSelect functions [end] ------------

        // ------- WBC_KrcLib.SyncVar functions [begin] ------------
        [JsonRpcMethodAttribute]
        private string Var_ShowVar(string varName)
        {
            return TryCrossComCall<string>(varName, itfSyncvar.ShowVar);
        }

        [JsonRpcMethodAttribute]
        private string Var_ShowVarDP(string datFile, string varName)
        {
            return TryCrossComCall<string, string>(datFile, varName, itfSyncvar.ShowVarDP);
        }

        [JsonRpcMethodAttribute]
        private string Var_SetVar(string varName, string val){
            return TryCrossComCall<string, string>(varName, val, itfSyncvar.SetVar);
        }

        [JsonRpcMethodAttribute]
        private string Var_SetVarDP(string datFile, string varName, string val){
            return TryCrossComCall<string, string, string>(datFile, varName, val, itfSyncvar.SetVarDP);
        }
        // ------- WBC_KrcLib.SyncFile functions [end] ------------
        
        [JsonRpcMethodAttribute]
        private Dictionary<String, String> File_NameList(string path, int fType, int flags){
            object pvarNames = null;
            object pvarInfo = null; 
            String res = TryCrossComCall<string, int, int, object, object>(path, fType, flags, ref pvarNames, ref pvarInfo, itfSyncfile.NameList);
            if (res == "Ok"){
                var names = (String[])pvarNames;
                var info = (String[])pvarInfo;
                var files = new Dictionary<String, String>();
                for (int i = 0; i < names.Length; i++){
                    files[names[i]] = info[i];
                }
                return files;
            } else {
                return null;
            }
        }
        
        [JsonRpcMethodAttribute]
        private string File_Copy(string src, string dest, int flags){
            return TryCrossComCall<string, string, int>(src, dest, flags, itfSyncfile.Copy);
        }

        [JsonRpcMethodAttribute]
        private string File_Delete(string fullName){
            return TryCrossComCall<string, bool>(fullName, true, itfSyncfile.Delete);
        }

        [JsonRpcMethodAttribute]
        private string File_GetFullName(string krcName){
            return TryCrossComCall<string>(krcName, itfSyncfile.GetFullName);
        }

        [JsonRpcMethodAttribute]
        private string File_CopyFile2Mem(string fullName){
            try
            {
                object fdat = itfSyncfile.CopyFile2Mem(fullName, 0);
                return System.Text.Encoding.Default.GetString((System.Byte[])fdat);
            }
            catch (Exception e)
            {
                if (cfgVerboseLvl > 1)
                    Console.WriteLine("exception " + e);
                JsonRpcContext.SetException(new JsonRpcException(-27000, e.Message, null));
                return "Error";
            }
        }

        [JsonRpcMethodAttribute]
        private string File_CopyMem2File(string fdat, string fullName, int nFlags){
            /*
        public enum CopyFlags
        {
            None = 0,
            Archive = 1,
            Modify = 3,
            Continue = 4,
            Recursive = 8,
            Refresh = 16,
            Update = 48,
            OverwriteExist = 64,
            NoDirEntries = 128,
            JunkDir = 256,
            ForceBinary = 512,
            ForceText = 1024,
            NoVersionCheck = 2048,
            OverwriteReadonly = 4096,
            NoKrlAnalysis = 8192
        }
            */
            try
            {
                itfSyncfile.CopyMem2File(System.Text.Encoding.Default.GetBytes(fdat), fullName, nFlags);
                return "Ok";
            }
            catch (Exception e)
            {
                if (cfgVerboseLvl > 1)
                    Console.WriteLine("exception " + e);
                JsonRpcContext.SetException(new JsonRpcException(-27000, e.Message, null));
                return "Error";
            }
        }

    }


    public class SocketListener
    {
        private readonly IPAddress cfgListenAddress;
        private readonly int cfgListenPort;
        private readonly bool cfgSsl;
        private readonly X509Certificate cfgServerCertificate;
        private readonly int cfgVerboseLvl;
        private TcpListener server;


        public SocketListener()
        {
            IConfiguration config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();
            cfgListenAddress = IPAddress.Any;
            IPAddress.TryParse(config["listenAddress"], out cfgListenAddress);
            cfgListenPort = 9900;
            Int32.TryParse(config["listenPort"], out cfgListenPort);
            cfgSsl = false;
            Boolean.TryParse(config["ssl"], out cfgSsl);
            cfgServerCertificate = cfgSsl == true ? X509Certificate.CreateFromCertFile(config["cert"]) : null;
            cfgVerboseLvl = config.GetSection("verboseLvl").Get<int>();
        }

        public void Start(Action<StreamWriter, string, ClientContext> handleRequest)
        {
            //var objServiceFactory = new KrcServiceFactoryClass();
            //var itfSyncvar = (ICKSyncVar)objServiceFactory.GetService("WBC_KrcLib.SyncVar", "CrossCom");
            //string sRobmodel = itfSyncvar.ShowVar("$TRAFONAME[]");
            //string sSysteminfo = itfSyncvar.ShowVar("$RCV_INFO[]");
            //Console.WriteLine(sSysteminfo);
            //Console.WriteLine(sRobmodel);
            //var server = new TcpListener(IPAddress.Parse("127.0.0.1"), listenPort);


            server = new TcpListener(cfgListenAddress, cfgListenPort);
            server.Start();
            //Console.WriteLine(" You can connected with Putty on a (RAW session) to {0} to issue JsonRpc requests.", server.LocalEndpoint);
            if (cfgVerboseLvl > 0)
                Console.WriteLine("JSON-RPC-to-Cross3 server starts on {0} {1}", server.LocalEndpoint, cfgSsl ? "(SSL)" : "");
            while (true)
            {
                try
                {
  
                    TcpClient client = server.AcceptTcpClient();
                    Task.Run( () => this.ProcessClientAsync(client, handleRequest) );
                }
                catch (Exception e)
                {
                    if (cfgVerboseLvl > 0)
                        Console.WriteLine("RPCServer exception " + e);
                }
            }
        }

        private SslStream SslWrapStream(NetworkStream stream)
        {
            var sslStream = new SslStream(stream, false);
            
            //X509Certificate serverCertificate = X509Certificate.CreateFromCertFile("mycert.pfx");
            //try {
                sslStream.AuthenticateAsServer(cfgServerCertificate, 
                        clientCertificateRequired: false,
                        checkCertificateRevocation: true);
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine("Exception: {0}", e.Message);
            //    if (e.InnerException != null)
            //    {
            //        Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
            //    }
            //    sslStream.Close();
            //    //client.Close();
            //    return null;
            //}

            return sslStream;
        }

        private void ProcessClientAsync(TcpClient client, Action<StreamWriter, string, ClientContext> handleRequest){
            StreamReader reader = null;
            StreamWriter writer = null;
            SslStream sslStream = null;
            NetworkStream stream = null;
            try 
            {
                stream = client.GetStream();
                if (cfgVerboseLvl > 0)
                    Console.WriteLine($"Client Connected: {client.Client.RemoteEndPoint}");

                if (cfgSsl){
                    sslStream = SslWrapStream(stream);
                    reader = new StreamReader(sslStream, Encoding.UTF8);
                    writer = new StreamWriter(sslStream, new UTF8Encoding(false));
                } else {
                    reader = new StreamReader(stream, Encoding.UTF8);
                    writer = new StreamWriter(stream, new UTF8Encoding(false));
                }

                //var reader = new StreamReader(sslStream, Encoding.UTF8);
                //var writer = new StreamWriter(sslStream, new UTF8Encoding(false));
                var context = new ClientContext(){client = client};
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (cfgVerboseLvl > 1)
                        Console.WriteLine("REQUEST: {0}", line);
                    handleRequest(writer, line, context);
                }
            }
            catch (Exception e)
            {
                if (cfgVerboseLvl > 0)
                {
                    Console.WriteLine("Exception: {0}", e.Message);
                    if (e.InnerException != null)
                    {
                        Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                    }
                }
            }
            finally
            {
                if (cfgVerboseLvl > 0)
                    Console.WriteLine("Client Disconnect: {0}", client.Client.RemoteEndPoint);
                stream.Close();
                if (cfgSsl) sslStream.Close();
                client.Close();
            }

        }
    }
}