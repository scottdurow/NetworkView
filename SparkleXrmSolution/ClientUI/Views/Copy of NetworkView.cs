// NetworkView.cs
//


using ClientUI.D3Api;
using ClientUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ClientUI.Views
{
    public class NetworkView2
    {
        public static NetworkView2 view;
        public int width = 960;
        public int height = 500;
        public EntityNode root;
        public D3Element force;
        public D3Element svg;
        public D3Element link;
        public D3Element node;

        [PreserveCase]
        public static void Init()
        {
            view = new NetworkView2();
        }
        public NetworkView2()
        {
            force = D3.Layout.Force()
               .Size(new int[] { width, height })
               .On("tick", Tick);

    //        var force = d3.layout.force()
    //.size([width, height])
    //.on("tick", tick);

            svg = D3.Select("body").Append("svg")
               .Attr("width", width)
               .Attr("height", height);

        //var svg = d3.select("body").append("svg")
        //    .attr("width", width)
        //    .attr("height", height);

            link = svg.SelectAll(".link");
            node = svg.SelectAll(".node");

        //var link = svg.selectAll(".link"),
        //    node = svg.selectAll(".node");

            //root = new ForceNode("Test", 100);
            //Update();

        D3.Json("../js/json_data.js", delegate(object state,object json) {
          root = (EntityNode)json;
          Update();
        });



        }
        public void Update()
        {
            D3Element nodes = Flattern(root);
            D3Element links = D3.Layout.Tree().Links2(nodes);
            //var nodes = flatten(root),
            //links = d3.layout.tree().links(nodes);
            force.Nodes2(nodes)
                .Links2(links)
                .Start();

              //// Restart the force layout.
              //force
              //    .nodes(nodes)
              //    .links(links)
              //    .start();

            link = link.Data(links, delegate(D3Element d) { return d.Target.Id; });

              //// Update the links…
              //link = link.data(links, function(d) { return d.target.id; });
            link.Exit().Remove();
              //// Exit any old links.
              //link.exit().remove();
            link.Enter().Insert("line", ".node")
                .Attr("class", "link")
                .Attr<Func<EntityLink, int>>("x1", delegate(EntityLink d) { return d.Source.X; })
                .Attr<Func<EntityLink, int>>("y1", delegate(EntityLink d) { return d.Source.Y; })
                .Attr<Func<EntityLink, int>>("x2", delegate(EntityLink d) { return d.Target.X; })
                .Attr<Func<EntityLink, int>>("y2", delegate(EntityLink d) { return d.Target.Y; });
            
              //// Enter any new links.
              //link.enter().insert("line", ".node")
              //    .attr("class", "link")
              //    .attr("x1", function(d) { return d.source.x; })
              //    .attr("y1", function(d) { return d.source.y; })
              //    .attr("x2", function(d) { return d.target.x; })
              //    .attr("y2", function(d) { return d.target.y; });

              //// Update the nodes…
              //node = node.data(nodes, function(d) { return d.id; }).style("fill", color);
            node = node.Data(nodes, delegate(D3Element d) { return d.Id; }).
                Style("fill", Color);
              //// Exit any old nodes.
              node.Exit().Remove();

              //// Enter any new nodes.
              node.Enter().Append("circle")
                  .Attr("class", "node")
                  .Attr<Func<EntityNode, Number>>("cx", delegate(EntityNode d) { return d.X; })
                  .Attr<Func<EntityNode, Number>>("cy", delegate(EntityNode d) { return d.Y; })
                  .Attr<Func<EntityNode, Number>>("r", delegate(EntityNode d) { return (d.Size != null) ? Math.Sqrt(d.Size) / 10 : 4.5; })
                  .Style("fill", Color)
                  .On("click", click)
                  .Call(force.Drag);
        }
        // Color leaf nodes orange, and packages white or blue.
        //function color(d) {
        //  return d._children ? "#3182bd" : d.children ? "#c6dbef" : "#fd8d3c";
        //}
        public string Color(D3Element e)
        {
            EntityNode d = (EntityNode)(object)e;
            return (d._Children != null) ? "#3182bd" : d.Children != null ? "#c6dbef" : "#fd8d3c";
        }
        public void Tick(D3Element n, int i)
        {
            link.Attr<Func<EntityLink, Number>>("x1", delegate(EntityLink d) { return d.Source.X; })
              .Attr<Func<EntityLink, Number>>("y1", delegate(EntityLink d) { return d.Source.Y; })
              .Attr<Func<EntityLink, Number>>("x2", delegate(EntityLink d) { return d.Target.X; })
              .Attr<Func<EntityLink, Number>>("y2", delegate(EntityLink d) { return d.Target.Y; });

            node.Attr<Func<EntityNode, Number>>("cx", delegate(EntityNode d) { return d.X; })
              .Attr<Func<EntityNode, Number>>("cy", delegate(EntityNode d) { return d.Y; });
        }

        // Toggle children on click.
        public void click(D3Element n, int i) {
          if (!D3.Event.DefaultPrevented) {
              EntityNode d = (EntityNode)(object)n;
            if (d.Children!=null) {
              d._Children = d.Children;
              d.Children = null;
            } else {
              d.Children = d._Children;
              d._Children = null;
            }
            Update();
          }
        }
        public D3Element Flattern(EntityNode root)
        {
              //var nodes = [], i = 0;
            Stack<EntityNode> nodes = new Stack<EntityNode>();
            int i = 0;
            ListCallback<EntityNode> recurse = null;
            recurse = delegate(EntityNode node, int index, IReadonlyCollection<EntityNode> list)
        {
            if (node.Children != null)
            {
                node.Children.ForEach(recurse);
            }
            if (node.Id == null)
            {
                i = i + 1;
                node.Id = i.ToString();
            }
            nodes.Push(node);
            
        };
            recurse(root,0,null);
            return (D3Element)(object)nodes;
            //function recurse(node) {
            //  if (node.children) node.children.forEach(recurse);
            //  if (!node.id) node.id = ++i;
            //  nodes.push(node);
            //}

            //recurse(root);
            //return nodes;
            
        }
        
    }
}
