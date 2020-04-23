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
using RabbitMQ.Client.Events;

namespace Cocop.AmqpRequestResponseHelper
{
    /// <summary>
    /// Base class for classes that manage a consumer object bound to a queue.
    /// </summary>
    public abstract class ConsumerHolderBase : IDisposable
    {
        private readonly IModel m_channel;
	    private readonly string m_exchange;

        // Due to server-generated events, there can be a situation
        // where the consumer is cancelled right after this class
        // has confirmed it is still active.
        // Still, m_lockObject at least enables data synchronisation between threads.
        private readonly object m_lockObject = new object();

        private string m_consumerTag = null;

        // This indicates the reason why the object cannot be used if any
        private string m_consumerInactiveReason = "No consumer created successfully";

        private bool m_disposed = false;


        /// <summary>
        /// Constructor. Use this when the topic of to consume shall be generated.
        /// </summary>
        /// <param name="channel">Channel.</param>
        /// <param name="excName">Exchange name.</param>
        /// <exception cref="InvalidOperationException">Thrown if an error occurs.</exception>
        internal ConsumerHolderBase(IModel channel, string excName)
            : this(channel, excName, null)
        {
            // Empty ctor body (although another ctor called)
        }

        /// <summary>
        /// Constructor. Use this to explicitly specify the topic to consume.
        /// </summary>
        /// <param name="channel">Channel.</param>
        /// <param name="excName">Exchange name.</param>
        /// <param name="topic">The topic to listen.</param>
        /// <exception cref="InvalidOperationException">Thrown if an error occurs.</exception>
        internal ConsumerHolderBase(IModel channel, string excName, string topic)
        {
            m_channel = channel;
            m_exchange = excName;

            try
            {
                // Declaring an exchange.
                // Request-response could use a direct exchange, which is simpler than a topic-based exchange.
                // However, as topics are utilised in publish-subscribe scenarios anyway, this code uses
                // topics here as well to enable re-using an already existing topic exchange.
                m_channel.ExchangeDeclare(exchange: excName, type: "topic", durable: true, autoDelete: false);

                // Declaring a queue.
                // Empty queue name -> use a generated name.
                // The queue is durable -> survive restart.
                // However, "autodelete" makes sure (?) the queue is deleted if no-one uses it.
                // It is assumed that if the broker reboots quickly, this client will not notice it and keeps
                // using the same queue. In such a case, the channel object should reconnect by itself.
                var queueName = m_channel.QueueDeclare(queue: "", durable: true, exclusive: true, autoDelete: true, arguments: null).QueueName;

                // If the topic has not been specified, generating one from the queue name.
                TopicName = topic ?? "topic-" + queueName;

                // Binding the queue to the topic
                m_channel.QueueBind(queue: queueName, exchange: excName, routingKey: TopicName);

                // Creating a consumer for the queue.
                var consumer = new EventingBasicConsumer(m_channel);

                // Adding an event handler for message reception and other events
                consumer.Received += Consumer_Received;
                consumer.ConsumerCancelled += Consumer_ConsumerCancelled;
                consumer.Shutdown += Consumer_Shutdown;

                // Consuming the queue.
                // noAck = true -> "no manual acks" -> autoAck enabled
                m_consumerTag = m_channel.BasicConsume(queue: queueName, noAck: true, consumer: consumer);
            }
            catch (Exception e)
            {
                // Cleaning up
                Dispose();

                throw new InvalidOperationException("Failed to init consumer: " + e.Message, e);
            }
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

            // Cancelling the consumer
            if (ConsumerIsActive())
            {
                string consumerTagTemp = null;

                // At this point, the consumer may have become inactive, although the
                // following code assumes otherwise. However, if the stored consumer
                // tag has become null, the following exception handling block
                // supposedly takes care of the situation.

                lock (m_lockObject)
                {
                    consumerTagTemp = m_consumerTag;
                }

                try
                {
                    m_channel.BasicCancel(consumerTagTemp);
                }
                catch (Exception)
                {
                    // No can do! :/
                    // Cannot leak any exceptions from Dispose().
                }

                MarkConsumerInactive("User has disposed the object");
            }
        }

        #region Public or protected methods

        /// <summary>
        /// The name of the topic associated to the consumed queue.
        /// </summary>
        protected string TopicName
        {
            get;
            private set;
        }

        /// <summary>
        /// Checks if the consumer held is active. Throws an exception if not.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the consumer is inactive.</exception>
        protected void ExpectConsumerIsActive()
        {
            lock (m_lockObject)
            {
                if (m_disposed)
                {
                    throw new ObjectDisposedException("ConsumerHolderBase");
                }

                if (!ConsumerIsActive())
                {
                    throw new InvalidOperationException("The object is unusable. Reason: " + m_consumerInactiveReason);
                }
            }
        }

        /// <summary>
        /// Implements the handling of a delivery received by the consumer.
        /// </summary>
        /// <param name="properties">Message properties.</param>
        /// <param name="body">Message body.</param>
        protected abstract void HandleDeliveryImpl(IBasicProperties properties, byte[] body);

        #endregion Public or protected methods


        #region Private methods
        
        private bool ConsumerIsActive()
        {
            // Whether the consumer is active
            lock (m_lockObject)
            {
                return !string.IsNullOrEmpty(m_consumerTag);
            }
        }

        private void MarkConsumerInactive(string reason)
        {
            // Mark that the consumer is inactive
            lock (m_lockObject)
            {
                m_consumerTag = null;
                m_consumerInactiveReason = reason;
            }
        }

        private bool ConsumerTagEquals(string tag)
        {
            // Check equality of the consumer tag
            lock (m_lockObject)
            {
                return m_consumerTag != null && m_consumerTag == tag;
            }
        }

        private void Consumer_Shutdown(object sender, ShutdownEventArgs args)
        {
            // Shutdown occurred
            MarkConsumerInactive("Shutdown has occurred");
        }

        private void Consumer_ConsumerCancelled(object sender, ConsumerEventArgs args)
        {
            // Consumer cancelled

            if (!ConsumerTagEquals(args.ConsumerTag))
            {
                return; // Unexpected consumer tag
            }

            MarkConsumerInactive("Consumer has been cancelled");
        }

        private void Consumer_Received(object sender, BasicDeliverEventArgs args)
        {
            lock (m_lockObject)
            {
                if (m_disposed) return;
            }

            if (!ConsumerTagEquals(args.ConsumerTag))
            {
                return; // Unexpected consumer tag
            }

            // Message received. Notify child class.
            var props = args.BasicProperties;
            HandleDeliveryImpl(props, args.Body);
        }

        #endregion Private methods
    }
}
