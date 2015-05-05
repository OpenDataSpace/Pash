using System;
using System.Linq;
using NUnit.Framework;
using System.Management.Automation;
using System.IO;

namespace ReferenceTests.Commands
{
    [TestFixture]
    public class GetModuleTests : ModuleCommandTestBase
    {
        [Test]
        public void GetModuleWithoutArgsGetsAllLoaded()
        {
            var mod = CreateFile("", "psm1"); // make sure that at least one module is loaded
            var res = ReferenceHost.RawExecute(NewlineJoin(
                "Import-Module '" + mod +"'",
                "Get-Module"
            ));
            var modName = Path.GetFileNameWithoutExtension(mod);
            Assert.That(res.Count, Is.EqualTo(1));
            Assert.That((res[0].BaseObject as PSModuleInfo).Name, Is.EqualTo(modName));
        }

        [Test]
        public void GetModuleWithPartialNamesReturnsHits()
        {
            var tmpPath = Path.GetTempPath();
            var m1Path = Path.Combine(tmpPath, "foobar.psm1");
            var m2Path = Path.Combine(tmpPath, "foobaz.psm1");
            AddCleanupFile(m1Path);
            AddCleanupFile(m2Path);
            File.WriteAllText(m1Path, "");
            File.WriteAllText(m2Path, "");

            var res = ReferenceHost.RawExecute(NewlineJoin(
                "Import-Module '" + m1Path +"','" + m2Path + "'",
                "Get-Module fooba*"
            ));
            Assert.That(res.Count, Is.EqualTo(2));
            var names = from r in res select (r.BaseObject as PSModuleInfo).Name;
            Assert.That(names, Contains.Item("foobar"));
            Assert.That(names, Contains.Item("foobaz"));
        }

        [Test]
        public void GetModuleWithUnknownNameReturnsEmpty()
        {
            var mod = Path.Combine(Path.GetTempPath(), "foobar.psm1");
            AddCleanupFile(mod);
            File.WriteAllText(mod, "");
            var res = ReferenceHost.RawExecute(NewlineJoin(
                "Import-Module '" + mod +"'",
                "Get-Module bartest"
            ));
            Assert.That(res, Is.Empty);
        }

        [Test]
        public void GetModuleWithModuleLoadedToLocalScope()
        {
            var innerMod = CreateFile("function testfun { 'foo' }", "psm1");
            var script = CreateFile("Import-Module '" + innerMod + "' -Scope Local", "ps1");
            var outerMod = CreateFile(NewlineJoin(
                "function getMT { Get-Module }",
                "function execTest { testfun }",
                " & '" + script + "'"
            ), "psm1");

            ReferenceHost.Execute("Import-Module '" + outerMod + "'");

            // make sure that the function "testfun" is not available to outerMod because of local scope
            Assert.Throws<CommandNotFoundException>(delegate {
                ReferenceHost.RawExecuteInLastRunspace("execTest");
            });

            // make sure that the function "test" is not available to global scope
            Assert.Throws<CommandNotFoundException>(delegate {
                ReferenceHost.RawExecuteInLastRunspace("testfun");
            });

            var innerModName = Path.GetFileNameWithoutExtension(innerMod);
            var outerModName = Path.GetFileNameWithoutExtension(outerMod);

            // make sure innerMod is registered in outerMod, altough loaded in local scope
            var res = ReferenceHost.RawExecuteInLastRunspace("getMT | % { $_.Name }");
            Assert.That(res.Count, Is.EqualTo(2));
            var modNames = from r in res select r.BaseObject;
            Assert.That(modNames, Contains.Item(outerModName));
            Assert.That(modNames, Contains.Item(innerModName));

            // make sure innerMod is *not* registered in global scope, as it's only loaded as a submodule
            res = ReferenceHost.RawExecuteInLastRunspace("Get-Module | % { $_.Name }");
            Assert.That(res.Count, Is.EqualTo(1));
            Assert.That(res[0].BaseObject, Is.EqualTo(outerModName));
        }

        [Test]
        public void GetModuleWithNestedModuleLoadedToGlobalScope()
        {
            var innerMod = CreateFile("function testfun { 'foo' }", "psm1");
            var script = CreateFile("Import-Module '" + innerMod + "' -Global", "ps1");
            var outerMod = CreateFile(NewlineJoin(
                "function getMT { Get-Module }",
                "function execTest { testfun }",
                " & '" + script + "'"
            ), "psm1");

            ReferenceHost.Execute("Import-Module '" + outerMod + "'");

            // make sure that the function "testfun" available to outerMod because of global scope
            var res = ReferenceHost.RawExecuteInLastRunspace("execTest");
            Assert.That(res.Count, Is.EqualTo(1));
            Assert.That(res[0].BaseObject, Is.EqualTo("foo"));

            // make sure that the function "testfun" available to global scope
            res = ReferenceHost.RawExecuteInLastRunspace("testfun");
            Assert.That(res.Count, Is.EqualTo(1));
            Assert.That(res[0].BaseObject, Is.EqualTo("foo"));

            var innerModName = Path.GetFileNameWithoutExtension(innerMod);
            var outerModName = Path.GetFileNameWithoutExtension(outerMod);

            // make sure innerMod is registered in outerMod
            res = ReferenceHost.RawExecuteInLastRunspace("getMT | % { $_.Name }");
            Assert.That(res.Count, Is.EqualTo(2));
            var modNames = from r in res select r.BaseObject;
            Assert.That(modNames, Contains.Item(outerModName));
            Assert.That(modNames, Contains.Item(innerModName));

            // make sure innerMod is also registered in global scope
            res = ReferenceHost.RawExecuteInLastRunspace("Get-Module | % { $_.Name }");
            Assert.That(res.Count, Is.EqualTo(2));
            modNames = from r in res select r.BaseObject;
            Assert.That(modNames, Contains.Item(outerModName));
            Assert.That(modNames, Contains.Item(innerModName));
        }
    }
}

