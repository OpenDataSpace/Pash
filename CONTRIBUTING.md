Contributing to Pash
==============================

I'm so grateful that anyone would consider helping improve Pash. Thanks!

First thing, join http://groups.google.com/group/pash-project.

If you're looking for a place to start coding, pick a unit test marked `Explicit` (disabled). Enable it, and make it pass.

Building Pash on Windows, Linux, or Mac
---------------------------------------

I want it to be easy to build this project. You should be able to use:

- [Visual C# Express 2010](http://www.microsoft.com/express/) or later. Windows only, of course.

- [MonoDevelop 3.0](http://monodevelop.com/) or later, or Xamarin Studio, on any platform.

Open the solution at `Source/Pash.sln` and run. Or at the command line:

<!-- duplication with README.md here; keep them in synch -->

    > xbuild
    > mono Source/PashConsole/bin/Debug/Pash.exe

On **Windows**, use `MSBuild` instead of `xbuild`; the `mono` part is unecessary, and you'll probably need to add these to your PATH.

If you're experiencing difficulties with reading Pash output, e.g. it looks like `[%?%p1%{8}%...` - try adding `TERM=xterm`, like `TERM=xterm mono Source/PashConsole/bin/Debug/Pash.exe`. In particular, this is a known issue on Fedora systems.

Unit Tests
----

We use NUnit. In `master`, all tests pass all the time.

To run tests

    > xbuild /t:test

We want tests to pass on all three platforms all the time, although that can be difficult to guarantee. If you can, set up virtual machines so you can test on each platform.

PowerShell Reference Tests
----
For easier verification of compatibility with PowerShell there is a separate test infrastructure, `ReferenceTests` which can be run with either Pash or PowerShell. Every test in there should be written only against the public and documented PowerShell API. If new source files containing tests are added to `Source/ReferenceTests/ReferenceTests.csproj` they are automatically picked up by the PowerShell reference test project. The tests can then be run via `msbuild /t:RefTest` to verify that every tests that passes in Pash also passes in PowerShell.

Not every test needs to be duplicated as a reference test, but a few things probably warrant such an approach:

* Changes to the grammar where the published one is erroneous
* Dark corners and quirks of the language
* Behavior we have trouble getting right


Code Analysis
----
Code Analysis is enabled for Visual Studio builds. See the [code analysis documenation](/Documents/FxCop.md) for more details.

Travis
----

We're hooked up to Travis-CI. See https://travis-ci.org/profile/Pash-Project

We don't merge in to `master` if Travis reports an error. So keep an eye on that.


Contribution Guidelines
----


0. **You agree to the licensing requirements.**

	Pash is dual-licensed GPL/BSD. By sending a Pull Request, **you agree** to license your contributions by those same licenses.

	Please add yourself to AUTHORS.md.

1. **Make merging easy.**

	Any problem can be fixed, but only if we can merge that change. When you're preparing changes, keep an eye on making merging easy.

	The worst types of changes to merge are gross formatting changes, which ironically have the least impact on the way the code behaves.

2. **Make it obvious.**

	Write the cleanest code you can, giving your current understanding.

	If you can read code & know what it does without doubt, that's wonderful. If a function name and its implementation are obviously saying the same thing, that helps, too. Good automated testing also helps us know that code is correct.

	LINQ helps here a lot, turning imperative iteration in to declarative code.

	Keep code well-factored. Do not hesitate to Extract Method & Extract Class.

4. **Make change history clean.**

	Some day someone will look at this code and ask "why is it like this? How did it come to be this way? What was the developer thinking at the time?"

	You can engineer the change history as you work. Commit often, in tiny bits.

	Commit **refactorings** (which tend to modify a lot of code but hopefully don't change behavior) **separately from features/bugs** (which tend to modify less code, but deliberately introduce behavior changes). One way I like to work is to make most of my commits be refactorings, reshaping the code to make my new feature trivial to implement, followed by a single, simple commit that introduces the new behavior.

	**Good commit messages** should explain *why* you made this change, what other changes you considered, and why you rejected them, unless those things are obvious or irrelevant.

5. **Don't write code you don't need.**

	If you write some code you think we'll need some day, but there isn't a specific use for it now, it's just noise. Don't bother. How do you know it works? How do you know it won't break before you need it? At the very least, write tests for it.

Coding Style:
----

- Use the default Visual Studio formatting settings. Keep files formatted that way at all times.

Beyond that, use your judgement to write the best code you know how.
