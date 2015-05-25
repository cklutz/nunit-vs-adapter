// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Core;
using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System.Runtime.Remoting;

namespace NUnit.VisualStudio.TestAdapter
{
    /// <summary>
    /// NUnitEventListener implements the EventListener interface and
    /// translates each event into a message for the VS test platform.
    /// </summary>
    public class NUnitEventListener : MarshalByRefObject, EventListener, IDisposable // Public for testing
    {
        private readonly ITestExecutionRecorder testLog;
        private readonly TestConverter testConverter;

        public override object InitializeLifetimeService()
        {
            // Give the listener an infinite lease lifetime by returning null
            // http://msdn.microsoft.com/en-us/magazine/cc300474.aspx#edupdate
            // This also means RemotingServices.Disconnect() must be called to prevent memory leaks
            // http://nbevans.wordpress.com/2011/04/17/memory-leaks-with-an-infinite-lifetime-instance-of-marshalbyrefobject/
            return null;
        }

        public NUnitEventListener(ITestExecutionRecorder testLog, TestConverter testConverter)
        {
            this.testLog = testLog;
            this.testConverter = testConverter;
            StdOut = new StringBuilder();
            StdErr = new StringBuilder();
            DebugTrace = new StringBuilder();
            AdditionalInfo = new StringBuilder();
            OutputTestCaseOutputAsMessage = true;
        }

        public bool CombineTestCaseOutputIntoStdOut { get; set; }
        public bool OutputTestCaseOutputAsMessage { get; set; }

        public void RunStarted(string name, int testCount)
        {
            testLog.SendMessage(TestMessageLevel.Informational, "Run started: " + name);
        }

        public void RunFinished(Exception exception)
        {
        }

        public void RunFinished(NUnit.Core.TestResult result)
        {
        }

        public StringBuilder StdOut { get; private set; }
        public StringBuilder StdErr { get; private set; }
        public StringBuilder DebugTrace { get; private set; }
        public StringBuilder AdditionalInfo { get; private set; }

        public void SuiteStarted(TestName testName)
        {

        }

        public void SuiteFinished(NUnit.Core.TestResult result)
        {
            if ((result.IsError || result.IsFailure) &&
                (result.FailureSite == FailureSite.SetUp || result.FailureSite == FailureSite.TearDown))
            {
                testLog.SendMessage(
                    TestMessageLevel.Error,
                    string.Format("{0} failed for test fixture {1}", result.FailureSite, result.FullName));
                if (result.Message != null)
                    testLog.SendMessage(TestMessageLevel.Error, result.Message);
                if (result.StackTrace != null)
                    testLog.SendMessage(TestMessageLevel.Error, result.StackTrace);
            }
        }

        public void TestStarted(TestName testName)
        {
            TestCase ourCase = testConverter.GetCachedTestCase(testName.UniqueName);

            // Simply ignore any TestName not found in the cache
            if (ourCase != null)
            {
                this.testLog.RecordStart(ourCase);
                // StdOut = testName.FullName + "\r";
            }

        }

        public void TestFinished(NUnit.Core.TestResult result)
        {
            TestResult ourResult = testConverter.ConvertTestResult(result);
            AddMessage(ourResult, TestResultMessage.StandardOutCategory, StdOut);
            AddMessage(ourResult, TestResultMessage.StandardErrorCategory, StdErr);
            AddMessage(ourResult, TestResultMessage.DebugTraceCategory, DebugTrace);
            AddMessage(ourResult, TestResultMessage.AdditionalInfoCategory, AdditionalInfo);
            this.testLog.RecordEnd(ourResult.TestCase, ourResult.Outcome);
            this.testLog.RecordResult(ourResult);

            StdOut.Clear();
            StdErr.Clear();
            DebugTrace.Clear();
            AdditionalInfo.Clear();
        }

        private static void AddMessage(TestResult testResult, string category, StringBuilder buffer)
        {
            string text = buffer.ToString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                testResult.Messages.Add(new TestResultMessage(category, text));
            }
        }

        public void TestOutput(TestOutput testOutput)
        {
            string message = testOutput.Text;
            int length = message.Length;
            int drop = message.EndsWith(Environment.NewLine)
                ? Environment.NewLine.Length
                : message[length - 1] == '\n' || message[length - 1] == '\r'
                    ? 1
                    : 0;
            if (drop > 0)
                message = message.Substring(0, length - drop);

            if (OutputTestCaseOutputAsMessage && !string.IsNullOrWhiteSpace(message))
            {
                testLog.SendMessage(TestMessageLevel.Informational, message);
            }
            
            if (CombineTestCaseOutputIntoStdOut)
            {
                StdOut.AppendLine(message);
            }
            else
            {
                switch (testOutput.Type)
                {
                    case TestOutputType.Out:
                        StdOut.AppendLine(message);
                        break;
                    case TestOutputType.Error:
                        StdErr.AppendLine(message);
                        break;
                    case TestOutputType.Trace:
                        DebugTrace.AppendLine(message);
                        break;
                    default:
                        AdditionalInfo.AppendLine(message);
                        break;
                }
            }
        }

        public void UnhandledException(Exception exception)
        {
        }

        #region IDisposable
        private bool disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    RemotingServices.Disconnect(this);
                }
            }
            disposed = true;
        }

        ~NUnitEventListener()
        {
            Dispose(false);
        }
        #endregion
    }
}
