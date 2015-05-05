﻿// Copyright (C) Pash Contributors. License: GPL/BSD. See https://github.com/Pash-Project/Pash/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace ReferenceTests.Providers
{
    [TestFixture]
    class AliasProviderTests : ReferenceTestBase
    {
        [Test]
        public void ListAllAliases()
        {
            string result = ReferenceHost.Execute(new string[] {
                "Set-Alias testalias Get-ChildItem",
                "Get-ChildItem alias: | ? { $_.Name -eq 'testalias' } | % { $_.Name }"
            });

            Assert.AreEqual("testalias" + Environment.NewLine, result);
        }

        [Test]
        public void ListAllAliasesUsingBackslashInDriveName()
        {
            string result = ReferenceHost.Execute(new string[] {
                "Set-Alias testalias Get-ChildItem",
                String.Format(@"Get-ChildItem alias:{0} | ? {{ $_.Name -eq 'testalias' }} | % {{ $_.Name }}", Path.DirectorySeparatorChar)
            });

            Assert.AreEqual("testalias" + Environment.NewLine, result);
        }

        [Test]
        public void ListSingleAlias()
        {
            string result = ReferenceHost.Execute(new string[] {
                "Set-Alias testalias Get-ChildItem",
                @"Get-ChildItem alias:testalias | % { $_.Name }"
            });

            Assert.AreEqual("testalias" + Environment.NewLine, result);
        }

        [Test]
        public void ListSingleAliasWithBackslashInDriveName()
        {
            string result = ReferenceHost.Execute(new string[] {
                "Set-Alias testalias Get-ChildItem",
                String.Format(@"Get-ChildItem alias:{0}testalias | % {{ $_.Name }}", Path.DirectorySeparatorChar)
            });

            Assert.AreEqual("testalias" + Environment.NewLine, result);
        }

        [Test]
        public void GetContentForAlias()
        {
            string result = ReferenceHost.Execute(new string[] {
                "Set-Alias testalias Get-ChildItem",
                @"Get-Content alias:testalias"
            });

            Assert.AreEqual("Get-ChildItem" + Environment.NewLine, result);
        }

        [Test]
        public void SetAlias()
        {
            string result = ReferenceHost.Execute(new string[] {
                "$alias:AliasProviderTest = 'abc'",
                "$alias:AliasProviderTest"
            });

            Assert.AreEqual("abc" + Environment.NewLine, result);
        }

        [Test]
        public void SetContentForAlias()
        {
            string result = ReferenceHost.Execute(new string[] {
                "$alias:AliasProviderTest = 'abc'",
                "Set-Content alias:AliasProviderTest 'test'",
                "$alias:AliasProviderTest"
            });

            Assert.AreEqual("test" + Environment.NewLine, result);
        }

        [Test]
        public void SetContentForAliasUsingTwoItems()
        {
            Assert.Throws(Is.InstanceOf(typeof(Exception)), delegate
            {
                ReferenceHost.Execute(new string[] {
                    "$alias:AliasProviderTest = 'abc'",
                    "Set-Content alias:AliasProviderTest 'test1','test2'",
                });
            });
        }
    }
}
