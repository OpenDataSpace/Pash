using System;
using System.Linq;
using NUnit.Framework;

namespace ReferenceTests.Language
{
    public class Loops : ReferenceTestBase
    {
        [Test]
        public void For()
        {
            var cmd = "for ($i = 0; $i -ile 5; $i++) { $i }";
            ExecuteAndCompareTypedResult(cmd, 0, 1, 2, 3, 4, 5);
        }

        [Test]
        public void ForWithNonBoolCondition()
        {
            // the condition is always a substring from foo: "foo", "oo", "o", "". Last should be $false
            var cmd = "for ($i = 0; 'foo'.Substring($i); $i++) { $i }";
            ExecuteAndCompareTypedResult(cmd, 0, 1, 2);
        }

        [Test]
        public void While()
        {
            /* Should behave exactly like the for-loop */
            var cmd = "$i = 0; while ($i -ile 5) { $i; $i++ }";
            ExecuteAndCompareTypedResult(cmd, 0, 1, 2, 3, 4, 5);
        }

        [Test]
        public void WhileWithNonBoolCondition()
        {
            // the condition is always a substring from foo: "foo", "oo", "o", "". Last should be $false
            var cmd = "$i = 0; while ('foo'.Substring($i)) { $i; $i++ }";
            ExecuteAndCompareTypedResult(cmd, 0, 1, 2);
        }

        [Test]
        public void DoWhile()
        {
            /* Should behave exactly like the for-loop */
            var cmd = "$i = 0; do { $i; $i++ } while ($i -ile 5)";
            ExecuteAndCompareTypedResult(cmd, 0, 1, 2, 3, 4, 5);
        }

        [Test]
        public void DoWhileWithNonBoolCondition()
        {
            var cmd = "$i = 0; do { $i; $i++ } while ($null); $i";
            ExecuteAndCompareTypedResult(cmd, 0, 1);
        }

        [Test]
        public void DoUntil()
        {
            /* Should behave exactly like the do-while loop with inverted condition */
            var cmd = "$i = 0; do { $i; $i++ } until ($i -igt 5)";
            ExecuteAndCompareTypedResult(cmd, 0, 1, 2, 3, 4, 5);
        }

        [Test]
        public void DoUntilWithNonBoolCondition()
        {
            /* Should behave exactly like the do-while loop with inverted condition */
            var cmd = "$i = 0; do { $i; $i++ } until (4); $i";
            ExecuteAndCompareTypedResult(cmd, 0, 1);
        }

        [Test]
        public void ForLoopWithAssignmentStatementAsBodyShouldNotOutputAssignmentResultOnEachIteration()
        {
            var cmd = "$j = 0; for ($i = 0; $i -ile 10; $i++) { $j++ }; $j";
            ExecuteAndCompareTypedResult(cmd, 11);
        }

        [Test]
        public void ForEach()
        {
            string cmd = "foreach ($i in (0..5)) { $i }";
            ExecuteAndCompareTypedResult(cmd, 0, 1, 2, 3, 4, 5);
        }

        [Test]
        public void ForEachWithAssignmentStatementAsBodyShouldNotOutputAssignmentResultOnEachIteration()
        {
            string cmd = "$j = 0; foreach ($i in 0..10) { $j++ }; $j";
            ExecuteAndCompareTypedResult(cmd, 11);
        }

        [Test]
        public void ForEachWithOneElementWorks()
        {
            var cmd = "foreach ($num in 1) { $num }";
            ExecuteAndCompareTypedResult(cmd, 1);
        }

        [Test]
        public void ForEachWithNullDoesntExecute()
        {
            var cmd = "foreach ($i in $null) { 1 }";
            ExecuteAndCompareTypedResult(cmd, new object[0]);
        }

        [Test]
        public void ForEachCharacterInStringIsString()
        {
            var cmd = "foreach ($char in 'abc') { $char }";
            ExecuteAndCompareTypedResult(cmd, "abc");
        }

        [Test]
        public void ForEachCharacterInArray()
        {
            var cmd = "foreach ($char in 'abc'.ToCharArray()) { $char }";
            ExecuteAndCompareTypedResult(cmd, 'a', 'b', 'c');
        }

        [Test]
        public void BreakEndsExecutionWithoutLoop()
        {
            var cmd = "1; &{ 2; & { 3; break; 4; }; 5; }; 6;";
            ExecuteAndCompareTypedResult(cmd, 1, 2, 3);
        }

        [Test]
        public void BreakEndsEverythingFromScriptWithoutLoop()
        {
            var script = CreateFile("2; break; 3;", "ps1");
            var cmd = "1; . '" + script + "'; 4;";
            ExecuteAndCompareTypedResult(cmd, 1, 2);
        }

        [Test]
        public void ContinueEndsEverythingFromScriptWithoutLoop()
        {
            var script = CreateFile("2; continue; 3;", "ps1");
            var cmd = "1; . '" + script + "'; 4;";
            ExecuteAndCompareTypedResult(cmd, 1, 2);
        }

        [Test]
        public void ContinueEndsExecutionWithoutLoop()
        {
            var cmd = "1; &{ 2; & { 3; continue; 4; }; 5; }; 6;";
            ExecuteAndCompareTypedResult(cmd, 1, 2, 3);
        }

        [Test]
        public void ContinueInWhileLoopWorks()
        {
            var cmd = "$i = 0; while ($i -ilt 10) { $i++; continue; $i }; $i";
            ExecuteAndCompareTypedResult(cmd, 10);
        }

        [Test]
        public void ContinueInDoWhileLoopWorks()
        {
            var cmd = "$i = 0; do { $i++; $i; continue; $i * 2 } while ($i -ilt 10); $i";
            ExecuteAndCompareTypedResult(cmd, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 10);
        }

        [Test]
        public void ContinueInDoUntilLoopWorks()
        {
            var cmd = "$i = 0; do { $i++; $i; continue; $i * 2 } until ($i -ige 10); $i";
            ExecuteAndCompareTypedResult(cmd, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 10);
        }

        [Test]
        public void ContinueInForLoopWorks()
        {
            var cmd = "for ($i = 0; $i -ilt 10; $i++) { $i; continue; $i * 2; }; $i";
            ExecuteAndCompareTypedResult(cmd, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        [Test]
        public void ContinueInForeachLoopWorks()
        {
            var cmd = "$i = 0; foreach ($j in (1..10)) { $i = $j; $i; continue; $j * 2; }; $i";
            ExecuteAndCompareTypedResult(cmd, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 10);
        }

        [Test]
        public void BreakInWhileLoopWorks()
        {
            var cmd = "$i = 0; while ($i -ilt 10) { $i++; $i; break; $i * 2 }; $i";
            ExecuteAndCompareTypedResult(cmd, 1, 1);
        }

        [Test]
        public void BreakInDoWhileLoopWorks()
        {
            var cmd = "$i = 0; do { $i++; $i; break; $i * 2 } while ($i -ilt 10); $i";
            ExecuteAndCompareTypedResult(cmd, 1, 1);
        }

        [Test]
        public void BreakInDoUntilLoopWorks()
        {
            var cmd = "$i = 0; do { $i++; $i; break; $i * 2 } until ($i -ige 10); $i";
            ExecuteAndCompareTypedResult(cmd, 1, 1);
        }

        [Test]
        public void BreakInForLoopWorks()
        {
            var cmd = "for ($i = 0; $i -ilt 10; $i++) { $i; break; $i * 2; }; $i";
            ExecuteAndCompareTypedResult(cmd, 0, 0);
        }

        [Test]
        public void BreakInForeachLoopWorks()
        {
            var cmd = "$i = 0; foreach ($j in (1..10)) { $i = $j; $i; break; $j * 2; }; $i";
            ExecuteAndCompareTypedResult(cmd, 1, 1);
        }

        [Test]
        public void ContinueWorksOnlyInInnerLoop()
        {
            var cmd = "for ($i = 0; $i -ilt 2; $i++) { $j = 0; while($j -ilt 3) { $j++; continue; $j }; $j; $i; } ";
            ExecuteAndCompareTypedResult(cmd, 3, 0, 3, 1);
        }

        [Test]
        public void BreakWorksOnlyInInnerLoop()
        {
            var cmd = "for ($i = 0; $i -ilt 2; $i++) { $j = 0; while($j -ilt 3) { $j++; break; $j }; $j; $i; } ";
            ExecuteAndCompareTypedResult(cmd, 1, 0, 1, 1);
        }

        [Test]
        public void ContinueWorksInConditionalAndWritesDataBefore()
        {
            var cmd = "$i = 0; do { if ($true) { $i++; 1; continue; 2; }; 3; } until ($i -ige 2);";
            ExecuteAndCompareTypedResult(cmd, 1, 1);
        }

        [Test]
        public void BreakWorksInConditionalAndWritesDataBefore()
        {
            var cmd = "$i = 0; do { if ($true) { $i++; 1; break; 2; }; 3; } until ($i -ige 2);";
            ExecuteAndCompareTypedResult(cmd, 1);
        }
    }
}

