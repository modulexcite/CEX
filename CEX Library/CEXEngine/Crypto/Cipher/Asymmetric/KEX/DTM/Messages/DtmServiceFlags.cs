﻿namespace VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.KEX.DTM.Messages
{
    /// <summary>
    /// The flag indicating the type of service request
    /// </summary>
    public enum DtmServiceFlags : short
    {
        /// <summary>
        /// An internal error has occured
        /// </summary>
        Internal = 21,
        /// <summary>
        /// The host refused the connection
        /// </summary>
        Refusal = 22,
        /// <summary>
        /// The host was disconnected from the session
        /// </summary>
        Disconnected = 23,
        /// <summary>
        /// The host requires a re-transmission of the data
        /// </summary>
        Resend = 24,
        /// <summary>
        /// The host received data that was out of sequence
        /// </summary>
        OutOfSequence = 25,
        /// <summary>
        /// The data can not be recovered, attempt a resync
        /// </summary>
        DataLost = 26,
        /// <summary>
        /// Tear down the connection
        /// </summary>
        Terminate = 27,
        /// <summary>
        /// Response to a data lost messagem attempt to resync crypto stream
        /// </summary>
        Resync = 28,
        /// <summary>
        /// The response is an echo
        /// </summary>
        Echo = 29,
        /// <summary>
        /// The message is a keep alive
        /// </summary>
        KeepAlive = 30
    }
}
