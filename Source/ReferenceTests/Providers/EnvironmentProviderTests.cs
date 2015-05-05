﻿// Copyright (C) Pash Contributors. License GPL/BSD. See https://github.com/Pash-Project/Pash/
using System;
using System.IO;
using NUnit.Framework;

namespace ReferenceTests.Providers
{
    [TestFixture]
    class EnvironmentProviderTests : ReferenceTestBase
    {
        const string PashTestEnvironmentVariableName = "PashEnvironmentProviderTest";
        const string PashTestEnvironmentVariableName2 = "PashEnvironmentProviderTest2";

        [TearDown]
        public override void TearDown()
        {
            Environment.SetEnvironmentVariable(PashTestEnvironmentVariableName, null);
            Environment.SetEnvironmentVariable(PashTestEnvironmentVariableName2, null);
        }

        [Test]
        public void EnvironmentVariableValueCanBeRetrieved()
        {
            Environment.SetEnvironmentVariable(PashTestEnvironmentVariableName, "TestValue");
            string command = string.Format("$env:{0}", PashTestEnvironmentVariableName);
            string result = ReferenceHost.Execute(command);

            Assert.AreEqual("TestValue" + Environment.NewLine, result);
        }

        [Test]
        public void EnvironmentVariableValueCanBeSet()
        {
            string command = string.Format("$env:{0} = 'AnotherValue'", PashTestEnvironmentVariableName);
            ReferenceHost.Execute(command);
            string result = Environment.GetEnvironmentVariable(PashTestEnvironmentVariableName);

            Assert.AreEqual("AnotherValue", result);
        }

        [Test]
        public void GetChildItemsOfEnvironmentDriveReturnsAllEnvironmentVariables()
        {
            Environment.SetEnvironmentVariable(PashTestEnvironmentVariableName, "TestValue");
            Environment.SetEnvironmentVariable(PashTestEnvironmentVariableName2, "TestValue2");
            string result = ReferenceHost.Execute("get-childitem env: | foreach-object { $_.value }");

            StringAssert.Contains("TestValue" + Environment.NewLine, result);
            StringAssert.Contains("TestValue2" + Environment.NewLine, result);
        }

        [Test]
        public void GetChildItemsOfEnvironmentDriveFollowedBySlashReturnsAllEnvironmentVariables()
        {
            Environment.SetEnvironmentVariable(PashTestEnvironmentVariableName, "TestValue");
            Environment.SetEnvironmentVariable(PashTestEnvironmentVariableName2, "TestValue2");
            string command = string.Format("get-childitem env:{0} | foreach-object {{ $_.value }}", Path.DirectorySeparatorChar);
            string result = ReferenceHost.Execute(command);

            StringAssert.Contains("TestValue" + Environment.NewLine, result);
            StringAssert.Contains("TestValue2" + Environment.NewLine, result);
        }

        [Test]
        public void GetSingleEnviromentVariableFromEnvironmentDrive()
        {
            Environment.SetEnvironmentVariable(PashTestEnvironmentVariableName, "TestValue");
            string command = string.Format("get-childitem env:{0} | foreach-object {{ $_.value }}", PashTestEnvironmentVariableName);
            string result = ReferenceHost.Execute(command);

            StringAssert.Contains("TestValue" + Environment.NewLine, result);
        }

        [Test]
        public void GetSingleEnviromentVariableFromEnvironmentDriveWithSlashAfterDrive()
        {
            Environment.SetEnvironmentVariable(PashTestEnvironmentVariableName, "TestValue");
            string command = string.Format("get-childitem env:{0}{1} | foreach-object {{ $_.value }}", Path.DirectorySeparatorChar, PashTestEnvironmentVariableName);
            string result = ReferenceHost.Execute(command);

            StringAssert.Contains("TestValue" + Environment.NewLine, result);
        }

        [Test]
        public void GetSingleEnviromentVariableAfterOpeningEnvironmentDrive()
        {
            Environment.SetEnvironmentVariable(PashTestEnvironmentVariableName, "TestValue");
            string command = string.Format("get-childitem {0} | foreach-object {{ $_.value }}", PashTestEnvironmentVariableName);
            string result = ReferenceHost.Execute(new string[] {
                "Set-Location env:",
                command});

            StringAssert.Contains("TestValue" + Environment.NewLine, result);
        }

        [Test]
        public void GetContentForEnviromentVariable()
        {
            Environment.SetEnvironmentVariable(PashTestEnvironmentVariableName, "TestValue");
            string command = string.Format("Get-Content env:{0}{1}", Path.DirectorySeparatorChar, PashTestEnvironmentVariableName);
            string result = ReferenceHost.Execute(command);

            StringAssert.Contains("TestValue" + Environment.NewLine, result);
        }

        [Test]
        public void SetEnvironmentVariableToIntegerArray()
        {
            string result = ReferenceHost.Execute(new string[] {
                "$env:SetEnvironmentVariableToIntegerArrayTest = 1,2",
                "$env:SetEnvironmentVariableToIntegerArrayTest"
            });

            Assert.AreEqual("1 2" + Environment.NewLine, result);
        }

        [Test]
        public void SetContentForEnvironmentVariable()
        {
            string result = ReferenceHost.Execute(new string[] {
                "$env:EnvironmentProviderTestVariable = 'abc'",
                "Set-Content env:EnvironmentProviderTestVariable 'test'",
                "$env:EnvironmentProviderTestVariable"
            });

            Assert.AreEqual("test" + Environment.NewLine, result);
        }

        [Test]
        public void SetContentForEnvironmentVariableUsingTwoItems()
        {
            string result = ReferenceHost.Execute(new string[] {
                "$env:EnvironmentProviderTestVariable = 'abc'",
                "Set-Content env:EnvironmentProviderTestVariable 'test1','test2'",
                "$env:EnvironmentProviderTestVariable"
            });

            Assert.AreEqual("test1 test2" + Environment.NewLine, result);
        }
    }
}
