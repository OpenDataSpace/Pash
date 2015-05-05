﻿// Copyright (C) Pash Contributors. License: GPL/BSD. See https://github.com/Pash-Project/Pash/
using System;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Host;
using System.Management.Automation;
using System.Collections.Generic;

namespace Pash.Implementation
{
    internal class LocalRunspace : Runspace
    {
        private InitialSessionState _initialSessionState;
        private RunspaceConfiguration _runspaceConfiguration;
        private List<Pipeline> _runningPipelines;

        internal PSHost PSHost { get; set; }

        internal CommandManager CommandManager { get; private set; }

        // TODO: make sure to implement a Singleton DefaultRunspace pattern
        //internal static LocalRunspace DefaultRunspace { get; private set; }

        public LocalRunspace(PSHost host, RunspaceConfiguration configuration)
            : this(host, configuration, null)
        {

        }

        public LocalRunspace(PSHost host, InitialSessionState initialSessionState)
            : this(host, null, initialSessionState)
        {

        }

        internal LocalRunspace(PSHost host, RunspaceConfiguration configuration, InitialSessionState initialSessionState)
        {
            //TODO: we should support both RunspaceConfigurations and IntialSessionStates properly
            _runningPipelines = new List<Pipeline>();
            DefaultRunspace = this;
            PSHost = host;
            if (configuration == null)
                _runspaceConfiguration = RunspaceFactory.DefaultRunspaceConfiguration;
            else
                _runspaceConfiguration = configuration;
            ExecutionContext = new ExecutionContext(host, configuration);
            ExecutionContext.CurrentRunspace = this;
            _initialSessionState = initialSessionState;
        }

        public override InitialSessionState InitialSessionState
        {
            get
            {
                return this._initialSessionState;
            }
        }

        public override RunspaceConfiguration RunspaceConfiguration
        {
            get
            {
                return _runspaceConfiguration;
            }
        }

        public override RunspaceStateInfo RunspaceStateInfo
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override Version Version
        {
            get { return new Version(1, 0, 0, 0); }
        }

        // unusued - NYI
        public override event EventHandler<RunspaceStateEventArgs> StateChanged = delegate { };

        internal override SessionStateProxy GetSessionStateProxy()
        {
            return new SessionStateProxy(this);
        }

        public object GetVariable(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new NullReferenceException("Variable name can't be empty.");

            return ExecutionContext.GetVariable(name);
        }

        public void SetVariable(string name, object value)
        {
            if (string.IsNullOrEmpty(name))
                throw new NullReferenceException("Variable name can't be empty.");

            ExecutionContext.SetVariable(name, value);
        }

        #region CreateXXX Pipeline

        public override Pipeline CreateNestedPipeline()
        {
            return CreateNestedPipeline("", false);
        }

        public override Pipeline CreateNestedPipeline(string command, bool addToHistory)
        {
            // TODO: make sure to fail if not Open
            return new LocalPipeline(this, command, true);
        }

        public override Pipeline CreatePipeline()
        {
            // TODO: make sure to fail if not Open
            return CreatePipeline("", false);
        }

        public override Pipeline CreatePipeline(string command)
        {
            // TODO: make sure to fail if not Open
            return CreatePipeline(command, false);
        }

        public override Pipeline CreatePipeline(string command, bool addToHistory)
        {
            // TODO: take care of the command history
            // TODO: make sure to fail if not Open
            return new LocalPipeline(this, command, false);
        }
        #endregion

        #region OpenXXX Runspace
        public override void Open()
        {
            if (PSHost is LocalHost)
            {
                // TODO: make sure we use something like IHostSupportsInterativeSession for this
                // However, for now we need that reference and it's okay as we have a simple
                // one-to-one relationship between host and runspace
                ((LocalHost)PSHost).OpenRunspace = this;
            }
            CommandManager = new CommandManager(this);
            InitializeDefaultSnapins();
            InitializeSession();
        }

        public override void OpenAsync()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region CloseXXX Runspace
        public override void Close()
        {
        }

        public override void CloseAsync()
        {
            throw new NotImplementedException();
        }
        #endregion

        // internals
        internal void AddRunningPipeline(Pipeline pipeline)
        {
            _runningPipelines.Add(pipeline);
        }

        internal void RemoveRunningPipeline(Pipeline pipeline)
        {
            _runningPipelines.Remove(pipeline);
        }

        internal override Pipeline GetCurrentlyRunningPipeline()
        {
            if (_runningPipelines.Count < 1)
            {
                return null;
            }
            return _runningPipelines[_runningPipelines.Count - 1];
        }

        internal void InitializeDefaultSnapins()
        {
            ExecutionContext.SessionState.SessionStateGlobal.LoadDefaultPSSnapIns();
            ExecutionContext.SessionState.SessionStateGlobal.SetCurrentDrive();
        }

        private void InitializeSession()
        {
            if (_initialSessionState == null)
                return;

            AddInitialSessionVariables();
            AddInitialSessionCommands();
            AddInitialSessionModules();
        }

        void AddInitialSessionModules()
        {
            var moduleLoader = new ModuleLoader(ExecutionContext);
            foreach (var mod in _initialSessionState.Modules)
            {
                // Is it correct that ModuleSpecification.Name can be a path? Well, we use it like this for now.
                moduleLoader.LoadModuleByName(mod.Name, true);
            }
        }

        private void AddInitialSessionCommands()
        {

            foreach (SessionStateCommandEntry cmdEntry in _initialSessionState.Commands)
            {
                if (cmdEntry is SessionStateAliasEntry)
                {
                    var aliasEntry = (SessionStateAliasEntry)cmdEntry;
                    var aliasInfo = new AliasInfo(aliasEntry.Name, aliasEntry.Definition, aliasEntry.Description,
                                                  CommandManager, aliasEntry.Options);
                    ExecutionContext.SessionState.Alias.Set(aliasInfo, "global");
                }
                else if (cmdEntry is SessionStateFunctionEntry)
                {
                    var funEntry = (SessionStateFunctionEntry)cmdEntry;
                    var scriptBlock = new ScriptBlock(Parser.ParseInput(funEntry.Definition));
                    var funInfo = new FunctionInfo(funEntry.Name, scriptBlock, null, funEntry.Options);
                    ExecutionContext.SessionState.Function.Set(funInfo);
                }
            }
        }

        private void AddInitialSessionVariables()
        {
            foreach (SessionStateVariableEntry variableEntry in _initialSessionState.Variables)
            {
                SetVariable(variableEntry.Name, variableEntry.Value);
            }
        }
    }
}
