﻿// Copyright (C) Pash Contributors. License: GPL/BSD. See https://github.com/Pash-Project/Pash/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation.Host;
using NUnit.Framework;
using System.IO;
using Pash.Implementation;
using System.Security;

namespace TestHost
{
    class TestHostUserInterface : LocalHostUserInterface
    {
        private TestHostRawUserInterface _rawUI;
        public TextReader InputStream;
        public Action<string> OnWriteErrorLineString = delegate(string s) { Assert.Fail(s); };

        internal TestHostUserInterface() : base()
        {
            _rawUI = new TestHostRawUserInterface();
        }

        internal TestHostUserInterface(int width, int height) : this()
        {
            _rawUI.BufferSize = new Size(width, height);
        }

        internal void SetInput(string input)
        {
            InputStream = new StringReader(input);
            InteractiveIO = true;
        }

        //  need to override Readxxx and Writexxx methods. The other methods use them

        public override PSHostRawUserInterface RawUI
        {
            get { return _rawUI; }
        }

        internal override string ReadLine(bool addToHistory, string intialValue = "")
        {
            var val = ReadLine();
            return val == null ? null : intialValue + val;
        }

        public override string ReadLine()
        {
            if (InputStream == null)
            {
                return null;
            }
            WriteLine(); // newline a user usually does at the end
            return InputStream.ReadLine();
        }

        public override SecureString ReadLineAsSecureString()
        {
            var val = ReadLine();
            if (val == null)
            {
                return null;
            }
            var secStr = new SecureString();
            foreach (var c in val)
            {
                secStr.AppendChar(c);
            }
            return secStr;
        }

        public override void Write(string value)
        {
            Log.Append(value);
        }

        public StringBuilder Log = new StringBuilder();

        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            Log.Append(value);
        }

        public override void WriteDebugLine(string message)
        {
            throw new NotImplementedException();
        }

        public override void WriteErrorLine(string value)
        {
            this.OnWriteErrorLineString(value);
        }

        public override void WriteLine()
        {
            this.Log.AppendLine();
        }

        public override void WriteLine(string value)
        {
            this.Log.AppendLine(value);
        }

        public override void WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            this.Log.AppendLine(value);
        }

        public override void WriteProgress(long sourceId, System.Management.Automation.ProgressRecord record)
        {
            throw new NotImplementedException();
        }

        public override void WriteVerboseLine(string message)
        {
            this.Log.AppendLine(message);
        }

        public override void WriteWarningLine(string message)
        {
            this.Log.AppendLine(message);
        }

        public string GetOutput()
        {
            return Log.ToString();
        }
    }
}
