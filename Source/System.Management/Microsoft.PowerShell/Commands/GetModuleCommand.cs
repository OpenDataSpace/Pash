// Copyright (C) Pash Contributors. License: GPL/BSD. See https://github.com/Pash-Project/Pash/
using System;
using System.Linq;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    [CmdletAttribute("Get", "Module", DefaultParameterSetName="Loaded"
                     /*, HelpUri="http://go.microsoft.com/fwlink/?LinkID=141552" */ )]
    [OutputType(typeof(PSModuleInfo))]
    public sealed class GetModuleCommand : ModuleCmdletBase
    {
        /*
        [ParameterAttribute(ParameterSetName="Available")]
        [ParameterAttribute(ParameterSetName="Loaded")]
        public SwitchParameter All { get; set; }
        */

        /*
        [ParameterAttribute(ParameterSetName="Loaded", ValueFromPipelineByPropertyName=true)]
        [ParameterAttribute(ParameterSetName="PsSession", ValueFromPipelineByPropertyName=true)]
        [ParameterAttribute(ParameterSetName="Available", ValueFromPipelineByPropertyName=true)]
        [ParameterAttribute(ParameterSetName="CimSession", ValueFromPipelineByPropertyName=true)]
        public ModuleSpecification[] FullyQualifiedName { get; set; }
        */

        /*
        [ParameterAttribute(ParameterSetName="PsSession")]
        [ParameterAttribute(ParameterSetName="CimSession")]
        [ParameterAttribute(ParameterSetName="Available", Mandatory=true)]
        public SwitchParameter ListAvailable { get; set; }
        */

        /*
        [ParameterAttribute(ParameterSetName="CimSession", ValueFromPipeline=true, Position=0)]
        [ParameterAttribute(ParameterSetName="Available", ValueFromPipeline=true, Position=0)]
        [ParameterAttribute(ParameterSetName="PsSession", ValueFromPipeline=true, Position=0)]
        */
        [ParameterAttribute(ParameterSetName="Loaded", ValueFromPipeline=true, Position=0)]
        public string[] Name { get; set; }

        /*
        [ParameterAttribute(ParameterSetName="PsSession", Mandatory=true)]
        [ValidateNotNullAttribute]
        public PSSession PSSession { get; set; }
        */

        /*
        [ParameterAttribute(ParameterSetName="PsSession")]
        [ParameterAttribute(ParameterSetName="CimSession")]
        [ParameterAttribute(ParameterSetName="Available")]
        public SwitchParameter Refresh { get; set; }
        */

        protected override void ProcessRecord()
        {
            var modules = SessionState.LoadedModules.GetAll();
            var rawPatterns = (Name == null || Name.Length == 0) ? new []{ "*" } : Name;
            var patterns = (from p in rawPatterns select new WildcardPattern(p, WildcardOptions.IgnoreCase)).ToArray();
            foreach (var mod in modules.Values)
            {
                if (WildcardPattern.IsAnyMatch(patterns, mod.Name))
                {
                    WriteObject(mod);
                }
            }
        }

    }
}

