// XrmForm.cs
//

using System;
using System.Collections.Generic;
using System.Html;
using Xrm;

namespace ClientUI.ViewModels
{
    public class XrmForm
    {
        public static void OpenRecordInNewWindow(string id, string typeName)
        {
            // We need to open in a new window and openEntityForm will replace the current window.
            // This can be replaced when Carina is released
            try
            {
                // In the outlook client we can't use main.aspx with openStdWindow because it returns 'The given key was not present in the dictionary.'
                // string url = "{0}/main.aspx?etn={1}&extraqs=&id=%7b{2}%7d&pagetype=entityrecord";
                string url = "{0}/userdefined/edit.aspx?etc={3}&id=%7b{2}%7d";
                int? etc = (int?)Script.Literal("Mscrm.EntityPropUtil.EntityTypeName2CodeMap[{0}]", typeName);
                int height = 1300; // Height of the Record Page.
                int width = 900; // Width of the Record Page.
                string serverUrl = String.Format(url, Page.Context.GetClientUrl(), typeName, id.Replace("{", "").Replace("}", ""), etc);
                string name = id.Replace("{", "").Replace("}", "").Replace("-", "");
                WindowInstance win = Window.Open(serverUrl, "drillOpen", String.Format("height={0},width={1}", height, width));
                Script.Literal("{0}.focus()",win);
                //Utility.OpenWebResource(@"dev1_/html/Redirect.htm", serverUrl, width, height);
            }
            catch { }
        }
    }
}
