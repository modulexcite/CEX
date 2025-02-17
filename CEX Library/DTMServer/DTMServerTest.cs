﻿using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.KEX.DTM;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.KEX.DTM.Arguments;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.KEX.DTM.Messages;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.KEX.DTM.Structures;

namespace DTMServerTest
{
    internal class DTMServerTest
    {
        #region Fields
        int Port = 1024;            // listening port
        private DtmKex _dtmServer;  // server is responder
        private static ManualResetEvent _initDone = new ManualResetEvent(false);
        private const string CON_TITLE = "DTM> ";
        #endregion

        #region Constructor
        public DTMServerTest()
        {
        }
        #endregion

        #region Key Exchange
        public void TestExchange()
        {
            // dtm server exchange parameters X11RNS1R2
            DtmParameters srvDtmParams = DtmParamSets.FromName(DtmParamSets.DtmParamNames.X41RNT1R1);       // preset contains all the settings required for the exchange

            // dtm server id
            DtmClient srvDmtId = new DtmClient(
                new byte[] { 3, 3, 3, 3 },      // the clients public id, (should be at least 32 bytes, can be used as a contact lookup and initial auth)
                new byte[] { 4, 4, 4, 4 });     // the clients secret id, (secret id can be anything.. a serialized structure, signed data, hash, etc)

            // create the server
            _dtmServer = new DtmKex(srvDtmParams, srvDmtId);
            _dtmServer.IdentityReceived += new DtmKex.IdentityReceivedDelegate(OnIdentityReceived);         // returns the client public and secret id fields, used to authenticate a host
            _dtmServer.PacketReceived += new DtmKex.PacketReceivedDelegate(OnPacketReceived);               // notify that a packet has been received (optional)
            _dtmServer.SessionEstablished += new DtmKex.SessionEstablishedDelegate(OnSessionEstablished);   // notify when the vpn state is up
            _dtmServer.PacketSent += new DtmKex.PacketReceivedDelegate(OnPacketSent);                       // notify when a packet has been sent to the remote host (optional)
            _dtmServer.DataReceived += new DtmKex.DataTransferredDelegate(OnDataReceived);                  // returns the decrypted message data
            _dtmServer.FileReceived += new DtmKex.FileTransferredDelegate(OnFileReceived);                  // notify that a file transfer has completed
            _dtmServer.FileRequest += new DtmKex.FileRequestDelegate(OnFileRequest);                        // notify that the remote host wants to send a file, can cancel or provide a path for the new file
            _dtmServer.SessionError += new DtmKex.SessionErrorDelegate(OnSessionError);                     // notify of any error conditions; includes the exception, and a severity code contained in the option flag
            
            Console.WriteLine(CON_TITLE + "Waiting for a connection..");

            // server starts listening
            _dtmServer.Listen(IPAddress.Any, Port);
            // wait for the key exchange to complete
            _initDone.WaitOne();
            // start the message stream
            StartMessageStream();
        }
        #endregion

        #region Events
        /// <summary>
        /// Fires when a post-exchange packet containing processed data is received
        /// </summary>
        private void OnDataReceived(object owner, DtmDataReceivedEventArgs args)
        {
            Console.WriteLine(Encoding.ASCII.GetString(args.Message.ToArray()));
            Console.Write(CON_TITLE);
        }

        private void OnFileReceived(object owner, DtmPacketEventArgs args)
        {
            // file transfer is complete
            Console.WriteLine(CON_TITLE + "The file transfer has completed!");
            Console.WriteLine(CON_TITLE);
        }

        private void OnFileRequest(object owner, DtmFileRequestEventArgs args)
        {
            // set the args.Cancel to true to refuse a file
            //args.Cancel = true;
            // get the file name
            string fileName = args.FilePath;
            // if accept, prepend the destination directory and set the path
            args.FilePath = Path.Combine(@"C:\Tests\Saved\Test", fileName);
        }

        /// <summary>
        /// Fires when a packet containing an identity is received, the args contain the id
        /// </summary>
        private void OnIdentityReceived(object owner, DtmIdentityEventArgs args)
        {
            Console.WriteLine(CON_TITLE + String.Format("Server received an identity packet: {0}", IdToString(args.DtmID.Identity)));
        }

        /// <summary>
        /// Fires each time a packet is received, the args contain the exchange state. 
        /// The size of the payload and a Cancel token, when set to true, will terminate the session
        /// </summary>
        private void OnPacketReceived(object owner, DtmPacketEventArgs args)
        {
            if (!((DtmKex)owner).IsEstablished)
                Console.WriteLine(CON_TITLE + String.Format("Server received a packet; {0}", (DtmExchangeFlags)args.Message));

            // exchange completed
            if (args.Message == (short)DtmExchangeFlags.Established)
                _initDone.Set();
        }

        /// <summary>
        /// Fires each time a packet is sent, the args contain the exchange state and the echo flag.
        /// </summary>
        private void OnPacketSent(object owner, DtmPacketEventArgs args)
        {
            if (!((DtmKex)owner).IsEstablished)
                Console.WriteLine(CON_TITLE + String.Format("Server sent a packet; {0}", (DtmExchangeFlags)args.Message));
        }

        /// <summary>
        /// Fired when the session is fully established, the args contain the forward and return symmetric session keys
        /// </summary>
        private void OnSessionEstablished(object owner, DtmEstablishedEventArgs args)
        {
            Console.WriteLine(CON_TITLE + "The Server VPN state is UP");
        }

        /// <summary>
        /// Fires when an error has occured; contains the exception and the errors operational severity
        /// </summary>
        private void OnSessionError(object owner, DtmErrorEventArgs args)
        {
            // in case window is closed; should call disconnect in a forms closing event
            if (_dtmServer.IsConnected)
                Console.WriteLine(CON_TITLE + "Severity:" + (DtmErrorSeverity)args.Severity + "Message: " + args.Message);
        }
        #endregion

        #region Helpers
        /// <summary>
        /// A simple example of a message loop; proper method would involve a simple 'send' button
        /// </summary>
        private void StartMessageStream()
        {
            Console.WriteLine();
            Console.WriteLine(CON_TITLE + "Key Exchange completed!");
            Console.WriteLine(CON_TITLE + "Type a message and press *Enter* to send..");
            Console.WriteLine(CON_TITLE + "Type *Quit* to Exit..");
            Console.Write(CON_TITLE);
            byte[] btmsg;
            string smsg;

            // test sending files
            /*_dtmServer.SendFile(@"C:\Tests\Saved\tiny.txt");
            _dtmServer.SendFile(@"C:\Tests\Saved\small.txt");
            _dtmServer.SendFile(@"C:\Tests\Saved\medium.txt");
            _dtmServer.SendFile(@"C:\Tests\Saved\large.txt");*/

            do
            {
                smsg = Console.ReadLine();
                if (smsg.ToUpper().Equals("QUIT"))
                {
                    // tear down connection and dispose of the session
                    // should always be called when a client disconnects to alert the remote host
                    _dtmServer.Disconnect();
                    break;
                }

                // byte encode
                btmsg = Encoding.ASCII.GetBytes(smsg);
                // send the message
                if (btmsg.Length > 0)
                    _dtmServer.Send(new MemoryStream(btmsg));

                Console.Write(CON_TITLE);
            }
            while (true);
        }

        /// <summary>
        /// Creates a serialized request packet (DtmPacket)
        /// </summary>
        private MemoryStream CreateRequest(DtmPacketTypes Message, short State)
        {
            MemoryStream ret = new DtmPacket(Message, 0, 0, State).ToStream();
            ret.Seek(0, SeekOrigin.Begin);
            return ret;
        }

        /// <summary>
        /// Get the packet header from the stream
        /// </summary>
        private DtmPacket ReadPacket(Stream PacketStream)
        {
            return new DtmPacket(PacketStream);
        }

        private string IdToString(byte[] Id)
        {
            string sid = "";
            for (int i = 0; i < Id.Length; i++)
                sid += Id[i].ToString();

            return sid;
        }
        #endregion
    }
}
