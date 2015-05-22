using System;
using System.ComponentModel.Composition;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace NUnit.VisualStudio.TestAdapter
{
    [Export(typeof(ISettingsProvider))]
    [SettingsName(AdapterConstants.SettingsName)]
    public class NUnitTestAdapterSettingsProvider : ISettingsProvider
    {
        private readonly XmlSerializer serializer;

        public NUnitTestAdapterSettingsProvider()
        {
            serializer = new XmlSerializer(typeof(NUnitTestAdapterSettings));
            Name = AdapterConstants.SettingsName;
            Settings = new NUnitTestAdapterSettings();
        }

        public string Name { get; private set; }
        public NUnitTestAdapterSettings Settings { get; private set; }

        public void Load(XmlReader reader)
        {
            if (reader.Read() && reader.Name.Equals(AdapterConstants.SettingsName))
            {
                Settings = (NUnitTestAdapterSettings)serializer.Deserialize(reader);
            }
        }
    }
}