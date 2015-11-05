// D3Api.cs
//

using ClientUI.ViewModels;
using jQueryApi;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ClientUI.D3Api
{
    [Imported]
    [IgnoreNamespace]
    [ScriptName("d3")]
    public static class D3
    {
        [ScriptName("layout")]
        public static D3Element Layout;
        public static D3Event Event;
        public static D3Element Behavior;
        [ScriptName("select")]
        public static D3Element SelectFromElement(object element)
        {
            return null;
        }
        public static D3Element Select(string elementName)
        {
            return null;
        }
        public static void Json(string url, Action<object,object> callback)
        {
           
        }
    }
    [Imported]
    [IgnoreNamespace]
    public abstract class D3Event 
    {
        public Number dx;
        public Number dy;
        public Number PageX;
        public Number PageY;
        public Number clientX;
        public Number clientY;
        public Number[] translate;
        public Number scale;
        public D3Event sourceEvent;
        public Number x;
        public Number y;
        public Number keyCode;
        public bool altKey;
        public string type;
        public bool DefaultPrevented;
        public D3Element Target;
        public abstract void StopPropagation();
    }



   
    [Imported]
    [IgnoreNamespace]
    public abstract class D3Element
    {
        
        public Number a;
        public Number b;
        public Number c;
        public Number d;
        public Number e;
        public Number f;

        public string Id;
        public Number px;
        public Number py;
        public Number X;
        public Number Y;
        public bool Fixed;
        public D3Element Target;
        public D3Element Source;
        public D3Event Event;
        public abstract D3Element Tick();
        public abstract D3Element Transition();
        public abstract D3Element Duration(int duration);
        public abstract decimal Alpha();
        public abstract D3Element Size(int[] widthheight);
        //public abstract ForceElement On(string eventName, Action hander);
        public abstract D3Element On(string eventName, D3Delegate call);
        public abstract D3Element Force();
        public abstract D3Element Tree();
        [ScriptName("nodes")]
        public abstract D3Element Nodes2(D3Element nodes);
        [ScriptName("links")]
        public abstract D3Element Links2(D3Element links);

        public abstract D3Element Nodes();
        public abstract D3Element Node();
        public  D3Element ParentNode;
        public  D3Element ParentElement;
        public abstract D3Element Links();
        public abstract D3Element LinkDistance(int p);
        public abstract D3Element Charge(int p);
        public abstract D3Element Friction(Number f);
        public abstract D3Element Gravity(Number p);
        public D3Element Drag;

        [ScriptName("drag")]
        public abstract D3Element OnDrag();
        public abstract D3Element Zoom();
        public abstract D3Element ScaleExtent(Number[] extent);
        [ScriptName("scale")]
        public abstract Number GetScale();
        public abstract D3Element Scale(Number scale);

        public abstract void Resume();


        public abstract D3Element Data(D3Element links, Func<D3Element, string> getId);

        public abstract D3Element Exit();

        public abstract D3Element Start();
        public abstract D3Element Stop();
        public abstract D3Element Remove();

        public abstract D3Element Enter();

        public abstract D3Element Insert(string p1, string p2);

        public abstract D3Element Append(string elementName);


        public abstract D3Element Attr<T>(string name, T value);

        public abstract D3Element Text(Func<EntityNode,String> getTextDelegate);

        public abstract D3Element SelectAll(string path);

        public abstract D3Element Style(string style, Func<D3Element, string> call);
        public abstract D3Element Style(string style, string value);
        public abstract D3Element Style(string style, Number value);
     

        public abstract D3Element Call(D3Element func);


        public abstract D3Element GetTransformToElement(D3Element tooltipParent);
        public abstract D3Element Translate(Number x, Number y);
        [ScriptName("translate")]
        public abstract Number[] Translate2();
         [ScriptName("translate")]
        public abstract D3Element Translate3(Number[] vector);
        public abstract Number[] Center();
        [ScriptName("center")]
        public abstract D3Element Center2(Number[] center);
        public abstract Number GetAttribute(string p);


        public abstract D3Element AppendChild(D3Element element);


        public abstract D3Element Html(string html);


        public abstract D3Element GetScreenCTM();
       
    }
    public delegate void D3Delegate(D3Element d, int i);
    

}
