﻿#region Directives
using System;
using VTDev.Libraries.CEXEngine.CryptoException;
using VTDev.Libraries.CEXEngine.Crypto.Enumeration;
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
    /// Contains sets of predefined Generalized Merkle Signature Scheme parameters.
    /// <para>Use the FromId(byte[]) or FromName(GMSSParamSets) to return a deep copy of a parameter set.</para>
    /// </summary>
    /// 
    /// <revisionHistory>
    /// <revision date="2015/06/07" version="1.0.1.0">Initial release</revision>
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
    /// <description><h4>Parameter Sets:</h4></description>
    /// <list type="table">
    /// <listheader>
    ///     <term>Strength</term>
    ///     <term>N</term>
    ///     <term>V1, V2, V3, V4</term>
    /// </listheader>
    /// 
    /// <item><description>Low</description></item>
    /// <item><description>33</description></item>
    /// <item><description>6, 12, 17, 22</description></item>
    /// 
    /// <item><description>Medium</description></item>
    /// <item><description>49</description></item>
    /// <item><description>19, 26, 32, 38</description></item>
    /// 
    /// <item><description>Medium</description></item>
    /// <item><description>54</description></item>
    /// <item><description>21, 27, 34, 44</description></item>
    /// 
    /// <item><description>High</description></item>
    /// <item><description>58</description></item>
    /// <item><description>24, 30, 37, 44</description></item>
    /// 
    /// <item><description>High</description></item>
    /// <item><description>60</description></item>
    /// <item><description>24, 30, 37, 45</description></item>
    /// 
    /// <item><description>High</description></item>
    /// <item><description>63</description></item>
    /// <item><description>26, 33, 40, 51</description></item>
    /// 
    /// <item><description>High</description></item>
    /// <item><description>66</description></item>
    /// <item><description>27, 35, 43, 54</description></item>
    /// </list>
    /// 
    /// <description><h4>Guiding Publications:</h4></description>
    /// <list type="number">
    /// <item><description>Selecting Parameters for the Generalized Merkle Signature Scheme Signature Scheme<cite>Generalized Merkle Signature Scheme Parameters</cite>.</description></item>
    /// </list>
    /// </remarks>
    public static class GMSSParamSets
    {
        #region Enums
        /// <summary>
        /// Set id is defined as: N: 2 <c>pow</c> base maximum signatures
        /// </summary>
        public enum GMSSParamNames : int
        {
            /// <summary>
            /// Creates 2^10 (1024) signatures using the parameter set: (P(2, (5, 5), (3, 3), (3, 3)))
            /// <para>H: 10, W: 3, K: 2, PublicKey Size: 36, PrivateKey Size: 1806</para>
            /// </summary>
            N2P10,
            /// <summary>
            /// Creates 2^20 (1048576) signatures using the parameter set: (P(2, (10, 10), (5, 4), (2, 2)))
            /// <para>H: 10,10, W: 5,4, K: 2,2, PublicKey Size: 36, PrivateKey Size: 6846</para>
            /// </summary>
            N2P20,
            /// <summary>
            /// Creates 2^40 (1099511627776) signatures using the parameter set: (P(2, (10, 10, 10, 10), (9, 9, 9, 3), (2, 2, 2, 2)))
            /// <para>H: 10, 10, 10, 10, W: 9, 9, 9, 3 K: 2, 2, 2, 2, PublicKey Size: 36, PrivateKey Size: 14534</para>
            /// </summary>
            N2P40
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Retrieve a parameter set by its identity code
        /// </summary>
        /// 
        /// <param name="OId">The 4 byte parameter set identity code</param>
        /// 
        /// <returns>A populated parameter set</returns>
        /// 
        /// <exception cref="CryptoAsymmetricException">Thrown if an invalid or unknown OId is specified</exception>
        public static GMSSParameters FromId(byte[] OId)
        {
            if (OId == null)
                throw new CryptoAsymmetricException("GMSSParamSets:FromId", "OId can not be null!", new ArgumentException());
            if (OId.Length != 4)
                throw new CryptoAsymmetricException("GMSSParamSets:FromId", "OId must be 4 bytes in length!", new ArgumentException());
            if (OId[0] != (byte)AsymmetricEngines.GMSS)
                throw new CryptoAsymmetricException("GMSSParamSets:FromId", "OId is not a valid GMSS parameter id!", new ArgumentException());

            if (OId[3] == 1)
                return (GMSSParameters)GMSSN2P10.DeepCopy();
            else if (OId[3] == 2)
                return (GMSSParameters)GMSSN2P20.DeepCopy();
            else if (OId[3] == 3)
                return (GMSSParameters)GMSSN2P40.DeepCopy();
            else
                throw new CryptoAsymmetricException("GMSSParamSets:FromId", "OId does not identify a valid param set!", new ArgumentException());
        }

        /// <summary>
        /// Retrieve a parameter set by its enumeration name
        /// </summary>
        /// 
        /// <param name="Name">The enumeration name</param>
        /// 
        /// <returns>A populated parameter set</returns>
        /// 
        /// <exception cref="CryptoAsymmetricException">Thrown if an invalid or unknown OId is specified</exception>
        public static GMSSParameters FromName(GMSSParamNames Name)
        {
            switch (Name)
            {
                case GMSSParamNames.N2P10:
                    return (GMSSParameters)GMSSN2P10.DeepCopy();
                case GMSSParamNames.N2P20:
                    return (GMSSParameters)GMSSN2P20.DeepCopy();
                case GMSSParamNames.N2P40:
                    return (GMSSParameters)GMSSN2P40.DeepCopy();
                default:
                    throw new CryptoAsymmetricException("GMSSParamSets:FromName", "The Parameter Name is not recognized!", new ArgumentException());
            }
        }

        /// <summary>
        /// Retrieve the parameter OId by its enumeration name
        /// </summary>
        /// 
        /// <param name="Name">The enumeration name</param>
        /// 
        /// <returns>The 4 byte OId field</returns>
        /// 
        /// <exception cref="CryptoAsymmetricException">Thrown if an invalid or unknown OId is specified</exception>
        public static byte[] GetID(GMSSParamNames Name)
        {
            switch (Name)
            {
                case GMSSParamNames.N2P10:
                    return new byte[] { 5, 1, 1, 1 };
                case GMSSParamNames.N2P20:
                    return new byte[] { 5, 1, 2, 1 };
                case GMSSParamNames.N2P40:
                    return new byte[] { 5, 1, 3, 1 };
                default:
                    throw new CryptoAsymmetricException("GMSSParamSets:GetID", "The Parameter Name is not recognized!", new ArgumentException());
            }
        }
        #endregion

        #region Parameter Sets
        /// <summary>
        /// Creates 2^10 (1024) signatures using the parameter set: (P(2, (5, 5), (3, 3), (3, 3)))
        /// <para>H: 10, W: 3, K: 2, PublicKey Size: 36, PrivateKey Size: 1806</para>
        /// </summary>
        public static GMSSParameters GMSSN2P10 = new GMSSParameters(new byte[] { 5, 1, 1, 1 }, 1, new int[] { 10 }, new int[] { 3 }, new int[] { 2 }, Digests.SHA256);
        /// <summary>
        /// Creates 2^20 (1048576) signatures using the parameter set: (P(2, (10, 10), (5, 4), (2, 2)))
        /// <para>H: 10,10, W: 5,4, K: 2,2, PublicKey Size: 36, PrivateKey Size: 6846</para>
        /// </summary>
        public static GMSSParameters GMSSN2P20 = new GMSSParameters(new byte[] { 5, 1, 2, 1 }, 2, new int[] { 10, 10 }, new int[] { 5, 4 }, new int[] { 2, 2 }, Digests.SHA256);
        /// <summary>
        /// Creates 2^40 (1099511627776) signatures using the parameter set: (P(2, (10, 10, 10, 10), (9, 9, 9, 3), (2, 2, 2, 2)))
        /// <para>H: 10, 10, 10, 10, W: 9, 9, 9, 3 K: 2, 2, 2, 2, PublicKey Size: 36, PrivateKey Size: 14534</para>
        /// </summary>
        public static GMSSParameters GMSSN2P40 = new GMSSParameters(new byte[] { 5, 1, 3, 1 }, 4, new int[] { 10, 10, 10, 10 }, new int[] { 9, 9, 9, 3 }, new int[] { 2, 2, 2, 2 }, Digests.SHA256);
        #endregion
    }
}
