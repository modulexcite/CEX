﻿#region Directives
using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
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
// Implementation Details:
// An implementation of a Secure File Deletion class.
// Written by John Underhill, August 1, 2014
// contact: develop@vtdev.com</para>
#endregion

namespace VTDev.Libraries.CEXEngine.Security
{
    /// <summary>
    /// <h3>Secure File Deletion class.</h3>
    /// </summary>
    /// 
    /// <remarks>
    /// <description><h4>Guiding Publications:</h4></description>
    /// <list type="number">
    /// <item><description>NIST SP800-88R1<cite>SP800-88R1</cite>: Table A-5 clear and purge on an ATA drive.</description></item>
    /// </list> 
    /// </remarks>
    public sealed class SecureDelete : IDisposable
    {
        #region Constants
        private const int BUFFER_SIZE = 4096;
        #endregion

        #region Fields
        private bool _isDisposed = false;
        private byte[] _rndBuffer = new byte[BUFFER_SIZE];
        private byte[] _revBuffer = new byte[BUFFER_SIZE];
        private byte[] _zerBuffer = new byte[BUFFER_SIZE];
        #endregion

        #region Properties
        /// <summary>
        /// The file size in bytes
        /// </summary>
        public long FileSize { get; private set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Initialize the class
        /// </summary>
        public SecureDelete()
        {
            // get random buffer
            using (System.Security.Cryptography.RNGCryptoServiceProvider rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
                rng.GetBytes(_rndBuffer);

            // create reverse random buffer
            Buffer.BlockCopy(_rndBuffer, 0, _revBuffer, 0, _revBuffer.Length);
            Array.Reverse(_revBuffer);
        }

        /// <summary>
        /// Finalize objects
        /// </summary>
        ~SecureDelete()
        {
            Dispose(false);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Delete a file
        /// </summary>
        /// 
        /// <param name="FilePath">Path to file</param>
        /// 
        /// <returns>Success</returns>
        public bool Delete(string FilePath)
        {
            if (!File.Exists(FilePath))
                return false;

            // permissions ToDo: always works?
            //if (!CanDelete(FilePath))
            //    return false;

            FileSize = GetFileSize(FilePath);

            if (FileSize < 1)
                return false;

            // three overwrite passes are more than enough..
            //OverWrite(FilePath, _zerBuffer);    // 0x0
            OverWrite(FilePath, _rndBuffer);    // random
            OverWrite(FilePath, _revBuffer);    // reverse random
            OverWrite(FilePath, _zerBuffer);    // 0x0

            // rename 30 times
            Rename(ref FilePath);

            // delete
            DeleteFile(FilePath);

            return !File.Exists(FilePath);
        }
        #endregion

        #region Private Methods
        private void DeleteFile(string FilePath)
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
        }

        private void OverWrite(string FilePath, byte[] Buffer)
        {
            long bytesWritten = 0;
            int bufferSize = BUFFER_SIZE;

            if (FileSize < BUFFER_SIZE)
                bufferSize = (int)FileSize;

            using (FileStream outputWriter = new FileStream(FilePath, FileMode.Open, FileAccess.Write, FileShare.Read, Buffer.Length, FileOptions.WriteThrough))
            {
                while (bytesWritten < FileSize)
                {
                    outputWriter.Write(Buffer, 0, bufferSize);
                    bytesWritten += bufferSize;

                    if (FileSize - bytesWritten < bufferSize)
                        bufferSize = (int)(FileSize - bytesWritten);
                }
            }
        }

        private void Rename(ref string FilePath)
        {
            string newPath = "";
            string dirPath = Path.GetDirectoryName(FilePath);

            for (int i = 0; i < 30; i++)
            {
                newPath = Path.Combine(dirPath, Path.GetRandomFileName());

                if (File.Exists(newPath))
                    File.Delete(newPath);

                File.Move(FilePath, newPath);
                FilePath = newPath;
            }
        }
        #endregion

        #region Helpers
        private bool CanDelete(string FilePath)
        {
            return (GetFilePermission(FilePath, FileSystemRights.Write) && GetFilePermission(FilePath, FileSystemRights.Delete));
        }

        private bool GetFilePermission(string FilePath, FileSystemRights AccessRight)
        {
            try
            {
                AuthorizationRuleCollection rules = File.GetAccessControl(FilePath).GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
                WindowsIdentity identity = WindowsIdentity.GetCurrent();

                foreach (FileSystemAccessRule rule in rules)
                {
                    if (identity.Groups.Contains(rule.IdentityReference))
                    {
                        if ((AccessRight & rule.FileSystemRights) == AccessRight)
                        {
                            if (rule.AccessControlType == AccessControlType.Allow)
                                return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        private long GetFileSize(string FilePath)
        {
            try
            {
                return new FileInfo(FilePath).Length;
            }
            catch { }
            return -1;
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
                    if (_revBuffer != null)
                    {
                        Array.Clear(_revBuffer, 0, _revBuffer.Length);
                        _revBuffer = null;
                    }
                    if (_rndBuffer != null)
                    {
                        Array.Clear(_rndBuffer, 0, _rndBuffer.Length);
                        _rndBuffer = null;
                    }
                    if (_zerBuffer != null)
                    {
                        Array.Clear(_zerBuffer, 0, _zerBuffer.Length);
                        _zerBuffer = null;
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
