using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace GDrive
{
    class AuthRedirectionServer
    {
        string authorizationCode = null;

        public string AuthorizationCode
        {
            get
            {
                return authorizationCode;
            }
            private set
            {
                authorizationCode = value;
            }
        }

        TcpListener server = null;
        Thread listenThread = null;

        public bool StartServer(int port)
        {
            try
            {
                server = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
                server.Start();

                listenThread = new Thread(Listen);
                listenThread.Start();
            }
            catch (Exception e)
            {
                Debug.LogError(e);

                if (server != null)
                    server.Stop();

                server = null;

                return false;
            }

            return true;
        }

        void Listen()
        {
            while (AuthorizationCode == null)
            {

                TcpClient client = server.AcceptTcpClient();
                NetworkStream stream = client.GetStream();
                stream.ReadTimeout = 2000;

                //Thread.Sleep(100);

                //stream.WriteByte(0);
                //stream.Flush();

                MemoryStream ms = new MemoryStream();
                byte[] bytes = new byte[4096];
                int readBytes;

                while ((readBytes = stream.Read(bytes, 0, bytes.Length)) > 0)
                {
                    ms.Write(bytes, 0, readBytes);
                    ms.Flush();

                    string s = Encoding.UTF8.GetString(ms.ToArray());
                    if (s.Contains("\r\n\r\n"))
                    {
                        // get auth code
                        string code = s.Substring(0, s.IndexOf("\r\n")).Split(' ')[1];
					
                        // checking if request has auth code
                        int index = code.IndexOf("/?code=");
                        if (index == -1)
                        {
                            byte[] header404 = Encoding.UTF8.GetBytes(
                                "HTTP/1.1 404 Not Found\r\n" +
                                "\r\n\r\n");
                            stream.Write(header404, 0, header404.Length);
                            break;
                        }
                        // check end
					
                        code = code.Substring(code.IndexOf("/?code=") + 7);

                        AuthorizationCode = code;

                        // response "close this window"
                        byte[] body = Encoding.UTF8.GetBytes(
                            "<html><head></head><body>Please close this window.</body></html>\r\n");
                        byte[] header = Encoding.UTF8.GetBytes(
                            "HTTP/1.1 200 OK\r\n" +
                            "Connection: Close\r\n" +
                            "Content-Type: text/html\r\n" +
                            "Content-Length: " + body.Length + "\r\n" +
                            "\r\n\r\n");

                        stream.Write(header, 0, header.Length);
                        stream.Write(body, 0, body.Length);

                        break;
                    }
                }

                client.Close();
                ms.Dispose();		
            }
        }

        public void StopServer()
        {
            try
            {
                server.Stop();

                listenThread.Abort();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                server = null;
                listenThread = null;
            }
        }
    }
}