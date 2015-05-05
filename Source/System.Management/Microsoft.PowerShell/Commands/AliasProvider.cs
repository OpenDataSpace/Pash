﻿// Copyright (C) Pash Contributors. License: GPL/BSD. See https://github.com/Pash-Project/Pash/
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using Microsoft.PowerShell.Commands;
using System.Management.Automation.Provider;
using System.Management.Automation;
using System.Management;

namespace Microsoft.PowerShell.Commands
{
    [CmdletProvider("Alias", ProviderCapabilities.ShouldProcess)]
    public sealed class AliasProvider : SessionStateProviderBase
    {
        public const string ProviderName = "Alias";

        protected override Collection<PSDriveInfo> InitializeDefaultDrives()
        {
            PSDriveInfo item = new PSDriveInfo("Alias", base.ProviderInfo, string.Empty, string.Empty, null);
            Collection<PSDriveInfo> collection = new Collection<PSDriveInfo>();
            collection.Add(item);
            return collection;
        }

        protected override object NewItemDynamicParameters(string path, string type, object newItemValue)
        {
            return new AliasProviderDynamicParameters();
        }

        protected override object SetItemDynamicParameters(string path, object value)
        {
            return new AliasProviderDynamicParameters();
        }

        internal override bool CanRenameItem(object item)
        {
            throw new NotImplementedException();
        }

        internal override object GetSessionStateItem(string name)
        {
            Path path = PathIntrinsics.RemoveDriveName(name);
            path = path.TrimStartSlash();
            return SessionState.Alias.Get(path);
        }

        internal override System.Collections.IDictionary GetSessionStateTable()
        {
            return SessionState.Alias.GetAll();
        }

        internal override object GetValueOfItem(object item)
        {
            var aliasInfo = item as AliasInfo;
            if (aliasInfo != null)
            {
                return aliasInfo.Definition;
            }
            return base.GetValueOfItem(item);
        }

        internal override void RemoveSessionStateItem(string name)
        {
            throw new NotImplementedException();
        }

        internal override void SetSessionStateItem(string name, object value, bool writeItem)
        {
            Path path = PathIntrinsics.RemoveDriveName(name);
            path = path.TrimStartSlash();

            if (value is Array)
            {
                var array = (Array)value;
                if (array.Length > 1)
                {
                    throw new PSArgumentException("value");
                }
                value = array.GetValue(0);
            }
            
            var aliasInfo = new AliasInfo(path, value.ToString(), null);
            SessionState.Alias.Set(aliasInfo, "global");

            var a = SessionState.Alias.Get(path);
            Console.WriteLine(a.Definition);
        }

        protected override void GetItem(string path)
        {
            path = PathIntrinsics.RemoveDriveName(new Path(path).TrimEndSlash());
            GetChildItems(path, false);
        }
    }
}
