/*
 * phosphorus five, copyright 2014 - thomas@magixilluminate.com
 * phosphorus five is licensed as mit, see the enclosed LICENSE file for details
 */

using System;
using System.IO;
using System.Web.UI;
using System.Reflection;
using System.Collections.Generic;
using phosphorus.types;
using core = phosphorus.ajax.core;

namespace phosphorus.ajax.widgets
{
    /// <summary>
    /// general ajax html element
    /// </summary>
    public abstract class Widget : Control, IAttributeAccessor
    {
        private bool _render;
        private bool _renderInvisible;
        private List<core.Attribute> _beforeChanges;

        /// <summary>
        /// initializes a new instance of the <see cref="phosphorus.ajax.widgets.Widget"/> class
        /// </summary>
        public Widget ()
        {
            Attributes = new List<core.Attribute> ();
            _beforeChanges = new List<core.Attribute> ();
        }

        /// <summary>
        /// gets or sets the tag name used to render the html element
        /// </summary>
        /// <value>the tag name</value>
        public string Tag {
            get { return ViewState["Tag"] as string; }
            set {
                ViewState ["Tag"] = value;
            }
        }

        /// <summary>
        /// gets or sets the tag name used to render the html element when it is invisible
        /// </summary>
        /// <value>the tag name</value>
        public string InvisibleTag {
            get { return ViewState ["InvisibleTag"] == null ? "span" : ViewState["InvisibleTag"] as string; }
            set { ViewState ["InvisibleTag"] = value; }
        }

        /// <summary>
        /// gets or sets a value indicating whether this <see cref="phosphorus.ajax.widgets.Widget"/> should close an empty tag immediately.
        /// the default value for this property is false, which mean that if the element has no content, either as children controls, or 
        /// as innerHTML, then the tag to render the element will still create an end tag element. sometimes this is not what you wish, 
        /// though for most elements this is according to the standard. if you have a "void element", meaning an element that cannot have 
        /// any content at all, such as img, br, hr and such, then you can set this value to true, and the tag will be "self-closed". please 
        /// also be aware of the HasEndTag property, which often can be set to false for the same type of html elements that are void elements
        /// </summary>
        /// <value><c>true</c> if it closes an empty tag immediately; otherwise, <c>false</c></value>
        public bool SelfClosed {
            get { return ViewState ["SelfClosed"] == null ? false : (bool)ViewState["SelfClosed"]; }
            set { ViewState ["SelfClosed"] = value; }
        }

        /// <summary>
        /// gets or sets a value indicating whether this instance has an end tag or not. some elements does not require an end tag 
        /// at all, examples here are p, td, li, and so on. for these tags you can choose to explicitly turn off the rendering of the 
        /// end tag, and such save a couple of bytes of http traffic. the default value of this property is true
        /// </summary>
        /// <value><c>true</c> if this instance has an end tag; otherwise, <c>false</c></value>
        public bool HasEndTag {
            get { return ViewState ["HasEndTag"] == null ? true : (bool)ViewState["HasEndTag"]; }
            set { ViewState ["HasEndTag"] = value; }
        }

        /// <summary>
        /// gets the attributes for the widget
        /// </summary>
        /// <value>the attributes</value>
        protected List<core.Attribute> Attributes {
            get;
            private set;
        }

        /// <summary>
        /// gets or sets the named attribute for the widget. notice that attribute might exist, even if 
        /// return value is null, since attributes can have "null values", such as for instance "controls" 
        /// for the html5 video element, or the "disabled" attribute on form elements. if you wish to 
        /// check for the existence of an attribute, then use the HasAttribute method. if you wish 
        /// to remove an attribute, use the RemoveAttribute
        /// </summary>
        /// <param name="key">attribute to retrieve or set</param>
        public string this [string key] {
            get { return GetAttribute (key); }
            set { SetAttribute (key, value); }
        }

        public override bool Visible {
            get {
                return base.Visible;
            }
            set {
                if (!base.Visible && value && IsTrackingViewState && IsPhosphorusRequest) {
                    // this control was made visible during this request and should be rendered as html
                    // unless any of its ancestors are invisible
                    _render = true;
                } else if (base.Visible && !value && IsTrackingViewState && IsPhosphorusRequest) {
                    // this control was made invisible during this request and should be rendered 
                    // with its invisible  html unless any of its ancestors are invisible
                    _renderInvisible = true;
                }
                base.Visible = value;
            }
        }

        protected override void LoadViewState (object savedState)
        {
            object[] tmp = savedState as object[];
            base.LoadViewState (tmp [0]);

            // adding attributes that previously have been changed
            core.Attribute[] atrs = tmp [1] as core.Attribute [];
            foreach (core.Attribute idx in atrs) {
                Attributes.RemoveAll (
                    delegate(core.Attribute idx2) {
                        return idx.Name == idx2.Name;
                    });
                idx.InViewState = true;
            }
            Attributes.AddRange (atrs);

            // removing attributes that have been previously removed
            if (tmp [2] != null) {
                foreach (core.Attribute idx in tmp [2] as core.Attribute []) {
                    Attributes.RemoveAll (
                        delegate(core.Attribute idx2) {
                            return idx.Name == idx2.Name;
                        });
                }
            }

            // duplicating attributes to diff against attributes to render such that we can see which 
            // attributes to remove during rendering
            if (IsPhosphorusRequest)
                _beforeChanges.AddRange (Attributes);
        }

        protected override void LoadControlState (object state)
        {
            object[] obj = state as object[];
            Visible = (bool)obj [0];
            base.LoadControlState (obj [1]);
        }

        protected override object SaveViewState ()
        {
            object[] retVal = new object [3];
            retVal [0] = base.SaveViewState ();

            // dirty attributes and attributes that are already in the viewstate due to previous changes
            List<core.Attribute> dirty = Attributes.FindAll (
                delegate(core.Attribute idx) {
                    return idx.Dirty || idx.InViewState;
                });
            retVal [1] = dirty.ToArray ();

            // attributes that have been removed
            if (_beforeChanges != null) {
                List<core.Attribute> removed = _beforeChanges.FindAll (
                    delegate(core.Attribute idx) {
                        return !Attributes.Exists (
                            delegate(core.Attribute idx2) {
                                return idx.Name == idx2.Name;
                            });
                    });
                retVal [2] = removed.ToArray ();
            }
            return retVal;
        }

        protected override object SaveControlState ()
        {
            object[] obj = new object [2];
            obj [0] = Visible;
            obj [1] = base.SaveControlState ();
            return obj;
        }
        
        public override void RenderControl (HtmlTextWriter writer)
        {
            if (AreAncestorsVisible ()) {
                if (Visible) {
                    if (IsPhosphorusRequest && !IsAncestorRendering ()) {
                        if (_render) {
                            Node tmp = new Node (ClientID);
                            tmp ["outerHTML"].Value = GetWidgetHtml ();
                            (Page as core.IAjaxPage).Manager.RegisterWidgetChanges (tmp);
                        } else {
                            // only place where we really return "true json updates" back to control
                            Node tmp = GetJsonChanges ();
                            if (tmp.Count > 0) {
                                (Page as core.IAjaxPage).Manager.RegisterWidgetChanges (tmp);
                            }
                            RenderChildren (writer);
                        }
                    } else {
                        RenderHtmlResponse (writer);
                    }
                } else {
                    if (IsPhosphorusRequest && _renderInvisible && !IsAncestorRendering ()) {
                        Node tmp = new Node (ClientID);
                        tmp ["outerHTML"].Value = GetWidgetInvisibleHtml ();
                        (Page as core.IAjaxPage).Manager.RegisterWidgetChanges (tmp);
                    } else {
                        writer.Write (GetWidgetInvisibleHtml ());
                    }
                }
            }
        }

        /// <summary>
        /// returns the json changes that should be returned back to client
        /// </summary>
        /// <returns>the changes the widget has during this request</returns>
        protected virtual Node GetJsonChanges ()
        {
            Node tmp = new Node (ClientID);
            foreach (core.Attribute idx in Attributes) {
                if (idx.Dirty) {
                    tmp [idx.Name].Value = new Node (idx.OldValue, idx.Value);
                }
            }
            foreach (core.Attribute idx in _beforeChanges) {
                if (!Attributes.Exists (
                    delegate(core.Attribute obj) {
                    return obj.Name == idx.Name;
                })) {
                    tmp ["__pf_delete"].Value = idx.Name;
                }
            }
            if (ViewState.IsItemDirty ("Tag")) {
                tmp ["tagName"].Value = Tag;
            }
            return tmp;
        }

        protected override void OnInit (EventArgs e)
        {
            Page.RegisterRequiresControlState(this);
            base.OnInit (e);

            if (Page.IsPostBack)
                LoadFormData ();

            if (IsPhosphorusRequest) {

                if (Page.Request.Params ["__pf_widget"] == ClientID) {

                    Page.LoadComplete += delegate {
                        // event was raised for this widget
                        InvokeEventHandler (Page.Request.Params ["__pf_event"]);
                    };
                }
            }
        }

        /// <summary>
        /// loads the form data from the http request, override this in your own widgets if you 
        /// have widgets that posts data to the server
        /// </summary>
        protected virtual void LoadFormData ()
        {
            // only elements which are not disabled should post data
            if (this ["disabled"] == null) {
                // only elements with a given name attribute should post data
                if (!string.IsNullOrEmpty (this ["name"])) {

                    // different types of html elements loads their data in different ways
                    switch (Tag.ToLower ()) {
                    case "input":
                        switch (this ["type"]) {
                        case "radio":
                        case "checkbox":
                            if (!string.IsNullOrEmpty (Page.Request.Params [this ["name"]]))
                                this ["checked"] = null;
                            break;
                        default:
                            this ["value"] = Page.Request.Params [this ["name"]];
                            break;
                        }
                        break;
                    case "textarea":
                        this ["innerHTML"] = Page.Request.Params [this ["name"]];
                        break;
                    case "select":
                        string value = Page.Request.Params [this ["name"]];
                        foreach (Control idx in Controls) {
                            Widget widget = idx as Widget;
                            if (widget != null) {
                                if (widget ["value"] == value) {
                                    widget ["selected"] = "true";
                                } else {
                                    widget.Attributes.RemoveAll (
                                        delegate(core.Attribute idxAtr) {
                                            return idxAtr.Name == "selected";
                                        });
                                }
                            }
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// returns the html for the widget, override if you need to create your own custom html in your widgets
        /// </summary>
        /// <returns>the widget's html</returns>
        protected virtual string GetWidgetHtml ()
        {
            using (MemoryStream stream = new MemoryStream ()) {
                using (HtmlTextWriter txt = new HtmlTextWriter (new StreamWriter (stream))) {
                    RenderHtmlResponse (txt);
                    txt.Flush ();
                }
                stream.Seek (0, SeekOrigin.Begin);
                using (TextReader reader = new StreamReader (stream)) {
                    return reader.ReadToEnd ();
                }
            }
        }

        /// <summary>
        /// returns the invisible widget's html
        /// </summary>
        /// <returns>the html to render when widget is not visible</returns>
        protected virtual string GetWidgetInvisibleHtml ()
        {
            return string.Format (@"<{0} id=""{1}"" style=""display:none important!;""></{0}>", InvisibleTag, ClientID);
        }

        /// <summary>
        /// returns true if this request is an ajax request
        /// </summary>
        /// <value><c>true</c> if this instance is an ajax request; otherwise, <c>false</c></value>
        public bool IsPhosphorusRequest {
            get { return !string.IsNullOrEmpty (Page.Request.Params ["__pf_event"]); }
        }

        /// <summary>
        /// invokes the given event handler
        /// </summary>
        /// <param name="eventHandlerName">event handler name</param>
        protected virtual void InvokeEventHandler (string eventName)
        {
            string eventHandlerName = null;
            if (HasAttribute (eventName)) {
                // probable "onclick" or other types of automatically generated mapping between server method and javascript handler
                eventHandlerName = this [eventName];
            } else {
                // WebMethod invocation
                eventHandlerName = eventName;
            }
            Control owner = this.Parent;
            while (!(owner is Page) && !(owner is UserControl) && !(owner is MasterPage))
                owner = owner.Parent;
            MethodInfo method = owner.GetType ().GetMethod (eventHandlerName, 
                                                            BindingFlags.Instance | 
                                                            BindingFlags.Public | 
                                                            BindingFlags.NonPublic | 
                                                            BindingFlags.FlattenHierarchy);
            if (method == null)
                throw new NotImplementedException ("method + '" + eventHandlerName + "' could not be found");

            // need to verify method has WebMethod attribute
            object[] atrs = method.GetCustomAttributes (typeof (core.WebMethod), false /* for security reasons we want method to be explicitly marked as WebMethod */);
            if (atrs == null || atrs.Length == 0)
                throw new AccessViolationException ("method + '" + eventHandlerName + "' is illegal to invoke over http");

            method.Invoke (owner, new object[] { this, new EventArgs() });
        }

        private bool AreAncestorsVisible ()
        {
            // if this control and all of its ancestors are visible, then return true, else false
            Control idx = this.Parent;
            while (idx != null) {
                if (!idx.Visible)
                    break;
                idx = idx.Parent;
                if (idx == null)
                    return true;
            }
            return false;
        }

        private bool IsAncestorRendering ()
        {
            // returns true if any of its ancestors are rendering html
            Control idx = this.Parent;
            while (idx != null) {
                Widget wdg = idx as Widget;
                if (wdg != null && wdg._render)
                    return true;
                idx = idx.Parent;
            }
            return false;
        }

        /// <summary>
        /// renders the html response
        /// </summary>
        /// <param name="writer">where to render</param>
        protected virtual void RenderHtmlResponse (HtmlTextWriter writer)
        {
            writer.Write (string.Format (@"<{0} id=""{1}""", Tag, ClientID));
            RenderAttributes (writer);
            if (HasContent) {
                writer.Write (">");
                RenderChildren (writer);
                if (HasEndTag) {
                    writer.Write (string.Format ("</{0}>", Tag));
                }
            } else {
                if (HasEndTag) {
                    if (SelfClosed) {
                        writer.Write (" />");
                    } else {
                        writer.Write (">");
                        writer.Write (string.Format ("</{0}>", Tag));
                    }
                } else {
                    writer.Write (">");
                }
            }
        }

        /// <summary>
        /// gets a value indicating whether this instance has content or not
        /// </summary>
        /// <value><c>true</c> if this instance has content; otherwise, <c>false</c></value>
        protected abstract bool HasContent {
            get;
        }

        /// <summary>
        /// renders the attributes of widget
        /// </summary>
        /// <param name="writer">where to render</param>
        protected virtual void RenderAttributes (HtmlTextWriter writer)
        {
            foreach (core.Attribute idx in Attributes) {
                string name = idx.Name;
                string value;
                if (idx.Name.StartsWith ("on") && core.Utilities.IsLegalMethodName (idx.Value)) {
                    value = "pf.e(event)";
                } else {
                    value = idx.Value;
                }
                writer.Write (" ");
                if (value == null) {
                    writer.Write (string.Format (@"{0}", name));
                } else {
                    writer.Write (string.Format (@"{0}=""{1}""", name, value.Replace ("\"", "\\\"")));
                }
            }
        }

        /// <summary>
        /// returns the value of the attribute with the specified key. please notice that an attribute might exist, even though 
        /// this method returns null
        /// </summary>
        /// <param name="key">name of attribute</param>
        public virtual string GetAttribute (string key)
        {
            core.Attribute atr = Attributes.Find (
                delegate (core.Attribute idx) {
                return idx.Name == key;
            });
            if (atr == null)
                return null;
            return atr.Value;
        }

        /// <summary>
        /// sets the attribute with the specified key
        /// </summary>
        /// <param name="key">name of attribute</param>
        /// <param name="value">value to set the attribute to, if this is null, an empty attribute will be added</param>
        public virtual void SetAttribute (string key, string value)
        {
            core.Attribute atr = Attributes.Find (
                delegate (core.Attribute idx) {
                    return idx.Name == key;
                });
            if (atr != null) {
                if (IsTrackingViewState && atr.Value != value && atr.OldValue == null) {
                    atr.OldValue = atr.Value;
                    atr.Dirty = true;
                }
                atr.Value = value;
            } else {
                atr = new core.Attribute (key, value);
                atr.Dirty = IsTrackingViewState;
                Attributes.Add (atr);
            }
        }

        /// <summary>
        /// removes the attribute with the given name
        /// </summary>
        /// <param name="key">name of attribute to remove</param>
        public virtual void RemoveAttribute (string key)
        {
            Attributes.RemoveAll (
                delegate(core.Attribute idx) {
                    return idx.Name == key;
                });
        }

        /// <summary>
        /// determines whether this instance has the attribute with the specified name
        /// </summary>
        /// <returns><c>true</c> if this instance has the attribute with the specified name; otherwise, <c>false</c></returns>
        /// <param name="key">name of the attribute you wish to check if exists</param>
        public virtual bool HasAttribute (string key)
        {
            if (Attributes.Exists (
                delegate(core.Attribute idx) {
                    return idx.Name == key;
                }))
                return true;
            return false;
        }
    }
}

