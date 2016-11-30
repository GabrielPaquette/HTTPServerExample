/*
File: Assignment 5 - program.cs
Developer: Gabriel Paquette
Date: Nov 24, 2016
Description: The code contains in this file runs the server which listens for clients
             on a specific port and IP specified in the commandline arguments. When
             the server connects to a client, the server tries to look up the data
             specified in the client request. 

HOW TO: Create a folder with any .jpg, .html, .txt, .gif files in it.
	Run the program via the command-line. (.exe, IP of computer, port number, path to file you created.)
	Open a new browser and in the URL enter IP:Port/fileName.extention.
	The contents of the file should display to the screen.

NOTE: This Program used 127.0.0.1 for the IP and 13000 for the Port while testing.
*/

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace Assignment_5
{
    class Program
    {
        static string dataPath = "";
        static int port = 0;
        static IPAddress ip = default(IPAddress);

        static TcpListener server = null;

        /*
        Name: Main
        Parameters: The strings passed to the program are the IP address to listen on, the 
                    port number to listen on, and the path to the directory which contains 
                    the files that will be sent back to the client
        Description: This function initalizes the IP, port and path, and then starts the server
        */
        static void Main(string[] args)
        {
            if (init(args) == 0)
            {
                runServer();
            }
        }


        /*
        Name: init
        Parameters: this is the command line arguments
        Description: This function checks if there are 3 commandlines arguments.
                     Then it tries to parse out the IP address and the port number.
                     If either of them can't be parse, then an error code is returned. 
        Return: This is a flag to determine what went wrong.
        */
        static int init(string[] args)
        {
            if (args.Length == 3)
            {
                //set the path
                dataPath = args[0];

                //get the IP address
                if (!(IPAddress.TryParse(args[1], out ip)))
                {
                    return 1;
                }

                //get the port number
                if (!(int.TryParse(args[2], out port)))
                {
                    return 2;
                }
            }

            return 0;
        }


        /*
        Name: runServer
        Description: This function connects to a client, reads in the data sent by the client,
                     parses out the file name to look up and then sends back the data requested 
                     to the client. If the file could not be found, then a 404 error is sent back
        */
        static void runServer()
        {
            //start a server using the IP and Port number
            server = new TcpListener(ip, port);
            server.Start();

            byte[] readString = new byte[5000];
            string data = null;
            
            string dataString = "";
            string extension = "";
            string MIMEType = "";

            byte[] header = { };
            byte[] content = { };

            while (true)
            {
                data = null;
                dataString = "";
                extension = "";

                // Perform a blocking call to accept requests.
                // You could also user server.AcceptSocket() here.
                TcpClient client = server.AcceptTcpClient();
                // Get a stream object for reading and writing
                NetworkStream stream = client.GetStream();
                stream.Read(readString, 0, readString.Length);
           
                // Translate data bytes to a ASCII string.
                data = Encoding.ASCII.GetString(readString, 0, readString.Length);

                try {
                    //parses out the name of the file that needs to be looked up.
                    //as well as gets the extention type of the file
                    dataString = getDataString(data, out extension);

                    //determines what kind of content type is required to sent to the client
                    //using the extention of the file
                    MIMEType = determineMIMEType(extension);

                    //creates the complete dataPath
                    dataString = dataPath + dataString;

                    //if there is an approriate MIME type that can be used for the file type
                    if (MIMEType != "")
                    { 
                        //creates the header and content messages to send to the server
                        header = createResponseMessage(MIMEType, dataString, out content);

                        //writes to the client 
                        stream.Write(header, 0, header.Length);
                        stream.Write(content, 0, content.Length);

                        stream.Flush();
                    }
                }
                catch (FileNotFoundException)
                {
                    //if the file can't be found, and it's not looking for an icon, send a 404 error
                    if (extension != ".ico")
                    {
                        header = createFileNotFoundException(out content);

                        stream.Write(header, 0, header.Length);
                        stream.Write(content, 0, content.Length);

                        stream.Flush();
                    }
                }
                catch (Exception ex)
                {
                    header = createFileNotFoundException(out content);

                    stream.Write(header, 0, header.Length);
                    stream.Write(content, 0, content.Length);

                    stream.Flush();
                }
                
                // Shutdown and end connection
                client.Close();
            }
        }


        /*
        Name: createFileNotFoundException
        Parameters: outs the content message
        Description: this function is called when an exeption is thrown or when the file can't be found
        Return: returns a byte array of the header to send to the client
        */
        static byte[] createFileNotFoundException(out byte[] content)
        {
            string body = "<html><header>404 File Not Found</header><html>";

            string header = "HTTP/1.1 404 Not Found\n";
            header += "Date:" + DateTime.Now;
            header += "Content-Length: " + body.Length + "\n";
            header += "Content-Type: text/html\n\n";

            byte[] bodyByte = Encoding.ASCII.GetBytes(body);
            byte[] headerByte = Encoding.ASCII.GetBytes(header);

            content = bodyByte;
            return headerByte;
        }


        /*
        Name: createResponseMessage
        Parameters: string contentType -> this is the type of the file that will be sent to the client. 
                                          it is used in the header
                    string dataString -> this is the path to the file that was requested
                    out byte[] contnet -> this is a byte array of the content that will be sent
        Description: this function creates the header and content of the message that will be sent to the cleint
        Return: returns a byte array of the header to send to the client
        */
        static byte[] createResponseMessage(string contentType, string dataString, out byte[] content)
        {
            //reads the data at the specified path
            byte[] msg = File.ReadAllBytes(dataString);
            
            //creates the header
            string header = "HTTP/1.1 200 OK\n";
            header += "Date:" + DateTime.Now;
            header += "Content-Length: " + msg.Length + "\n";
            header += "Content-Type: " + contentType + "\n\n";
            
            //convert the header to bytes
            byte[] byteHeader = Encoding.ASCII.GetBytes(header);

            content = msg;
            return byteHeader;
            
        }


        /*
        Name: determineMIMEType
        Parameters: string extension -> this is the extension of the file that was requested
        Description: this function determine the content type string that will be included in 
                     the header message to the client
        Return: returns the MIME string or "" if there is no appropriate content type
        */
        static string determineMIMEType(string extension)
        {
            string MIMEString = "";

            switch (extension)
            {
                case ".png":
                    MIMEString = "image/png";
                    break;
                case ".gif":
                    MIMEString = "image/gif";
                    break;
                case ".jpg":
                case ".jpeg":
                    MIMEString = "image/jpeg";
                    break;
                case ".htm":
                case ".html":
                    MIMEString = "text/html";
                    break;
                case ".txt":
                    MIMEString = "text/plain";
                    break;
                default:
                    MIMEString = "";
                    break;
            }

            return MIMEString;
        }


        /*
        Name:getDataString
        Parameters: string data -> this is the full message that is send from the client
                    out string extension -> this is the string that will contain the 
                                            extention of the file that will be sent back
                                            to the client
        Description: This function parses out the file name and extention, and the returns 
                     the name
        Return: returns the name of the file that the client requested.
        */
        static string getDataString(string data, out string extension)
        {
            int firstSlash = 0;
            int secondSlash = 0;
            string dataString = "";
            string exten = "";


            //the original string looks like this:
            //GET /FILE.EXT HTTP/
            //it finds the two slashes, and then get the extention and the file name
            firstSlash = data.IndexOf('/') + 1;
            secondSlash = data.IndexOf('/', firstSlash) - 5;

            dataString = data.Substring(firstSlash, (secondSlash - firstSlash));

            exten = dataString.Substring(dataString.LastIndexOf('.'), dataString.Length - dataString.LastIndexOf('.'));

            extension = exten;
            return dataString;
        }
    }
}
