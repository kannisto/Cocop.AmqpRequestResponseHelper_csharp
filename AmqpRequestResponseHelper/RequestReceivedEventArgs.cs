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

namespace Cocop.AmqpRequestResponseHelper
{
    /// <summary>
    /// Event arguments for the RequestReceivedEvent.
    /// </summary>
    public class RequestReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="repl">"Reply to" reference.</param>
        /// <param name="corrId">Correlation ID.</param>
        /// <param name="msg">The received message.</param>
        internal RequestReceivedEventArgs(string repl, string corrId, byte[] msg) : base()
        {
            ReplyTo = repl;
            CorrelationId = corrId;
            Message = msg;
        }

        /// <summary>
        /// "Reply to" reference.
        /// </summary>
        public string ReplyTo { get; }

        /// <summary>
        /// Correlation ID.
        /// </summary>
        public string CorrelationId { get; }

        /// <summary>
        /// The received message.
        /// </summary>
        public byte[] Message { get; }
    }
}
