using System;
using Xrm;


namespace NetworkView.ClientHooks.Ribbon
{
    public static class AccountRibbon
    {
        public static void OpenNetworkView()
        {

            string data = string.Format("id={0}", Page.Data.Entity.GetId());
            data = GlobalFunctions.encodeURIComponent(data);
            Utility.OpenWebResource("dev1_/html/NetworkView.htm", data, 1024, 768);
        }
    }
}
