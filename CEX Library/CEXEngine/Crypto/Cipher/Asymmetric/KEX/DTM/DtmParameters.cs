﻿#region Directives
using System;
using System.IO;
using System.Runtime.InteropServices;
using VTDev.Libraries.CEXEngine.Crypto.Common;
using VTDev.Libraries.CEXEngine.Crypto.Enumeration;
using VTDev.Libraries.CEXEngine.CryptoException;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.KEX.DTM.Structures;
#endregion

#region License Information
// The GPL Version 3 License
// 
// Copyright (C) 2015 John Underhill
// This file is part of the CEX Cryptographic library.

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 
// Written by John Underhill, August 21, 2015
// contact: develop@vtdev.com
#endregion

namespace VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.KEX.DTM
{
    #region DtmParameters
    /// <summary>
    /// The DtmParameters class.
    /// <para>The DtmParameters class is used to define the working parameters used by the DTM Key Exchange using a DtmKex instance.</para>
    /// <para>The bytes <c>0</c> through <c>3</c> are the Auth-Phase asymmetric parameters OId.
    /// The next 4 bytes are the Primary-Phase asymmetric parameters OId.
    /// Bytes <c>8</c> and <c>9</c> identify the Auth-Phase DtmSession symmetric cipher parameters.
    /// Bytes <c>10</c> and <c>11</c> identify the Primary-Phase DtmSession symmetric cipher parameters.
    /// The last <c>4</c> bytes are used to uniquely identify the parameter set.</para>
    /// </summary>
    /// 
    /// <seealso cref="VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.KEX.DTM.Structures.DtmClient">VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.KEX.DTM.Structures DtmClient structure</seealso>
    /// <seealso cref="VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.KEX.DTM.Structures.DtmIdentity">VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.KEX.DTM.Structures DtmIdentity structure</seealso>
    /// <seealso cref="VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.KEX.DTM.Structures.DtmPacket">VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.KEX.DTM.Structures DtmPacket structure</seealso>
    /// <seealso cref="VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.KEX.DTM.Structures.DtmSession">VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.KEX.DTM.Structures DtmSession structure</seealso>
    /// <seealso cref="VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.KEX.DTM.DtmKex">VTDev.Libraries.CEXEngine.Crypto.Cipher.Asymmetric.KEX.DTM DtmKex class</seealso>
    /// 
    /// <revisionHistory>
    /// <revision date="2015/05/23" version="1.4.0.0">Initial release</revision>
    /// </revisionHistory>
    public sealed class DtmParameters : IDisposable, ICloneable
    {
        #region Private Fields
        private bool _isDisposed = false;
        #endregion

        #region Public Fields <c></c>
        /// <summary>
        /// The DtmParameters Identifier field; should be 16 bytes describing the parameter set (see class notes)
        /// </summary>
        public byte[] OId;
        /// <summary>
        /// The <c>Auth-Phase</c> Asymmetric parameters OId; can be the Asymmetric cipher parameters OId, or a serialized Asymmetric Parameters class
        /// </summary>
        public byte[] AuthPkeId;
        /// <summary>
        /// The <c>Primary-Phase</c> Asymmetric parameters OId; can be the Asymmetric cipher parameters OId, or a serialized Asymmetric Parameters class
        /// </summary>
        public byte[] PrimaryPkeId;
        /// <summary>
        /// The <c>Auth-Phase</c> Symmetric sessions cipher parameters; contains a complete description of the Symmetric cipher
        /// </summary>
        public DtmSession AuthSession;
        /// <summary>
        /// The <c>Primary-Phase</c> Symmetric sessions cipher parameters; contains a complete description of the Symmetric cipher
        /// </summary>
        public DtmSession PrimarySession;
        /// <summary>
        /// The Prng type used to pad messages
        /// </summary>
        public Prngs RandomEngine;
        /// <summary>
        /// (Optional) The maximum number of pseudo-random bytes to append to the <c>Primary-Phase</c> Asymmetric Public key before encryption
        /// </summary>
        public int MaxAsmKeyAppend;
        /// <summary>
        /// (Optional) The maximum number of pseudo-random bytes to prepend to the <c>Primary-Phase</c> Asymmetric Public key before encryption
        /// </summary>
        public int MaxAsmKeyPrePend;
        /// <summary>
        /// (Optional) The maximum number of pseudo-random bytes to append to the <c>Primary-Phase</c> Client Identity before encryption
        /// </summary>
        public int MaxAsmParamsAppend;
        /// <summary>
        /// (Optional) The maximum number of pseudo-random bytes to prepend to the <c>Primary-Phase</c> Asymmetric Client Identity before encryption
        /// </summary>
        public int MaxAsmParamsPrePend;
        /// <summary>
        /// (Optional) The maximum number of pseudo-random bytes to append to the <c>Primary-Phase</c> Symmetric key before encryption
        /// </summary>
        public int MaxSymKeyAppend;
        /// <summary>
        /// (Optional) The maximum number of pseudo-random bytes to prepend to the <c>Primary-Phase</c> Symmetric key before encryption
        /// </summary>
        public int MaxSymKeyPrePend;
        /// <summary>
        /// (Optional) The maximum number of pseudo-random bytes to append to a <c>Post-Exchange</c> message before encryption
        /// </summary>
        public int MaxMessageAppend;
        /// <summary>
        /// (Optional) The maximum number of pseudo-random bytes to prepend to a <c>Post-Exchange</c> message before encryption
        /// </summary>
        public int MaxMessagePrePend;
        /// <summary>
        /// (Optional) The maximum delay time before sending the <c>Primary-Phase</c> Asymmetric key; the minimum time is 1 half max, a value of <c>0</c> has no delay 
        /// </summary>
        public int MaxAsmKeyDelayMS;
        /// <summary>
        /// (Optional) The maximum delay time before sending the <c>Primary-Phase</c> Symmetric key; the minimum time is 1 half max, a value of <c>0</c> has no delay
        /// </summary>
        public int MaxSymKeyDelayMS;
        /// <summary>
        /// (Optional) The maximum delay time before sending <c>Post-Exchange</c> message traffic; the minimum time is <c>0</c>, a value of <c>0</c> has no delay
        /// </summary>
        public int MaxMessageDelayMS;
        #endregion

        #region Constructor
        /// <summary>
        /// The DtmParameters primary constructor
        /// </summary>
        /// 
        /// <param name="OId">The DtmParameters Identifier field; must be 16 bytes in length</param>
        /// <param name="AuthPkeId">The <c>Auth-Phase</c> Asymmetric parameters OId; can be the Asymmetric cipher parameters OId, or a serialized Asymmetric Parameters class</param>
        /// <param name="PrimaryPkeId">The <c>Primary-Phase</c> Asymmetric parameters OId; can be the Asymmetric cipher parameters OId, or a serialized Asymmetric Parameters class</param>
        /// <param name="AuthSession">The <c>Auth-Phase</c> Symmetric sessions cipher parameters; contains a complete description of the Symmetric cipher</param>
        /// <param name="PrimarySession">The <c>Primary-Phase</c> Symmetric sessions cipher parameters; contains a complete description of the Symmetric cipher</param>
        /// <param name="RandomEngine">(Optional) The Prng used to pad messages, defaults to CTRPrng</param>
        /// <param name="MaxAsmKeyAppend">(Optional) The maximum number of pseudo-random bytes to append to the <c>Primary-Phase</c> Asymmetric Public key before encryption</param>
        /// <param name="MaxAsmKeyPrePend">(Optional) The maximum number of pseudo-random bytes to prepend to the <c>Primary-Phase</c> Asymmetric Public key before encryption</param>
        /// <param name="MaxAsmParamsAppend">(Optional) The maximum number of pseudo-random bytes to append to the <c>Primary-Phase</c> Client Identity before encryption</param>
        /// <param name="MaxAsmParamsPrePend">(Optional) The maximum number of pseudo-random bytes to prepend to the <c>Primary-Phase</c> Asymmetric Client Identity before encryption</param>
        /// <param name="MaxSymKeyAppend">(Optional) The maximum number of pseudo-random bytes to append to the <c>Primary-Phase</c> Symmetric key before encryption</param>
        /// <param name="MaxSymKeyPrePend">(Optional) The maximum number of pseudo-random bytes to prepend to the <c>Primary-Phase</c> Symmetric key before encryption</param>
        /// <param name="MaxMessageAppend">(Optional) The maximum number of pseudo-random bytes to append to a <c>Post-Exchange</c> message before encryption</param>
        /// <param name="MaxMessagePrePend">(Optional) The maximum number of pseudo-random bytes to prepend to a <c>Post-Exchange</c> message before encryption</param>
        /// <param name="MaxAsmKeyDelayMS">(Optional) The maximum delay time before sending the <c>Primary-Phase</c> Asymmetric key; the minimum time is 1 half max, a value of <c>0</c> has no delay</param>
        /// <param name="MaxSymKeyDelayMS">(Optional) The maximum delay time before sending the <c>Primary-Phase</c> Symmetric key; the minimum time is 1 half max, a value of <c>0</c> has no delay</param>
        /// <param name="MaxMessageDelayMS">(Optional) The maximum delay time before sending message traffic; the minimum time is <c>0</c>, a value of <c>0</c> has no delay</param>
        public DtmParameters(byte[] OId, byte[] AuthPkeId, byte[] PrimaryPkeId, DtmSession AuthSession, DtmSession PrimarySession, Prngs RandomEngine = Prngs.CTRPrng, int MaxAsmKeyAppend = 0, int MaxAsmKeyPrePend = 0, int MaxAsmParamsAppend = 0, 
            int MaxAsmParamsPrePend = 0, int MaxSymKeyAppend = 0, int MaxSymKeyPrePend = 0, int MaxMessageAppend = 0, int MaxMessagePrePend = 0, int MaxAsmKeyDelayMS = 0, int MaxSymKeyDelayMS = 0, int MaxMessageDelayMS = 0)
        {
            this.OId = OId;
            this.AuthPkeId = AuthPkeId;
            this.PrimaryPkeId = PrimaryPkeId;
            this.AuthSession = AuthSession;
            this.PrimarySession = PrimarySession;
            this.RandomEngine = RandomEngine;
            this.MaxAsmKeyAppend = MaxAsmKeyAppend;
            this.MaxAsmKeyPrePend = MaxAsmKeyPrePend;
            this.MaxAsmParamsAppend = MaxAsmParamsAppend;
            this.MaxAsmParamsPrePend = MaxAsmParamsPrePend;
            this.MaxSymKeyAppend = MaxSymKeyAppend;
            this.MaxSymKeyPrePend = MaxSymKeyPrePend;
            this.MaxMessageAppend = MaxMessageAppend;
            this.MaxMessagePrePend = MaxMessagePrePend;
            this.MaxAsmKeyDelayMS = MaxAsmKeyDelayMS;
            this.MaxSymKeyDelayMS = MaxSymKeyDelayMS;
            this.MaxMessageDelayMS = MaxMessageDelayMS;
        }

        /// <summary>
        /// Constructs a DtmParameters from a byte array
        /// </summary>
        /// 
        /// <param name="ParametersArray">The byte array containing the DtmParameters structure</param>
        public DtmParameters(byte[] ParametersArray) :
            this(new MemoryStream(ParametersArray))
        {
        }

        /// <summary>
        /// Constructs a DtmIdentity from a stream
        /// </summary>
        /// 
        /// <param name="ParametersStream">Stream containing a serialized DtmParameters</param>
        /// 
        /// <returns>A populated DtmParameters</returns>
        public DtmParameters(Stream ParametersStream)
        {
            BinaryReader reader = new BinaryReader(ParametersStream);
            int len;
            byte[] data;

            len = reader.ReadInt32();
            OId = reader.ReadBytes(len);
            len = reader.ReadInt32();
            AuthPkeId = reader.ReadBytes(len);
            len = reader.ReadInt32();
            PrimaryPkeId = reader.ReadBytes(len);
            len = reader.ReadInt32();
            data = reader.ReadBytes(len);
            AuthSession = new DtmSession(data);
            len = reader.ReadInt32();
            data = reader.ReadBytes(len);
            PrimarySession = new DtmSession(data);
            RandomEngine = (Prngs)reader.ReadByte();
            MaxAsmKeyAppend = reader.ReadInt32();
            MaxAsmKeyPrePend = reader.ReadInt32();
            MaxAsmParamsAppend = reader.ReadInt32();
            MaxAsmParamsPrePend = reader.ReadInt32();
            MaxSymKeyAppend = reader.ReadInt32();
            MaxSymKeyPrePend = reader.ReadInt32();
            MaxMessageAppend = reader.ReadInt32();
            MaxMessagePrePend = reader.ReadInt32();
            MaxAsmKeyDelayMS = reader.ReadInt32();
            MaxSymKeyDelayMS = reader.ReadInt32();
            MaxMessageDelayMS = reader.ReadInt32();
        }

        private DtmParameters()
        {
        }

        /// <summary>
        /// Finalize objects
        /// </summary>
        ~DtmParameters()
        {
            Dispose(false);
        }
        #endregion

        #region Static Methods
        /// <summary>
        /// Deserialize an DtmParameters
        /// </summary>
        /// 
        /// <param name="ParametersStream">Stream containing a serialized DtmParameters</param>
        /// 
        /// <returns>A populated DtmParameters</returns>
        public static DtmParameters DeSerialize(Stream ParametersStream)
        {
            return new DtmParameters(ParametersStream);
        }

        /// <summary>
        /// Serialize an DtmParameters structure
        /// </summary>
        /// 
        /// <param name="Paramaters">A DtmParameters structure</param>
        /// 
        /// <returns>A stream containing the DtmParameters data</returns>
        public static Stream Serialize(DtmParameters Paramaters)
        {
            return Paramaters.ToStream();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Get the class Size in bytes
        /// </summary>
        /// 
        /// <returns>Serialized class size</returns>
        public int GetHeaderSize()
        {
            return (int)Serialize(this).Length;
        }

        /// <summary>
        /// Reset all struct members
        /// </summary>
        public void Reset()
        {
            Array.Clear(OId, 0, OId.Length);
            Array.Clear(AuthPkeId, 0, AuthPkeId.Length);
            Array.Clear(PrimaryPkeId, 0, PrimaryPkeId.Length);
            AuthSession.Reset();
            PrimarySession.Reset();
            RandomEngine = Prngs.CTRPrng;
            MaxAsmKeyAppend = 0;
            MaxAsmKeyPrePend = 0;
            MaxAsmParamsAppend = 0;
            MaxAsmParamsPrePend = 0;
            MaxSymKeyAppend = 0;
            MaxSymKeyPrePend = 0;
            MaxMessageAppend = 0;
            MaxMessagePrePend = 0;
            MaxAsmKeyDelayMS = 0;
            MaxSymKeyDelayMS = 0;
            MaxMessageDelayMS = 0;
        }
        /// <summary>
        /// Returns the DtmParameters as an encoded byte array
        /// </summary>
        /// 
        /// <returns>The serialized DtmParameters</returns>
        public byte[] ToBytes()
        {
            return ToStream().ToArray();
        }

        /// <summary>
        /// Returns the DtmParameters as an encoded MemoryStream
        /// </summary>
        /// 
        /// <returns>The serialized DtmParameters</returns>
        public MemoryStream ToStream()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            byte[] data;

            writer.Write(OId.Length);
            writer.Write(OId);
            writer.Write(AuthPkeId.Length);
            writer.Write(AuthPkeId);
            writer.Write(PrimaryPkeId.Length);
            writer.Write(PrimaryPkeId);
            data = AuthSession.ToBytes();
            writer.Write(data.Length);
            writer.Write(data);
            data = PrimarySession.ToBytes();
            writer.Write(data.Length);
            writer.Write(data);
            writer.Write((byte)RandomEngine);
            writer.Write(MaxAsmKeyAppend);
            writer.Write(MaxAsmKeyPrePend);
            writer.Write(MaxSymKeyAppend);
            writer.Write(MaxAsmParamsAppend);
            writer.Write(MaxAsmParamsPrePend);
            writer.Write(MaxSymKeyPrePend);
            writer.Write(MaxMessageAppend);
            writer.Write(MaxMessagePrePend);
            writer.Write(MaxAsmKeyDelayMS);
            writer.Write(MaxSymKeyDelayMS);
            writer.Write(MaxMessageDelayMS);
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }
        #endregion

        #region Overrides
        /// <summary>
        /// Get the hash code for this object
        /// </summary>
        /// 
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            int result = 1;

            for (int i = 0; i < OId.Length; i++)
                result += 31 * OId[i];
            for (int i = 0; i < AuthPkeId.Length; i++)
                result += 31 * AuthPkeId[i];
            for (int i = 0; i < PrimaryPkeId.Length; i++)
                result += 31 * PrimaryPkeId[i];

            result += 31 * PrimarySession.EngineType;
            result += 31 * PrimarySession.IvSize;
            result += 31 * PrimarySession.KdfEngine;
            result += 31 * PrimarySession.KeySize;
            result += 31 * PrimarySession.RoundCount;
            result += 31 * AuthSession.EngineType;
            result += 31 * AuthSession.IvSize;
            result += 31 * AuthSession.KdfEngine;
            result += 31 * AuthSession.KeySize;
            result += 31 * AuthSession.RoundCount;
            result += 31 * (int)RandomEngine;
            result += 31 * MaxAsmKeyAppend;
            result += 31 * MaxAsmKeyPrePend;
            result += 31 * MaxAsmParamsAppend;
            result += 31 * MaxAsmParamsPrePend;
            result += 31 * MaxSymKeyAppend;
            result += 31 * MaxSymKeyPrePend;
            result += 31 * MaxMessageAppend;
            result += 31 * MaxMessagePrePend;
            result += 31 * MaxAsmKeyDelayMS;
            result += 31 * MaxSymKeyDelayMS;
            result += 31 * MaxMessageDelayMS;

            return result;
        }

        /// <summary>
        /// Compare this object instance with another
        /// </summary>
        /// 
        /// <param name="Obj">Object to compare</param>
        /// 
        /// <returns>True if equal, otherwise false</returns>
        public override bool Equals(Object Obj)
        {
            if (this == Obj)
                return true;
            if (Obj == null && this != null)
                return false;

            DtmParameters other = (DtmParameters)Obj;
            if (GetHashCode() != other.GetHashCode())
                return false;

            return true;
        }
        #endregion

        #region IClone
        /// <summary>
        /// Create a shallow copy of this DtmParameters instance
        /// </summary>
        /// 
        /// <returns>The DtmParameters copy</returns>
        public object Clone()
        {
            return new DtmParameters(OId, AuthPkeId, PrimaryPkeId, AuthSession, PrimarySession, RandomEngine, MaxAsmKeyAppend, MaxAsmKeyPrePend, MaxAsmParamsAppend, MaxAsmParamsPrePend, 
                MaxSymKeyAppend, MaxSymKeyPrePend, MaxMessageAppend, MaxMessagePrePend, MaxAsmKeyDelayMS, MaxSymKeyDelayMS, MaxMessageDelayMS);
        }

        /// <summary>
        /// Create a deep copy of this DtmParameters instance
        /// </summary>
        /// 
        /// <returns>The DtmParameters copy</returns>
        public object DeepCopy()
        {
            return new DtmParameters(ToStream());
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
                    Reset();
                }
                finally
                {
                    _isDisposed = true;
                }
            }
        }
        #endregion
    }
    #endregion
}
