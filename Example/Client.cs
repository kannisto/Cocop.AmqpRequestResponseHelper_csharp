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
using Cocop.AmqpRequestResponseHelper;

namespace Example
{
    /// <summary>
    /// Implements a class to test the request-response client.
    /// </summary>
    class Client
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public Client()
        {
            // Empty ctor body
        }

        /// <summary>
        /// Runs the class.
        /// </summary>
        /// <param name="channel">Channel.</param>
        /// <param name="excName">Exchange name.</param>
        /// <param name="topicName">Topic name to send messages to.</param>
        public void Run(IModel channel, string excName, string topicName)
        {
            using (var client = new RequestResponseClient(channel, excName, topicName))
            {
                SendMessages(client);
            }
        }

        private static void SendMessages(RequestResponseClient client)
        {
            while (true)
            {
                try
                {
                    Console.WriteLine("Please give a message to be sent or 'q' to quit:");
                    Console.Write("> ");
                    var input = Console.ReadLine();

                    if (input.Trim().ToLower() == "q")
                    {
                        return;
                    }

                    Console.WriteLine("Requesting...");

                    var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
                    var responseBytes = client.PerformRequestSync(inputBytes, 5000);
                    var response = System.Text.Encoding.UTF8.GetString(responseBytes);

                    Console.WriteLine("Response: \"" + response + "\"");
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("The request has timed out.");
                }
            }
        }
    }
}
