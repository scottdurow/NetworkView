// TestPageView.cs
//

using ClientUI.ViewModels;
using SparkleXrm;
using System;
using System.Collections.Generic;
using System.Html;
using System.Runtime.CompilerServices;

namespace ClientUI.Views
{
    public class TestPageView
    {
        public static TestPageViewModel _vm;

        [PreserveCase]
        public static void Init()
        {
            _vm = new TestPageViewModel(GetQueryStringData());
            ViewBase.RegisterViewModel(_vm);
        }

        private static List<string[]> GetQueryStringData()
        {
            // Get query string
            // Get the target entity if passed on query string
            if (Window.Location.Search.Length > 0)
            {
                List<string[]> paramCollection = new List<string[]>();
                string[] dataParameter = Window.Location.Search.Split("&");
                //string data = (string)(object)Script.Literal("decodeURIComponent({0})", dataParameter[1]);
                //string[] parameters = data.Split("&");
                foreach (string param in dataParameter)
                {
                    string[] nameValue = param.Split("=");
                    paramCollection.Add(nameValue);
                }
                return paramCollection;
            }
            return null;
        }
    }
}
