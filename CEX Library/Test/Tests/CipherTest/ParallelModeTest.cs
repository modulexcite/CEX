﻿#region Directives
using System;
using System.Security.Cryptography;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Symmetric.Block;
using VTDev.Libraries.CEXEngine.Crypto.Cipher.Symmetric.Block.Mode;
using VTDev.Libraries.CEXEngine.Crypto.Common;
using VTDev.Libraries.CEXEngine.Tools;
#endregion

namespace VTDev.Projects.CEX.Test.Tests.CipherTest
{
    /// <remarks>
    /// Compares the output of modes processed in parallel with their linear counterparts
    /// </remarks>
    public class ParallelModeTest : ITest
    {
        #region Constants
        private const string DESCRIPTION = "Compares output from parallel and linear modes for equality.";
        private const string FAILURE = "FAILURE! ";
        private const string SUCCESS = "SUCCESS! Parallel tests have executed succesfully.";
        #endregion

        #region Events
        public event EventHandler<TestEventArgs> Progress;
        protected virtual void OnProgress(TestEventArgs e)
        {
            var handler = Progress;
            if (handler != null) handler(this, e);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get: Test Description
        /// </summary>
        public string Description { get { return DESCRIPTION; } }
        #endregion

        #region Public Methods
        /// <summary>
        /// Compares CTR linear with parallel output for equivalence
        /// </summary>
        /// 
        /// <returns>State</returns>
        public string Test()
        {
            try
            {
                ParallelTest();

                return SUCCESS;
            }
            catch (Exception Ex)
            {
                string message = Ex.Message == null ? "" : Ex.Message;
                throw new Exception(FAILURE + message);
            }
        }
        #endregion

        #region Private
        private void ParallelTest()
        {
            byte[] data;
            byte[] dec1;
            byte[] dec2;
            byte[] enc1;
            byte[] enc2;
            int blockSize;
            KeyParams keyParam = new KeyParams(GetBytes(32), GetBytes(16));

            // CTR mode
            using (CTR cipher = new CTR(new RDX()))
            {
                data = GetBytes(1036);

                // how to calculate an ideal block size 
                int plen = (data.Length / cipher.ParallelMinimumSize) * cipher.ParallelMinimumSize;
                // you can factor it up or down or use a default
                if (plen > cipher.ParallelMaximumSize)
                    plen = 1024;

                // set parallel block size
                cipher.ParallelBlockSize = plen;

                // encrypt //
                // parallel
                cipher.Initialize(true, keyParam);
                cipher.IsParallel = true;
                blockSize = cipher.ParallelBlockSize;
                enc1 = Transform1(cipher, data, blockSize);

                // normal mode
                cipher.Initialize(true, keyParam);
                cipher.IsParallel = false;
                blockSize = cipher.BlockSize;
                enc2 = Transform1(cipher, data, blockSize);

                // decrypt
                cipher.Initialize(false, keyParam);
                cipher.IsParallel = true;
                blockSize = cipher.ParallelBlockSize;
                dec1 = Transform1(cipher, enc1, blockSize);

                cipher.Initialize(false, keyParam);
                cipher.IsParallel = false;
                blockSize = cipher.BlockSize;
                dec2 = Transform1(cipher, enc1, blockSize);
            }

            if (Compare.AreEqual(enc1, enc2) == false)
                throw new Exception("Parallel CTR: Encrypted output is not equal!");
            if (Compare.AreEqual(dec1, dec2) == false)
                throw new Exception("Parallel CTR: Decrypted output is not equal!");

            OnProgress(new TestEventArgs("Passed Parallel CTR encryption and decryption tests.."));

            // CBC mode
            using (CBC cipher = new CBC(new RDX()))
            {
                // must be divisible by block size, add padding if required
                data = GetBytes(2048);

                // encrypt
                cipher.ParallelBlockSize = 1024;

                // encrypt only in normal mode for cbc
                cipher.Initialize(true, keyParam);
                blockSize = cipher.BlockSize;
                enc1 = Transform1(cipher, data, blockSize);

                // decrypt
                cipher.Initialize(false, keyParam);
                cipher.IsParallel = true;
                blockSize = cipher.ParallelBlockSize;
                dec1 = Transform1(cipher, enc1, blockSize);

                cipher.Initialize(false, keyParam);
                cipher.IsParallel = false;
                blockSize = cipher.BlockSize;
                dec2 = Transform1(cipher, enc1, blockSize);
            }

            if (Compare.AreEqual(dec1, dec2) == false)
                throw new Exception("Parallel CBC: Decrypted output is not equal!");

            OnProgress(new TestEventArgs("Passed Parallel CBC decryption tests.."));

            // CFB mode
            using (CFB cipher = new CFB(new RDX()))
            {
                // must be divisible by block size, add padding if required
                data = GetBytes(2048);

                // encrypt
                cipher.ParallelBlockSize = 1024;

                // encrypt only in normal mode for cfb
                cipher.Initialize(true, keyParam);
                blockSize = cipher.BlockSize;
                enc1 = Transform1(cipher, data, blockSize);

                // decrypt
                cipher.Initialize(false, keyParam);
                cipher.IsParallel = true;
                blockSize = cipher.ParallelBlockSize;
                dec1 = Transform1(cipher, enc1, blockSize);

                cipher.Initialize(false, keyParam);
                cipher.IsParallel = false;
                blockSize = cipher.BlockSize;
                dec2 = Transform1(cipher, enc1, blockSize);
            }

            if (Compare.AreEqual(dec1, dec2) == false)
                throw new Exception("Parallel CFB: Decrypted output is not equal!");
            if (Compare.AreEqual(dec1, data) == false)
                throw new Exception("Parallel CFB: Decrypted output is not equal!");

            OnProgress(new TestEventArgs("Passed Parallel CFB decryption tests.."));

            // dispose container
            keyParam.Dispose();
        }

        private byte[] GetBytes(int Size)
        {
            byte[] data = new byte[Size];

            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
                rng.GetBytes(data);

            return data;
        }

        private byte[] Transform1(ICipherMode Cipher, byte[] Data, int BlockSize)
        {
            // best way, use the offsets
            int blocks = Data.Length / BlockSize;
            byte[] outData = new byte[Data.Length];

            for (int i = 0; i < blocks; i++)
                Cipher.Transform(Data, i * BlockSize, outData, i * BlockSize);

            // last partial in CTR
            if (blocks * BlockSize < Data.Length)
                Cipher.Transform(Data, blocks * BlockSize, outData, blocks * BlockSize);

            return outData;
        }

        private byte[] Transform2(ICipherMode Cipher, byte[] Data, int BlockSize)
        {
            // slower, mem copy can be expensive on large data..
            int blocks = Data.Length / BlockSize;
            byte[] outData = new byte[Data.Length];
            byte[] inBlock = new byte[BlockSize];
            byte[] outBlock = new byte[BlockSize];

            if (Cipher.Name == "CTR")
            {
                Cipher.Transform(Data, outData);
            }
            else
            {
                for (int i = 0; i < blocks; i++)
                {
                    Buffer.BlockCopy(Data, i * BlockSize, inBlock, 0, BlockSize);
                    Cipher.Transform(inBlock, outBlock);
                    Buffer.BlockCopy(outBlock, 0, outData, i * BlockSize, BlockSize);
                }

                if (blocks * BlockSize < Data.Length)
                    Cipher.Transform(Data, blocks * BlockSize, outData, blocks * BlockSize);
            }

            return outData;
        }
        #endregion
    }
}
