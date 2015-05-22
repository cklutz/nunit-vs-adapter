// ****************************************************************
// Copyright (c) 2013 NUnit Software. All rights reserved.
// ****************************************************************

using System;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace NUnit.VisualStudio.TestAdapter
{
    /// <summary>
    /// TestLogger wraps an IMessageLogger and adds various
    /// utility methods for sending messages. Since the
    /// IMessageLogger is only provided when the discovery
    /// and execution objects are called, we use two-phase
    /// construction. Until Initialize is called, the logger
    /// simply swallows all messages without sending them
    /// anywhere.
    /// </summary>
    public class TestLogger : IMessageLogger
    {
        private IMessageLogger messageLogger;

        int Verbosity { get; set; }

        public TestLogger()
        {
            Verbosity = 0;
        }

        public void Initialize(IMessageLogger messageLoggerParam, NUnitTestAdapterSettings settings)
        {
            messageLogger = messageLoggerParam;
            Verbosity = settings.Verbosity;
        }

        public void AssemblyNotSupportedWarning(string sourceAssembly)
        {
            SendWarningMessage("Assembly not supported: " + sourceAssembly);
        }

        public void DependentAssemblyNotFoundWarning(string dependentAssembly, string sourceAssembly)
        {
            SendWarningMessage("Dependent Assembly " + dependentAssembly + " of " + sourceAssembly + " not found. Can be ignored if not a NUnit project.");
        }

        public void UnsupportedFrameworkWarning(string assembly, Exception ex)
        {
            SendWarningMessage("Attempt to load assembly with unsupported test framework in  " + assembly + ": " + ex);
        }

        public void LoadingAssemblyFailedWarning(string dependentAssembly, string sourceAssembly)
        {
            SendWarningMessage("Assembly " + dependentAssembly + " loaded through " + sourceAssembly + " failed. Assembly is ignored. Correct deployment of dependencies if this is an error.");
        }

        public void NUnitLoadError(string sourceAssembly)
        {
            SendErrorMessage("NUnit failed to load " + sourceAssembly);
        }

        private TestMessageLevel GetEffectiveMessageLevel(TestMessageLevel suggestedLevel)
        {
            if (Verbosity >= 99)
            {
                // This will make sure that the message _is_ displayed in a TFS build report. No matter what.
                return TestMessageLevel.Error;
            }

            return suggestedLevel;
        }

        public void SendErrorMessage(string message)
        {
            SendMessage(TestMessageLevel.Error, message);
        }

        public void SendErrorMessage(string message, Exception ex)
        {
            SendMessage(TestMessageLevel.Error, message);
            SendErrorMessage(ex.ToString());
        }

        public void SendWarningMessage(string message)
        {
            SendMessage(TestMessageLevel.Warning, message);
        }

        public void SendWarningMessage(string message, Exception ex)
        {
            SendMessage(TestMessageLevel.Warning, message);
            SendMessage(TestMessageLevel.Warning, ex.ToString());
        }

        public void SendInformationalMessage(string message)
        {
            SendMessage(TestMessageLevel.Informational, message);
        }

        public void SendDebugMessage(string message)
        {
            if (Verbosity > 0)
            {
                SendMessage(TestMessageLevel.Informational, message);
            }
        }

        public void SendMessage(TestMessageLevel testMessageLevel, string message)
        {
            if (messageLogger != null)
            {
                testMessageLevel = GetEffectiveMessageLevel(testMessageLevel);
                messageLogger.SendMessage(testMessageLevel, message);
            }
        }
    }
}
