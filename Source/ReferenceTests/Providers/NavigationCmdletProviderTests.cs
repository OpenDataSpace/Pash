﻿using System;
using System.Linq;
using NUnit.Framework;
using TestPSSnapIn;
using System.Management.Automation;
using System.Collections.Generic;
using NUnit.Framework.Constraints;

namespace ReferenceTests.Providers
{
    class MessageMatcher
    {
        List<string> _alternatives;

        public MessageMatcher(string msg)
        {
            _alternatives = (from m in msg.Split(new [] { "__OR__" }, StringSplitOptions.None) select m.Trim()).ToList();
        }

        public override bool Equals(object obj)
        {
            if (obj is string)
            {
                return _alternatives.Contains((string)obj);
            }
            else if (obj is MessageMatcher)
            {
                return _alternatives.SequenceEqual(((MessageMatcher)obj)._alternatives);
            }
            return false;
        }

        public override int GetHashCode()
        {
            // XOR of all hashcodes from _alternatives
            return (from a in _alternatives select a.GetHashCode()).Aggregate((l, r) => l ^ r);
        }

        public override string ToString()
        {
            return String.Join(" or ", from a in _alternatives select '"' + a + '"');
        }
    }

    public class NavigationCmdletProviderTests : ReferenceTestBaseWithTestModule
    {
        private const string _defDrive = TestNavigationProvider.DefaultDrivePath;
        private const string _defRoot = TestNavigationProvider.DefaultDriveRoot + "/";
        private const string _secDrive = TestNavigationProvider.SecondDrivePath;
        private const string _secRoot = TestNavigationProvider.SecondDriveRoot + "/";

        private List<string> ExecutionMessages { get { return TestNavigationProvider.Messages; } }

        EqualConstraint AreMatchedBy(params string[] expected)
        {
            // constraint like EqualTo, but allowing messages to be prepended by "? " to be optional
            var msgs = TestNavigationProvider.Messages;
            var optional = (from m in expected where m.StartsWith("? ") select m).Count();
            if (msgs.Count == expected.Length - optional)
            {
                var nonOpts = (from m in expected where !m.StartsWith("? ") select new MessageMatcher(m));
                return Is.EqualTo(nonOpts);
            }
            else
            {
                var allExp = from m in expected select new MessageMatcher(m.StartsWith("? ") ? m.Substring(2) : m);
                return Is.EqualTo(allExp);
            }
        }

        string DriveToRoot(string path)
        {
            if (path.StartsWith(_defDrive))
            {
                return _defRoot + path.Substring(_defDrive.Length);
            }
            else if (path.StartsWith(_secDrive))
            {
                return _secRoot + path.Substring(_secDrive.Length);
            }
            return path;
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            // set the "existing" items before each test
            TestNavigationProvider.ExistingPaths = new List<string>()
            {
                _defRoot + "foo/bar.txt",
                _defRoot + "foo/baz.doc",
                _defRoot + "foo/foo/bla.txt",
                _defRoot + "bar.doc",
                _defRoot + "bar/foo.txt",
                _secRoot + "foo/blub.doc",
                _secRoot + "foo/bar.txt",
                _secRoot + "bar.txt"
            };
            TestNavigationProvider.Messages.Clear();
        }

        [TestCase(_defDrive, "leaf", false)]
        [TestCase(_defDrive, "any", true)]
        [TestCase(_defDrive + "notExisting", "any", false)]
        [TestCase(_defDrive + "notExisting", "leaf", false)]
        [TestCase(_defDrive + "foo", "leaf", false)]
        [TestCase(_defDrive + "foo", "any", true)]
        [TestCase(_defDrive + "bar.doc", "leaf", true)]
        [TestCase(_defDrive + "bar.doc", "any", true)]
        [TestCase(_secDrive + "foo/blub.doc", "leaf", true)]
        [TestCase(_secDrive + "foo/blub.doc", "any", true)]
        public void NavigationProviderSupportsTestPathAnyLeaf(string path, string type, bool expected)
        {
            var cmd = "Test-Path " + path + " -PathType " + type;
            ExecuteAndCompareTypedResult(cmd, expected);
            Assert.That(TestNavigationProvider.Messages[0], Is.EqualTo("ItemExists " + DriveToRoot(path)));
        }

        [TestCase(_defDrive, true)]
        [TestCase(_defDrive + "notExisting", true)] // although not existing, because only IsContainer is checked
        [TestCase(_defDrive + "foo", true)]
        [TestCase(_defDrive + "bar.doc", false)]
        [TestCase(_secDrive + "foo/blub.doc", false)]
        public void NavigationProviderSupportsTestPathContainer(string path, bool expected)
        {
            var cmd = "Test-Path " + path + " -PathType container";
            ExecuteAndCompareTypedResult(cmd, expected);
            Assert.That(TestNavigationProvider.Messages[0], Is.EqualTo("IsItemContainer " + DriveToRoot(path)));
        }

        [TestCase(_defDrive, _defRoot)]
        [TestCase(_secDrive, _secRoot)]
        public void NavigationProviderSupportsGetItem(string drive, string root)
        {
            var cmd = "Get-Item " + drive + "foo";
            ReferenceHost.Execute(cmd);
            Assert.That(ExecutionMessages, AreMatchedBy(
                "ItemExists " + root + "foo",
                "GetItem " + root + "foo"
            ));
        }

        [Test]
        public void NavigationProviderSupportsGetItemWithWildcard()
        {
            var cmd = "Get-Item " + _defDrive + "foo/b*";
            ReferenceHost.Execute(cmd);
            // with PS some operations are called twice at the beginning. We won't check for this behavior
            Assert.That(ExecutionMessages, AreMatchedBy(
                "ItemExists " + _defRoot + "foo",
                "HasChildItems " + _defRoot + "foo",
                "GetChildNames " + _defRoot + "foo ReturnMatchingContainers",
                "? ItemExists " + _defRoot + "foo", // optional
                "? HasChildItems " + _defRoot + "foo", // optional
                "? GetChildNames " + _defRoot + "foo ReturnMatchingContainers", // optional
                "GetItem " + _defRoot + "foo/bar.txt",
                "GetItem " + _defRoot + "foo/baz.doc"
            ));
        }

        [Test]
        public void NavigationProviderSupportsNewItem()
        {
            var path = _defDrive + "newItem.tmp";
            var rpath = _defRoot + "newItem.tmp";
            var cmd = "New-Item " + path  + " -ItemType testType -Value testValue";
            ReferenceHost.Execute(cmd);
            Assert.That(ExecutionMessages, AreMatchedBy("NewItem " + rpath + " testType testValue"));
        }

        [Test]
        public void NavigationProviderSupportsRemoveItem()
        {
            var cmd = "Remove-Item " + _defDrive + "bar.doc";
            var rpath = _defRoot + "bar.doc";
            ReferenceHost.Execute(cmd);
            Assert.That(ExecutionMessages, AreMatchedBy(
                "ItemExists " + rpath,
                "HasChildItems " + rpath,
                "RemoveItem " + rpath + " False"
            ));
        }

        [Test]
        public void NavigationProviderSupportsRemoveItemWithRecursion()
        {
            var cmd = "Remove-Item -Recurse " + _defDrive + "foo";
            var rpath = _defRoot + "foo";
            ReferenceHost.Execute(cmd);
            Assert.That(ExecutionMessages, AreMatchedBy(
                "ItemExists " + rpath,
                "? HasChildItems " + rpath, // optional
                "RemoveItem " + rpath + " True"
            ));
        }

        [Test]
        public void NavigationProviderThrowsOnRemoveNodeWithoutRecursion()
        {
            var cmd = "Remove-Item " + _defDrive + "foo";
            Assert.Throws<CmdletInvocationException>(delegate {
                ReferenceHost.RawExecuteInLastRunspace(cmd);
            });
        }

        [Test]
        public void NavigationProviderSupportsRenameItem()
        {
            var cmd = "Rename-Item " + _defDrive + "foo -NewName foobar";
            var rpath = _defRoot + "foo";
            ReferenceHost.Execute(cmd);
            // Powershell shomehow calls ItemExists twice. We won't check for this behavior
            Assert.That(ExecutionMessages, AreMatchedBy(
                "ItemExists " + rpath,
                "? ItemExists " + rpath, // optional
                "RenameItem " + rpath + " foobar"
            ));
        }

        [Test]
        public void NavigationProviderSupportsGetChildItemWithRecursion()
        {
            var cmd = "Get-ChildItem " + _defDrive + "foo -Recurse";
            ReferenceHost.Execute(cmd);
            // Powershell calls "IsItemContainer " + _defRoot + "foo" twice, we don't
            Assert.That(ExecutionMessages, AreMatchedBy(
                "? IsItemContainer " + _defRoot + "foo",
                "ItemExists " + _defRoot + "foo",
                "IsItemContainer " + _defRoot + "foo",
                "GetChildItems " + _defRoot + "foo True"
            ));
        }

        [Test]
        public void NavigationProviderSupportsGetChildItem()
        {
            var cmd = "Get-ChildItem " + _defDrive + "foo";
            ReferenceHost.Execute(cmd);
            Assert.That(ExecutionMessages, AreMatchedBy(
                "ItemExists " + _defRoot + "foo",
                "IsItemContainer " + _defRoot + "foo",
                "GetChildItems " + _defRoot + "foo False"
            ));
        }

        [Test]
        public void NavigationProviderSupportsGetChildItemWithoutArg()
        {
            var cmd = "Set-Location " + _defDrive + "foo; Get-ChildItem";
            ReferenceHost.Execute(cmd);
            var rootWithoutSlash = _defRoot.Substring(0, _defRoot.Length - 1);
            // PS calls NormalizeRelativePath for Set-Location. I don't know why, so we skip it
            Assert.That(ExecutionMessages, AreMatchedBy(
                "ItemExists " + _defRoot + "foo",
                "? NormalizeRelativePath " + _defRoot + "foo " + rootWithoutSlash,
                "IsItemContainer " + _defRoot + "foo",
                "ItemExists " + _defRoot + "foo",
                "IsItemContainer " + _defRoot + "foo",
                "GetChildItems " + _defRoot + "foo False"
            ));
        }


        [Test]
        public void NavigationProviderSupportsGetChildItemFromLeaf()
        {
            var cmd = "Get-ChildItem " + _defDrive + "bar.doc";
            ReferenceHost.Execute(cmd);
            Assert.That(ExecutionMessages, AreMatchedBy(
                "ItemExists " + _defRoot + "bar.doc",
                "IsItemContainer " + _defRoot + "bar.doc",
                "GetItem " + _defRoot + "bar.doc"
            ));
        }

        [TestCase("-Include '*.txt'")]
        [TestCase("-Include '*.*' -Exclude '*.doc'")]
        public void NavigationProviderSupportsGetChildItemWithFilter(string filter)
        {
            var cmd = "Get-ChildItem -Recurse " + _defDrive + " " + filter;
            ReferenceHost.Execute(cmd);
            Assert.That(ExecutionMessages, AreMatchedBy(
                "ItemExists " + _defRoot,
                "IsItemContainer " + _defRoot,
                "GetChildNames " + _defRoot + " ReturnAllContainers",
                "IsItemContainer " + _defRoot + "foo",
                "GetChildNames " + _defRoot + "foo ReturnAllContainers",
                "GetItem " + _defRoot + "foo/bar.txt",
                "IsItemContainer " + _defRoot + "foo/bar.txt",
                "IsItemContainer " + _defRoot + "foo/baz.doc",
                "IsItemContainer " + _defRoot + "foo/foo",
                "GetChildNames " + _defRoot + "foo/foo ReturnAllContainers",
                "GetItem " + _defRoot + "foo/foo/bla.txt",
                "IsItemContainer " + _defRoot + "foo/foo/bla.txt",
                "IsItemContainer " + _defRoot + "bar.doc",
                "IsItemContainer " + _defRoot + "bar",
                "GetChildNames " + _defRoot + "bar ReturnAllContainers",
                "GetItem " + _defRoot + "bar/foo.txt",
                "IsItemContainer " + _defRoot + "bar/foo.txt"
            ));
        }

        [Test]
        public void NavigationProviderSupportsGetChildItemWithFilterInPath()
        {
            var cmd = "Get-ChildItem -Recurse " + _secDrive + "*.txt";
            ReferenceHost.Execute(cmd);
            var secRootWithoutSlash = _secRoot.Substring(0, _secRoot.Length - 1);
            Assert.That(ExecutionMessages, AreMatchedBy(
                // next 3 with or without last slash because of the #trailingSeparatorAmbiguity
                "ItemExists " + _secRoot +
                " __OR__ ItemExists " + secRootWithoutSlash,
                "HasChildItems " + _secRoot +
                " __OR__ HasChildItems " + secRootWithoutSlash,
                "GetChildNames " + _secRoot + " ReturnMatchingContainers" +
                " __OR__ GetChildNames " + secRootWithoutSlash + " ReturnMatchingContainers",
                "IsItemContainer " + _secRoot + "bar.txt",
                "ItemExists " + _secRoot,
                "IsItemContainer " + _secRoot,
                "GetChildNames " + _secRoot + " ReturnAllContainers",
                "IsItemContainer " + _secRoot + "foo",
                "GetChildNames " + _secRoot + "foo ReturnAllContainers",
                "IsItemContainer " + _secRoot + "foo/blub.doc",
                "GetItem " + _secRoot + "foo/bar.txt",
                "IsItemContainer " + _secRoot + "foo/bar.txt",
                "GetItem " + _secRoot + "bar.txt",
                "IsItemContainer " + _secRoot + "bar.txt"
            ));
        }

        [Test]
        public void NavigationProviderSupportsGetChildItemWithFilterInPathWithoutRecursion()
        {
            var cmd = NewlineJoin(
                "Set-Location " + _secDrive,
                "Get-ChildItem ./*.txt"
            );
            ReferenceHost.Execute(cmd);
            var secRootWithoutSlash = _secRoot.Substring(0, _secRoot.Length - 1);
            Assert.That(ExecutionMessages, AreMatchedBy(
                "ItemExists " + _secRoot,
                "? NormalizeRelativePath " + _secRoot + " " + secRootWithoutSlash,
                "IsItemContainer " + _secRoot,
                // next 3 with or without last slash because of the #trailingSeparatorAmbiguity
                "ItemExists " + _secRoot +
                " __OR__ ItemExists " + secRootWithoutSlash,
                "HasChildItems " + _secRoot +
                " __OR__ HasChildItems " + secRootWithoutSlash,
                "GetChildNames " + _secRoot + " ReturnMatchingContainers" +
                " __OR__ GetChildNames " + secRootWithoutSlash + " ReturnMatchingContainers",
                "GetItem " + _secRoot + "bar.txt"
            ));
        }

        [Test]
        public void NavigationProviderSupportsGetChildNames()
        {
            var cmd = "Get-ChildItem " + _defDrive + "foo -Name";
            ReferenceHost.Execute(cmd);
            Assert.That(ExecutionMessages, AreMatchedBy(
                "ItemExists " + _defRoot + "foo",
                "GetChildNames " + _defRoot + "foo ReturnMatchingContainers"
            ));
        }

        [Test]
        public void NavigationProviderSupportsGetChildNamesFromLeaf()
        {
            var cmd = "Get-ChildItem " + _defDrive + "bar.doc -Name";
            ReferenceHost.Execute(cmd);
            Assert.That(ExecutionMessages, AreMatchedBy(
                "ItemExists " + _defRoot + "bar.doc",
                "GetChildNames " + _defRoot + "bar.doc ReturnMatchingContainers"
            ));
        }

        [Test]
        public void NavigationProviderSupportsGetChildNamesWithRecursion()
        {
            // We here have a #trailingSeparatorAmbiguity without a trailing slash after "foo" at this point.
            // Because we won't bother, we simply append the trialing slash in the cmd and PS/Pash will behave likewise
            var cmd = "Get-ChildItem " + _defDrive + "foo -Name -Recurse";
            ExecuteAndCompareTypedResult(cmd, "bar.txt", "baz.doc", "foo", "foo/bla.txt");
            var gcnfooPrefix = "GetChildNames " + _defRoot + "foo";
            Assert.That(ExecutionMessages, AreMatchedBy(
                "ItemExists " + _defRoot + "foo",
                // out algorithm checks if that item is a container, don't know why PS doesn't do it
                "? IsItemContainer " + _defRoot + "foo",
                // because of #trailingSeparatorAmbiguity the next two messages might have a slash after 'foo' or not
                gcnfooPrefix + "/ ReturnMatchingContainers __OR__ " + gcnfooPrefix + " ReturnMatchingContainers",
                gcnfooPrefix + "/ ReturnAllContainers __OR__ " + gcnfooPrefix + " ReturnAllContainers",
                "IsItemContainer " + _defRoot + "foo/bar.txt",
                "IsItemContainer " + _defRoot + "foo/baz.doc",
                "IsItemContainer " + _defRoot + "foo/foo",
                "GetChildNames " + _defRoot + "foo/foo ReturnMatchingContainers",
                "GetChildNames " + _defRoot + "foo/foo ReturnAllContainers",
                "IsItemContainer " + _defRoot + "foo/foo/bla.txt"
            ));
        }

        [TestCase("-Include *.txt")]
        [TestCase("-Include *.* -Exclude *.doc")]
        public void NavigationProviderSupportsGetChildNamesWithRecursionAndFilter(string filter)
        {
            var cmd = "Get-ChildItem " + _defDrive + "foo -Name -Recurse " + filter;
            ExecuteAndCompareTypedResult(cmd, "bar.txt", "foo/bla.txt");
            var gcnfooPrefix = "GetChildNames " + _defRoot + "foo";
            Assert.That(ExecutionMessages, AreMatchedBy(
                "ItemExists " + _defRoot + "foo",
                "? IsItemContainer " + _defRoot + "foo", // PS does this with filter, but not without. We don't, so optional
                // because of #trailingSeparatorAmbiguity the next two messages might have a slash after 'foo' or not
                gcnfooPrefix + "/ ReturnMatchingContainers __OR__ " + gcnfooPrefix + " ReturnMatchingContainers",
                gcnfooPrefix + "/ ReturnAllContainers __OR__ " + gcnfooPrefix + " ReturnAllContainers",
                "IsItemContainer " + _defRoot + "foo/bar.txt",
                "IsItemContainer " + _defRoot + "foo/baz.doc",
                "IsItemContainer " + _defRoot + "foo/foo",
                "GetChildNames " + _defRoot + "foo/foo ReturnMatchingContainers",
                "GetChildNames " + _defRoot + "foo/foo ReturnAllContainers",
                "IsItemContainer " + _defRoot + "foo/foo/bla.txt"
            ));
        }

        [Test]
        public void NavigationProviderSupportsGetChildNamesWithFilterInPathDoesntWork()
        {
            var cmd = "Get-ChildItem " + _defDrive + "foo/*.txt -Name -Recurse";
            ExecuteAndCompareTypedResult(cmd, "bar.txt");
            Assert.That(ExecutionMessages, AreMatchedBy(
                "ItemExists " + _defRoot + "foo",
                "HasChildItems " + _defRoot + "foo",
                "GetChildNames " + _defRoot + "foo ReturnMatchingContainers",
                // PS calls these operations twice, we don't. So they are optional
                "? ItemExists " + _defRoot + "foo",
                "? HasChildItems " + _defRoot + "foo",
                "? GetChildNames " + _defRoot + "foo ReturnMatchingContainers",
                "IsItemContainer " + _defRoot + "foo/bar.txt"
            ));
        }

        [Test]
        public void NavigationProviderSupportsGetChildNamesWithGlobbingDoesntCheckContainer()
        {
            // What does this mean? Calling this method with "foo" as path obviously returns the element names
            // from foo. Globbing would resolve * to foo and others, but then we won't return the elements from foo, only
            // the "foo" name itself!
            var cmd = "Get-ChildItem " + _defDrive + "* -Name";
            ExecuteAndCompareTypedResult(cmd, "foo", "bar.doc", "bar");
            var rootWithoutSlash = _defRoot.Substring(0, _defRoot.Length - 1);
            // PS calls all functions twice. We don't. Also, because of the #trailingSeparatorAmbiguity
            // we don't have trailing slashes. So we check two different message sets
            Assert.That(ExecutionMessages, AreMatchedBy(
                "ItemExists " + _defRoot,
                "HasChildItems " + _defRoot,
                "GetChildNames " + _defRoot + " ReturnMatchingContainers",
                "ItemExists " + _defRoot,
                "HasChildItems " + _defRoot,
                "GetChildNames " + _defRoot + " ReturnMatchingContainers"
            ).Or.Matches(AreMatchedBy(
                "ItemExists " + rootWithoutSlash,
                "HasChildItems " + rootWithoutSlash,
                "GetChildNames " + rootWithoutSlash + " ReturnMatchingContainers"
            )));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void NavigationProviderSupportsCopyItem(bool recurse)
        {
            var recurseParam = recurse ? " -Recurse" : "";
            var cmd = "Copy-Item " + _defDrive + "foo " + _secDrive + recurseParam;
            ReferenceHost.Execute(cmd);
            // In Pash the first two operations are called in reverse order (because of our code design).
            // Also Pash doesn't remove the trailing slash, this is tagged as #trailingSeparatorAmbiguity.
            // PS isn't consistent when or when not to remove/append trailing separators, so it seems to not matter
            // We simply leave out the trailing slash in the command so PS and Pash are acting likewise
            Assert.That(ExecutionMessages, AreMatchedBy(
                "ItemExists " + _defRoot + "foo",
                "IsItemContainer " + _secRoot,
                "IsItemContainer " + _defRoot + "foo",
                "CopyItem " + _defRoot + "foo " + _secRoot + " " + recurse
            ).Or.Matches(AreMatchedBy(
                "IsItemContainer " + _secRoot,
                "ItemExists " + _defRoot + "foo",
                "IsItemContainer " + _defRoot + "foo",
                "CopyItem " + _defRoot + "foo " + _secRoot + " " + recurse
            )));
        }

        [Test]
        public void NavigationProviderThrowsOnCopyItemToOtherProvider()
        {
            var cmd = "Copy-Item " + _defDrive + "bar.doc variable:\\";
            var e = Assert.Throws<ExecutionWithErrorsException>(delegate
            {
                ReferenceHost.Execute(cmd);
            });
            Assert.That(e.Errors.Length, Is.EqualTo(1));
            Assert.That(e.Errors[0].Exception, Is.TypeOf(typeof(PSArgumentException)));
        }

        [Test]
        public void NavigationProviderThrowsOnCopyItemContainerOnLeaf()
        {
            var cmd = "Copy-Item -Recurse " + _defDrive + "foo/ " + _secDrive + "bar.txt";
            var e = Assert.Throws<ExecutionWithErrorsException>(delegate
            {
                ReferenceHost.Execute(cmd);
            });
            Assert.That(e.Errors.Length, Is.EqualTo(1));
            Assert.That(e.Errors[0].Exception, Is.TypeOf(typeof(PSArgumentException)));
        }

        [Test, Ignore("This somehow doesn't work as Powershell mixes up provider with the last parameter")]
        public void NavigationProviderSupportsCopyItemWithoutContainers()
        {
            var cmd = "Copy-Item " + _defDrive + "foo/ " + _secDrive + " -Recurse -Container:$false";
            ReferenceHost.Execute(cmd);
            Assert.That(ExecutionMessages, AreMatchedBy(
                // Mesages?
            ));
        }

        [Test]
        public void NavigationProviderThrowsOnMoveItemToOtherProvider()
        {
            var cmd = "Move-Item " + _defDrive + "bar.doc variable:\\";
            var e = Assert.Throws<ExecutionWithErrorsException>(delegate
            {
                ReferenceHost.Execute(cmd);
            });
            Assert.That(e.Errors.Length, Is.EqualTo(1));
            Assert.That(e.Errors[0].Exception, Is.TypeOf(typeof(PSArgumentException)));
        }

        [Test]
        public void NavigationProviderSupportsMoveItem()
        {
            var cmd = "Move-Item " + _defDrive + "foo " + _secDrive;
            ReferenceHost.Execute(cmd);
            Assert.That(ExecutionMessages, AreMatchedBy(
                "ItemExists " + _defRoot + "foo",
                // PS triple checks the existance. In Pash we only need to get to know this once
                "? ItemExists " + _defRoot + "foo",
                "? ItemExists " + _defRoot + "foo",
                "MoveItem " + _defRoot + "foo " + _secRoot
            ));
        }

        [Test]
        public void NavigationProviderSupportsMoveItemContainerOnLeaf()
        {
            var cmd = "Move-Item " + _defDrive + "foo " + _secDrive + "bar.txt";
            ReferenceHost.Execute(cmd);
            Assert.That(ExecutionMessages, AreMatchedBy(
                "ItemExists " + _defRoot + "foo",
                // PS triple checks the existance. In Pash we only need to get to know this once
                "? ItemExists " + _defRoot + "foo",
                "? ItemExists " + _defRoot + "foo",
                "MoveItem " + _defRoot + "foo " + _secRoot + "bar.txt"
            ));
        }

        [Test]
        public void NavigationProviderSupportsResolvePath()
        {
            var cmd = "(Resolve-Path " + _defDrive + "foo/*.txt).Path";
            var sep = System.IO.Path.DirectorySeparatorChar;
            ExecuteAndCompareTypedResult(cmd, TestNavigationProvider.DefaultDriveName + ":" + sep + "foo/bar.txt");
            Assert.That(ExecutionMessages, AreMatchedBy(
                "ItemExists " + _defRoot + "foo",
                "HasChildItems " + _defRoot + "foo",
                "GetChildNames " + _defRoot + "foo ReturnMatchingContainers"
            ));
        }

        [Test]
        public void NavigationProviderSupportsResolvePathHome()
        {
            var cmd = NewlineJoin(
                "Set-Location " + _defDrive,
                "(Resolve-Path ~/).ProviderPath"
            );
            // note that the trailing slash is removed by PS
            ExecuteAndCompareTypedResult(cmd, TestNavigationProvider.HomePath);
        }

        [Test]
        public void NavigationProviderSupportsResolvePathRelative()
        {
            var cmd = NewlineJoin(
                "Set-Location " + _defDrive,
                "Resolve-Path " + _defDrive + "foo/*.txt -Relative"
            );
            var rootWithoutSlash = _defRoot.Substring(0, _defRoot.Length -1);
            ExecuteAndCompareTypedResult(cmd, "./foo/bar.txt");
            // PS calls NormalizeRelativePath for Set-Location. I don't know why, so we skip it
            Assert.That(ExecutionMessages, AreMatchedBy(
                "ItemExists " + _defRoot,
                "? NormalizeRelativePath " + _defRoot + " " + rootWithoutSlash,
                "IsItemContainer " + _defRoot,
                "ItemExists " + _defRoot + "foo",
                "HasChildItems " + _defRoot + "foo",
                "GetChildNames " + _defRoot + "foo ReturnMatchingContainers",
                "NormalizeRelativePath " + _defRoot + "foo/bar.txt " + _defRoot
            ));
        }

        [TestCase("../bar.doc")]
        [TestCase("../foo/../bar.doc")]
        [TestCase("./../bar.doc")]
        public void NavigationProviderSupportsGetItemWithRelativePath(string relpath)
        {
            var cmd = NewlineJoin(
                "Set-Location " + _defDrive + "foo",
                "Get-Item " + relpath
            );
            ReferenceHost.Execute(cmd);
            var rootWithoutSlash = _defRoot.Substring(0, _defRoot.Length - 1);
            // PS calls NormalizeRelativePath for Set-Location. I don't know why, so we skip it
            Assert.That(ExecutionMessages, AreMatchedBy(
                "ItemExists " + _defRoot + "foo",
                "? NormalizeRelativePath " + _defRoot + "foo " + rootWithoutSlash,
                "IsItemContainer " + _defRoot + "foo",
                "ItemExists " + _defRoot + "bar.doc",
                "GetItem " + _defRoot + "bar.doc"
            ));
        }

    }
}

