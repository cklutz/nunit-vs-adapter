# nunit-vs-adapter
Runs NUnit tests inside the Visual Studio 2012 or 2013 Test Explorer window.

Changes/Additions in this fork:
* Support for parallelism based on test assemblies.
* Setting options in ".runsettings" instead of the Windows Registry

## Support for parallelism based on test assemblies

By default the adapter runs all test (assemblies) sequentially, the specifiy which assemblies should be run in parallel do the following:

Create a ".runsettings" file that contains the following:

```xml
   <?xml version="1.0" encoding="utf-8"?>
   <RunSettings>
      <NUnitTestAdapterSettings>
         <ParallelizeAssemblies>true</ParallelizeAssemblies>
      </NUnitTestAdapterSettings>
   </RunSettings>
```

This tells the adapter that there are (potentially) assemblies to run in parallel.

Next you need to specify which assemblies can be run in parallel. To do so you have two options:

1) In the respective assemblies set an assembly attribute named "NUnitAdapterAssemblyParallelize" (the namespace is irrelevant you can declare your own type for that purpose)

Example:

```C#
    // AssemblyInfo.cs
    // ... other settings
    
    [assembly: Demo.NUnit.Tests.Properties.NUnitAdapterAssemblyParallelize]

    namespace Demo.NUnit.Tests.Properties
    {
       internal class NUnitAdapterAssemblyParallelizeAttribute : Attribute
       {
       }
    }
```

2) Include the assembly file name in the ".runsettings" file:

Example:
```xml
   <?xml version="1.0" encoding="utf-8"?>
   <RunSettings>
    <NUnitTestAdapterSettings>
      <ParallelizeAssemblies>true</ParallelizeAssemblies>
      <ParallelizeAssembliesNames>
        <AssemblyNamePattern>*\*.NUnit.Tests.dll</AssemblyNamePattern>
      </ParallelizeAssembliesNames>
    </NUnitTestAdapterSettings>
   </RunSettings>
```
## Setting options in ".runsettings" instead of the Windows Registry

Various options (ShadowCopy, Verbosity, etc.) are read from a ".runsettings" file rather than the registry.
This not only allows to them to be set for individual test runs, rather then globally, and also makes them more accessible.
The later being of major importance for TFS build servers.

Example:
```xml
   <?xml version="1.0" encoding="utf-8"?>
   <RunSettings>
    <NUnitTestAdapterSettings>
      <Verbosity>1</Verbosity>
    </NUnitTestAdapterSettings>
   </RunSettings>
```

Note: Setting Verbosity to a value of 99+ issues all messages as "Errors", thus making them easily visible in TFS-build reports.
