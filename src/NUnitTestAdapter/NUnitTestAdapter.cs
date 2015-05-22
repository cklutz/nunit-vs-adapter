// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using NUnit.Core;
using NUnit.Util;

namespace NUnit.VisualStudio.TestAdapter
{
    public interface INUnitTestAdapter
    {
        TestPackage CreateTestPackage(string sourceAssembly, NUnitTestAdapterSettings settings);
    }

    /// <summary>
    /// NUnitTestAdapter is the common base for the
    /// NUnit discoverer and executor classes.
    /// </summary>
    public abstract class NUnitTestAdapter : INUnitTestAdapter
    {
        // Our logger used to display messages
        protected TestLogger TestLog;
        // The adapter version
        private readonly string adapterVersion;

        #region Constructor

        /// <summary>
        /// The common constructor initializes NUnit services 
        /// needed to load and run tests and sets some properties.
        /// </summary>
        protected NUnitTestAdapter()
        {
            ServiceManager.Services.AddService(new DomainManager());
            ServiceManager.Services.AddService(new ProjectService());

            ServiceManager.Services.InitializeServices();

            adapterVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            TestLog = new TestLogger();
        }

        #endregion

        #region Protected Helper Methods

        private const string Name = "NUnit VS Adapter";

        protected NUnitTestAdapterSettings GetSettings(IDiscoveryContext discoveryContext)
        {
            if (discoveryContext == null || discoveryContext.RunSettings == null)
                return new NUnitTestAdapterSettings();

            return GetSettings(discoveryContext.RunSettings);
        }

        protected NUnitTestAdapterSettings GetSettings(IRunContext runContext)
        {
            if (runContext == null || runContext.RunSettings == null)
                return new NUnitTestAdapterSettings();

            return GetSettings(runContext.RunSettings);
        }

        protected NUnitTestAdapterSettings GetSettings(IRunSettings runSettings)
        {
            try
            {
                Info("Trying to get settings");
                var provider = runSettings.GetSettings(AdapterConstants.SettingsName) as NUnitTestAdapterSettingsProvider;
                var settings = provider != null ? provider.Settings : new NUnitTestAdapterSettings();
                return settings;
            }
            catch (Exception ex)
            {
                Error("Error getting settings", ex);
                throw;
            }
        }

        protected void Info(string str)
        {
            var msg = string.Format("{0} {1} {2}", Name, adapterVersion, str);
            TestLog.SendInformationalMessage(msg);
            Trace.WriteLine(msg);
        }

        protected void Error(string str, Exception ex = null)
        {
            if (ex != null)
            {
                var msg = string.Format("{0} {1} {2}: {3}", Name, adapterVersion, str, ex);
                TestLog.SendErrorMessage(msg);
                Trace.WriteLine(msg);
            }
            else
            {
                var msg = string.Format("{0} {1} {2}", Name, adapterVersion, str);
                TestLog.SendErrorMessage(msg);
                Trace.WriteLine(msg);
            }
        }

        protected void Info(string method, string function)
        {
            var msg = string.Format("{0} {1} {2} is {3}",Name, adapterVersion, method, function);
            TestLog.SendInformationalMessage(msg);
            Trace.WriteLine(msg);
        }

        protected void Debug(string method, string function)
        {
            var msg = string.Format("{0} {1} {2} is {3}", Name, adapterVersion, method, function);
            TestLog.SendDebugMessage(msg);
            Trace.WriteLine(msg);
        }

        protected static void CleanUpRegisteredChannels()
        {
            foreach (IChannel chan in ChannelServices.RegisteredChannels)
                ChannelServices.UnregisterChannel(chan);
        }

        public TestPackage CreateTestPackage(string sourceAssembly, NUnitTestAdapterSettings settings)
        {
             var package = new TestPackage(sourceAssembly);
             package.Settings["ShadowCopyFiles"] = settings.ShadowCopy;
             TestLog.SendDebugMessage("ShadowCopyFiles is set to :" + package.Settings["ShadowCopyFiles"]);
             return package;
        }

        #endregion
    }

    
}
