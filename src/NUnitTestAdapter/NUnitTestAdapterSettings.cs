using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Core;

namespace NUnit.VisualStudio.TestAdapter
{
    public class NUnitTestAdapterSettings : TestRunSettings
    {
        private static readonly XmlSerializer s_serializer = new XmlSerializer(typeof(NUnitTestAdapterSettings));

        public NUnitTestAdapterSettings()
            : base(AdapterConstants.SettingsName)
        {
            ParallelizeAssembliesUnqualifiedAttributeName = "NUnitAdapterAssemblyParallelize";
            ParallelizeAssembliesNames = new List<string>();
        }

        [XmlArray]
        [XmlArrayItem(ElementName = "AssemblyNamePattern")]
        public List<string> ParallelizeAssembliesNames { get; set; }
        public string ParallelizeAssembliesUnqualifiedAttributeName { get; set; }
        public bool ParallelizeAssemblies { get; set; }

        public bool UseVsKeepEngineRunning { get; set; }
        public bool ShadowCopy { get; set; }
        public int Verbosity { get; set; }
        public LoggingThreshold LoggingThreshold { get; set; }

        public override XmlElement ToXml()
        {
            using (var sw = new StringWriter())
            {
                s_serializer.Serialize(sw, this);
                var doc = new XmlDocument();
                doc.LoadXml(s_serializer.ToString());
                return doc.DocumentElement;
            }
        }
    }
}