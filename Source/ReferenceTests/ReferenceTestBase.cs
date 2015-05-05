// Copyright (C) Pash Contributors. License: GPL/BSD. See https://github.com/Pash-Project/Pash/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text.RegularExpressions;
using TestPSSnapIn;
using NUnit.Framework;
using System.Text;
using System.Collections;

namespace ReferenceTests
{
    public class ReferenceTestBase
    {
        private List<string> _createdFiles;
        private List<string> _createdDirs;
        private Regex _whiteSpaceRegex;
        private bool _isMonoRuntime;
        private string _startDir;

        public string AssemblyDirectory { get; set; }

        private string _binaryTestModule;
        public string BinaryTestModule
        {
            get
            {
                if (_binaryTestModule == null)
                {
                    _binaryTestModule = new Uri(typeof(TestCommand).Assembly.CodeBase).LocalPath;
                }
                return _binaryTestModule;
            }
        }

        public string BinaryTestModuleName
        {
            get
            {
                return Path.GetFileNameWithoutExtension(BinaryTestModule);
            }
        }

        public ReferenceTestBase()
        {
            _createdFiles = new List<string>();
            _createdDirs = new List<string>();
            AssemblyDirectory = Path.GetDirectoryName(new Uri(GetType().Assembly.CodeBase).LocalPath);
            _whiteSpaceRegex = new Regex(@"\s");
            // prevents the project with Powershell to complain about the second part of the expression below
#pragma warning disable 0429 
            _isMonoRuntime = ReferenceTestInfo.IS_PASH && Type.GetType("Mono.Runtime") != null;
#pragma warning restore 0429
        }

        [SetUp]
        public virtual void SetUp()
        {
            _startDir = Environment.CurrentDirectory;
        }

        [TearDown]
        public virtual void TearDown()
        {
            RemoveCreatedFiles();
            if (!String.IsNullOrEmpty(_startDir) && !_startDir.Equals(Environment.CurrentDirectory))
            {
                Environment.CurrentDirectory = _startDir;
            }
        }

        private String QuoteWithSpace(string input)
        {
            if (_whiteSpaceRegex.IsMatch(input))
            {
                return String.Format(@"""{0}""", input);
            }
            return input;
        }

        private Process CreateDotNetProcess(string command, params string[] args)
        {
            string realCmd = null;
            List<string> realArgs = new List<string>();
            if (_isMonoRuntime)
            {
                realCmd = "mono";
                realArgs.Add(QuoteWithSpace(command));
            }
            else
            {
                realCmd = command;
            }

            foreach (var arg in args)
            {
                realArgs.Add(QuoteWithSpace(arg));
            }
            var process = new Process();
            process.StartInfo.FileName = realCmd;
            process.StartInfo.Arguments = String.Join(" ", realArgs);
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            return process;
        }

        public Process CreatePowershellOrPashProcess(params string[] args)
        {
            // NOTE: make sure when using functions with "args", that the arguments
            // only use *SINGLE* quotes itself

            return CreateDotNetProcess(ReferenceTestInfo.SHELL_EXECUTABLE, args);
        }

        public string FinishProcess(Process process, bool closeInput = true)
        {
            if (closeInput)
            {
                process.StandardInput.Flush();
                process.StandardInput.Close();
            }
            var output = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(3000))
            {
                process.Kill();
                throw new Exception(ReferenceTestInfo.SHELL_NAME + " didn't exit");
            }
            return output;
        }

        public void ImportTestCmdlets()
        {
            ReferenceHost.ImportModules(new string[] {  typeof(TestCommand).Assembly.Location });
        }

        public void CleanImports()
        {
            ReferenceHost.ImportModules(null);
        }

        public string CreateFile(string script, string extension)
        {
            var tempDir = Path.GetTempPath();
            var fileName = String.Format("TempFile{0}.{1}", _createdFiles.Count, extension.TrimStart('.'));
            var filePath = Path.Combine(tempDir, fileName);
            File.WriteAllText(filePath, script);
            _createdFiles.Add(filePath);

            return filePath;
        }

        public string[] ReadLinesFromFile(string filePath)
        {
            return File.ReadAllLines(filePath);
        }

        public void AddCleanupFile(string file)
        {
            _createdFiles.Add(file);
        }

        public void AddCleanupDir(string dir)
        {
            _createdDirs.Add(dir);
        }

        public void RemoveCreatedFiles()
        {
            foreach (var file in _createdFiles)
            {
                if (new FileInfo(file).Exists)
                {
                    File.Delete(file);
                }
            }
            _createdFiles.Clear();
            foreach (var dir in _createdDirs)
            {
                if (new DirectoryInfo(dir).Exists)
                {
                    Directory.Delete(dir, false);
                }
            }
            _createdDirs.Clear();
        }

        public static void ExecuteAndCompareType(string cmd, params Type[] expectedTypes)
        {
            var results = ReferenceHost.RawExecute(cmd);
            Assert.AreEqual(expectedTypes.Length, results.Count);
            for (int i = 0; i < expectedTypes.Length; i++)
            {
                Assert.AreSame(expectedTypes[i], results[i].BaseObject.GetType());
            }
        }

        public static void ExecuteAndCompareTypedResult(string cmd, params object[] expectedValues)
        {
            var results = ReferenceHost.RawExecute(cmd);
            if (expectedValues == null)
            {
                Assert.That(results.Count, Is.EqualTo(1));
                Assert.That(results[0], Is.Null);
                return;
            }
            Assert.AreEqual(expectedValues.Length, results.Count,
                            "Expected: " + SafeStringJoin(", ", expectedValues) + Environment.NewLine +
                            "Was:      " + SafeStringJoin(", ", results));
            for (int i = 0; i < expectedValues.Length; i++)
            {
                var expected = expectedValues[i];
                if (expected == null)
                {
                    Assert.IsNull(results[i]);
                    continue;
                }
                Assert.NotNull(results[i], i + "th result is null, but should be " + expectedValues[i].ToString());
                var res = results[i].BaseObject;
                var restype = res.GetType();
                Assert.AreSame(expected.GetType(), restype);
                if (restype == typeof(double))
                {
                    var dres = (double)res;
                    var diff = dres - ((double)expected);
                    var msg = String.Format("Not equal: {0} != {1}", expected, dres);
                    Assert.LessOrEqual(diff, Math.Abs(dres) * 0.00001, msg);
                }
                else
                {
                    Assert.AreEqual(expected, res);
                }
            }
        }


        public static string CmdletName(Type cmdletType)
        {
            var attribute = System.Attribute.GetCustomAttribute(cmdletType, typeof(CmdletAttribute))
                            as CmdletAttribute;
            return string.Format("{0}-{1}", attribute.VerbName, attribute.NounName);
        }

        public static string NewlineJoin(params string[] parts)
        {
            return String.Join(Environment.NewLine, parts) + Environment.NewLine;
        }


        public static string CreateObjectsCommand(Dictionary<string, object>[] data)
        {
            var sb = new StringBuilder("@(");
            foreach (var person in data)
            {
                sb.Append("(new-object psobject -property @{");
                foreach (var pair in person)
                {
                    sb.Append(pair.Key);
                    sb.Append("=");
                    if (pair.Value == null)
                    {
                        sb.Append("$null;");
                    }
                    else
                    {
                        sb.Append("\"");
                        var val = pair.Value.ToString().Replace("\n", "`n").Replace("\r", "`r");
                        val = val.Replace("\"", "`\"").Replace("\t", "`t");
                        sb.Append(val);
                        sb.Append("\";");
                    }
                }
                sb.Remove(sb.Length - 1, 1);
                sb.Append("}),");
            }
            sb.Remove(sb.Length - 1, 1);
            sb.Append(")");
            return sb.ToString();
        }

        static string SafeStringJoin(string sep, IEnumerable<object> objs)
        {
            var strs = from o in objs select o == null ? "" : o.ToString();
            return String.Join(sep, strs);
        }
    }
}

