//
// Please make sure to read and understand the files README.md and LICENSE.txt.
// 
// This file was prepared in the research project COCOP (Coordinating
// Optimisation of Complex Industrial Processes).
// https://cocop-spire.eu/
//
// Author: Petri Kannisto, Tampere University, Finland
// File created: 5/2018
// Last modified: 3/2020

using System;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace Cocop.AmqpRequestResponseHelper
{
    /// <summary>
    /// A helper class to implement a request-response client for AMQP.
    /// </summary>
    public class RequestResponseClient : ConsumerHolderBase
    {
        private readonly IModel m_channel;
        private readonly string m_exchangeName;
        private readonly string m_targetName;

        private readonly System.Collections.Concurrent.BlockingCollection<byte[]> m_respQueue = new System.Collections.Concurrent.BlockingCollection<byte[]>();

        // Correlation ID enables the association of a response to a particular request
        private string m_currentCorrelationId = "";

        private readonly object m_lockObject = new object();


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="channel">Channel.</param>
        /// <param name="excName">Exchange name.</param>
        /// <param name="tgtName">Target topic name.</param>
        /// <exception cref="RabbitMQ.Client.Exceptions.AlreadyClosedException">Thrown if the channel is closed. The RabbitMQ API reference does not specify, which exceptions may occur, but this is likely possible.</exception>
        /// <exception cref="RabbitMQ.Client.Exceptions.OperationInterruptedException">Thrown if the operation fails. The RabbitMQ API reference does not specify, which exceptions may occur, but this is likely possible.</exception>
        public RequestResponseClient(IModel channel, string excName, string tgtName)
            : base(channel, excName)
        {
            m_channel = channel;
            m_exchangeName = excName;
            m_targetName = tgtName;
        }

        /// <summary>
        /// Performs a request in the synchronous (blocking) fashion.
        /// 
        /// Please note that this class does not support concurrent requests.
        /// That is, when a request has been sent and a response is awaited,
        /// it is not possible to send another request before the response arrives 
        /// for the first request.
        /// Therefore, to execute requests concurrently, create multiple instances
        /// of this class.
        /// </summary>
        /// <param name="message">Message to be sent.</param>
        /// <param name="timeout">Timeout value in milliseconds.</param>
        /// <returns>Response.</returns>
        /// <exception cref="RabbitMQ.Client.Exceptions.AlreadyClosedException">Thrown if the channel is closed. The RabbitMQ API reference does not specify, which exceptions may occur, but this is likely possible.</exception>
        /// <exception cref="RabbitMQ.Client.Exceptions.OperationInterruptedException">Thrown if the operation fails. The RabbitMQ API reference does not specify, which exceptions may occur, but this is likely possible.</exception>
        /// <exception cref="TimeoutException">Thrown if a timeout occurs.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the object is no longer active.</exception>
        public byte[] PerformRequestSync(byte[] message, int timeout)
        {
            ExpectConsumerIsActive();

            try
            {
                var props = m_channel.CreateBasicProperties();
                props.ReplyTo = TopicName;

                // Generate and assign a correlation ID
                lock (m_lockObject)
                {
                    m_currentCorrelationId = Guid.NewGuid().ToString();
                    props.CorrelationId = m_currentCorrelationId;
                }

                // Send the message
                m_channel.BasicPublish(exchange: m_exchangeName, routingKey: m_targetName, basicProperties: props, body: message);

                // This will block execution until a message arrives
                if (!m_respQueue.TryTake(out byte[] response, timeout))
                {
                    throw new TimeoutException("The request timed out");
                }
                else
                {
                    return response;
                }
            }
            finally
            {
                lock (m_lockObject)
                {
                    m_currentCorrelationId = "";
                }
            }
        }

        /// <summary>
        /// Performs a request in the asynchronous fashion.
        /// 
        /// Please note that this class does not support concurrent requests.
        /// That is, when a request has been sent and a response is awaited,
        /// it is not possible to send another request before the response arrives 
        /// for the first request.
        /// Therefore, to execute requests concurrently, create multiple instances
        /// of this class.
        /// </summary>
        /// <param name="message">Message to be sent.</param>
        /// <param name="timeout">Timeout value in milliseconds.</param>
        /// <returns>Response.</returns>
        /// <exception cref="RabbitMQ.Client.Exceptions.AlreadyClosedException">Thrown if the channel is closed. The RabbitMQ API reference does not specify, which exceptions may occur, but this is likely possible.</exception>
        /// <exception cref="RabbitMQ.Client.Exceptions.OperationInterruptedException">Thrown if the operation fails. The RabbitMQ API reference does not specify, which exceptions may occur, but this is likely possible.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the object is no longer active.</exception>
        public async Task<byte[]> PerformRequestAsync(byte[] message, int timeout)
        {
            ExpectConsumerIsActive();

            return await Task.Run(() =>
            {
                return PerformRequestSync(message, timeout);
            });
        }

        protected override void HandleDeliveryImpl(IBasicProperties properties, byte[] body)
        {
            lock (m_lockObject)
            {
                if (properties.CorrelationId != m_currentCorrelationId)
                {
                    // Unexpected correlation ID
                    return;
                }
            }

            m_respQueue.Add(body);
        }
    }
}
