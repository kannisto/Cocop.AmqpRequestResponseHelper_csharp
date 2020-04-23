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
    /// Implements a class to test the request-response server.
    /// </summary>
    class Server : IDisposable
    {
        private RequestResponseServer m_reqRespServer = null;

        private bool m_disposed = false;
        private readonly object m_lockObject = new object();


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="channel">Channel.</param>
        /// <param name="excName">Exchange name.</param>
        /// <param name="topicName">Topic name to send messages to.</param>
        public Server(IModel channel, string excName, string topicName)
        {
            m_reqRespServer = new RequestResponseServer(channel, excName, topicName);
        }

        /// <summary>
        /// Disposes the object.
        /// </summary>
        public void Dispose()
        {
            lock (m_lockObject)
            {
                m_disposed = true;
            }

            try
            {
                if (m_reqRespServer != null)
                {
                    m_reqRespServer.Dispose();
                    m_reqRespServer = null;
                }
            }
            catch { } // No can do
        }

        /// <summary>
        /// Runs the class.
        /// </summary>
        public void Run()
        {
            // Already disposed?
            lock (m_lockObject)
            {
                if (m_disposed)
                {
                    throw new ObjectDisposedException("Server");
                }
            }

            m_reqRespServer.RequestReceived += ReqRespServer_RequestReceived;

            while (true)
            {
                Console.WriteLine("Awaiting requests...");
                Console.WriteLine("Give Q to exit.");

                var input = Console.ReadLine().Trim().ToLower();

                if (input == "q")
                {
                    break;
                }
            }

            m_reqRespServer.RequestReceived -= ReqRespServer_RequestReceived;
        }

        private void ReqRespServer_RequestReceived(object source, RequestReceivedEventArgs args)
        {
            lock (m_lockObject)
            {
                // Ensuring the object has not been disposed. It is notable that disposal could
                // occur right after this check before this function has finished. However, the
                // following try-catch block is expected to handle such situations.

                if (m_disposed)
                {
                    return;
                }
            }

            try
            {
                // Printing the request
                var messageIn = System.Text.Encoding.UTF8.GetString(args.Message);
                Console.WriteLine("Got message \"" + messageIn + "\"");

                // Creating a response
                var response = "Your request arrived at " + DateTime.Now.ToString();
                var responseBytes = System.Text.Encoding.UTF8.GetBytes(response);
                
                // Responding
                m_reqRespServer.SendResponse(args, responseBytes);
                Console.WriteLine("Sent a response: \"" + response + "\"");
            }
            catch (Exception e)
            {
                var printText = string.Format("{0}: {1}", e.GetType().Name, e.Message);
                Console.WriteLine(printText);
            }
        }
    }
}
