﻿#region Directives
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VTDev.Libraries.CEXEngine.Crypto.Digest;
using VTDev.Libraries.CEXEngine.Crypto.Enumeration;
using VTDev.Libraries.CEXEngine.CryptoException;
using VTDev.Libraries.CEXEngine.Utility;
#endregion

#region License Information
// The MIT License (MIT)
// 
// Copyright (c) 2015 John Underhill
// This file is part of the CEX Cryptographic library.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// 
// Written by John Underhill, January 22, 2015
// contact: develop@vtdev.com
#endregion

namespace VTDev.Libraries.CEXEngine.Crypto.Processing
{
    /// <summary>
    /// <h3>Digest stream helper class.</h3>
    /// <para>Wraps Message Digest stream functions in an easy to use interface.</para>
    /// </summary> 
    /// 
    /// <example>
    /// <description>Example of hashing a Stream:</description>
    /// <code>
    /// byte[] hash;
    /// using (IDigest digest = new SHA512())
    /// {
    ///     using (StreamDigest dstrm = new StreamDigest(digest, [false]))
    ///     {
    ///         // assign the input stream
    ///         dstrm.Initialize(InputStream, [true]);
    ///         // get the digest
    ///         hash = dstrm.ComputeHash([Length], [InOffset]);
    ///     }
    /// }
    /// </code>
    /// </example>
    /// 
    /// <revisionHistory>
    /// <revision date="2015/01/23" version="1.3.0.0">Initial release</revision>
    /// <revision date="2015/07/01" version="1.4.0.0">Added library exceptions</revision>
    /// </revisionHistory>
    /// 
    /// <seealso cref="VTDev.Libraries.CEXEngine.Crypto.Digest">VTDev.Libraries.CEXEngine.Crypto.Digest Namespace</seealso>
    /// <seealso cref="VTDev.Libraries.CEXEngine.Crypto.Digest.IDigest">VTDev.Libraries.CEXEngine.Crypto.Digest.IDigest Interface</seealso>
    /// <seealso cref="VTDev.Libraries.CEXEngine.Crypto.Enumeration.Digests">VTDev.Libraries.CEXEngine.Crypto.Enumeration.Digests Enumeration</seealso>
    /// 
    /// <remarks>
    /// <description><h4>Implementation Notes:</h4></description>
    /// <list type="bullet">
    /// <item><description>Uses any of the implemented <see cref="Digests">Digests</see> using either the <see cref="IDigest">interface, or a Digests enumeration member</see>.</description></item>
    /// <item><description>Digest can be Disposed when this class is <see cref="Dispose()">Disposed</see>, set the DisposeEngine parameter in the class Constructor to true to dispose automatically.</description></item>
    /// <item><description>Input Stream can be Disposed when this class is Disposed, set the DisposeStream parameter in the <see cref="Initialize(Stream, bool)"/> call to true to dispose automatically.</description></item>
    /// <item><description>Implementation has a Progress counter that returns total sum of bytes processed per either <see cref="ComputeHash(long, long)">ComputeHash([InOffset], [OutOffset])</see> calls.</description></item>
    /// </list>
    /// </remarks>
    public sealed class StreamDigest : IDisposable
    {
        #region Events
        /// <summary>
        /// Progress indicator delegate
        /// </summary>
        /// 
        /// <param name="sender">Event owner object</param>
        /// <param name="e">Progress event arguments containing percentage and bytes processed as the UserState param</param>
        public delegate void ProgressDelegate(object sender, System.ComponentModel.ProgressChangedEventArgs e);

        /// <summary>
        /// Progress Percent Event; returns bytes processed as an integer percentage
        /// </summary>
        public event ProgressDelegate ProgressPercent;
        #endregion

        #region Constants
        private static int BUFFER_SIZE = 64 * 1024;
        #endregion

        #region Fields
        private int _blockSize;
        private IDigest _digestEngine;
        private bool _disposeEngine = false;
        private bool _disposeStream = false;
        private Stream _inStream;
        private bool _isConcurrent = true;
        private bool _isDisposed = false;
        private bool _isInitialized = false;
        private long _progressInterval;
        #endregion

        #region Properties
        /// <summary>
        /// Enable file reads and digest processing to run concurrently
        /// <para>The default is true, but will revert to false if the stream is not a FileStream, 
        /// or the file size is less that 65535 bytes in length.</para>
        /// </summary>
        public bool IsConcurrent
        {
            get { return _isConcurrent; }
            set { _isConcurrent = value; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Initialize the class with a digest instance
        /// <para>Digest must be fully initialized before calling this method.</para>
        /// </summary>
        /// 
        /// <param name="Digest">The initialized <see cref="IDigest"/> instance</param>
        /// <param name="DisposeEngine">Dispose of digest engine when <see cref="Dispose()"/> on this class is called; default is false</param>
        /// 
        /// <exception cref="CryptoProcessingException">Thrown if a null <see cref="IDigest">Digest</see> is used</exception>
        public StreamDigest(IDigest Digest, bool DisposeEngine = false)
        {
            if (Digest == null)
                throw new CryptoProcessingException("StreamDigest:CTor", "The Digest can not be null!", new ArgumentNullException());

            _digestEngine = Digest;
            _blockSize = _digestEngine.BlockSize;
            _disposeEngine = DisposeEngine;
        }

        /// <summary>
        /// Initialize the class with a digest enumeration
        /// </summary>
        /// 
        /// <param name="Digest">The digest enumeration member</param>
        /// <param name="DisposeEngine">Dispose of digest engine when <see cref="Dispose()"/> on this class is called; default is false</param>
        /// 
        /// <exception cref="System.ArgumentNullException">Thrown if a null <see cref="IDigest">Digest</see> is used</exception>
        public StreamDigest(Digests Digest, bool DisposeEngine = false)
        {
            _digestEngine = GetDigest(Digest);
            _blockSize = _digestEngine.BlockSize;
            _disposeEngine = DisposeEngine;
        }

        private StreamDigest()
        {
        }

        /// <summary>
        /// Finalize objects
        /// </summary>
        ~StreamDigest()
        {
            Dispose(false);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize internal state
        /// </summary>
        /// 
        /// <param name="InStream">The Source stream to be transformed</param>
        /// <param name="DisposeStream">Dispose of streams when <see cref="Dispose()"/> on this class is called</param>
        /// 
        /// <exception cref="CryptoProcessingException">Thrown if a null Input stream is used</exception>
        public void Initialize(Stream InStream, bool DisposeStream = false)
        {
            if (InStream == null)
                throw new CryptoProcessingException("StreamDigest:Initialize", "The Input stream can not be null!", new ArgumentNullException());

            _disposeStream = DisposeStream;
            _inStream = InStream;
            _isInitialized = true;
        }

        /// <summary>
        /// Process the entire length of the Input Stream
        /// </summary>
        /// 
        /// <returns>The Message Digest</returns>
        /// 
        /// <exception cref="CryptoProcessingException">Thrown if ComputeHash is called before Initialize(), or if Size + Offset is longer than Input stream</exception>
        public byte[] ComputeHash()
        {
            if (!_isInitialized)
                throw new CryptoProcessingException("StreamDigest:ComputeHash", "Initialize() must be called before a write operation can be performed!", new InvalidOperationException());
            if (_inStream.Length < 1)
                throw new CryptoProcessingException("StreamDigest:ComputeHash", "The Input stream is too short!", new ArgumentOutOfRangeException());

            if (_inStream.Length < BUFFER_SIZE || !_inStream.GetType().Equals(typeof(FileStream)))
                _isConcurrent = false;

            long dataLen = _inStream.Length - _inStream.Position;
            CalculateInterval(dataLen);

            return Compute(dataLen);
        }

        /// <summary>
        /// Process a length within the Input stream using an Offset
        /// </summary>
        /// 
        /// <returns>The Message Digest</returns>
        /// 
        /// <param name="Length">The number of bytes to process</param>
        /// <param name="Offset">The Input Stream positional offset</param>
        /// 
        /// <exception cref="CryptoProcessingException">Thrown if ComputeHash is called before Initialize(), or if Size + Offset is longer than Input stream</exception>
        public byte[] ComputeHash(long Length, long Offset)
        {
            if (!_isInitialized)
                throw new CryptoProcessingException("StreamDigest:ComputeHash", "Initialize() must be called before a ComputeHash operation can be performed!", new InvalidOperationException());
            if (Length - Offset < 1 || Length - Offset > _inStream.Length)
                throw new CryptoProcessingException("StreamDigest:ComputeHash", "The Input stream is too short!", new ArgumentOutOfRangeException());

            if (_inStream.Length - Offset < BUFFER_SIZE || !_inStream.GetType().Equals(typeof(FileStream)))
                _isConcurrent = false;

            long dataLen = Length - Offset;
            CalculateInterval(dataLen);
            _inStream.Position = Offset;

            return Compute(dataLen);
        }
        #endregion

        #region Private Methods
        private void CalculateInterval(long Offset)
        {
            long interval = (_inStream.Length - Offset) / 100;

            if (interval < _blockSize)
                _progressInterval = _blockSize;
            else
                _progressInterval = interval - (interval % _blockSize);

            if (_progressInterval == 0)
                _progressInterval = _blockSize;
        }

        private void CalculateProgress(long Size, bool Completed = false)
        {
            if (Completed || Size % _progressInterval == 0)
            {
                if (ProgressPercent != null)
                {
                    double progress = 100.0 * (double)Size / _inStream.Length;
                    ProgressPercent(this, new System.ComponentModel.ProgressChangedEventArgs((int)progress, (object)Size));
                }
            }
        }
        
        private byte[] Compute(long Length)
        {
            long bytesTotal = 0;
            byte[] chkSum = new byte[_digestEngine.DigestSize];

            if (!_isConcurrent)
            {
                int bytesRead = 0;
                byte[] buffer = new byte[_blockSize];
                long maxBlocks = Length / _blockSize;

                for (int i = 0; i < maxBlocks; i++)
                {
                    bytesRead = _inStream.Read(buffer, 0, _blockSize);
                    _digestEngine.BlockUpdate(buffer, 0, bytesRead);
                    bytesTotal += bytesRead;
                    CalculateProgress(bytesTotal);
                }

                // last block
                if (bytesTotal < Length)
                {
                    buffer = new byte[Length - bytesTotal];
                    bytesRead = _inStream.Read(buffer, 0, buffer.Length);
                    _digestEngine.BlockUpdate(buffer, 0, buffer.Length);
                    bytesTotal += buffer.Length;
                }
            }
            else
            {
                bytesTotal = ConcurrentStream(_inStream, Length);
            }

            // get the hash
            _digestEngine.DoFinal(chkSum, 0);
            CalculateProgress(bytesTotal);

            return chkSum;
        }
        
        private long ConcurrentStream(Stream Input, long Length = -1)
        {
            long bytesTotal = 0;
            if (Input.CanSeek)
            {
                if (Length > -1)
                {
                    if (Input.Position + Length > Input.Length)
                        throw new IndexOutOfRangeException();
                }

                if (Input.Position >= Input.Length)
                    return 0;
            }

            ConcurrentQueue<byte[]> queue = new ConcurrentQueue<byte[]>();
            AutoResetEvent dataReady = new AutoResetEvent(false);
            AutoResetEvent prepareData = new AutoResetEvent(false);

            Task reader = Task.Factory.StartNew(() =>
            {
                long total = 0;

                for (; ; )
                {
                    byte[] data = new byte[BUFFER_SIZE];
                    int bytesRead = Input.Read(data, 0, data.Length);

                    if ((Length == -1) && (bytesRead != BUFFER_SIZE))
                        data = data.SubArray(0, bytesRead);
                    else if ((Length != -1) && (total + bytesRead >= Length))
                        data = data.SubArray(0, (int)(Length - total));

                    total += data.Length;
                    queue.Enqueue(data);
                    dataReady.Set();

                    if (Length == -1)
                    {
                        if (bytesRead != BUFFER_SIZE)
                            break;
                    }
                    else if (Length == total)
                    {
                        break;
                    }
                    else if (bytesRead != BUFFER_SIZE)
                    {
                        throw new EndOfStreamException();
                    }

                    prepareData.WaitOne();
                }
            });

            Task hasher = Task.Factory.StartNew(() =>
            {
                IDigest h = (IDigest)_digestEngine;
                long total = 0;

                for (; ; )
                {
                    dataReady.WaitOne();
                    byte[] data;
                    queue.TryDequeue(out data);
                    prepareData.Set();
                    total += data.Length;

                    if ((Length == -1) || (total < Length))
                    {
                        h.BlockUpdate(data, 0, data.Length);
                        CalculateProgress(total);
                    }
                    else
                    {
                        int bytesRead = data.Length;
                        bytesRead = bytesRead - (int)(total - Length);
                        h.BlockUpdate(data, 0, data.Length);
                        CalculateProgress(total);
                    }

                    if (Length == -1)
                    {
                        if (data.Length != BUFFER_SIZE)
                            break;
                    }
                    else if (Length == total)
                    {
                        break;
                    }
                    else if (data.Length != BUFFER_SIZE)
                    {
                        throw new EndOfStreamException();
                    }
                    bytesTotal = total;
                }
            });

            reader.Wait();
            hasher.Wait();

            return bytesTotal;
        }

        private IDigest GetDigest(Digests Digest)
        {
            switch (Digest)
            {
                case Digests.Blake256:
                    return new Blake256();
                case Digests.Blake512:
                    return new Blake512();
                case Digests.Keccak256:
                    return new Keccak256();
                case Digests.Keccak512:
                    return new Keccak512();
                case Digests.SHA256:
                    return new SHA256();
                case Digests.Skein256:
                    return new Skein256();
                case Digests.Skein512:
                    return new Skein512();
                case Digests.Skein1024:
                    return new Skein1024();
                default:
                    return new SHA512();
            }
        }
        #endregion

        #region IDispose
        /// <summary>
        /// Dispose of this class
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool Disposing)
        {
            if (!_isDisposed && Disposing)
            {
                try
                {
                    if (_disposeEngine)
                    {
                        if (_digestEngine != null)
                        {
                            _digestEngine.Dispose();
                            _digestEngine = null;
                        }
                    }
                    if (_disposeStream)
                    {
                        if (_inStream != null)
                        {
                            _inStream.Dispose();
                            _inStream = null;
                        }
                    }
                }
                finally
                {
                    _isDisposed = true;
                }
            }
        }
        #endregion
    }
}
