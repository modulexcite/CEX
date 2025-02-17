﻿#region Directives
using System;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Symmetric.Block;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Symmetric.Block.Mode;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Symmetric.Block.Padding;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Symmetric.Stream;
using VTDev.Libraries.CEXEngine.Crypto.Common;
using VTDev.Libraries.CEXEngine.Crypto.Enumeration;
using VTDev.Libraries.CEXEngine.CryptoException;

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
// Written by John Underhill, May 19, 2015
// contact: develop@vtdev.com
#endregion

namespace VTDev.Libraries.CEXEngine.Crypto.Processing
{
    /// <summary>
    /// <h3>Packet cipher helper class.</h3>
    /// <para>Wraps encryption/decryption of a byte array in a continuous operation.</para>
    /// 
    /// </summary> 
    /// 
    /// <example>
    /// <description>Example of encrypting and decrypting a packet stream:</description>
    /// <code>
    /// public static void PacketCipherTest()
    /// {
    ///     const int BLSZ = 1024;
    ///     KeyParams key;
    ///     byte[] data;
    ///     MemoryStream instrm;
    ///     MemoryStream outstrm = new MemoryStream();
    /// 
    ///     using (KeyGenerator kg = new KeyGenerator())
    ///     {
    ///         // get the key
    ///         key = kg.GetKeyParams(32, 16);
    ///         // 2 * 1200 byte packets
    ///         data = kg.GetBytes(BLSZ * 2);
    ///     }
    ///     // data to encrypt
    ///     instrm = new MemoryStream(data);
    /// 
    ///     // Encrypt a stream //
    ///     // create the outbound cipher
    ///     using (ICipherMode cipher = new CTR(new RDX()))
    ///     {
    ///         // initialize the cipher for encryption
    ///         cipher.Initialize(true, key);
    ///         // set block size
    ///         ((CTR)cipher).ParallelBlockSize = BLSZ;
    /// 
    ///         // encrypt the stream
    ///         using (PacketCipher pc = new PacketCipher(cipher))
    ///         {
    ///             byte[] inbuffer = new byte[BLSZ];
    ///             byte[] outbuffer = new byte[BLSZ];
    ///             int bytesread = 0;
    /// 
    ///             while ((bytesread = instrm.Read(inbuffer, 0, BLSZ)) > 0)
    ///             {
    ///                 // encrypt the buffer
    ///                 pc.Write(inbuffer, 0, outbuffer, 0, BLSZ);
    ///                 // add it to the output stream
    ///                 outstrm.Write(outbuffer, 0, outbuffer.Length);
    ///             }
    ///         }
    ///     }
    /// 
    ///     // reset stream position
    ///     outstrm.Seek(0, SeekOrigin.Begin);
    ///     MemoryStream tmpstrm = new MemoryStream();
    /// 
    ///     // Decrypt a stream //
    ///     // create the inbound cipher
    ///     using (ICipherMode cipher = new CTR(new RDX()))
    ///     {
    ///         // initialize the cipher for decryption
    ///         cipher.Initialize(false, key);
    ///         // set block size
    ///         ((CTR)cipher).ParallelBlockSize = BLSZ;
    /// 
    ///         // decrypt the stream
    ///         using (PacketCipher pc = new PacketCipher(cipher))
    ///         {
    ///             byte[] inbuffer = new byte[BLSZ];
    ///             byte[] outbuffer = new byte[BLSZ];
    ///             int bytesread = 0;
    /// 
    ///             while ((bytesread = outstrm.Read(inbuffer, 0, BLSZ)) > 0)
    ///             {
    ///                 // process the encrypted bytes
    ///                 pc.Write(inbuffer, 0, outbuffer, 0, BLSZ);
    ///                 // write to stream
    ///                 tmpstrm.Write(outbuffer, 0, outbuffer.Length);
    ///             }
    ///         }
    ///     }
    /// 
    ///     // compare decrypted output with data
    ///     if (!Compare.AreEqual(tmpstrm.ToArray(), data))
    ///         throw new Exception();
    /// }
    /// </code>
    /// </example>
    /// 
    /// <revisionHistory>
    /// <revision date="2015/05/19" version="1.3.8.0">Initial release</revision>
    /// <revision date="2015/07/01" version="1.4.0.0">Added library exceptions</revision>
    /// </revisionHistory>
    /// 
    /// <seealso cref="VTDev.Libraries.CEXEngine.Crypto.Common.CipherDescription">VTDev.Libraries.CEXEngine.Crypto.Processing.Structures CipherDescription Interface</seealso>
    /// <seealso cref="VTDev.Libraries.CEXEngine.Crypto.Cipher.Symmetric.Stream">VTDev.Libraries.CEXEngine.Crypto.Cipher.Symmetric.Stream Interface</seealso>
    /// <seealso cref="VTDev.Libraries.CEXEngine.Crypto.Cipher.Symmetric.Block.Mode.ICipherMode">VTDev.Libraries.CEXEngine.Crypto.Cipher.Symmetric.Block.Mode.ICipherMode Interface</seealso>
    /// <seealso cref="VTDev.Libraries.CEXEngine.Crypto.Cipher.Symmetric.Block">VTDev.Libraries.CEXEngine.Crypto.Cipher.Symmetric.Block Namespace</seealso>
    /// <seealso cref="VTDev.Libraries.CEXEngine.Crypto.Enumeration.SymmetricEngines">VTDev.Libraries.CEXEngine.Crypto.Engines Enumeration</seealso>
    /// 
    /// <remarks>
    /// <description><h4>Implementation Notes:</h4></description>
    /// <list type="bullet">
    /// <item><description>This instance does not use padding; input and output arrays must be block aligned.</description></item>
    /// <item><description>Uses any of the implemented <see cref="ICipherMode">Cipher Mode</see> wrapped <see cref="SymmetricEngines">Block Ciphers</see>, or any of the implemented <see cref="IStreamCipher">Stream Ciphers</see>.</description></item>
    /// <item><description>Cipher Engine can be Disposed when this class is <see cref="Dispose()">Disposed</see>, set the DisposeEngine parameter in the class Constructor to true to dispose automatically.</description></item>
    /// <item><description>Changes to the Cipher or StreamCipher <see cref="ParallelBlockSize">ParallelBlockSize</see> must be set after initialization.</description></item>
    /// </list>
    /// </remarks>
    public class PacketCipher : IDisposable
    {
        #region Enums
        /// <summary>
        /// ParallelBlockProfile enumeration
        /// </summary>
        public enum BlockProfiles : int
        {
            /// <summary>
            /// Set parallel block size as a division of 100 segments
            /// </summary>
            ProgressProfile = 0,
            /// <summary>
            /// Set parallel block size for maximum possible speed
            /// </summary>
            SpeedProfile
        }
        #endregion

        #region Constants
        // Max array size allocation base; multiply by processor count for actual
        // byte/memory allocation during parallel loop execution
        private const int MAXALLOC_MB100 = 100000000;
        // default parallel block size
        private const int PARALLEL_DEFBLOCK = 64000;
        #endregion

        #region Fields
        private ICipherMode _cipherEngine;
        private IStreamCipher _streamCipher;
        private bool _isEncryption = true;
        private int _blockSize = PARALLEL_DEFBLOCK;
        private bool _disposeEngine = false;
        private bool _isCounterMode = false;
        private bool _isDisposed = false;
        private bool _isParallel = false;
        private bool _isStreamCipher = false;
        private int _processorCount;
        #endregion

        #region Properties
        /// <summary>
        /// Get/Set: Automatic processor parallelization
        /// </summary>
        public bool IsParallel
        {
            get { return _isParallel; }
            set { _isParallel = value; }
        }

        /// <summary>
        /// Get/Set: Parallel block size. Must be a multiple of <see cref="ParallelMinimumSize"/>.
        /// </summary>
        /// 
        /// <exception cref="CryptoProcessingException">Thrown if a parallel block size is not evenly divisible by ParallelMinimumSize, 
        /// or the size is less than ParallelMinimumSize or more than ParallelMaximumSize values</exception>
        public int ParallelBlockSize
        {
            get { return _blockSize; }
            set
            {
                if (value % ParallelMinimumSize != 0)
                    throw new CryptoProcessingException("PacketCipher:ParallelBlockSize", String.Format("Parallel block size must be evenly divisible by ParallelMinimumSize: {0}", ParallelMinimumSize), new ArgumentException());
                if (value > ParallelMaximumSize || value < ParallelMinimumSize)
                    throw new CryptoProcessingException("PacketCipher:ParallelBlockSize", String.Format("Parallel block must be Maximum of ParallelMaximumSize: {0} and evenly divisible by ParallelMinimumSize: {1}", ParallelMaximumSize, ParallelMinimumSize), new ArgumentOutOfRangeException());

                _blockSize = value;
            }
        }

        /// <summary>
        /// Get: Maximum input size with parallel processing
        /// </summary>
        public int ParallelMaximumSize
        {
            get { return MAXALLOC_MB100; }
        }

        /// <summary>
        /// Get: The smallest parallel block size. Parallel blocks must be a multiple of this size.
        /// </summary>
        public int ParallelMinimumSize
        {
            get
            {
                if (_isStreamCipher)
                {
                    if (_streamCipher.GetType().Equals(typeof(Fusion)))
                        return ((Fusion)_streamCipher).ParallelMinimumSize;
                    else
                        return 0;
                }
                else
                {
                    if (_cipherEngine.GetType().Equals(typeof(CTR)))
                        return ((CTR)_cipherEngine).ParallelMinimumSize;
                    else if (_cipherEngine.GetType().Equals(typeof(CBC)) && !_isEncryption)
                        return ((CBC)_cipherEngine).ParallelMinimumSize;
                    else if (_cipherEngine.GetType().Equals(typeof(CFB)) && !_isEncryption)
                        return ((CFB)_cipherEngine).ParallelMinimumSize;
                    else
                        return 0;
                }
            }
        }

        /// <summary>
        /// Get: The system processor count
        /// </summary>
        public int ProcessorCount
        {
            get { return _processorCount; }
            private set { _processorCount = value; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Initialize the class with a CipherDescription Structure; containing the cipher implementation details, and a <see cref="KeyParams"/> class containing the Key material.
        /// <para>This constructor creates and configures cryptographic instances based on the cipher description contained in a CipherDescription. 
        /// Cipher modes, padding, and engines are destroyed automatically through this classes Dispose() method.</para>
        /// </summary>
        /// 
        /// <param name="Encryption">Cipher is an encryptor</param>
        /// <param name="Description">A <see cref="CipherDescription"/> containing the cipher description</param>
        /// <param name="KeyParam">A <see cref="KeyParams"/> class containing the encryption Key material</param>
        /// 
        /// <exception cref="CryptoProcessingException">Thrown if an invalid <see cref="CipherDescription">CipherDescription</see> or <see cref="KeyParams">KeyParams</see> is used</exception>
        public PacketCipher(bool Encryption, CipherDescription Description, KeyParams KeyParam)
        {
            if (!CipherDescription.IsValid(Description))
                throw new CryptoProcessingException("PacketCipher:CTor", "The key Header is invalid!", new ArgumentException());
            if (KeyParam == null)
                throw new CryptoProcessingException("PacketCipher:CTor", "KeyParam can not be null!", new ArgumentNullException());

            _disposeEngine = true;
            _isEncryption = Encryption;
            _blockSize = Description.BlockSize;
            _isParallel = false;

            if (_isStreamCipher = IsStreamCipher((SymmetricEngines)Description.EngineType))
            {
                _streamCipher = GetStreamEngine((SymmetricEngines)Description.EngineType, Description.RoundCount, (Digests)Description.KdfEngine);
                _streamCipher.Initialize(KeyParam);

                if (_streamCipher.GetType().Equals(typeof(Fusion)))
                {
                    if (_isParallel = ((Fusion)_streamCipher).IsParallel)
                        _blockSize = ((Fusion)_streamCipher).ParallelBlockSize;
                }
            }
            else
            {
                _cipherEngine = GetCipher((CipherModes)Description.CipherType, (SymmetricEngines)Description.EngineType, Description.RoundCount, Description.BlockSize, (Digests)Description.KdfEngine);
                _cipherEngine.Initialize(_isEncryption, KeyParam);

                if (_isCounterMode = _cipherEngine.GetType().Equals(typeof(CTR)))
                {
                    if (_isParallel = ((CTR)_cipherEngine).IsParallel)
                        _blockSize = ((CTR)_cipherEngine).ParallelBlockSize;
                }
                else
                {
                    if (_cipherEngine.GetType().Equals(typeof(CBC)))
                    {
                        if (_isParallel = ((CBC)_cipherEngine).IsParallel && !((CBC)_cipherEngine).IsEncryption)
                            _blockSize = ((CBC)_cipherEngine).ParallelBlockSize;
                    }
                    else if (_cipherEngine.GetType().Equals(typeof(CFB)))
                    {
                        if (_isParallel = ((CFB)_cipherEngine).IsParallel && !((CFB)_cipherEngine).IsEncryption)
                            _blockSize = ((CFB)_cipherEngine).ParallelBlockSize;
                    }
                }
            }
        }

        /// <summary>
        /// Initialize the class with a Block <see cref="ICipherMode">Cipher</see> and optional <see cref="IPadding">Padding</see> instances.
        /// <para>This constructor requires a fully initialized <see cref="CipherModes">CipherMode</see> instance.
        /// If the <see cref="PaddingModes">PaddingMode</see> parameter is null, X9.23 padding will be used if required.</para>
        /// </summary>
        /// 
        /// <param name="Cipher">The <see cref="SymmetricEngines">Block Cipher</see> wrapped in a <see cref="ICipherMode">Cipher</see> mode</param>
        /// <param name="Padding">The <see cref="IPadding">Padding</see> instance</param>
        /// <param name="DisposeEngine">Dispose of cipher engine when <see cref="Dispose()"/> on this class is called</param>
        /// 
        /// <exception cref="CryptoProcessingException">Thrown if a null or uninitialized <see cref="ICipherMode">Cipher</see> is used</exception>
        public PacketCipher(ICipherMode Cipher, IPadding Padding = null, bool DisposeEngine = false)
        {
            if (Cipher == null)
                throw new CryptoProcessingException("PacketCipher:CTor", "The Cipher can not be null!", new ArgumentNullException());
            if (!Cipher.IsInitialized)
                throw new CryptoProcessingException("PacketCipher:CTor", "The Cipher has not been initialized!", new ArgumentException());

            _disposeEngine = DisposeEngine;
            _cipherEngine = Cipher;
            _isStreamCipher = false;
            _blockSize = _cipherEngine.BlockSize;
            _isEncryption = _cipherEngine.IsEncryption;
            _isParallel = false;

            if (_isCounterMode = _cipherEngine.GetType().Equals(typeof(CTR)))
            {
                if (_isParallel = ((CTR)_cipherEngine).IsParallel)
                    _blockSize = ((CTR)_cipherEngine).ParallelBlockSize;
            }
            else
            {
                if (_cipherEngine.GetType().Equals(typeof(CBC)))
                    _isParallel = ((CBC)_cipherEngine).IsParallel && !((CBC)_cipherEngine).IsEncryption;
            }
        }

        /// <summary>
        /// Initialize the class with a <see cref="IStreamCipher">Stream Cipher</see> instance.
        /// <para>This constructor requires a fully initialized <see cref="SymmetricEngines">StreamCipher</see> instance.</para>
        /// </summary>
        /// 
        /// <param name="Cipher">The initialized <see cref="IStreamCipher">Stream Cipher</see> instance</param>
        /// <param name="DisposeEngine">Dispose of cipher engine when <see cref="Dispose()"/> on this class is called</param>
        /// 
        /// <exception cref="CryptoProcessingException">Thrown if a null or uninitialized <see cref="ICipherMode">Cipher</see> is used</exception>
        public PacketCipher(IStreamCipher Cipher, bool DisposeEngine = true)
        {
            if (Cipher == null)
                throw new CryptoProcessingException("PacketCipher:CTor", "The Cipher can not be null!", new ArgumentNullException());
            if (!Cipher.IsInitialized)
                throw new CryptoProcessingException("PacketCipher:CTor", "The Cipher has not been initialized!", new ArgumentException());

            _disposeEngine = DisposeEngine;
            _streamCipher = Cipher;
            _isStreamCipher = true;
            _blockSize = 1024;
            _isCounterMode = false;

            // set defaults
            if (_streamCipher.GetType().Equals(typeof(Fusion)))
            {
                if (_isParallel = ((Fusion)_streamCipher).IsParallel)
                    _blockSize = ((Fusion)_streamCipher).ParallelBlockSize;
            }
            else
            {
                _isParallel = false;
            }
        }

        private PacketCipher()
        {
        }

        /// <summary>
        /// Finalize objects
        /// </summary>
        ~PacketCipher()
        {
            Dispose(false);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Process a length within the Input stream using Offsets
        /// </summary>
        /// 
        /// <param name="Input">The Input Stream</param>
        /// <param name="InOffset">The Input Stream positional offset</param>
        /// <param name="Output">The Output Stream</param>
        /// <param name="OutOffset">The Output Stream positional offset</param>
        /// <param name="Length">The number of bytes to process</param>
        public void Write(byte[] Input, int InOffset, byte[] Output, int OutOffset, int Length)
        {
            if (!_isStreamCipher)
            {
                if (_isEncryption)
                    Encrypt(Input, InOffset, Output, OutOffset, Length);
                else
                    Decrypt(Input, InOffset, Output, OutOffset, Length);
            }
            else
            {
                ProcessStream(Input, InOffset, Output, OutOffset, Length);
            }
        }
        #endregion

        #region Crypto
        private void Decrypt(byte[] Input, int InOffset, byte[] Output, int OutOffset, int Length)
        {
            // no padding, input lengths must align
            Length += InOffset;

            while (InOffset < Length)
            {
                _cipherEngine.Transform(Input, InOffset, Output, OutOffset);
                InOffset += _blockSize;
                OutOffset += _blockSize;
            }
        }

        private void Encrypt(byte[] Input, int InOffset, byte[] Output, int OutOffset, int Length)
        {
            // no padding, input lengths must align
            Length += InOffset;

            while (InOffset < Length)
            {
                _cipherEngine.Transform(Input, InOffset, Output, OutOffset);
                InOffset += _blockSize;
                OutOffset += _blockSize;
            }
        }

        private IBlockCipher GetBlockEngine(SymmetricEngines EngineType, int RoundCount, int BlockSize, Digests KdfEngine)
        {
            if (EngineType == SymmetricEngines.RDX)
                return new RDX(BlockSize);
            else if (EngineType == SymmetricEngines.RHX)
                return new RHX(RoundCount, BlockSize, KdfEngine);
            else if (EngineType == SymmetricEngines.RSM)
                return new RSM(RoundCount, BlockSize, KdfEngine);
            else if (EngineType == SymmetricEngines.SHX)
                return new SHX(RoundCount, KdfEngine);
            else if (EngineType == SymmetricEngines.SPX)
                return new SPX(RoundCount);
            else if (EngineType == SymmetricEngines.TFX)
                return new TFX(RoundCount);
            else if (EngineType == SymmetricEngines.THX)
                return new THX(RoundCount, KdfEngine);
            else if (EngineType == SymmetricEngines.TSM)
                return new TSM(RoundCount, KdfEngine);
            else
                return new RHX(RoundCount, BlockSize, KdfEngine);
        }

        private ICipherMode GetCipher(CipherModes CipherType, SymmetricEngines EngineType, int RoundCount, int BlockSize, Digests KdfEngine)
        {
            if (CipherType == CipherModes.CBC)
                return new CBC(GetBlockEngine(EngineType, RoundCount, BlockSize, KdfEngine));
            else if (CipherType == CipherModes.CFB)
                return new CFB(GetBlockEngine(EngineType, RoundCount, BlockSize, KdfEngine), BlockSize * 8);
            else if (CipherType == CipherModes.OFB)
                return new OFB(GetBlockEngine(EngineType, RoundCount, BlockSize, KdfEngine));
            else
                return new CTR(GetBlockEngine(EngineType, RoundCount, BlockSize, KdfEngine));
        }

        private IPadding GetPadding(PaddingModes PaddingType)
        {
            if (PaddingType == PaddingModes.ISO7816)
                return new ISO7816();
            else if (PaddingType == PaddingModes.PKCS7)
                return new PKCS7();
            else if (PaddingType == PaddingModes.TBC)
                return new TBC();
            else if (PaddingType == PaddingModes.X923)
                return new X923();
            else
                return new PKCS7();
        }

        private IStreamCipher GetStreamEngine(SymmetricEngines EngineType, int RoundCount, Digests KdfEngine)
        {
            if (EngineType == SymmetricEngines.ChaCha)
                return new ChaCha(RoundCount);
            else if (EngineType == SymmetricEngines.Fusion)
                return new Fusion(RoundCount, KdfEngine);
            else if (EngineType == SymmetricEngines.Salsa)
                return new Salsa20(RoundCount);
            else
                return null;
        }

        private bool IsStreamCipher(SymmetricEngines EngineType)
        {
            return EngineType == SymmetricEngines.ChaCha ||
                EngineType == SymmetricEngines.Salsa ||
                EngineType == SymmetricEngines.Fusion;
        }

        private void ProcessStream(byte[] Input, int InOffset, byte[] Output, int OutOffset, int Length)
        {
            Length += InOffset;

            while (InOffset < Length)
            {
                _streamCipher.Transform(Input, InOffset, Output, OutOffset);
                InOffset += _blockSize;
                OutOffset += _blockSize;
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
                        if (_cipherEngine != null)
                        {
                            _cipherEngine.Dispose();
                            _cipherEngine = null;
                        }
                        if (_streamCipher != null)
                        {
                            _streamCipher.Dispose();
                            _streamCipher = null;
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
