﻿#region Directives
using System;
using System.IO;
using VTDev.Libraries.CEXEngine.Crypto.Mac;
using VTDev.Libraries.CEXEngine.Crypto.Prng;
using VTDev.Libraries.CEXEngine.Crypto.Processing;
using VTDev.Libraries.CEXEngine.Tools;
#endregion

namespace VTDev.Projects.CEX.Test.Tests.MacTest
{
    /// <summary>
    /// Compares the normal mode of StreamMac with the Concurrent mode for equality
    /// </summary>
    public class StreamMacTest : ITest
    {
        #region Constants
        private const string DESCRIPTION = "Compares the normal mode of StreamMac with the Concurrent mode for equality.";
        private const string FAILURE = "FAILURE! ";
        private const string SUCCESS = "SUCCESS! All StreamMac tests have executed succesfully.";
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
        /// Tests for correctness of parallel processing mode in the StreamMac implementation
        /// by comparing digest output between both modes performed on random temp files
        /// </summary>
        /// 
        /// <returns>Status</returns>
        public string Test()
        {
            try
            {
                MacTests();

                return SUCCESS;
            }
            catch (Exception Ex)
            {
                string message = Ex.Message == null ? "" : Ex.Message;
                throw new Exception(FAILURE + message);
            }
        }
        #endregion

        #region Tests
        private string CreateTempFile(int Size)
        {
            string path = Path.GetTempFileName();
            byte[] data = new CSPRng().GetBytes(Size);

            File.WriteAllBytes(path, data);

            return path;
        }

        private void MacTests()
        {
            string path = CreateTempFile(117674);
            byte[] Ikm = new CSPRng().GetBytes(32);
            byte[] x1 = MacTest1(path, Ikm);
            byte[] y1 = MacTest2(path, Ikm);

            if (File.Exists(path))
                File.Delete(path);

            if ((Compare.AreEqual(x1, y1) == false))
                throw new Exception("Failed! Mac outputs are not equal");

            OnProgress(new TestEventArgs("Passed round 1 comparisons.."));

            path = CreateTempFile(69041);
            x1 = MacTest1(path, Ikm);
            y1 = MacTest2(path, Ikm);

            if (File.Exists(path))
                File.Delete(path);

            if ((Compare.AreEqual(x1, y1) == false))
                throw new Exception("Failed! Mac outputs are not equal");

            OnProgress(new TestEventArgs("Passed round 2 comparisons.."));

            path = CreateTempFile(65536);
            x1 = MacTest1(path, Ikm);
            y1 = MacTest2(path, Ikm);

            if (File.Exists(path))
                File.Delete(path);

            if ((Compare.AreEqual(x1, y1) == false))
                throw new Exception("Failed! Mac outputs are not equal");

            OnProgress(new TestEventArgs("Passed round 3 comparisons.."));

            if (!MacTest3(Ikm))
                throw new Exception("Failed! Mac outputs are not equal");

            OnProgress(new TestEventArgs("Passed round 4 comparisons.."));
        }

        private byte[] MacTest1(string FileName, byte[] IKm)
        {
            using (FileStream inStream = new FileStream(FileName, FileMode.Open))
            {
                using (StreamMac mac = new StreamMac(new SHA512HMAC(IKm)))
                {
                    mac.Initialize(inStream);
                    mac.IsConcurrent = true;
                    return mac.ComputeMac();
                }
            }
        }

        private byte[] MacTest2(string FileName, byte[] IKm)
        {
            using (FileStream inStream = new FileStream(FileName, FileMode.Open))
            {
                using (StreamMac mac = new StreamMac(new SHA512HMAC(IKm)))
                {
                    mac.Initialize(inStream);
                    mac.IsConcurrent = false;
                    return mac.ComputeMac();
                }
            }
        }

        private bool MacTest3(byte[] IKm)
        {
            byte[] data = new CSPRng().GetBytes(33033);
            byte[] hash1;
            byte[] hash2;

            using (StreamMac mac1 = new StreamMac(new SHA512HMAC(IKm)))
            {
                mac1.Initialize(new MemoryStream(data));
                mac1.IsConcurrent = false;
                hash1 =  mac1.ComputeMac();
            }

            using (SHA512HMAC mac2 = new SHA512HMAC(IKm))
                hash2 = mac2.ComputeMac(data);

            return Compare.AreEqual(hash1, hash2);
        }
        #endregion
    }
}
