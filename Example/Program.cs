//
// Please make sure to read and understand the files README.md and LICENSE.txt.
// 
// This file was prepared in the research project COCOP (Coordinating
// Optimisation of Complex Industrial Processes).
// https://cocop-spire.eu/
//
// Author: Petri Kannisto, Tampere University, Finland
// File created: 3/2020
// Last modified: 3/2020

using System;
using RabbitMQ.Client;
using RmqEx = RabbitMQ.Client.Exceptions;

namespace Example
{
    /// <summary>
    /// Program to test the server and client. Start one instance for the client and another for the server.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("The command line arguments are not as expected.");
                Console.WriteLine();
                Console.WriteLine("To run:");
                Console.WriteLine("> Example (host) (exchange) (server-topic) (username)");
                return;
            }

            // Processing command line arguments
            var host = args[0];
            var exchangeName = args[1];
            var topicName = args[2];
            var username = args[3];
            
            try
            {
                // The loop enables reconnection if the connection fails
                while (true)
                {
                    // Asking for password
                    Console.Write(string.Format("Password for user {0}: ", username));
                    var password = Console.ReadLine();

                    var factory = new ConnectionFactory()
                    {
                        HostName = host,
                        UserName = username,
                        Password = password
                    };

                    // Apply secure communication
                    // More information: https://www.rabbitmq.com/ssl.html
                    factory.Ssl.Enabled = true;
                    factory.Ssl.ServerName = host;
                    factory.Ssl.Version = System.Security.Authentication.SslProtocols.Tls12;

                    Console.Write("Setting up a connection... ");

                    try
                    {
                        using (var connection = factory.CreateConnection())
                        using (var channel = connection.CreateModel())
                        {
                            Console.WriteLine("done!");

                            RunMode(channel: channel, excName: exchangeName, topicName: topicName);
                            return;
                        }
                    }
                    // Instead of exceptions, another way to react to connection loss is to 
                    // sign up for the event "ModelShutdown" of the channel object.
                    catch (RmqEx.AlreadyClosedException)
                    {
                        Console.WriteLine("!!! AlreadyClosedException");
                    }
                    catch (RmqEx.BrokerUnreachableException)
                    {
                        Console.WriteLine("!!! BrokerUnreachableException");
                    }
                    catch (RmqEx.OperationInterruptedException)
                    {
                        Console.WriteLine("!!! OperationInterruptedException");
                    }

                    // Asking for reconnect
                    Console.WriteLine("Failed to connect. Retry with another password (y/n)?");
                    var input = Console.ReadLine().Trim().ToLower();

                    if (input == "n")
                    {
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.WriteLine(e.ToString());
                Console.WriteLine();
                Console.WriteLine("Press a key to exit.");
                Console.ReadKey();
            }
        }
        
        private static void RunMode(IModel channel, string excName, string topicName)
        {
            bool repeat = true;

            // Letting the user choose which mode to run
            while (repeat)
            {
                Console.WriteLine("Please select a mode to run:");
                Console.Write("S - server; C - client > ");
                var input = Console.ReadLine().Trim().ToLower();

                switch (input)
                {
                    case "s":

                        repeat = false;

                        using (var server = new Server(channel: channel, excName: excName, topicName: topicName))
                        {
                            server.Run();
                        }

                        break;

                    case "c":

                        repeat = false;
                        new Client().Run(channel: channel, excName: excName, topicName: topicName);
                        break;

                    default:

                        Console.WriteLine("Unexpected input \"" + input + "\"");
                        break;
                }
            }
        }
    }
}
