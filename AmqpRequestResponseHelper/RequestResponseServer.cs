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
using RabbitMQ.Client;

namespace Cocop.AmqpRequestResponseHelper
{
    /// <summary>
    /// A class that acts as a request-response server for an AMQP message bus.
    /// </summary>
    public class RequestResponseServer : ConsumerHolderBase
    {
        /// <summary>
        /// Event handler delegate for the RequestReceived event.
        /// </summary>
        /// <param name="source">Source.</param>
        /// <param name="args">Event arguments.</param>
        public delegate void RequestReceivedEventHandler(object source, RequestReceivedEventArgs args);

        /// <summary>
        /// Raised when a request is received.
        /// </summary>
        public event RequestReceivedEventHandler RequestReceived;


        private readonly IModel m_channel;
        private readonly string m_exchange;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="channel">Channel.</param>
        /// <param name="excName">Exchange name.</param>
        /// <param name="servTopic">Server topic name.</param>
        /// <exception cref="RabbitMQ.Client.Exceptions.AlreadyClosedException">Thrown if the channel is closed. The RabbitMQ API reference does not specify, which exceptions may occur, but this is likely possible.</exception>
        /// <exception cref="RabbitMQ.Client.Exceptions.OperationInterruptedException">Thrown if the operation fails. The RabbitMQ API reference does not specify, which exceptions may occur, but this is likely possible.</exception>
        public RequestResponseServer(IModel channel, string excName, string servTopic)
            : base(channel, excName, servTopic)
        {
            m_channel = channel;
            m_exchange = excName;
        }
        
        /// <summary>
        /// Sends a response to a request.
        /// </summary>
        /// <param name="args">Event arguments of the related request.</param>
        /// <param name="msg">Message.</param>
        /// <exception cref="RabbitMQ.Client.Exceptions.AlreadyClosedException">Thrown if the channel is closed. The RabbitMQ API reference does not specify, which exceptions may occur, but this is likely possible.</exception>
        /// <exception cref="RabbitMQ.Client.Exceptions.OperationInterruptedException">Thrown if the operation fails. The RabbitMQ API reference does not specify, which exceptions may occur, but this is likely possible.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the object is no longer active.</exception>
        public void SendResponse(RequestReceivedEventArgs args, byte[] msg)
        {
            ExpectConsumerIsActive();

            var replyProps = m_channel.CreateBasicProperties();
            replyProps.CorrelationId = args.CorrelationId;

            m_channel.BasicPublish(exchange: m_exchange, routingKey: args.ReplyTo,
                  basicProperties: replyProps, body: msg);

            // AutoAck is enabled -> no manual acking performed
        }

        protected override void HandleDeliveryImpl(IBasicProperties properties, byte[] body)
        {
            // A request has arrived in the queue!

            // Raising an event
            var myEventArgs = new RequestReceivedEventArgs(properties.ReplyTo, properties.CorrelationId, body);

            try
            {
                RequestReceived?.Invoke(this, myEventArgs);
            }
            catch { } // No can do
        }
    }
}
