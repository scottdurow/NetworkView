// TestPageViewModel.cs
//

using KnockoutApi;
using SparkleXrm;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xrm;

namespace ClientUI.ViewModels
{
    public class TestPageViewModel : ViewModelBase
    {
        [PreserveCase]
        public Observable<string> Messages;
        private List<string[]> list;

       

        public TestPageViewModel(List<string[]> list)
        {
        
            this.list = list;

            Messages = Knockout.Observable<string>("");

            AddMessage("Query string data parameter:");
            // Add parameters
            foreach (string[] param in list)
            {
                AddMessage(String.Format("{0}={1}", param[0], param[1]));
            }
        }

        public void AddMessage(string message)
        {
            Messages.SetValue(Messages.GetValue() + message + "\n");

        }

        public void OpenUserCustomCommand()
        {
            // Get current user
            string userId = Page.Context.GetUserId();
            OpenRecordInNewWindow(userId, "systemuser");


        }
        public void OpenUserSDKCommand()
        {
            // Get current user
            string userId = Page.Context.GetUserId();
            Utility.OpenEntityForm("systemuser", userId, null);


        }
             
        public static void OpenRecordInNewWindow(string id, string typeName)
        {
        try
            {
                // Unsupported: Because there is no way of opening a new window (rather than replace the current window)
                // we have to use an unsupported option to popup a new window when viewing the search results. This is so that
                // we don't loose the previous search results
                if (Script.Literal("typeof(window.parent.openStdWinWithFeatures)") != "undefined")
                {
                    // In the outlook client we can't use main.aspx with openStdWindow because it returns 'The given key was not present in the dictionary.'
                    //string url = "{0}/main.aspx?etn={1}&extraqs=&id=%7b{2}%7d&pagetype=entityrecord";
                    string url = "{0}/userdefined/edit.aspx?etc={3}&id=%7b{2}%7d";

                    // Lookup etc from typeName
                    int? etc = (int?)Script.Literal("Mscrm.EntityPropUtil.EntityTypeName2CodeMap[{0}]", typeName);
                    int height = 1300; // Height of the Record Page.
                    int width = 900; // Width of the Record Page.
                    string windowName = (id + typeName).Replace("-","").Replace("_","").Replace(" ",""); // Can't have these chars in window target
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
                Utility.AlertDialog("Fallback to SDK", null);
            }

            // Fall back to a supported option - but this will open in the same window
            Utility.OpenEntityForm(typeName, id, null);
       
       
        }
    }

}
