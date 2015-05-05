﻿// Copyright (C) Pash Contributors. License: GPL/BSD. See https://github.com/Pash-Project/Pash/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management;
using System.Management.Automation;
using System.Management.Automation.Provider;
using Microsoft.PowerShell.Commands;

namespace Microsoft.PowerShell.Commands
{
    [CmdletProvider("Variable", ProviderCapabilities.ShouldProcess)]
    public sealed class VariableProvider : SessionStateProviderBase
    {
        public const string ProviderName = "Variable";

        protected override Collection<PSDriveInfo> InitializeDefaultDrives()
        {
            PSDriveInfo item = new PSDriveInfo("Variable", base.ProviderInfo, string.Empty, string.Empty, null);
            Collection<PSDriveInfo> collection = new Collection<PSDriveInfo>();
            collection.Add(item);
            return collection;
        }

        internal override object GetSessionStateItem(string name)
        {
            Path path = PathIntrinsics.RemoveDriveName(name);
            path = path.TrimStartSlash();
            return SessionState.PSVariable.Get(path);
        }

        internal override bool CanRenameItem(object item)
        {
            PSVariable variable = item as PSVariable;

            if (variable == null)
                return false;

            // TODO: the rename can be Force'ed
            if (((variable.Options & ScopedItemOptions.Constant) != ScopedItemOptions.None) ||
                ((variable.Options & ScopedItemOptions.ReadOnly) != ScopedItemOptions.None))
                return false;

            return true;
        }

        internal override IDictionary GetSessionStateTable()
        {
            return (IDictionary)SessionState.PSVariable.GetAll();
        }

        internal override void SetSessionStateItem(string name, object value, bool writeItem)
        {
            Path path = PathIntrinsics.RemoveDriveName(name);
            name = path.TrimStartSlash();

            PSVariable variable = null;
            if (value != null)
            {
                variable = value as PSVariable;
                if (variable == null)
                {
                    variable = new PSVariable(name, value);
                }
                else if (String.Compare(name, variable.Name, true, System.Globalization.CultureInfo.CurrentCulture) != 0)
                {
                    PSVariable var = new PSVariable(name, variable.Value, variable.Options, variable.Attributes);
                    var.Description = variable.Description;
                    variable = var;
                }
            }
            else
            {
                variable = new PSVariable(name, null);
            }
            // TODO: can be Force'ed
            SessionState.PSVariable.Set(variable);
            PSVariable item = SessionState.PSVariable.Get(variable.Name);
            if (writeItem && (item != null))
            {
                WriteItemObject(item, item.Name, false);
            }
        }

        internal override void RemoveSessionStateItem(string name)
        {
            // TODO: can be Force'ed
            SessionState.PSVariable.Remove(name);
        }

        protected override void GetItem(string name)
        {
            name = PathIntrinsics.RemoveDriveName(new Path(name).TrimEndSlash());
            GetChildItems(name, false);
        }

        internal override object GetValueOfItem(object item)
        {
            var variable = item as PSVariable;
            if (variable != null)
            {
                return variable.Value;
            }
            return base.GetValueOfItem(item);
        }
    }
}
