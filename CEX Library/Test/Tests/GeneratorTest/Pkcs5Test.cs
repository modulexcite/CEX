﻿#region Directives
using System;
using System.Text;
using VTDev.Libraries.CEXEngine.Crypto.Digest;
using VTDev.Libraries.CEXEngine.Crypto.Generator;
using VTDev.Libraries.CEXEngine.Crypto.Mac;
using VTDev.Libraries.CEXEngine.Tools;
#endregion

namespace VTDev.Projects.CEX.Test.Tests.GeneratorTest
{
    /// <summary>
    /// Tests PKCS5 V2 with SHA-2 
    /// </summary>
    public class Pkcs5Test : ITest
    {
        #region Constants
        private const string DESCRIPTION = "PKCS5 VER2 SHA-2 test vectors.";
        private const string FAILURE = "FAILURE! ";
        private const string SUCCESS = "SUCCESS! All PKCS5 tests have executed succesfully.";
        #endregion

        #region Events
        public event EventHandler<TestEventArgs> Progress;
        protected virtual void OnProgress(TestEventArgs e)
        {
            var handler = Progress;
            if (handler != null) handler(this, e);
        }
        #endregion

        #region Vectors
        private static readonly byte[][] _salt = 
        {
            Encoding.ASCII.GetBytes("salt"),
            Encoding.ASCII.GetBytes("saltSALTsaltSALTsaltSALTsaltSALTsalt"),
        };
        private static readonly byte[][] _ikm = 
        {
            Encoding.ASCII.GetBytes("password"),
            Encoding.ASCII.GetBytes("passwordPASSWORDpassword")
        };
        private static readonly byte[][] _output = 
        {
            HexConverter.Decode("120fb6cffcf8b32c43e7225256c4f837a86548c92ccc35480805987cb70be17b"),
            HexConverter.Decode("ae4d0c95af6b46d32d0adff928f06dd02a303f8ef3c251dfd6e2d85a95474c43"),
            HexConverter.Decode("c5e478d59288c841aa530db6845c4c8d962893a001ce4e11a4963873aa98134a"),
            HexConverter.Decode("348c89dbcbd32b2f32d814b8116e84cf2b17347ebc1800181c4e2a1fb8dd53e1c635518c7dac47e9")
        };
        #endregion

        #region Properties
        /// <summary>
        /// Get: Test Description
        /// </summary>
        public string Description { get { return DESCRIPTION; } }
        #endregion

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        /// 
        /// <returns>Status</returns>
        public string Test()
        {
            try
            {
                PKCSTest(32, 1, _salt[0], _ikm[0], _output[0]);
                PKCSTest(32, 2, _salt[0], _ikm[0], _output[1]);
                PKCSTest(32, 4096, _salt[0], _ikm[0], _output[2]);
                PKCSTest(40, 4096, _salt[1], _ikm[1], _output[3]);
                OnProgress(new TestEventArgs("Passed PKCS5 vector tests.."));

                return SUCCESS;
            }
            catch (Exception Ex)
            {
                string message = Ex.Message == null ? "" : Ex.Message;
                throw new Exception(FAILURE + message);
            }
        }
        #endregion

        #region Private Methods
        private void PKCSTest(int Size, int Iterations, byte[] Salt, byte[] Key, byte[] Output)
        {
            byte[] outBytes = new byte[Size];

            using (PKCS5 gen = new PKCS5(new SHA256(), Iterations))
            {
                gen.Initialize(Salt, Key);
                gen.Generate(outBytes, 0, Size);
            }

            if (Compare.AreEqual(outBytes, Output) == false)
                throw new Exception("PKCS5: Values are not equal! Expected: " + HexConverter.ToString(Output) + " Received: " + HexConverter.ToString(outBytes));

            using (PKCS5 gen = new PKCS5(new SHA256HMAC(), Iterations))
            {
                gen.Initialize(Salt, Key);
                gen.Generate(outBytes, 0, Size);
            }

            if (Compare.AreEqual(outBytes, Output) == false)
                throw new Exception("PKCS5: Values are not equal! Expected: " + HexConverter.ToString(Output) + " Received: " + HexConverter.ToString(outBytes));
        }
        #endregion
    }
}
