using System;
using System.Collections.Generic;
using System.IO;
using Hudson.SoloSoft.Communications;
using NetMQ;
using Newtonsoft.Json;
using NetMQ.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Net.Configuration;
using System.Diagnostics;
using System.Windows.Forms;

public class Solo_Client
{
    public class Message
    {
        public string action_handle { get; set; }

        public Dictionary<string, string> action_vars { get; set; }
    }

    public static void Main(string[] args)
    {
        // SoloServer t = new SoloServer();
        // t.Start(11139);
        SoloClient client = new SoloClient();
        client.Connect(11139);
        string T = "0000";
        string S = "go";
        string status = "";
        byte[] msg;
        string programPath = @"C:\Program Files (x86)\Hudson Robotics\SoloSoft\SOLOSoft.exe";
        Dictionary<string, string> response;
        using (var server = new ResponseSocket("tcp://*:2001"))
        {
            while (S != "Shutdown")
            {

                Console.Out.WriteLine(server.ToString());
                //int charCount = Encoding.ASCII.GetChars(responseBytes, 0, bytesReceived, responseChars, 0);
                //System.Threading.Thread.Sleep(10000);
                string t = server.ReceiveFrameString();

                // check if SOLOSoft already running
                Process[] processes = Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(programPath));
                if (processes.Length == 0)
                {
                    Console.Out.WriteLine("SOLOSoft not open");
                    // Program is not running, open it
                    Process.Start(programPath);

                    // Wait for program to open then press 'Enter'
                    System.Threading.Thread.Sleep(6000);
                    SendKeys.SendWait("{ENTER}");

                    System.Threading.Thread.Sleep(1000);
                    SendKeys.SendWait("{ENTER}");

                    Console.Out.WriteLine("SOLOSoft opened");


                    Console.Out.WriteLine(client.RunCommand("CLOSEALLFILES"));
                    Console.Out.WriteLine("Closed all files");
                }
                else
                {
                    Console.Out.WriteLine("SOLOSoft already open");
                    //Console.Out.WriteLine(client.RunCommand("CLOSEALLFILES"));
                }

                Console.Out.WriteLine(t);
                Message m = JsonConvert.DeserializeObject<Message>(t);
                Console.Out.WriteLine(m.action_handle);
                if (m.action_handle == ("run_protocol"))
                {
                    string[] f = m.action_vars["hso_contents"].Split('\n');
                    File.WriteAllLines("C:\\labautomation\\instructions_wei\\" + m.action_vars["hso_basename"], f);
                    //Console.Out.WriteLine(client.IsConnected);

                    S = m.action_handle;
                    T = client.RunCommand("LOAD C:\\labautomation\\instructions_wei\\" + m.action_vars["hso_basename"]);
                    Console.Out.WriteLine(T);
                    T = client.RunCommand("RUN C:\\labautomation\\instructions_wei\\" + m.action_vars["hso_basename"]);
                    Console.Out.WriteLine(T);
                    // client.RunCommand("RUN C:\\labautomation\\instructions_wei\\shuck_tip.hso"));
                    try
                    {
                        // try and check the status
                        status = client.RunCommand("GETSTATUS");
                        while (status != "IDLE")
                        {
                            System.Threading.Thread.Sleep(10000);
                            status = client.RunCommand("GETSTATUS");
                        }

                        /// check if SoloSoft Running at end of protocol
                        processes = Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(programPath));
                        if (processes.Length == 0)
                        {
                            Console.Out.WriteLine("SOLOSoft is already closed");
                        }
                        else
                        {
                            /// Close SOLOSoft at end of protocol
                            Process soloSoft = processes[0];
                            soloSoft.Kill();
                            Console.Out.WriteLine("Process Killed: " + programPath);
                            System.Threading.Thread.Sleep(3000);
                        }

                        // Send response if SOLO protocol finished
                        response = new Dictionary<string, string>();
                        response.Add("action_response", "StepStatus.SUCCEEDED");
                        response.Add("action_msg", "yay");
                        response.Add("action_log", "birch");
                        msg = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response));
                        server.SendFrame(msg);


                    }

                    catch (Exception ex)
                    {
                        // SOLOSoft has crashed :(
                        Console.WriteLine("SOLO has crashed!");
                        throw ex;
                    }
                }
                else if (m.action_handle == ("refill_tips"))
                {
                    int boxIndex = Int32.Parse(m.action_vars["position"]);
                    string filePath = "C:\\ProgramData\\Hudson Robotics\\SoloSoft\\SoloSoft\\TipCounts.csv";
                    int targetRow = boxIndex;
                    int targetColumn = 3;
                    Console.WriteLine("Hello, World!");
                    string refillLine = "1";
                    for (int i = 0; i < 95; i++)
                    {
                        refillLine = refillLine + "|1";
                    }
                    // Read the contents of the CSV file
                    string[] lines = File.ReadAllLines(filePath);

                    // Check if the target row exists
                    if (targetRow > 0 && targetRow <= lines.Length)
                    {
                        // Split the line into columns
                        string[] columns = lines[targetRow - 1].Split(',');
                        // Check if the target column exists
                        if (targetColumn > 0 && targetColumn <= columns.Length)
                        {
                            // Update the clientalue at the target column
                            columns[targetColumn - 1] = refillLine;
                            // Join the columns back into a line
                            string updatedLine = string.Join(",", columns);
                            // Update the line in the CSV file
                            lines[targetRow - 1] = updatedLine;
                            // Write the updated contents back to the CSV file
                            File.WriteAllLines(filePath, lines);
                            Console.WriteLine("Value updated successfully.");
                        }
                        else
                        {
                            Console.WriteLine("Invalid target column.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid target row.");
                    }

                    // Send response if SOLO protocol finished
                    response = new Dictionary<string, string>();
                    response.Add("action_response", "StepStatus.SUCCEEDED");
                    response.Add("action_msg", "yay");
                    response.Add("action_log", "birch");
                    msg = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response));
                    server.SendFrame(msg);
                }

            }
        }
    }
    public Solo_Client()
    {
    }

}
