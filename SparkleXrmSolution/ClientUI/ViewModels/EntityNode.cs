// ForceNode.cs
//

using ClientUI.D3Api;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xrm.Sdk;

namespace ClientUI.ViewModels
{
    public class EntityNodeLink
    {
        public EntityNodeLink(EntityNode node,EntityLink link,int weight)
        {
            Target = node;
            Weight = weight;
            Link = link;

        }
        public EntityNode Target;
        public EntityLink Link;
        public int Weight;

    }
    public class EntityNode 
    {
        public EntityNode(string name, int size)
        {
            Name = name;
            Size = size;
           
        }
        public string Name;
        public Number Size;
        public string Id;
        public Number X;
        public Number Y;
        public object SourceData;
        public EntityNode OverflowNode;
        public EntityNode ReplacedByOverflow;
        public List<EntityNode> Children;
        public List<EntityNode> _Children;
        public List<EntityLink> Links = new List<EntityLink>();
        public Dictionary<string, EntityNodeLink> LinkedToIds;
        public bool LoadedActivities = false;
        public bool LoadedConnections = false;
        public int ActivityCount = 0;
        public bool IsActivity = false;
        public EntityNode ParentNode;
        public bool Fixed = false;
        public bool Root;
        [PreserveCase]
        public List<FormCell> GetQuickViewForm(NetworkViewModel vm)
        {
            Entity record = (Entity)SourceData;
            if (record == null)
                return null;
            List<FormCell> form = new List<FormCell>();

            // Get the Quick Form defintion 
            if (vm.Config.QuickViewForms.ContainsKey(record.LogicalName))
            {
                foreach (string field in vm.Config.QuickViewForms[record.LogicalName].Keys)
                {
                    string label = vm.Config.QuickViewForms[record.LogicalName][field];
                    string value = GetStringValue(field, record);
                    form.Add(new FormCell(label, value));

                }
            }
          
            return form;
        }

        private static string GetStringValue(string field,Entity record)
        {
            string stringValue = null;
            if (field == null)
                return null;


            if (record.FormattedValues.ContainsKey(field + "name"))
            {
                return record.FormattedValues[field + "name"];
            }
            else
            {
                object value = record.GetAttributeValue(field);
                if (value == null)
                    return stringValue;
                Type valueType = value.GetType();
                switch (valueType.Name)
                {
                    case "EntityReference":
                        stringValue = ((EntityReference)value).Name.ToString();
                        break;
                    
                    default:
                       
                        stringValue = value.ToString();
                       
                        break;
                }
            }
                
            
            return stringValue;
        }
    }
}
