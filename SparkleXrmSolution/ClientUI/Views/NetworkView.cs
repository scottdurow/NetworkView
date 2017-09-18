// NetworkView.cs
//


using ClientUI.D3Api;
using ClientUI.ViewModels;
using jQueryApi;
using KnockoutApi;
using SparkleXrm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Html;
using System.Runtime.CompilerServices;
using Xrm;
using Xrm.Sdk;

namespace ClientUI.Views
{
    [Imported]
    [IgnoreNamespace]
    [ScriptName("window")]
    public static class GlobalSettings
    {
        // Global Config settings
        [PreserveCase]
        [ScriptName("GraphOptions")]
        public static GraphOptions GraphOptions;

    }
    /// <summary>
    /// Network Graph MVVM View
    /// </summary>
    public class NetworkView
    {
        #region Fields
        public static NetworkView view;
        public NetworkViewModel vm;
        public int width = 960;
        public int height = 500;
        private Number minZoom = 0.2;
        private Number maxZoom = 1.5;
        private EntityNode root;
        private D3Element force;
        private D3Element svg;
        private D3Element link;
        private D3Element node;
        private D3Element dragBehavior;
        private D3Element SVGexactTip;
        private D3Element zoom;
        private Stack<EntityNode> nodeData;
        private Stack<EntityLink> linkData;
        private bool toggle = false;
        private bool stickyInfoBox = true;
        private string currentInfoBoxNode = null;
        private DateTime InfoBoxClosed = DateTime.Now;
        private bool infoBoxPinned = false;
        #endregion     

        #region Constructors
        [PreserveCase]
        public static void Init()
        {
            LocalisedContentLoader.FallBackLCID = 0;
            LocalisedContentLoader.SupportedLCIDs.Add(1033);
            LocalisedContentLoader.LoadContent("dev1_/js/NetworkViewConfig.js", 1033, delegate()
            {
                // Check the webresource parameters
                Dictionary<string, string> data = GetQueryStringData();

                // Webresource accepts the root id and entity type name (etn)
                if (!data.ContainsKey("etn"))
                    data["etn"] = "account";
                if (!data.ContainsKey("id"))
                    data["id"] = "6F456AE5-237A-E711-A953-000D3AB4A3E9";

                Guid id = new Guid(data["id"]);

                NetworkViewModel vm = new NetworkViewModel(id, data["etn"], GlobalSettings.GraphOptions);
                view = new NetworkView(vm);
            });

        }
        public NetworkView(NetworkViewModel viewModel)
        {
            height = jQuery.Window.GetHeight();
            width = jQuery.Window.GetWidth();

            vm = viewModel;

            force = D3.Layout.Force()
               .Size(new int[] { width, height })
               .LinkDistance(150)
               .Friction(0.7)
               .Charge(-700)
               .On("tick", Tick);
    
            zoom = D3.Behavior
                .Zoom()
                .ScaleExtent(new Number[] { minZoom, maxZoom })
                .Center2(new Number[] { width / 2, height / 2 })
                .On("zoom", zoomed);

            svg = D3.Select("#networksvg")
                .Attr("width", width)
                .Attr("height", height)
                .Call(zoom)
                .Append("g");

            dragBehavior = force.OnDrag()
                .On("dragstart", dragstart);

            // Add InfoBox
            SVGexactTip = D3.Select("#infoBox").Style("opacity", 0);

            nodeData = (Stack<EntityNode>)(object)force.Nodes();
            linkData = (Stack<EntityLink>)(object)force.Links();
            link = svg.SelectAll(".link");
            node = svg.SelectAll(".node");

            Update();

            ViewBase.RegisterViewModel(vm);

            // Create event listeners for MVVM
            vm.OnNewNodes += OnNodesChange;
            vm.OnSelectedNodesAdded += vm_OnSelectedNodesAdded;
            vm.OnSelectedNodesCleared += vm_OnSelectedNodesCleared;
            vm.OnSelectedLinksAdded+=vm_OnSelectedLinksAdded;
            vm.OnInfoBoxClose += vm_OnInfoBoxClose;
            vm.OnZoom += vm_OnZoom;
            jQuery.Window.Resize(OnResize);
            jQuery.OnDocumentReady(delegate()
            {
                Window.SetTimeout(delegate()
                {
                    vm.ProcessQueue();
                }, 10);
            });

        }
        #endregion

        #region Event Handlers
        private void vm_OnSelectedLinksAdded(object sender, EventArgs e)
        {
            foreach (string key in vm.HighlightedLinksToRemove.GetItems())
            {
                UnHighlightLink(key);
            }

            vm.HighlightedLinksToRemove.RemoveAll();

            foreach (string key in vm.HighlightedLinks.GetItems())
            {
                HighlightLink(key);
            }
        }
        
        private void vm_OnZoom(object sender, int direction)
        {
            ZoomControl(direction);
        }

        private void vm_OnInfoBoxClose(object sender, EventArgs e)
        {
            HideInfoBox();
        }

        private void vm_OnSelectedNodesCleared(object sender, EventArgs e)
        {
            foreach (EntityReference entity in vm.HighlightedEntities.GetItems())
            {
                UnSelectEntity(entity);
            }
        }
        
        private void vm_OnSelectedNodesAdded(object sender, EventArgs e)
        {
            // Remove unselected items
            foreach (EntityReference entity in vm.HighlightedEntitiesToRemove.GetItems())
            {
                UnSelectEntity(entity);
            }

            vm.HighlightedEntitiesToRemove.RemoveAll();

            foreach (EntityReference entity in vm.HighlightedEntities.GetItems())
            {
                SelectEntity(entity);
            }
        }

        private void OnNodesChange(object sender, EventArgs e)
        {

            if (vm.SuspendLayout)
                return;
            nodeData.Clear();
            List<EntityNode> nodeList = vm.Nodes;
            for (int i = 0; i < nodeList.Count; i++)
            {
                nodeData.Push(nodeList[i]);
            }
            linkData.Clear();
            List<EntityLink> linkList = vm.Links;
            for (int i = 0; i < linkList.Count; i++)
            {
                linkData.Push(linkList[i]);
            }

            Update();
            if (e == null)
            {
                FastForward(force, (decimal)0.01, 100);
            }
            else
            {
                force.Friction(0.01);
                FastForward(force, (decimal)0.09, 100);
                force.Friction(0.9);
                Update();

            }

        }

        private void OnResize(jQueryEvent e)
        {
            int sheight = jQuery.Window.GetHeight();
            int swidth = jQuery.Window.GetWidth();

            D3.Select("#networksvg")
                .Attr<int>("width", swidth)
                .Attr<int>("height", sheight); ;
        }
        public void zoomed(D3Element d, int i)
        {
            svg.Attr("transform", "translate(" + D3.Event.translate + ")scale(" + D3.Event.scale + ")");
        }



        public void dragstart(D3Element d, int i)
        {

            d.Fixed = true;
            D3.Event.sourceEvent.StopPropagation();
        }

        public void dragmove(D3Element d, int i)
        {
            d.px += D3.Event.dx;
            d.py += D3.Event.dy;
            d.X += D3.Event.dx;
            d.Y += D3.Event.dy;
            //D3.Event.sourceEvent.StopPropagation(); 
            Tick(null, 0); // this is the key to make it work together with updating both px,py,x,y on d !
        }

        public void dragend(D3Element d, int i)
        {

            d.Fixed = true; // of course set the node to fixed so the force doesn't include the node in its auto positioning stuff
            Tick(null, 0);
            force.Resume();
        }

        public void Tick(D3Element n, int i)
        {

            link.Attr<Func<EntityLink, Number>>("x1", delegate(EntityLink d) { return d.Source.X; })
              .Attr<Func<EntityLink, Number>>("y1", delegate(EntityLink d) { return d.Source.Y; })
              .Attr<Func<EntityLink, Number>>("x2", delegate(EntityLink d) { return d.Target.X; })
              .Attr<Func<EntityLink, Number>>("y2", delegate(EntityLink d) { return d.Target.Y; });

            node.Attr<Func<EntityNode, Number>>("cx", delegate(EntityNode d) { return d.X; })
              .Attr<Func<EntityNode, Number>>("cy", delegate(EntityNode d) { return d.Y; });


            node.Attr<Func<EntityNode, string>>("transform", delegate(EntityNode d) { return "translate(" + d.X + "," + d.Y + ")"; });


        }

        #endregion

        #region Methods
        private static Dictionary<string, string> GetQueryStringData()
        {
            // Get query string
            // Get the target entity if passed on query string
            if (Window.Location.Search.Length > 0)
            {

                string[] dataParameter = Window.Location.Search.Split("=");
                string data = (string)(object)Script.Literal("decodeURIComponent({0})", dataParameter[1]);
                string[] parameters = data.Split("&");
                Dictionary<string, string> dataPairs = new Dictionary<string, string>();

                foreach (string param in parameters)
                {
                    string[] nameValue = param.Split("=");
                    dataPairs[nameValue[0]] = nameValue[1];


                }
                return dataPairs;
            }
            return new Dictionary<string, string>();
        }

        private static void HighlightLink(string key)
        {
            D3Element link = D3.Select("#" + key);
            if (link!=null)
            {
                link.Attr("filter", "url(#selected-glow)");
                link.Attr("class", "link link-selected"); ;
            }
        }

        private static void UnHighlightLink(string key)
        {
            D3Element link = D3.Select("#" + key);
            if (link != null)
            {
                link.Attr<string>("filter", null);
                link.Attr("class", "link");
            }
        }

        private string GetID(string id)
        {
            return "ID" + id.Replace("-", "").ToLowerCase();
        }

        private void SelectEntity(EntityReference entity)
        {
            string key =  GetID(entity.Id.ToString());
            D3.Select("#" + key).SelectAll(".entityImage").Attr("filter", "url(#selected-glow)");
            D3.Select("#" + key).SelectAll(".entityImage").Transition().Attr("transform", "scale(0.6)").Transition().Attr("transform", "scale(2)");
        }

        private void UnSelectEntity(EntityReference entity)
        {
            string key = GetID(entity.Id.ToString());
            EntityNode node = vm.idIndex[key];
            D3.Select("#" + key).SelectAll(".entityImage").Attr("filter", "url(#no-glow)");
            D3.Select("#" + key).SelectAll(".entityImage").Transition().Attr("transform", "scale(1)");
        }
 
        public void Update()
        {

            this.svg.SelectAll("text").Remove();
            this.svg.SelectAll("image").Remove();
           
            link = link.Data((D3Element)(object)linkData,// null);
            delegate(D3Element d) {
                string id = ((EntityLink)(object)d).Id;
                return id; 
             
            });
            node = node.Data((D3Element)(object)nodeData, delegate(D3Element d) {
                string id = ((Entity)(((EntityNode)(object)d).SourceData)).Id;
               
                return id; 
            });
          
            link.Enter()
                .Insert("svg:line", ".node")
                .Attr<Func<EntityLink, string>>("id", delegate(EntityLink d)
                {
                    return d.Id;
                })       
                .Attr("class", "link");

            node.Enter().Append("svg:g")
                .Attr<Func<EntityNode, string>>("id", delegate(EntityNode d)
                {
                    Entity entity = (Entity)d.SourceData;
                    return GetID(entity.Id);
                })
                .Attr("class", "node")
                .Attr("filter", "url(#blur1)")
                .On("click", delegate(D3Element d, int i)
                {
                    HighlightNode(d);
                    ShowInfoBox(d,true);   
                })

                .On("mouseover", delegate(D3Element d, int i)
                {
                    HighlightNode(d);
                    if (!infoBoxPinned)
                    { 
                        ShowInfoBox(d,false);
                    }
                })

                .On("mouseout", delegate(D3Element d, int i)
                {
                    UnHighlightNode(d);
                    if (stickyInfoBox || infoBoxPinned)
                        return;
                    HideInfoBox();

                })
                .On("dblclick", delegate(D3Element d, int i)
                {    
                    d.Fixed = false;
                    D3.Event.StopPropagation();
                   
                    // Expand overflow nodes if there any
                    EntityNode entityNode = (EntityNode)(object)d;
                    vm.ExpandOverflow(entityNode);
                })
                 .Call(dragBehavior);


            node.Append("svg:image")
                 .Attr("class", "chromeImage")
                 .Attr<Func<EntityNode, string>>("xlink:href", delegate(EntityNode d)
                    {
                        Entity entity = ((Entity)d.SourceData);
                        switch (entity.LogicalName)
                        {
                            case "account":
                            case "contact":
                                return "../images/network.png";
                            default:
                                return null;
                        }
                    }
                )
                 .Attr<Func<EntityNode, string>>("x", delegate(EntityNode d) { return GetXY(d, (decimal)1.5); })
                 .Attr<Func<EntityNode, string>>("y", delegate(EntityNode d) { return GetXY(d, (decimal)1.5); })
                 .Attr<Func<EntityNode, string>>("width", delegate(EntityNode d) { return GetHeightWidth(d, (decimal)1.5); })
                 .Attr<Func<EntityNode, string>>("height", delegate(EntityNode d) { return GetHeightWidth(d, (decimal)1.5); })
                 .Attr<Func<EntityNode, string>>("visibility", delegate(EntityNode d)
                    {
                        Entity entity = ((Entity)d.SourceData);
                        switch (entity.LogicalName)
                        {
                            case "account":
                            case "contact":
                                return null;
                            default:
                                return "hidden";
                        }
                    })
                 .Attr<Func<EntityNode, string>>("filter", delegate(EntityNode d)
                     {
                         return GetFilter(d);
                     });
               
            node.Append("svg:image")
             .Attr("class", "entityImage")
             .Attr<Func<EntityNode, string>>("xlink:href", delegate(EntityNode d)
                {
                    Entity entity = ((Entity)d.SourceData);
                    switch (entity.LogicalName)
                    {
                        case "overflow":
                            return "../images/overflow.png";
                        case "account":
                            return "../images/account.png";
                        case "contact":
                            return "../images/contact.png";
                        case "incident":
                            return "/_imgs/Navbar/ActionImgs/Cases_32.png";
                        case "contract":
                            return "/_imgs/Navbar/ActionImgs/Contract_32.png";
                        case "opportunity":
                            return "/_imgs/Navbar/ActionImgs/Opportunity_32.png";
                        case "lead":
                            return "/_imgs/Navbar/ActionImgs/Lead_32.png";
                        case "phonecall":
                            return "/_imgs/Navbar/ActionImgs/PhoneCall_32.png";
                        case "email":
                            return "/_imgs/Navbar/ActionImgs/Email_32.png";
                        case "task":
                            return "/_imgs/Navbar/ActionImgs/Task_32.png";
                        case "appointment":
                            return "/_imgs/Navbar/ActionImgs/Appointment_32.png";
                        default:
                            // Custom entity image
                            return "/_imgs/Navbar/ActionImgs/Documents_32.png";
                    }
                })
             .Attr<Func<EntityNode, string>>("x", delegate(EntityNode d) { return GetXY(d, (decimal)0.5); })
             .Attr<Func<EntityNode, string>>("y", delegate(EntityNode d) { return GetXY(d, (decimal)0.5); })
             .Attr<Func<EntityNode, string>>("width", delegate(EntityNode d) { return GetHeightWidth(d, (decimal)0.5); })
             .Attr<Func<EntityNode, string>>("height", delegate(EntityNode d) { return GetHeightWidth(d, (decimal)0.5); })
             .Attr("filter", "url(#blur2)");

            node.Append("svg:text")
             .Attr("class", "nodetext")
             .Attr<Func<EntityNode, int>>("dx", delegate(EntityNode d)
                {
                    Entity entity = ((Entity)d.SourceData);
                    switch (entity.LogicalName)
                    {
                        case "overflow":
                            return -3;
                        default:
                            return -15;

                    }

                })
             .Attr<Func<EntityNode, int>>("dy", delegate(EntityNode d)
                {
                    Entity entity = ((Entity)d.SourceData);
                    switch (entity.LogicalName)
                    {
                        case "overflow":
                            return 3;
                        default:
                            return -15;
                    }
                })
             .Text(delegate(EntityNode d)
                {
                    Entity entity = (Entity)d.SourceData;
                    if (entity.LogicalName == "overflow")
                    {
                        if (d.Children != null)
                        {
                            return d.Children.Count.ToString();
                        }
                        return "";
                    }
                    else
                    {
                        EntitySetting entitySetting = vm.Config.Entities[entity.LogicalName];
                        // If there is no name attribute setting, we'll use the name attribute so you can add an alias.
                        string name = entity.GetAttributeValueString(entitySetting!=null && entitySetting.NameAttribute!=null ? entitySetting.NameAttribute : "name");
                        if (name != null && name.Length > 50) name = name.Substr(0, 50) + "...";
                        return name;
                    }

                });

            // Exit any old links.
            this.link.Exit().Remove();
            this.node.Exit().Remove();
            force.Start();

            Dictionary<string, string> uniqueKeyCache = new Dictionary<string, string>();
            foreach (EntityLink l in vm.Links)
            {
                string id = l.Id;
                if (uniqueKeyCache.ContainsKey(id))
                    Debug.WriteLine("Duplicate key " + id);
                else
                    uniqueKeyCache[id] = id;
            }

            foreach (EntityNode l in vm.Nodes)
            {
                string id = ((Entity)(((EntityNode)(object)l).SourceData)).Id;
                
                if (uniqueKeyCache.ContainsKey(id))
                    Debug.WriteLine("Duplicate key " + id);
                else
                    uniqueKeyCache[id] = id;
            }
        }

        private static string GetFilter(EntityNode d)
        {
            if (d.Root)
                return "url(#root-node-glow)";
            else
                return "";
        }

        private void HideInfoBox()
        {
            infoBoxPinned = false;
            SVGexactTip.Style("opacity", "0");
            SVGexactTip.Transition()
              .Style("left",
                      -1000 + "px")
              .Style("top",
                      -1000 + "px");

        }
       
        private void HighlightNode(D3Element d)
        {

            D3Element thisnode1 = D3.SelectFromElement(D3.Event.Target.ParentNode).SelectAll(".chromeImage");
            D3Element thisnode2 = D3.SelectFromElement(D3.Event.Target.ParentNode).SelectAll(".entityImage");

            thisnode1.Transition().Attr("transform", "scale(1.2)");
            thisnode2.Transition().Attr("transform", "scale(2)");
        }

        private void UnHighlightNode(D3Element d)
        {
            D3Element thisnode = D3.SelectFromElement(D3.Event.Target.ParentNode).SelectAll("image");
            thisnode.Transition().Attr("transform", "scale(1)");
        }

        private void ShowInfoBox(D3Element d, bool clicked)
        {
           
            EntityNode entityNode = (EntityNode)(object)d;
            vm.SelectedNode.SetValue(entityNode);

            if (clicked)
            {
                if (((Entity)entityNode.SourceData).Id == currentInfoBoxNode)
                {
                   
                    infoBoxPinned = false;
                }
                else
                {
                    infoBoxPinned = true;
                    currentInfoBoxNode = ((Entity)entityNode.SourceData).Id;
                }
            }
          
            SVGexactTip.Style("opacity", "1");

            D3Element thisnode = D3.SelectFromElement(D3.Event.Target.ParentNode).SelectAll("image");
            D3Element matrix = thisnode.Node().GetScreenCTM()
               .Translate(thisnode.Node().GetAttribute("cx"),
                        thisnode.Node().GetAttribute("cy"));

            int swidth = jQuery.Window.GetWidth();
            int sheight = jQuery.Window.GetHeight();

            int left = (Window.PageXOffset + matrix.e) + 50;
            int top = (Window.PageYOffset + matrix.f) - 10;
            if (top + 100 > sheight)
            {
                top = sheight - 100;
            }
            // Show the infobox on the left
            left = 20;
            SVGexactTip.Transition()
                .Style("left",
                        left + "px")
                .Style("top",
                        top + "px");
        }

        public string GetXY(EntityNode node, decimal multipler)
        {
            int size = 0;
            Entity entity = ((Entity)node.SourceData);
            size = GetSize(node);
            return "-" + (size * multipler).ToString() + "px";
        }

        private static int GetSize(EntityNode node)
        {
            Entity entity = ((Entity)node.SourceData);
            int size = 0;
            switch (entity.LogicalName)
            {
                case "account":
                    size = 15;

                    break;
                case "contact":
                    size = 10;
                    break;
                default:

                    size = 20;
                    break;

            }
            // Make bigger if the root node
            size = size + (node.Root ? 2 : 0);
            return size;
        }

        public string GetHeightWidth(EntityNode node, decimal multipler)
        {
            int size = 0;
            Entity entity = ((Entity)node.SourceData);
            size = GetSize(node) * 2;
            
            return (size * multipler).ToString() + "px";
        }

        public void FastForward(D3Element layout, decimal alpha, int max)
        {
            int i = 0;
            while ((layout.Alpha() > alpha) && (i < max) && !vm.CancelRequested)
            {
                layout.Tick();
                i++;
            }
        }

        private void ZoomControl(int direction)
        {
            svg.Call((D3Element)(object)zoom.Event);

            // Record the coordinates (in data space) of the center (in screen space).
            Number[] center0 = zoom.Center();
            Number[] translate0 = zoom.Translate2();
            Number[] coordinates0 = coordinates(center0);
            Number scale = zoom.GetScale();
            scale = scale * Math.Pow(1.5, +direction);
            if (scale <= minZoom)
                scale = minZoom;
            else if (scale >= maxZoom)
                scale = maxZoom;

            zoom.Scale(scale);

            // Translate back to the center.
            Number[] center1 = point(coordinates0);
            zoom.Translate3(new Number[] { translate0[0] + center0[0] - center1[0], translate0[1] + center0[1] - center1[1] });

            svg.Transition().Duration(750).Call((D3Element)(object)zoom.Event);
        }

        private Number[] coordinates(Number[] point)
        {
            Number scale = zoom.GetScale();
            Number[] translate = zoom.Translate2();
            return new Number[] { (point[0] - translate[0]) / scale, (point[1] - translate[1]) / scale };
        }

        private Number[] point(Number[] coordinates)
        {
            Number scale = zoom.GetScale();
            Number[] translate = zoom.Translate2();
            return new Number[] { coordinates[0] * scale + translate[0], coordinates[1] * scale + translate[1] };
        }
        #endregion
    }
}
