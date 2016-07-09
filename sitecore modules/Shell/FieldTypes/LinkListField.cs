using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Web.UI;
using System.Xml;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Resources;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;

namespace Monoco.CMS.FieldTypes
{
    public class LinkListField : Sitecore.Web.UI.HtmlControls.Control, IContentField
    {
        private const string InternalLinkDialogUrl = "/sitecore/shell/Applications/Dialogs/Internal Link.aspx";
        private const string ExternalLinkDialogUrl = "/sitecore/shell/Applications/Dialogs/External Link.aspx";
        private const string MediaLinkDialogUrl = "/sitecore/shell/Applications/Dialogs/Media Link.aspx";
        
        private string _itemid;
        private string Source
        {
            get
            {
                return base.GetViewStateString("source");
            }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                string text = MainUtil.UnmapPath(value);
                if (text.EndsWith("/"))
                {
                    text = text.Substring(0, text.Length - 1);
                }
                SetViewStateString("source", text);
            }
        }
        private XmlValue XmlValue
        {
            get
            {
                return new XmlValue(GetViewStateString("XmlValue"), "link");
            }
            set
            {
                SetViewStateString("XmlValue", value.ToString());
            }
        }
        public string ItemID
        {
            get
            {
                return _itemid;
            }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                _itemid = value;
            }
        }
        public LinkListField(Sitecore.Data.Fields.Field field)
        {

        }
        public LinkListField()
        {
            Class = "scContentControlMultilist";
            Background = "white";
            Activation = true;
        }
        public void EditLink(ClientPipelineArgs args)
        {
            var index = System.Convert.ToInt32(args.Parameters["index"]);
            var node = GetNodeByIndex(GetDocument(), index);

            if (args.IsPostBack)
            {

                if (!string.IsNullOrEmpty(args.Result) && args.Result != "undefined")
                {
                    string result = args.Result;

                    var xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(result);
                    string selectText = this.GetSelectText(xmlDocument.SelectSingleNode("link"));

                    var xmlDocument2 = new XmlDocument();
                    xmlDocument2.LoadXml(GetValue());

                    xmlDocument2.DocumentElement.ReplaceChild(
                        xmlDocument2.ImportNode(xmlDocument.SelectSingleNode("link"), true),
                        GetNodeByIndex(xmlDocument2, index)
                        );

                    SetValue(xmlDocument2.OuterXml);
                    SetModified();

                    Sitecore.Context.ClientPage.ClientResponse.Eval(
                        string.Concat(new string[]
                            {
                                "scContent.linklistUpdateLink('", ID, "', {index: " + index + ", text:'", selectText,
                                "'})"
                            })
                        );
                }
            }
            else
            {
                var urlString = new UrlString(args.Parameters["url"]);

                // Sitecore 7.2 uses urlHandle to transfer the va variable, 
                // so if we're 7.2 or above, use UrlHandle, otherwise, use urlString
                var isPre72Version = Sitecore.Configuration.Settings.GetBoolSetting("Monoco.CMS.Linklist.SitecoreVersionIsPre7.2", false);
                var isPre75Version = Sitecore.Configuration.Settings.GetBoolSetting("Monoco.CMS.Linklist.SitecoreVersionIsPre7.5", false);
                if (isPre72Version || (isPre75Version && args.Parameters["url"] == InternalLinkDialogUrl))
                {
                    urlString.Append("va", node.OuterXml);
                }
                else
                {
                    var urlHandle = new UrlHandle();
                    urlHandle["va"] = node.OuterXml;
                    urlHandle.Add(urlString);
                }
                urlString.Append("ro", Source);
                Sitecore.Context.ClientPage.ClientResponse.ShowModalDialog(urlString.ToString(), true);
                args.WaitForPostBack();
            }
        }
        /// <summary>
        /// Inserts a new link into the link list.
        /// </summary>
        /// <param name="args">Sitecore Client arguments.</param>
        public void InsertLink(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            if (args.IsPostBack)
            {
                // Make sure that the result was not empty or undefined (result when user cancels out of link dialog)
                if (!string.IsNullOrEmpty(args.Result) && args.Result != "undefined")
                {
                    // Get the result from the link dialog
                    var result = args.Result;
                    var xmlDocument = new XmlDocument();

                    xmlDocument.LoadXml(result);
                    var selectText = this.GetSelectText(xmlDocument.SelectSingleNode("link"));
                    var xmlDocument2 = new XmlDocument();
                    xmlDocument2.LoadXml(this.GetValue());

                    var linksNode = xmlDocument2.SelectSingleNode("links");
                    if (linksNode != null)
                    {
                        // import the node into the document and set the value.
                        var linkNode = xmlDocument.SelectSingleNode("link");
                        if (linkNode != null)
                        {
                            linksNode.AppendChild(xmlDocument2.ImportNode(linkNode, true));
                            SetValue(xmlDocument2.OuterXml);
                            SetModified();
                        }
                    }

                    //xmlDocument2.SelectSingleNode("links").AppendChild(xmlDocument2.ImportNode(xmlDocument.SelectSingleNode("link"), true));
                    

                    // Call Sitecore client to update the link list client side.
                    Sitecore.Context.ClientPage.ClientResponse.Eval(string.Concat(new string[]
					{
						"scContent.linklistInsertLink('", ID, "', {text:'", selectText, "'})" }));
                }
            }
            else
            {
                // Show the dialog using ShowModalDialog.
                var urlString = new UrlString(args.Parameters["url"]);
                
                // Sitecore 7.2 uses urlHandle to transfer the va variable, 
                // so if we're 7.2 or above, use UrlHandle, otherwise, use urlString
                var isPre72Version = Sitecore.Configuration.Settings.GetBoolSetting("Monoco.CMS.Linklist.SitecoreVersionIsPre7.2", false);
                var isPre75Version = Sitecore.Configuration.Settings.GetBoolSetting("Monoco.CMS.Linklist.SitecoreVersionIsPre7.5", false);
                if (isPre72Version || (isPre75Version && args.Parameters["url"] == InternalLinkDialogUrl))
                {
                    urlString.Append("va", XmlValue.ToString());
                }
                else
                {
                    var urlHandle = new UrlHandle();
                    urlHandle["va"] = XmlValue.ToString();
                    urlHandle.Add(urlString);
                }
                urlString.Append("ro", Source);
                Sitecore.Context.ClientPage.ClientResponse.ShowModalDialog(urlString.ToString(), true);
                args.WaitForPostBack();
            }
        }
        /// <summary>
        /// Message handler for Sitecore events.
        /// </summary>
        /// <param name="message">Message from Sitecore client.</param>
        public override void HandleMessage(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            base.HandleMessage(message);
            if (message["id"] == ID && message.Name != null)
            {
                string name = message.Name;

                switch (name)
                {
                    case "action:edit":
                        // Arguments:
                        // index: index of the node to move
                        EditNode(int.Parse(message.Arguments["index"]));
                        break;
                    case "action:move":
                        // Arguments
                        // direction: up|down direction to move the node
                        // index: index of the node to move
                        var direction = message.Arguments["direction"];
                        var nodeIndex = int.Parse(message.Arguments["index"]);
                        MoveNodeByIndex(direction, nodeIndex);
                        break;
                    case "action:delete":
                        // Arguments from message is comma delimited list of index positions of nodes to remove.
                        var remove = message.Arguments["remove"];
                        
                        if (!string.IsNullOrEmpty(remove))
                        {
                            var array = remove.Split(new[] { ',' });
                            var array2 = array;
                            for (var i = 0; i < array2.Length; i++)
                            {
                                var s = array2[i];
                                var index = int.Parse(s);
                                RemoveEntryByIndex(index);

                                Sitecore.Context.ClientPage.ClientResponse.Eval(
                                string.Concat(new []
                                    {
                                        "scContent.linklistRemoveLink('", ID, "', {index: " + index + "})"
                                    })
                                );
                            }
                        }
                        break;
                    case "contentlink:internallink":
                        Insert(InternalLinkDialogUrl);
                        break;
                    case "contentlink:media":
                        Insert(MediaLinkDialogUrl);
                        break;
                    case "contentlink:externallink":
                        Insert(ExternalLinkDialogUrl);
                        break;
                }

            }
        }
        private void EditNode(int index)
        {
            string url = String.Empty;

            var node = GetNodeByIndex(GetDocument(), index);

            // Get node type to display the correct dialog.
            switch (node.Attributes["linktype"].Value)
            {
                case "external":
                    url = ExternalLinkDialogUrl;
                    break;
                case "internal":
                    url = InternalLinkDialogUrl;
                    break;
                case "media":
                    url = MediaLinkDialogUrl;
                    break;
            }

            // Create a url string and start the EditLink method using pipeline.
            Assert.ArgumentNotNull(url, "url");
            var parameters = new NameValueCollection
                                 {
                                     { "url", url },
                                     { "index", index.ToString(CultureInfo.InvariantCulture) }
                                 };
            Sitecore.Context.ClientPage.Start(this, "EditLink", parameters);
        }
        /// <summary>
        /// Moves the specified node <see cref="nodeIndex"/> in <see cref="direction"/>.
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="nodeIndex"></param>
        private void MoveNodeByIndex(string direction, int nodeIndex)
        {
            var document = GetDocument();
            var nodeByIndex = GetNodeByIndex(document, nodeIndex);
            if (direction == "up")
            {
                document.SelectSingleNode("links").InsertBefore(nodeByIndex, nodeByIndex.PreviousSibling);
            }
            else
            {
                document.SelectSingleNode("links").InsertAfter(nodeByIndex, nodeByIndex.NextSibling);
            }
            SetValue(document.OuterXml);
            SetModified();
        }
        private void RemoveEntryByIndex(int index)
        {
            var document = GetDocument();
            var nodeByIndex = GetNodeByIndex(document, index);
            document.SelectSingleNode("/links").RemoveChild(nodeByIndex);
            SetValue(document.OuterXml);
        }
        private XmlNode GetNodeByIndex(XmlDocument document, int index)
        {
            return document.SelectSingleNode("/links/link[position() = " + (index + 1) + "]");
        }

        private void Insert(string url)
        {
            Assert.ArgumentNotNull(url, "url");
            var parameters = new NameValueCollection
			{

				{
					"url",
					url
				}
			};
            Sitecore.Context.ClientPage.Start(this, "InsertLink", parameters);
        }
        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            string text = Sitecore.Context.ClientPage.ClientRequest.Form[ID + "_value"];
            if (text != null)
            {
                if (GetViewStateString("Value", string.Empty) != text)
                {
                    SetModified();
                }
                SetViewStateString("Value", text);
            }
        }
        protected override void OnPreRender(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnPreRender(e);
            base.ServerProperties["Value"] = base.ServerProperties["Value"];
        }
        protected override void DoRender(HtmlTextWriter output)
        {
            Assert.ArgumentNotNull(output, "output");

            // Create the markup for the <select>...
            output.AddAttribute(HtmlTextWriterAttribute.Class, "scContentControl");
            output.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
            output.RenderBeginTag(HtmlTextWriterTag.Table); // <table>
            output.RenderBeginTag(HtmlTextWriterTag.Tr); // <tr>
            output.RenderBeginTag(HtmlTextWriterTag.Td); // <td>
            output.AddAttribute(HtmlTextWriterAttribute.Id, string.Format("{0}_List", this.ID));
            output.AddAttribute(HtmlTextWriterAttribute.Size, "10");
            output.AddAttribute(HtmlTextWriterAttribute.Class, "scContentControl");
            output.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
            output.AddAttribute("ondblclick", String.Format("scContent.linklistDblClick('{0}')", ID));

            output.RenderBeginTag(HtmlTextWriterTag.Select); // <select>

            var nodes = GetDocument().SelectNodes("/links/link");
            if (nodes != null)
            {
                // Render all link nodes
                foreach (XmlNode selectNode in nodes)
                {
                    output.RenderBeginTag(HtmlTextWriterTag.Option);
                    output.Write(GetSelectText(selectNode));
                    output.RenderEndTag();
                }
            }

            output.RenderEndTag(); // </select>
            output.RenderEndTag(); // </td>
            output.AddAttribute(HtmlTextWriterAttribute.Width, "16");
            output.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
            output.RenderBeginTag(HtmlTextWriterTag.Td);

            // Add tool buttons for moving, removing and editing nodes.
            RenderButton(output, "Core/16x16/arrow_blue_up.png", "javascript:scContent.linklistMoveUp('" + ID + "')");
            output.Write("<br />");

            RenderButton(output, "Core/16x16/arrow_blue_down.png", "javascript:scContent.linklistMoveDown('" + ID + "')");
            output.Write("<br />");

            RenderButton(output, "Applications/16x16/delete.png", string.Format("javascript:scContent.linklistDelete('{0}')", ID));
            output.Write("<br />");

            RenderButton(output, "Imaging/16x16/pencil.png", string.Format("javascript:scContent.linklistEdit('{0}')", ID));
            output.RenderEndTag(); // </td>
            output.RenderEndTag(); // </tr>
            output.RenderEndTag(); // </table>
        }
        /// <summary>
        /// Formats the list item text based on a link node.
        /// </summary>
        /// <param name="selectNode"></param>
        /// <returns></returns>
        private string GetSelectText(XmlNode node)
        {
            return string.Format("{0} ({1}: {2})",
                GetAttribute(node, "text"), 
                GetLinkType(node),
                GetLinkUrl(node));
        }

        /// <summary>
        /// Creates markup for a tool button.
        /// </summary>
        /// <param name="output"></param>
        /// <param name="icon"></param>
        /// <param name="click"></param>
        private void RenderButton(HtmlTextWriter output, string icon, string click)
        {
            Assert.ArgumentNotNull(output, "output");
            Assert.ArgumentNotNull(icon, "icon");
            Assert.ArgumentNotNull(click, "click");
            var imageBuilder = new ImageBuilder
            {
                Src = icon,
                Width = 16,
                Height = 16,
                Margin = "2px",
                OnClick = click
            };

            output.Write(imageBuilder.ToString());
        }
        /// <summary>
        /// Gets the link url attribute of a node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private object GetLinkUrl(XmlNode node)
        {
            return GetAttribute(node, "url");
        }
        /// <summary>
        /// Gets the link type attribute of a link node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private object GetLinkType(XmlNode node)
        {
            return GetAttribute(node, "linktype");
        }
        /// <summary>
        /// Gets an attribute from the node.
        /// </summary>
        /// <param name="selectNode"></param>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private string GetAttribute(XmlNode node, string name, string defaultValue = "")
        {
            string result;
            if (node != null && node.Attributes[name] != null)
            {
                result = node.Attributes[name].Value;
            }
            else
            {
                result = defaultValue;
            }
            return result;
        }
        /// <summary>
        /// Creates a new XmlDocument with nodes based on the field's value.
        /// </summary>
        /// <returns></returns>
        public XmlDocument GetDocument()
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(GetValue());
            return xmlDocument;
        }
        /// <summary>
        /// Returns the field's value (or an empty XML string)-
        /// </summary>
        /// <returns></returns>
        public string GetValue()
        {
            string result;
            if (!string.IsNullOrEmpty(Value)) { result = Value; }
            else { result = "<links />"; }
            
            return result;
        }
        /// <summary>
        /// Stores the field's value.
        /// </summary>
        /// <param name="value"></param>
        public void SetValue(string value)
        {
            if (Value != value)
            {
                SetModified();
            }
            Value = value;
        }
        /// <summary>
        /// Indicates to the Sitecore client that the content has been modified.
        /// </summary>
        protected void SetModified()
        {
            Sitecore.Context.ClientPage.Modified = true;
        }
    }
}
