// JoinSetting.cs
//

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ClientUI.ViewModels
{
    [Imported]
    [IgnoreNamespace]
    [ScriptName("Object")]
    public class JoinSetting
    {
        public string LeftEntity;
        public string RightEntity;
        public string LeftAttribute;
        public string RightAttribute;
        public string NameAttribute;
        public string Name;
        public Dictionary<string, string> ExcludeIds;
    }
}
