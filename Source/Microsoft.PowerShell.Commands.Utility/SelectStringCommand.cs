﻿// Copyright (C) Pash Contributors. License: GPL/BSD. See https://github.com/Pash-Project/Pash/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Management.Pash.Implementation;
using System.Text.RegularExpressions;
using Pash.Implementation;

namespace Microsoft.PowerShell.Commands
{
    [CmdletAttribute(VerbsCommon.Select, "String", DefaultParameterSetName = "File" /*HelpUri="http://go.microsoft.com/fwlink/?LinkID=113388"*/)] 
    [OutputType(typeof(MatchInfo), typeof(bool))]
    public sealed class SelectStringCommand : PSCmdlet
    {
        [Parameter]
        public SwitchParameter AllMatches { get; set; }

        [Parameter]
        public SwitchParameter CaseSensitive { get; set; }

        [Parameter]
        [ValidateCount(1, 2)]
        [ValidateNotNullOrEmpty]
        [ValidateRange(0, 2147483647)]
        public int[] Context { get; set; }

        [Parameter]
        [ValidateSet(
            "unicode",
            "utf7",
            "utf8",
            "utf32",
            "ascii",
            "bigendianunicode",
            "default",
            "oem")]
        [ValidateNotNullOrEmpty]
        public string Encoding { get; set; }

        [Parameter]
        [ValidateNotNullOrEmpty]
        public string[] Exclude { get; set; }

        [ParameterAttribute]
        [ValidateNotNullOrEmpty]
        public string[] Include { get; set; }

        [Parameter(
            ValueFromPipeline = true,
            Mandatory = true,
            ParameterSetName = "Object")]
        [AllowNull]
        [AllowEmptyString]
        public PSObject InputObject { get; set; }

        [Parameter]
        public SwitchParameter List { get; set; }

        [ParameterAttribute(
            Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "LiteralFile")]
        [Alias("PSPath")]
        public string[] LiteralPath {
            get { return InternalPaths; }
            set
            {
                AvoidWildcardExpansion = true;
                InternalPaths = value;
            }
        }

        bool AvoidWildcardExpansion;
        string[] InternalPaths { get; set; }

        [Parameter]
        public SwitchParameter NotMatch { get; set; }

        [Parameter(
            Position = 1,
            Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "File")]
        public string[] Path {
            get { return InternalPaths; }
            set { InternalPaths = value; }
        }

        [Parameter(
            Mandatory = true,
            Position = 0)]
        public string[] Pattern { get; set; }

        [Parameter]
        public SwitchParameter Quiet { get; set; }

        [Parameter]
        public SwitchParameter SimpleMatch { get; set; }

        int _lineNumber = 1;
        bool _matchedAtLeastOneItem;

        protected override void ProcessRecord()
        {
            if (Quiet.IsPresent && _matchedAtLeastOneItem)
            {
                return;
            }

            if (Path != null)
            {
                MatchInFiles();
            }
            else if (InputObject != null)
            {
                MatchInputObject();
            }
        }

        private void MatchInFiles()
        {
            foreach (string path in ResolvePaths())
            {
                MatchInLines(path, ReadLines(path));
            }
        }

        private IEnumerable<string> ReadLines(string path)
        {
            return File.ReadAllLines(path, EncodingMapping.GetEncoding(Encoding));
        }

        private IEnumerable<string> ResolvePaths()
        {
            foreach (string path in InternalPaths)
            {
                CmdletProvider provider;
                ProviderRuntime runtime = CreateProviderRuntime();
                foreach (string resolvedPath in InvokeProvider.ChildItem.Globber.GetGlobbedProviderPaths(path, runtime, out provider))
                {
                    yield return resolvedPath;
                }
            }
        }

        private ProviderRuntime CreateProviderRuntime()
        {
            var runtime = new ProviderRuntime(this);
            runtime.Include = Include == null ? new Collection<string>() : new Collection<string>(Include.ToList());
            runtime.Exclude = Exclude == null ? new Collection<string>() : new Collection<string>(Exclude.ToList());
            runtime.AvoidGlobbing = AvoidWildcardExpansion;
            return runtime;
        }

        /// <summary>
        /// When passing an array of strings using InputObject the array is turned into a single string
        /// with a space between each item of the array.
        /// 
        /// When an array of strings is passed down the pipeline each item of the array is searched
        /// individually.
        /// 
        /// https://technet.microsoft.com/en-us/library/hh849903.aspx
        /// </summary>
        private void MatchInputObject()
        {
            MatchInLines("InputStream", GetLines(InputObject));
        }

        private IEnumerable<string> GetLines(PSObject psObject)
        {
            var array = psObject.BaseObject as Array;
            if (array != null)
            {
                yield return String.Join(" ", array.OfType<object>().Select(item => item.ToString()));
            }
            else
            {
                yield return psObject.BaseObject.ToString();
            }
        }

        private void MatchInLines(string path, IEnumerable<string> lines)
        {
            foreach (string line in lines)
            {
                foreach (string pattern in Pattern)
                {
                    MatchInfo matchInfo = FindMatch(line, pattern, path);
                    if (matchInfo != null && !NotMatch.IsPresent)
                    {
                        WriteMatch(matchInfo);
                        if (ShouldStopProcessingAfterMatchFound())
                        {
                            return;
                        }
                        break;
                    }
                    else if (matchInfo == null && NotMatch.IsPresent)
                    {
                        WriteNotMatch(line, pattern, path);
                        if (ShouldStopProcessingAfterMatchFound())
                        {
                            return;
                        }
                        break;
                    }
                }
                _lineNumber++;
            }
        }

        MatchInfo FindMatch(string line, string pattern, string path)
        {
            if (SimpleMatch)
            {
                return FindSimpleMatch(line, pattern, path);
            }
            return FindRegexMatch(line, pattern, path);
        }

        private MatchInfo FindRegexMatch(string line, string pattern, string path)
        {
            var matches = new List<Match>();
            if (AllMatches.IsPresent)
            {
                matches = Regex.Matches(line, pattern, GetRegexOptions()).OfType<Match>().ToList();
            }
            else
            {
                Match match = Regex.Match(line, pattern, GetRegexOptions());
                if (match.Success)
                {
                    matches.Add(match);
                }
            }

            if (matches.Count > 0)
            {
                return new MatchInfo(path, pattern, matches, line, _lineNumber, !CaseSensitive);
            }
            return null;
        }

        private MatchInfo FindSimpleMatch(string line, string pattern, string path)
        {
            if (line.IndexOf(pattern, GetStringComparison()) >= 0)
            {
                return new MatchInfo(path, pattern, line, _lineNumber, !CaseSensitive);
            }
            return null;
        }

        private RegexOptions GetRegexOptions()
        {
            if (CaseSensitive)
            {
                return RegexOptions.None;
            }
            return RegexOptions.IgnoreCase;
        }

        private StringComparison GetStringComparison()
        {
            if (CaseSensitive)
            {
                return StringComparison.CurrentCulture;
            }
            return StringComparison.CurrentCultureIgnoreCase;
        }

        private bool ShouldStopProcessingAfterMatchFound()
        {
            return Quiet.IsPresent || List.IsPresent;
        }

        private void WriteMatch(MatchInfo match)
        {
            _matchedAtLeastOneItem = true;

            if (Quiet.IsPresent)
            {
                WriteObject(true);
            }
            else
            {
                WriteObject(match);
            }
        }

        private void WriteNotMatch(string line, string pattern, string path)
        {
            var match = new MatchInfo(path, pattern, line, _lineNumber, !CaseSensitive);
            WriteMatch(match);
        }

        /// <summary>
        /// Using -Quiet has different behaviour for files compared with items passed on the pipeline
        /// or via InputObject. Using files will result in $false being output if there are no matches.
        /// Using the pipeline or InputObject will result in nothing being output if there are no matches.
        /// 
        /// https://connect.microsoft.com/PowerShell/feedback/details/684218/select-strings-quiet-never-returns-false
        /// </summary>
        protected override void EndProcessing()
        {
            if (InternalPaths != null && Quiet.IsPresent && !_matchedAtLeastOneItem)
            {
                WriteObject(false);
            }
        }
    }
}
