﻿#region Directives
using System;
using System.IO;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Interfaces;
using VTDev.Libraries.CEXEngine.Crypto.Digest;
using VTDev.Libraries.CEXEngine.CryptoException;
using VTDev.Libraries.CEXEngine.Tools;
using VTDev.Libraries.CEXEngine.Crypto.Enumeration;
using VTDev.Libraries.CEXEngine.Crypto.Prng;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Sign.GMSS.Arithmetic;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Sign.GMSS.Utility;
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
// Principal Algorithms:
// An implementation of the Generalized Merkle Signature Scheme Asymmetric Signature Scheme.
// 
// Code Base Guides:
// Portions of this code based on the Bouncy Castle Based on the Bouncy Castle Java 
// <see href="http://bouncycastle.org/latest_releases.html">Release 1.51</see> version.
// 
// Implementation Details:
// An implementation of an Generalized Merkle Signature Scheme. 
// Written by John Underhill, July 06, 2015
// contact: develop@vtdev.com
#endregion

namespace VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Sign.GMSS
{
    /// <summary>
    /// An Generalized Merkle Signature Scheme Asymmetric Signature Scheme implementation
    /// </summary>
    /// 
    /// <example>
    /// <description>Example of using the signing and verifying an array:</description>
    /// <code>
    /// byte[] code;
    /// byte[] data = new byte[100];
    /// 
    /// GMSSKeyGenerator kpm = (GMSSParameters)GMSSParamSets.GMSSN2P10.DeepCopy();
    /// GMSSKeyGenerator gen = new GMSSKeyGenerator(kpm);
    /// IAsymmetricKeyPair keyPair = gen.GenerateKeyPair();
    ///
    /// // get the message code for an array of bytes
    /// using (GMSSSign sign = new GMSSSign(kpm))
    /// {
    ///     sign.Initialize(kp.PrivateKey);
    ///     code = sign.Sign(data, 0, data.Length);
    /// }
    ///
    /// // test the message for validity
    /// using (GMSSSign sign = new GMSSSign(kpm))
    /// {
    ///     sign.Initialize(kp.PublicKey);
    ///     bool valid = sign.Verify(data, 0, data.Length, code);
    /// }
    /// </code>
    /// </example>
    /// 
    /// <revisionHistory>
    /// <revision date="2015/07/06" version="1.4.0.0">Initial release</revision>
    /// </revisionHistory>
    /// 
    /// <seealso cref="VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Sign.GMSS.GMSSSign">VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Sign.GMSS GMSSSign Class</seealso>
    /// <seealso cref="VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Sign.GMSS.GMSSPublicKey">VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Sign.GMSS GMSSPublicKey Class</seealso>
    /// <seealso cref="VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Sign.GMSS.GMSSPrivateKey">VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Sign.GMSS GMSSPrivateKey Class</seealso>
    /// <seealso cref="VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Interfaces.IAsymmetricKeyPair">VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Interfaces IAsymmetricKeyPair Interface</seealso>
    /// <seealso cref="VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Interfaces.IAsymmetricKey">VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.Interfaces IAsymmetricKey Interface</seealso>
    /// <seealso cref="VTDev.Libraries.CEXEngine.Crypto.Prng.IRandom">VTDev.Libraries.CEXEngine.Crypto.Prng.IRandom Interface</seealso>
    /// <seealso cref="VTDev.Libraries.CEXEngine.Crypto.Enumeration.Prngs">VTDev.Libraries.CEXEngine.Crypto.Enumeration Prngs Enumeration</seealso>
    /// 
    /// <remarks>
    /// <description><h4>Guiding Publications:</h4></description>
    /// <list type="number">
    /// <item><description>Selecting Parameters for the Generalized Merkle Signature Scheme Signature Scheme<cite>Generalized Merkle Signature Scheme Parameters</cite>.</description></item>
    /// </list>
    /// </remarks>
    public sealed class GMSSSign : IAsymmetricSign
    {
        #region Constants
        private const string ALG_NAME = "GMSSSign";
        #endregion

        #region Fields
        private IAsymmetricKey _asmKey;
        private bool _isDisposed = false;
        private bool _isInitialized = false;
        // The raw GMSS public key
        private byte[] _pubKeyBytes;
        // Hash function for the construction of the authentication trees
        private IDigest _msgDigestTrees;
        // The length of the hash function output
        private int _mdLength;
        // The number of tree layers
        private int _numLayer;
        // The hash function used by the OTS
        private IDigest _msgDigestOTS;
        // An instance of the Winternitz one-time signature
        private WinternitzOTSignature _Ots;
        // The current main tree and subtree indices
        private int[] _index;
        // Array of the authentication paths for the current trees of all layers
        private byte[][][] _currentAuthPaths;
        // The one-time signature of the roots of the current subtrees
        private byte[][] _subtreeRootSig;
        // The GMSSParameterset
        private GMSSParameters _gmssPS;
        // The PRNG
        private GMSSRandom _gmssRandom;
        #endregion

        #region Properties
        /// <summary>
        /// Get: The cipher has been initialized with a key
        /// </summary>
        public bool IsInitialized
        {
            get { return _isInitialized; }
        }

        /// <summary>
        /// Get: This class is initialized for Signing with the Private key
        /// </summary>
        /// 
        /// <exception cref="CryptoAsymmetricSignException">Thrown if cipher has not been initialized</exception>
        public bool IsSigner
        {
            get 
            { 
                if (!_isInitialized)
                    throw new CryptoAsymmetricSignException("GMSSSign:IsSigner", "The signer has not been initialized!", new InvalidOperationException());

                return (_asmKey is GMSSPrivateKey);
            }
        }

        /// <summary>
        /// Get: The Signer name
        /// </summary>
        public string Name
        {
            get { return ALG_NAME; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Initialize this class
        /// </summary>
        /// 
        /// <param name="CipherParams">The GMSS cipher used to encrypt the hash</param>
        public GMSSSign(GMSSParameters CipherParams)
        {
            _gmssPS = CipherParams;
            _msgDigestTrees = GetDigest(CipherParams.DigestEngine);
            _msgDigestOTS = _msgDigestTrees;
            _mdLength = _msgDigestTrees.DigestSize;
            _gmssRandom = new GMSSRandom(_msgDigestTrees);
        }

        private GMSSSign()
        {
        }

        /// <summary>
        /// Finalize objects
        /// </summary>
        ~GMSSSign()
        {
            Dispose(false);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the Key for Sign (Private) or Verify (Public)
        /// </summary>
        /// 
        /// <param name="AsmKey">The <see cref="IAsymmetricKey"/> containing the Generalized Merkle Signature Scheme Public (verify) or Private (sign) key</param>
        /// 
        /// <exception cref="CryptoAsymmetricSignException">Thrown if an invalid key is used</exception>
        public void Initialize(IAsymmetricKey AsmKey)
        {
            if (!(AsmKey is GMSSPublicKey) && !(AsmKey is GMSSPrivateKey))
                throw new CryptoAsymmetricSignException("GMSSSign:Initialize", "The key pair is not a valid GMSS key pair!", new InvalidDataException());

            Reset();
            _asmKey = AsmKey;
            _isInitialized = true;

            if (AsmKey is GMSSPrivateKey)
                InitSign();
            else
                InitVerify();
        }

        /// <summary>
        /// Reset the underlying digest engine
        /// </summary>
        public void Reset()
        {
            //_rndEngine.Reset();
        }

        /// <summary>
        /// Get the signing code for a stream
        /// </summary>
        /// 
        /// <param name="InputStream">The stream containing the data</param>
        /// 
        /// <returns>The encrypted hash code</returns>
        /// 
        /// <exception cref="CryptoAsymmetricSignException">Thrown if an invalid key is used, or signer has not been initialized</exception>
        public byte[] Sign(Stream InputStream)
        {
            if (!_isInitialized)
                throw new CryptoAsymmetricSignException("GMSSSign:Sign", "The signer has not been initialized!", new InvalidOperationException());
            if (_asmKey == null)
                throw new CryptoAsymmetricSignException("GMSSSign:Sign", "The private key is invalid!", new InvalidDataException());
            if (!(_asmKey is GMSSPrivateKey))
                throw new CryptoAsymmetricSignException("GMSSSign:Sign", "The private key is invalid!", new InvalidDataException());

            byte[] data = ((MemoryStream)InputStream).ToArray();

            return GenerateSignature(data);
        }

        /// <summary>
        /// Get the signing code for a stream
        /// </summary>
        /// 
        /// <param name="Input">The byte array contining the data</param>
        /// <param name="Offset">The starting offset within the Input array</param>
        /// <param name="Length">The number of bytes to process</param>
        /// 
        /// <returns>The encrypted hash code</returns>
        /// 
        /// <exception cref="CryptoAsymmetricSignException">Thrown if input array is too short, signer is not initialized, or the key is invalid</exception>
        public byte[] Sign(byte[] Input, int Offset, int Length)
        {
            if (Input.Length - Offset < Length)
                throw new CryptoAsymmetricSignException("GMSSSign:Sign", "The input array is too short!", new ArgumentException());
            if (!_isInitialized)
                throw new CryptoAsymmetricSignException("GMSSSign:Sign", "The signer has not been initialized!", new InvalidOperationException());
            if (_asmKey == null)
                throw new CryptoAsymmetricSignException("GMSSSign:Sign", "The private key is invalid!", new InvalidDataException());
            if (!(_asmKey is GMSSPrivateKey))
                throw new CryptoAsymmetricSignException("GMSSSign:Sign", "The private key is invalid!", new InvalidDataException());

            byte[] data = new byte[Length];
            Array.Copy(Input, Offset, data, 0, Length);

            return GenerateSignature(data);
        }

        /// <summary>
        /// Test the hashed value of a stream against the decrypted code
        /// </summary>
        /// 
        /// <param name="InputStream">The stream containing the data to test</param>
        /// <param name="Code">The encrypted hash code</param>
        /// 
        /// <returns>Returns <c>true</c> if the codes match</returns>
        /// 
        /// <exception cref="CryptoAsymmetricSignException">Thrown if signer is not initialized, or the key is invalid</exception>
        public bool Verify(Stream InputStream, byte[] Code)
        {
            if (!_isInitialized)
                throw new CryptoAsymmetricSignException("GMSSSign:Verify", "The signer has not been initialized!", new InvalidOperationException());
            if (_asmKey == null)
                throw new CryptoAsymmetricSignException("GMSSSign:Verify", "The public key is invalid!", new InvalidDataException());
            if (!(_asmKey is GMSSPublicKey))
                throw new CryptoAsymmetricSignException("GMSSSign:Verify", "The public key is invalid!", new InvalidDataException());

            byte[] data = ((MemoryStream)InputStream).ToArray();

            return VerifySignature(data, Code);
        }

        /// <summary>
        /// Test the hashed value of a stream against the decrypted code
        /// </summary>
        /// 
        /// <param name="Input">The stream containing the data to test</param>
        /// <param name="Offset">The starting offset within the Input array</param>
        /// <param name="Length">The number of bytes to process</param>
        /// <param name="Code">The encrypted hash code</param>
        /// 
        /// <returns>Returns <c>true</c> if the codes match</returns>
        /// 
        /// <exception cref="CryptoAsymmetricSignException">Thrown if input array is too short, signer is not initialized, or the key is invalid</exception>
        public bool Verify(byte[] Input, int Offset, int Length, byte[] Code)
        {
            if (Input.Length - Offset < Length)
                throw new CryptoAsymmetricSignException("GMSSSign:Verify", "The input array is too short!", new ArgumentOutOfRangeException());
            if (!_isInitialized)
                throw new CryptoAsymmetricSignException("GMSSSign:Verify", "The signer has not been initialized!", new InvalidOperationException());
            if (_asmKey == null)
                throw new CryptoAsymmetricSignException("GMSSSign:Verify", "The public key is invalid!", new InvalidDataException());
            if (!(_asmKey is GMSSPublicKey))
                throw new CryptoAsymmetricSignException("GMSSSign:Verify", "The public key is invalid!", new InvalidDataException());

            byte[] data = new byte[Length];
            Array.Copy(Input, Offset, data, 0, Length);

            return VerifySignature(data, Code);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Initializes the signature algorithm for signing a message
        /// </summary>
        private void InitSign()
        {
            _msgDigestTrees.Reset();
            // set private key and take from it ots key, auth, tree and key counter, rootSign
            GMSSPrivateKey gmssPrivateKey = (GMSSPrivateKey)_asmKey;

            if (gmssPrivateKey.IsUsed)
                throw new Exception("Private key already used");

            // check if last signature has been generated
            if (gmssPrivateKey.GetCurrentIndex(0) >= gmssPrivateKey.GetNumLeafs(0))
                throw new Exception("No more signatures can be generated");

            // get numLayer
            _numLayer = _gmssPS.NumLayers;

            // get OTS Instance of lowest layer
            byte[] seed = gmssPrivateKey.CurrentSeeds[_numLayer - 1];
            byte[] OTSSeed = new byte[_mdLength];
            byte[] dummy = new byte[_mdLength];
            Array.Copy(seed, 0, dummy, 0, _mdLength);
            OTSSeed = _gmssRandom.NextSeed(dummy);
            _Ots = new WinternitzOTSignature(OTSSeed, GetDigest(_gmssPS.DigestEngine), _gmssPS.WinternitzParameter[_numLayer - 1]);

            byte[][][] helpCurrentAuthPaths = gmssPrivateKey.CurrentAuthPaths;
            _currentAuthPaths = new byte[_numLayer][][];

            // copy the main tree authentication path
            for (int j = 0; j < _numLayer; j++)
            {
                _currentAuthPaths[j] = ArrayUtils.CreateJagged<byte[][]>(helpCurrentAuthPaths[j].Length, _mdLength);
                for (int i = 0; i < helpCurrentAuthPaths[j].Length; i++)
                    Array.Copy(helpCurrentAuthPaths[j][i], 0, _currentAuthPaths[j][i], 0, _mdLength);
            }

            // copy index
            _index = new int[_numLayer];
            Array.Copy(gmssPrivateKey.Index, 0, _index, 0, _numLayer);

            // copy subtreeRootSig
            byte[] helpSubtreeRootSig;
            _subtreeRootSig = new byte[_numLayer - 1][];
            for (int i = 0; i < _numLayer - 1; i++)
            {
                helpSubtreeRootSig = gmssPrivateKey.SubtreeRootSig(i);
                _subtreeRootSig[i] = new byte[helpSubtreeRootSig.Length];
                Array.Copy(helpSubtreeRootSig, 0, _subtreeRootSig[i], 0, helpSubtreeRootSig.Length);
            }

            if (gmssPrivateKey.GetCurrentIndex(0) >= gmssPrivateKey.GetNumLeafs(0))
                gmssPrivateKey.IsUsed = true;
        }

        private void InitVerify()
        {
            _msgDigestTrees.Reset();
            GMSSPublicKey gmssPublicKey = (GMSSPublicKey)_asmKey;
            _pubKeyBytes = gmssPublicKey.PublicKey;
            // get numLayer
            _numLayer = _gmssPS.NumLayers;
        }

        /// <summary>
        /// This function signs the message that has been updated, making use of the private key.
        /// <para>For computing the signature, L1 and L2 are needed, as well as LES should be solved 
        /// for each layer in order to find the Oil-variables in the layer.
        /// The Vinegar-variables of the first layer are random generated.</para>
        /// </summary>
        /// 
        /// <param name="Message">The message</param>
        /// 
        /// <returns>The signature of the message</returns>
        private byte[] GenerateSignature(byte[] Message)
        {
            byte[] otsSig = new byte[_mdLength];
            byte[] authPathBytes;
            byte[] indexBytes;

            otsSig = _Ots.GetSignature(Message);
            // get concatenated lowest layer tree authentication path
            authPathBytes = GMSSUtil.ConcatenateArray(_currentAuthPaths[_numLayer - 1]);
            // put lowest layer index into a byte array
            indexBytes = GMSSUtil.IntToBytesLittleEndian(_index[_numLayer - 1]);
            // create first part of GMSS signature
            byte[] gmssSigFirstPart = new byte[indexBytes.Length + otsSig.Length + authPathBytes.Length];
            Array.Copy(indexBytes, 0, gmssSigFirstPart, 0, indexBytes.Length);
            Array.Copy(otsSig, 0, gmssSigFirstPart, indexBytes.Length, otsSig.Length);
            Array.Copy(authPathBytes, 0, gmssSigFirstPart, (indexBytes.Length + otsSig.Length), authPathBytes.Length);
            // create initial array with length 0 for iteration
            byte[] gmssSigNextPart = new byte[0];

            for (int i = _numLayer - 1 - 1; i >= 0; i--)
            {
                // get concatenated next tree authentication path
                authPathBytes = GMSSUtil.ConcatenateArray(_currentAuthPaths[i]);
                // put next tree index into a byte array
                indexBytes = GMSSUtil.IntToBytesLittleEndian(_index[i]);
                // create help array and copy actual gmssSig into it
                byte[] helpGmssSig = new byte[gmssSigNextPart.Length];
                Array.Copy(gmssSigNextPart, 0, helpGmssSig, 0, gmssSigNextPart.Length);
                // adjust length of gmssSigNextPart for adding next part
                gmssSigNextPart = new byte[helpGmssSig.Length + indexBytes.Length + _subtreeRootSig[i].Length + authPathBytes.Length];
                // copy old data (help array) and new data in gmssSigNextPart
                Array.Copy(helpGmssSig, 0, gmssSigNextPart, 0, helpGmssSig.Length);
                Array.Copy(indexBytes, 0, gmssSigNextPart, helpGmssSig.Length, indexBytes.Length);
                Array.Copy(_subtreeRootSig[i], 0, gmssSigNextPart, (helpGmssSig.Length + indexBytes.Length), _subtreeRootSig[i].Length);
                Array.Copy(authPathBytes, 0, gmssSigNextPart, (helpGmssSig.Length + indexBytes.Length + _subtreeRootSig[i].Length), authPathBytes.Length);
            }

            // concatenate the two parts of the GMSS signature
            byte[] gmssSig = new byte[gmssSigFirstPart.Length + gmssSigNextPart.Length];
            Array.Copy(gmssSigFirstPart, 0, gmssSig, 0, gmssSigFirstPart.Length);
            Array.Copy(gmssSigNextPart, 0, gmssSig, gmssSigFirstPart.Length, gmssSigNextPart.Length);

            // return the GMSS signature
            return gmssSig;
        }

        /// <summary>
        /// This function verifies the signature of the message that has been updated, with the aid of the public key
        /// </summary>
        /// 
        /// <param name="Message">The message</param>
        /// <param name="Signature">The signature of the message</param>
        /// 
        /// <returns>Returns true if the signature has been verified, false otherwise</returns>
        private bool VerifySignature(byte[] Message, byte[] Signature)
        {
            bool success = false;
            // int halfSigLength = signature.length >>> 1;
            _msgDigestOTS.Reset();
            WinternitzOTSVerify otsVerify;
            int otsSigLength;
            byte[] help = Message;
            byte[] otsSig;
            byte[] otsPublicKey;
            byte[][] authPath;
            byte[] dest;
            int nextEntry = 0;
            int index;

            // begin with message = 'message that was signed' and then in each step message = subtree root
            for (int j = _numLayer - 1; j >= 0; j--)
            {
                otsVerify = new WinternitzOTSVerify(GetDigest(_gmssPS.DigestEngine), _gmssPS.WinternitzParameter[j]);
                otsSigLength = otsVerify.GetSignatureLength();
                Message = help;
                // get the subtree index
                index = GMSSUtil.BytesToIntLittleEndian(Signature, nextEntry);
                // 4 is the number of bytes in integer
                nextEntry += 4;
                // get one-time signature
                otsSig = new byte[otsSigLength];
                Array.Copy(Signature, nextEntry, otsSig, 0, otsSigLength);
                nextEntry += otsSigLength;
                // compute public OTS key from the one-time signature
                otsPublicKey = otsVerify.Verify(Message, otsSig);

                // test if OTSsignature is correct
                if (otsPublicKey == null)
                    return false;

                // get authentication path from the signature
                authPath = ArrayUtils.CreateJagged<byte[][]>(_gmssPS.HeightOfTrees[j], _mdLength);//new byte[gmssPS.GetHeightOfTrees()[j]][mdLength];
                for (int i = 0; i < authPath.Length; i++)
                {
                    Array.Copy(Signature, nextEntry, authPath[i], 0, _mdLength);
                    nextEntry = nextEntry + _mdLength;
                }

                // compute the root of the subtree from the authentication path
                help = new byte[_mdLength];
                help = otsPublicKey;
                int count = 1 << authPath.Length;
                count = count + index;

                for (int i = 0; i < authPath.Length; i++)
                {
                    dest = new byte[_mdLength << 1];

                    if ((count % 2) == 0)
                    {
                        Array.Copy(help, 0, dest, 0, _mdLength);
                        Array.Copy(authPath[i], 0, dest, _mdLength, _mdLength);
                        count = count / 2;
                    }
                    else
                    {
                        Array.Copy(authPath[i], 0, dest, 0, _mdLength);
                        Array.Copy(help, 0, dest, _mdLength, help.Length);
                        count = (count - 1) / 2;
                    }

                    _msgDigestTrees.BlockUpdate(dest, 0, dest.Length);
                    help = new byte[_msgDigestTrees.DigestSize];
                    _msgDigestTrees.DoFinal(help, 0);
                }
            }

            // now help contains the root of the maintree
            // test if help is equal to the GMSS public key
            if (Compare.AreEqual(_pubKeyBytes, help))
                success = true;

            return success;
        }

        /// <summary>
        /// Get the digest engine
        /// </summary>
        /// 
        /// <param name="Digest">Engine type</param>
        /// 
        /// <returns>Instance of digest</returns>
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
                case Digests.Keccak1024:
                    return new Keccak1024();
                case Digests.SHA256:
                    return new SHA256();
                case Digests.SHA512:
                    return new SHA512();
                case Digests.Skein256:
                    return new Skein256();
                case Digests.Skein512:
                    return new Skein512();
                case Digests.Skein1024:
                    return new Skein1024();
                default:
                    throw new CryptoAsymmetricException("GMSSKeyGenerator:GetDigest", "The digest type is not supported!", new ArgumentException());
            }
        }

        /// <summary>
        /// Get the cipher engine
        /// </summary>
        /// 
        /// <param name="Prng">The Prng</param>
        /// 
        /// <returns>An initialized prng</returns>
        private IRandom GetPrng(Prngs Prng)
        {
            switch (Prng)
            {
                case Prngs.CTRPrng:
                    return new CTRPrng();
                case Prngs.SP20Prng:
                    return new SP20Prng();
                case Prngs.DGCPrng:
                    return new DGCPrng();
                case Prngs.CSPRng:
                    return new CSPRng();
                case Prngs.BBSG:
                    return new BBSG();
                case Prngs.CCG:
                    return new CCG();
                case Prngs.MODEXPG:
                    return new MODEXPG();
                case Prngs.QCG1:
                    return new QCG1();
                case Prngs.QCG2:
                    return new QCG2();
                default:
                    throw new CryptoAsymmetricSignException("GMSSEncrypt:GetPrng", "The Prng type is not supported!", new ArgumentException());
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
                    if (_msgDigestTrees != null)
                    {
                        _msgDigestTrees.Dispose();
                        _msgDigestTrees = null;
                    }
                    if (_gmssRandom != null)
                    {
                        _gmssRandom.Dispose();
                        _gmssRandom = null;
                    }
                    if (_index != null)
                    {
                        Array.Clear(_index, 0, _index.Length);
                        _index = null;
                    }
                    if (_currentAuthPaths != null)
                    {
                        Array.Clear(_currentAuthPaths, 0, _currentAuthPaths.Length);
                        _currentAuthPaths = null;
                    }
                    if (_subtreeRootSig != null)
                    {
                        Array.Clear(_subtreeRootSig, 0, _subtreeRootSig.Length);
                        _subtreeRootSig = null;
                    }
                    _mdLength = 0;
                    _numLayer = 0;
                }
                catch { }

                _isDisposed = true;
            }
        }
        #endregion
    }
}
