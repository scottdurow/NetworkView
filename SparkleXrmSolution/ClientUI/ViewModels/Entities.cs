// SystemUser.cs
//

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xrm.Sdk;

namespace ClientUI.ViewModels
{
    public class UserOrTeam : Entity
    {
        public static string EntityLogicalName = "systemuser";
        public UserOrTeam()
            : base("systemuser")
        {
            Parties = new Dictionary<string, EntityReference>();
        }

        [ScriptName("fullname")]
        public string FullName;

        [ScriptName("name")]
        public string Name;

        [ScriptName("internalemailaddress")]
        public string InternalEmailAddress;

        public bool IsTeam = false;
        [PreserveCase]
        public Dictionary<string, EntityReference> Parties;
    }

    public class ActivityParty : Entity
    {
        public static string EntityLogicalName = "activityparty";
        public ActivityParty()
            : base("activityparty")
        {
            
        }
        [ScriptName("activityid")]
        public EntityReference ActivityID;
        [ScriptName("partyid")]
        public EntityReference PartyId;
    }

    public class Connection : Entity
    {
         public static string EntityLogicalName = "connection";
        public Connection()
            : base("connection")
        {
            
        }
        [ScriptName("connectionid")]
        public Guid ConnectionId;

        [ScriptName("record1id")]
        public EntityReference Record1Id;

        [ScriptName("record2id")]
        public EntityReference Record2Id;

        [ScriptName("record1roleid")]
        public EntityReference Record1RoleId;

        [ScriptName("record2roleid")]
        public EntityReference Record2RoleId;
    }
}
