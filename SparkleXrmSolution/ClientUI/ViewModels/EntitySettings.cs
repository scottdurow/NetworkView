// EntitySettings.cs
//

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ClientUI.ViewModels
{
    [Imported]
    [IgnoreNamespace]
    [ScriptName("Object")]
    public class EntitySetting
    {
        public string DisplayName;
        public string LogicalName;
        public string NameAttribute;
        public string IdAttribute;
        public string FetchXml;
        public string ParentAttributeId;
        public bool LoadActivities;
        public bool LoadConnections;
        public bool Hierarchical;
        public JoinSetting[] Joins;
    }
}
