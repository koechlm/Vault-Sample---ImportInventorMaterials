﻿/*=====================================================================
  
  This file is part of the Autodesk Vault API Code Samples.

  Copyright (C) Autodesk Inc.  All rights reserved.

THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
PARTICULAR PURPOSE.
=====================================================================*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Autodesk.Connectivity.WebServices;
using Autodesk.Connectivity.WebServicesTools;
using Autodesk.DataManagement.Client.Framework.Vault.Currency.Connections;
using Autodesk.DataManagement.Client.Framework.Vault.Settings;
using Autodesk.DataManagement.Client.Framework.Vault.Results;
using VDF = Autodesk.DataManagement.Client.Framework;

namespace ADSK_ImportInvMaterialsSample
{
    public class Util
    {

        public static void DoAction(Action a)
        {
            try
            {
                a();
            }
            catch (Exception)
            {

            }
        }

        public static string GetAssemblyPath()
        {
            string prefix = "file:///";
            string codebase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            if (codebase.StartsWith(prefix))
                codebase = codebase.Substring(prefix.Length);

            return Path.GetDirectoryName(codebase);
        }

        /// <summary>
        /// Validate user's configuration admin rights.
        /// </summary>
        /// <returns>True if the user has access to Vault behavior configuration. Otherwise false.</returns>
        public static bool IsAdmin(Connection conn)
        {
            long userId = conn.WebServiceManager.SecurityService.Session.User.Id;
            if (userId > 0)
            {
                Permis[] permissions = conn.WebServiceManager.AdminService.GetPermissionsByUserId(userId);

                long mVaultSetOptions = 76; //Vault Set Options
                long mVaultGetOptions = 77; //Vault Get Options

                foreach (Permis p in permissions)
                {
                    if (p.Id == mVaultSetOptions || p.Id == mVaultGetOptions)
                        return true;
                }
            }
            return false;
        }



    }

    internal static class ExtensionMethods
    {
        internal static T[] ToSingleArray<T>(this T obj)
        {
            return new T[] { obj };
        }

        internal static bool IsNullOrEmpty<T>(this IEnumerable<T> collection)
        {
            return collection == null || collection.Count() == 0;
        }

        internal static List<T> ShallowCopy<T>(this List<T> origList)
        {
            List<T> newList = new List<T>();
            newList.AddRange(origList);
            return newList;
        }

        internal static VDF.Currency.FilePathAbsolute ToVDFPath(this string localPath)
        {
            return new VDF.Currency.FilePathAbsolute(localPath);
        }
    }
}
