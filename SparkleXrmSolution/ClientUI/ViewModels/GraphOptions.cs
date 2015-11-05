// GraphOptions.cs
//

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ClientUI.ViewModels
{
    [Imported]
    [IgnoreNamespace]
    [ScriptName("Object")]
    public class GraphOptions
    {
        public int IterationCountPerLoad;
        public bool Trace = true;
        public int DemoTickLength;
        public bool? DemoModeInitialState;
        public string AcitvityFetchXml;
        public string ConnectionFetchXml;
        public Dictionary<string, EntitySetting> Entities = new Dictionary<string, EntitySetting>();
        public Dictionary<string, Dictionary<string, string>> QuickViewForms = new Dictionary<string, Dictionary<string, string>>();
    }
}
