// OpportunityRibbon.cs
//

using System;
using System.Collections.Generic;
using System.Html;
using System.Runtime.CompilerServices;
using Xrm;

namespace ClientHooks.Ribbon
{
    public class OpportunityRibbon
    {
        [PreserveCase]
        public static bool SubGridEnableRule()
        {
            //Script.Alert("subgrid enablerule");
            XrmAttribute attribute =  Page.GetAttribute("description");

            return attribute.GetValue()!=null;

        }
        public static void OpenWindow(string type)
        {
            
            string webResourceName = "dev1_/html/ContactEditor.htm";
            string webResourceUrl = Page.Context.GetClientUrl() + "/" + GetWebresourceVersion() + "/WebResources/" + webResourceName;
            switch (type)
            {
                case "normal":

                    Window.Open(webResourceUrl);

                    break;
                case "sdk":
                    Utility.OpenWebResource(webResourceName, null, 400, 400);
                    break;
                case "forcenewwindow":
                    OpenWebResourceInNewWindow(webResourceName, null, 400, 400);
                    break;
                case "inline":
                    OpenWebResourceInLineDialog(webResourceName, null, 400, 400, delegate(object result)
                    {
                        Utility.AlertDialog("inline callback " + result.ToString(), null);
                    });

                    break;


            }

        }

        private static string GetWebresourceVersion()
        {
            string webresourceversion = "";
            if (Script.Literal("typeof(WEB_RESOURCE_ORG_VERSION_NUMBER)") != "undefined")
            {
                webresourceversion = Script.Literal("WEB_RESOURCE_ORG_VERSION_NUMBER").ToString();
            }
            return webresourceversion;
        }
        public static void OpenWebResourceInLineDialog(string webResourceName, string webResourceData, int width, int height, Action<object> callback)
        {

            string webResourceUrl = Page.Context.GetClientUrl() + "/" + GetWebresourceVersion() + "/WebResources/" + webResourceName + "?data=" + webResourceData;
            object dialogwindow = Script.Literal("new Mscrm.CrmDialog(Mscrm.CrmUri.create({0}), window, {1}, {2})", webResourceUrl, width, height);
            Dictionary<string, object> callbackReference = new Dictionary<string, object>("callback", (object)callback);
            Script.Literal("{0}.setCallbackReference({1})", dialogwindow, callbackReference);
            object returnValue = Script.Literal("{0}.show()", dialogwindow);

            if (returnValue != null)
                callback(returnValue);
        }

        public static void OpenWebResourceInNewWindow(string webResourceName, string webResourceData, int width, int height)
        {
            try
            {

                string webResourceUrl = Page.Context.GetClientUrl() + "/" + GetWebresourceVersion() + "/WebResources/" + webResourceName + "?Data=" + webResourceData;
                // Unsupported: Because there is no way of opening a new window (rather than replace the current window)
                // we have to use an unsupported option to popup a new window when viewing the search results. This is so that
                // we don't loose the previous search results
                if (Script.Literal("typeof(window.parent.openStdWin)") != "undefined")
                {

                    string features = (string)Script.Literal("window.parent.buildWindowFeatures({0},{1},{2})", width, height, "scrollbars=1,toolbar=1,menubar=1,location=1");
                    Script.Literal("window.parent.openStdWinWithFeatures({0},{1},{2},{3},{4})", webResourceUrl,
                            null,
                            features,
                            false,
                        null);
                    return;
                }
            }
            catch
            {
                Utility.AlertDialog("Fallback to supported approach",null);
            }
            // Fall back on supported approach
            Utility.OpenWebResource(@"jll_/html/ExternalRedirect.htm", webResourceData, width, height);

        }
        public static void OpenRecordInNewWindow(string id, string typeName)
        {
            try
            {
                // Unsupported: Because there is no way of opening a new window (rather than replace the current window)
                // we have to use an unsupported option to popup a new window when viewing the search results. This is so that
                // we don't loose the previous search results
                if (Script.Literal("typeof(window.parent.openStdWin)") != "undefined")
                {
                    // In the outlook client we can't use main.aspx with openStdWindow because it returns 'The given key was not present in the dictionary.'
                    //string url = "{0}/main.aspx?etn={1}&extraqs=&id=%7b{2}%7d&pagetype=entityrecord";
                    string url = "{0}/userdefined/edit.aspx?etc={3}&id=%7b{2}%7d";

                    // Lookup etc from typeName
                    int? etc = (int?)Script.Literal("Mscrm.EntityPropUtil.EntityTypeName2CodeMap[{0}]", typeName);
                    int height = 1300; // Height of the Record Page.
                    int width = 900; // Width of the Record Page.
                    string windowName = id + typeName;
                    string serverUrl = String.Format(url, Page.Context.GetClientUrl(), typeName, id.Replace("{", "").Replace("}", ""), etc);
                    //buildWindowFeatures(width, height, customWinFeatures);
                    //openStdWinWithFeatures(url, name, features, replace);
                    string features = (string)Script.Literal("window.parent.buildWindowFeatures({0},{1},{2})", width, height, "scrollbars=1,toolbar=1,menubar=1,location=1");
                    Script.Literal("window.parent.openStdWinWithFeatures({0},{1},{2},{3},{4})", serverUrl,
                            windowName,
                            features,
                            false,
                        null);
                    return;
                }
            }
            catch
            {
            }

            // Fall back to a supported option - but this will open in the same window
            Utility.OpenEntityForm(typeName, id, null);


        }
    }
}
