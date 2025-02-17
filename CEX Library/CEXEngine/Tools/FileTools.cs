﻿#region Directives
using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Diagnostics;
#endregion

namespace VTDev.Libraries.CEXEngine.Tools
{
    /// <summary>
    /// File methods wrapper class
    /// </summary>
    public sealed class FileTools
    {
        #region Constructor
        private FileTools() { }
        #endregion

        #region File Tools
        /// <summary>
        /// Safely create a full path
        /// </summary>
        /// 
        /// <param name="DirectoryPath">Directory path</param>
        /// <param name="FileName">File name</param>
        /// 
        /// <returns>Full path to file</returns>
        public static string JoinPaths(string DirectoryPath, string FileName)
        {
            const string slash = @"\";

            if (string.IsNullOrEmpty(DirectoryPath)) return string.Empty;
            if (string.IsNullOrEmpty(FileName)) return string.Empty;

            if (!DirectoryPath.EndsWith(slash))
                DirectoryPath += slash;

            if (FileName.StartsWith(slash))
                FileName = FileName.Substring(1);

            return DirectoryPath + FileName;
        }

        /// <summary>
        /// Test a file for create file access permissions
        /// </summary>
        /// 
        /// <param name="FilePath">Full path to file</param>
        /// <param name="AccessRight">File System right tested</param>
        /// 
        /// <returns>State</returns>
        public static bool HasPermission(string FilePath, FileSystemRights AccessRight)
        {
            if (string.IsNullOrEmpty(FilePath)) return false;

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
                return false;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Get the size of  file
        /// </summary>
        /// 
        /// <param name="FilePath">Full path to file</param>
        /// 
        /// <returns>File length</returns>
        public static long GetSize(string FilePath)
        {
            try
            {
                return File.Exists(FilePath) ? new FileInfo(FilePath).Length : 0;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Adds an extension to a file unique to the directory
        /// </summary>
        /// 
        /// <param name="FullPath">Full file path</param>
        /// 
        /// <returns>Unique filename in original path</returns>
        public static string GetUniqueName(string FullPath)
        {
            if (!IsValidPath(FullPath)) return string.Empty;
            if (!DirectoryTools.Exists(DirectoryTools.GetPath(FullPath))) return string.Empty;

            string folderPath = DirectoryTools.GetPath(FullPath);
            string fileName = Path.GetFileNameWithoutExtension(FullPath);
            string fileExtension = Path.GetExtension(FullPath);

            string filePath = Path.Combine(folderPath, fileName + fileExtension);

            for (int i = 1; i < 10240; i++)
            {
                // test unique names
                if (File.Exists(filePath))
                    filePath = Path.Combine(folderPath, fileName + " " + i.ToString() + fileExtension);
                else
                    break;
            }

            return filePath;
        }

        /// <summary>
        /// File is readable
        /// </summary>
        /// 
        /// <param name="FilePath">Full path to file</param>
        /// 
        /// <returns>Success</returns>
        public static bool IsReadable(string FilePath)
        {
            try
            {
                if (!File.Exists(FilePath)) return false;
                using (FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read)) { }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Test a file to see if it is readonly
        /// </summary>
        /// 
        /// <param name="FilePath">Full path to file</param>
        /// 
        /// <returns>Read only</returns>
        public static bool IsReadOnly(string FilePath)
        {
            if (!IsValidPath(FilePath)) return false;
            if (!File.Exists(FilePath)) return false;

            FileAttributes fa = File.GetAttributes(FilePath);
            return (fa.ToString().IndexOf(FileAttributes.ReadOnly.ToString()) > -1);
        }

        /// <summary>
        /// Test if file name is valid [has extension]
        /// </summary>
        /// 
        /// <param name="FileName">File name</param>
        /// 
        /// <returns>Valid</returns>
        public static bool IsValidName(string FileName)
        {
            try
            {
                return (!string.IsNullOrEmpty(FileName) ? !string.IsNullOrEmpty(Path.GetExtension(FileName)) : false);
            }
            catch 
            { 
                return false;
            }
        }

        /// <summary>
        /// Test path to see if directory exists and file name has proper format
        /// </summary>
        /// 
        /// <param name="FilePath">Full path to file</param>
        /// 
        /// <returns>Valid</returns>
        public static bool IsValidPath(string FilePath)
        {
            if (DirectoryTools.Exists(DirectoryTools.GetPath(FilePath)))
                if (IsValidName(FilePath))
                    return true;

            return false;
        }

        /// <summary>
        /// Set the ownership of a file
        /// </summary>
        /// 
        /// <param name="FilePath">Full path to file</param>
        /// <param name="Account">The account taking ownership</param>
        public static void SetOwner(string FilePath, NTAccount Account)
        {
            FileInfo finfo = new FileInfo(FilePath);
            FileSecurity fsecurity = finfo.GetAccessControl();
            fsecurity.SetOwner(Account);
            finfo.SetAccessControl(fsecurity);
        }
        #endregion

        #region Utilities
        /// <summary>
        /// Format bytes into larger sizes
        /// </summary>
        /// 
        /// <param name="bytes">Length in bytes</param>
        /// 
        /// <returns>Size string</returns>
        public static string FormatBytes(long bytes)
        {
            if (bytes < 0) return string.Empty;

            const int scale = 1024;
            string[] orders = new string[] { "TB", "GB", "MB", "KB", "Bytes" };
            long max = (long)Math.Pow(scale, orders.Length - 1);

            foreach (string order in orders)
            {
                if (bytes > max)
                    return string.Format("{0:##.#} {1}", Decimal.Divide(bytes, max), order);

                max /= scale;
            }

            return string.Empty;
        }

        /// <summary>
        /// Get the local profile path
        /// </summary>
        /// 
        /// <returns>Profile path</returns>
        public static string GetLocalProfile()
        {
            return Environment.UserDomainName + "\\" + Environment.UserName;
        }
        #endregion
    }
}
