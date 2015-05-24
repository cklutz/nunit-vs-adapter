// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

// #define LAUNCHDEBUGGER

namespace NUnit.VisualStudio.TestAdapter
{
    [ExtensionUri(ExecutorUri)]
    public sealed class NUnitTestExecutor : NUnitTestAdapter, ITestExecutor, IDisposable
    {
        ///<summary>
        /// The Uri used to identify the NUnitExecutor
        ///</summary>
        public const string ExecutorUri = "executor://NUnitTestExecutor";

        // The currently executing assembly runner(s)
        private readonly Dictionary<string, AssemblyRunner> activeRunners = new Dictionary<string, AssemblyRunner>();
        private AssemblyRunner currentRunner;


        #region ITestExecutor

        /// <summary>
        /// Called by the Visual Studio IDE to run all tests. Also called by TFS Build
        /// to run either all or selected tests. In the latter case, a filter is provided
        /// as part of the run context.
        /// </summary>
        /// <param name="sources">Sources to be run.</param>
        /// <param name="runContext">Context to use when executing the tests.</param>
        /// <param name="frameworkHandle">Test log to send results and messages through</param>
        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var settings = GetSettings(runContext);
            TestLog.Initialize(frameworkHandle, settings);
            Info("executing tests", "started");

            try
            {
                // Ensure any channels registered by other adapters are unregistered
                CleanUpRegisteredChannels();

                var tfsfilter = new TfsTestFilter(runContext);
                TestLog.SendDebugMessage("Keepalive:" + runContext.KeepAlive);
                var enableShutdown = (!settings.UseVsKeepEngineRunning) || !runContext.KeepAlive;
                if (!tfsfilter.HasTfsFilterValue)
                {
                    if (!(enableShutdown && !runContext.KeepAlive))  // Otherwise causes exception when run as commandline, illegal to enableshutdown when Keepalive is false, might be only VS2012
                        frameworkHandle.EnableShutdownAfterTestRun = enableShutdown;
                }

                bool parallelizeAssemblies = settings.ParallelizeAssemblies;
                var sourceAssemblies = new List<string>();
                var parallelSourceAssemblies = new List<string>();
                foreach (string sourceAssembly in GetSourceAssemblies(sources))
                {
                    if (!parallelizeAssemblies)
                    {
                        sourceAssemblies.Add(sourceAssembly);
                    }
                    else
                    {
                        if (!TryAddBySettingsPattern(sourceAssembly, parallelSourceAssemblies, settings))
                        {
                            if (!TryAddByCustomAttribute(sourceAssembly, parallelSourceAssemblies, settings))
                            {
                                Info(string.Format("Assembly '{0}' marked for sequential testing.", sourceAssembly));
                                sourceAssemblies.Add(sourceAssembly);
                            }
                        }
                    }
                }

                if (parallelSourceAssemblies.Any())
                {
                    RunParallel(parallelSourceAssemblies, frameworkHandle, tfsfilter, settings);
                }

                RunSequential(sourceAssemblies, frameworkHandle, tfsfilter, settings);
            }
            catch (Exception ex)
            {
                TestLog.SendErrorMessage("Exception thrown executing tests", ex);
            }
            finally
            {
                Info("executing tests", "finished");
            }
        }
        
        private static IEnumerable<string> GetSourceAssemblies(IEnumerable<string> sources)
        {
            foreach (var source in sources)
            {
                if (!Path.IsPathRooted(source))
                {
                    yield return Path.Combine(Environment.CurrentDirectory, source);
                }

                yield return source;
            }
        }

        private bool TryAddBySettingsPattern(string sourceAssembly, ICollection<string> parallelSourceAssemblies, NUnitTestAdapterSettings settings)
        {
            string pattern = settings.ParallelizeAssembliesNames.FirstOrDefault(n => GlobHelper.IsMatch(sourceAssembly, n));
            if (pattern != null)
            {
                Info(string.Format("Assembly '{0}' marked for parallel testing (by name pattern match '{1}').", sourceAssembly, pattern));
                parallelSourceAssemblies.Add(sourceAssembly);
                return true;
            }
            return false;
        }

        private bool TryAddByCustomAttribute(string sourceAssembly, ICollection<string> parallelSourceAssemblies, NUnitTestAdapterSettings settings)
        {
            var assembly = Assembly.ReflectionOnlyLoadFrom(sourceAssembly);
            foreach (var data in assembly.GetCustomAttributesData())
            {
                if (data.ToString().Contains(settings.ParallelizeAssembliesUnqualifiedAttributeName))
                {
                    Info(string.Format("Assembly '{0}' marked for parallel testing (by assembly attribute match '{1}').", sourceAssembly, data));
                    parallelSourceAssemblies.Add(sourceAssembly);
                    return true;
                }
            }
            return false;
        }

        private void RunSequential(IEnumerable<string> sources, IFrameworkHandle frameworkHandle, TfsTestFilter tfsfilter, NUnitTestAdapterSettings settings)
        {
            foreach (var source in sources)
            {
                using (currentRunner = new AssemblyRunner(TestLog, source, tfsfilter, this))
                {
                    currentRunner.RunAssembly(frameworkHandle, settings);
                }
                currentRunner = null;
            }
        }

        private void RunParallel(IEnumerable<string> sources, IFrameworkHandle frameworkHandle, TfsTestFilter tfsfilter, NUnitTestAdapterSettings settings)
        {
            Info("using parallel by assembly mode");
            var options = new ParallelOptions { MaxDegreeOfParallelism = settings.MaxDegreeOfParallelism };
            Parallel.ForEach(sources, options, source =>
            {
                AssemblyRunner runner = null;
                try
                {
                    runner = new AssemblyRunner(TestLog, source, tfsfilter, this);
                    lock (activeRunners)
                    {
                        activeRunners.Add(source, runner);
                    }

                    runner.RunAssembly(frameworkHandle, settings);
                }
                finally
                {
                    if (runner != null)
                    {
                        runner.Dispose();
                        lock (activeRunners)
                        {
                            activeRunners.Remove(source);
                        }
                    }
                }
            });
        }
        
        /// <summary>
        /// Called by the VisualStudio IDE when selected tests are to be run. Never called from TFS Build.
        /// </summary>
        /// <param name="tests">The tests to be run</param>
        /// <param name="runContext">The RunContext</param>
        /// <param name="frameworkHandle">The FrameworkHandle</param>
        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
#if LAUNCHDEBUGGER
            Debugger.Launch();
#endif
            var settings = GetSettings(runContext);
            TestLog.Initialize(frameworkHandle, settings);

            var enableShutdown = (!settings.UseVsKeepEngineRunning) || !runContext.KeepAlive;
            frameworkHandle.EnableShutdownAfterTestRun = enableShutdown;
            Debug("executing tests", "EnableShutdown set to " + enableShutdown);
            Info("executing tests", "started");

            // Ensure any channels registered by other adapters are unregistered
            CleanUpRegisteredChannels();

            var assemblyGroups = tests.GroupBy(tc => tc.Source);
            foreach (var assemblyGroup in assemblyGroups)
            {
                using (currentRunner = new AssemblyRunner(TestLog, assemblyGroup.Key, assemblyGroup, this))
                {
                    currentRunner.RunAssembly(frameworkHandle, settings);
                }

                currentRunner = null;
            }

            Info("executing tests", "finished");
        }

        void ITestExecutor.Cancel()
        {
            lock (activeRunners)
            {
                foreach (var runner in activeRunners.Values)
                {
                    runner.CancelRun();
                }
            }

            if (currentRunner != null)
            {
                currentRunner.CancelRun();
            }
        }

        #endregion

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (activeRunners)
                {
                    foreach (var runner in activeRunners.Values)
                    {
                        runner.Dispose();
                    }
                    activeRunners.Clear();
                }

                if (currentRunner != null)
                {
                    currentRunner.Dispose();
                }
            }
            currentRunner = null;
        }
    }
}
