using System;
using Xrm;


namespace NetworkView.ClientHooks.Ribbon
{
    public static class RibbonCommands
    {
        public static void OpenNetworkViewFromForm()
        {
            OpenNetworkView(Page.Data.Entity.GetId(), Page.Data.Entity.GetEntityName());   
        }
        public static void OpenNetworkView(string id, string etn)
        {
            string data = string.Format("id={0}&etn={1}", id, etn);
            data = GlobalFunctions.encodeURIComponent(data);
            Utility.OpenWebResource("dev1_/html/NetworkView.htm", data, 1024, 768);
        }
    }
}
