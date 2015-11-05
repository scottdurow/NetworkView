// FormCell.cs
//

using System;
using System.Collections.Generic;

namespace ClientUI.ViewModels
{
    public class FormCell
    {
        public FormCell(string label, string value)
        {
            Label = label;
            Value = value;
        }
        public string Label;
        public string Value;
    }
}
