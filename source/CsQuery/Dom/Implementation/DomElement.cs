﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using CsQuery.StringScanner;
using CsQuery.HtmlParser;
using CsQuery.ExtensionMethods;
using CsQuery.ExtensionMethods.Internal;
using CsQuery.Output;

namespace CsQuery.Implementation
{
    /// <summary>
    /// HTML elements.
    /// </summary>

    public class DomElement : DomContainer<DomElement>, IDomElement, IDomObject, IDomNode, 
        IAttributeCollection
    {

        #region private fields

        //private string _PathID;
        //private char _PathID;

        /// <summary>
        /// The dom attributes.
        /// </summary>

        private AttributeCollection _InnerAttributes;

        /// <summary>
        /// Backing field for _Style.
        /// </summary>

        private CSSStyleDeclaration _Style;

        /// <summary>
        /// Backing field for _Classes.
        /// </summary>

        private List<ushort> _Classes;

        /// <summary>
        /// Backing field for NodeNameID property.
        /// </summary>

        private ushort _NodeNameID;

        /// <summary>
        /// Gets the dom attributes.
        /// </summary>

        protected AttributeCollection InnerAttributes
        {
            get
            {
                if (_InnerAttributes == null)
                {
                    _InnerAttributes = new AttributeCollection();
                }
                return _InnerAttributes;
            }
            set
            {
                _InnerAttributes = value;
            }
        }

        /// <summary>
        /// Returns true if this node has any actual attributes (not class or style)
        /// </summary>

        public bool HasInnerAttributes
        {
            get
            {
                return _InnerAttributes != null &&
                    _InnerAttributes.HasAttributes;
            }
        }

        #endregion

        #region constructors

        /// <summary>
        /// Default constructor.
        /// </summary>

        public DomElement()
        {
            throw new InvalidOperationException("You can't create a DOM element using the default constructor. Please use the DomElement.Create static method instead.");
        }

        /// <summary>
        /// Create a new DomElement node of a nodeTipe determined by a token ID.
        /// </summary>
        ///
        /// <param name="tokenId">
        /// Token represnting an existing tokenized node type.
        /// </param>

        protected DomElement(ushort tokenId)
            : base()
        {
            _NodeNameID = tokenId;
        }


        #endregion

        #region static methods

        /// <summary>
        /// Creates a new element
        /// </summary>
        ///
        /// <param name="nodeName">
        /// The NodeName for the element (upper case).
        /// </param>
        ///
        /// <returns>
        /// A new element that inherits DomElement
        /// </returns>

        public static DomElement Create(string nodeName)
        {
            return Create(HtmlData.Tokenize(nodeName));
        }

        internal static DomElement Create(ushort nodeNameId)
        {
            switch (nodeNameId)
            {
                case HtmlData.tagA:
                    return new HtmlAnchorElement();
                case HtmlData.tagFORM:
                    return new HtmlFormElement();
                case HtmlData.tagINPUT:
                    return new HTMLInputElement();
                case HtmlData.tagLABEL:
                    return new HTMLLabelElement();
                case HtmlData.tagLI:
                    return new HTMLLIElement();
                case HtmlData.tagMETER:
                    return new HTMLMeterElement();
                case HtmlData.tagOPTION:
                    return new HTMLOptionElement();
                case HtmlData.tagPROGRESS:
                    return new HTMLProgressElement();
                case HtmlData.tagSELECT:
                    return new HTMLSelectElement();
                case HtmlData.tagTEXTAREA:
                    return new HTMLTextAreaElement();
                case HtmlData.tagSTYLE:
                    return new HTMLStyleElement();
                case HtmlData.tagSCRIPT:
                    return new HTMLScriptElement();
                default:
                    return new DomElement(nodeNameId);
            }
        }
        #endregion

        #region public properties

        /// <summary>
        /// An object encapsulating the Styles associated with this element.
        /// </summary>

        public override CSSStyleDeclaration Style
        {
            get
            {
                if (_Style == null)
                {
                    _Style = new CSSStyleDeclaration();
                    _Style.OnHasStylesChanged += new EventHandler<CSSStyleChangedArgs>(_Style_OnHasStylesChanged);

                }
                return _Style;
            }
            protected set
            {
                _Style = value;
                _Style.OnHasStylesChanged += new EventHandler<CSSStyleChangedArgs>(_Style_OnHasStylesChanged);
                if (_Style != null && _Style.HasStyles)
                {
                    AttributeRemoveFromIndex(HtmlData.tagSTYLE);
                }
                else
                {

                }
            }
        }

        void _Style_OnHasStylesChanged(object sender, CSSStyleChangedArgs e)
        {
            if (e.HasStyles)
            {
                AttributeRemoveFromIndex(HtmlData.tagSTYLE);
            }
            else
            {
                AttributeAddToIndex(HtmlData.tagSTYLE);
            }
        }

        /// <summary>
        /// Access to the IAttributeCollection interface for this element's attributes.
        /// </summary>
        ///
        /// <implementation>
        /// We don't actually refer to the inner AttributeCollection object here because we cannot allow
        /// users to set attributes directly in the object: they must use SetAttribute so that special
        /// handling for "class" and "style" as well as indexing can be performed. To avoid creating a
        /// wrapper object,.
        /// </implementation>

        public override IAttributeCollection Attributes
        {
            get
            {
                return this;
            }
        }

        /// <summary>
        /// gets and sets the value of the "class" attribute of the specified element.
        /// </summary>

        public override string ClassName
        {
            get
            {
                if (HasClasses)
                {
                    //return String.Join(" ", _Classes.Select(item=>DomData.TokenName(item)));
                    string className = "";
                    foreach (ushort clsId in _Classes)
                    {
                        className += (className == "" ? "" : " ") + HtmlData.TokenName(clsId);
                    }
                    return className;
                }
                else
                {
                    return "";
                }
            }
            set
            {
                SetClassName(value);
            }
        }

        /// <summary>
        /// Get or set value of the "id" attribute.
        /// </summary>

        public override string Id
        {
            get
            {
                return GetAttribute(HtmlData.IDAttrId, String.Empty);
            }
            set
            {
                if (!IsFragment)
                {
                    if (InnerAttributes.ContainsKey(HtmlData.IDAttrId))
                    {
                        Document.DocumentIndex.RemoveFromIndex(IndexKey('#', HtmlData.TokenizeCaseSensitive(Id), NodePath));
                    }
                    if (value != null)
                    {
                        Document.DocumentIndex.AddToIndex(IndexKey('#', HtmlData.TokenizeCaseSensitive(value), NodePath), this);
                    }
                }
                SetAttributeRaw(HtmlData.IDAttrId, value);
            }
        }

        /// <summary>
        /// The NodeName for the element. This always returns the name in upper case.
        /// </summary>11

        public override string NodeName
        {
            get
            {
                return HtmlData.TokenName(_NodeNameID).ToUpper();
            }
            
        }

        /// <summary>
        /// Gets the token that represents this element's NodeName
        /// </summary>

        public override ushort NodeNameID { 
            get { 
                return _NodeNameID; 
            } 
        }

        /// <summary>
        /// The value of the "type" attribute. For input elements, this property always returns a
        /// lowercase value and defaults to "text" if there is no type attribute. For other element types,
        /// it simply returns the value of the "type" attribute.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/XUL/Property/type
        /// </url>
        ///
        /// <implementation>
        /// TODO: in HTML5 type can be used on OL attributes (and maybe others?) and its value is case
        /// sensitive. The Type of input elements is always lower case, though. This behavior needs to be
        /// verified against the spec.
        /// </implementation>

        public override string Type
        {
            get
            {
                return GetAttribute(HtmlData.attrTYPE);
            }
            set
            {
                SetAttribute(HtmlData.attrTYPE, value);
            }
        }

        /// <summary>
        /// Gets or sets the name attribute of an DOM object, it only applies to the following elements:
        /// &lt;a&gt; , &lt;applet&gt; , &lt;form&gt; , &lt;frame&gt; , &lt;iframe&gt; , &lt;img&gt; ,
        /// &lt;input&gt; , &lt;map&gt; , &lt;meta&gt; , &lt;object&gt; , &lt;option&gt; , &lt;param&gt; ,
        /// &lt;select&gt; , and &lt;textarea&gt; .
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/element.name
        /// </url>
        ///
        /// <implementation>
        /// TODO: Verify that the attribute is applicable to this node type and return null otherwise.
        /// </implementation>

        public override string Name
        {
            get
            {
                return GetAttribute("name");
            }
            set
            {
                SetAttribute("name", value);
            }
        }

        /// <summary>
        /// The value of an input element, or the text of a textarea element.
        /// </summary>

        public override string DefaultValue
        {
            get
            {
                return hasDefaultValue() ?
                    (NodeNameID == HtmlData.tagTEXTAREA ? 
                        InnerText : 
                        GetAttribute("value")) :
                    base.DefaultValue;
            }
            set
            {
                if (!hasDefaultValue())
                {
                    base.DefaultValue = value;
                }
                else
                {
                    if (NodeNameID == HtmlData.tagTEXTAREA)
                    {
                        InnerText = value;
                    }
                    else
                    {
                        SetAttribute("value",value);
                    }
                }
            }
        }

        /// <summary>
        /// For input elements, the "value" property of this element. Returns null for other element
        /// types.
        /// </summary>
        /// <remarks>
        /// TODO: Value is only mapped to an attribute on certain elements. The HasValueProperty method 
        /// resolves this. When setting the Value property for any other element, it should still track
        /// the value but never render it.
        /// We do just the opposite; we don't return the value in that situation but always render it.
        /// This should be fixed to work like the DOM so setting Value doesn't render. 
        /// </remarks>

        public override string Value
        {
            get
            {
                return HtmlData.HasValueProperty(NodeNameID) ?
                        GetAttribute(HtmlData.ValueAttrId) :
                        null;
            }
            set
            {
                SetAttribute(HtmlData.ValueAttrId, value);
            }
        }

        /// <summary>
        /// Gets the type of the node.
        /// </summary>

        public override NodeType NodeType
        {
            get { return NodeType.ELEMENT_NODE; }
        }

        /// <summary>
        /// The direct parent of this node.
        /// </summary>

        public override IDomContainer ParentNode
        {
            get
            {
                return base.ParentNode;
            }
            internal set
            {
                base.ParentNode = value;
            }
        }

        /// <summary>
        /// Returns true if this node has any attributes.
        /// </summary>

        public override bool HasAttributes
        {
            get
            {
                return HasClasses ||
                    HasStyles ||
                    HasInnerAttributes;
            }
        }

        /// <summary>
        /// Returns true if this node has any styles defined.
        /// </summary>

        public override bool HasStyles
        {
            get
            {
                return _Style != null && _Style.HasStyles;
            }
        }

        /// <summary>
        /// Returns true if this node has CSS classes.
        /// </summary>

        public override bool HasClasses
        {
            get
            {
                return _Classes != null && _Classes.Count > 0;
            }
        }


        /// <summary>
        /// Gets a value indicating whether this object type should be indexed.
        /// </summary>

        public override bool IsIndexed
        {
            get
            {
                //return !IsDisconnected && Document.IsIndexed;
                return (DocInfo & DocumentInfo.IsIndexed) != 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether HTML is allowed as a child of this element. It is possible
        /// for this value to be false but InnerTextAllowed to be true for elements which can have inner
        /// content, but no child HTML markup, such as &lt;textarea&gt; and &lt;script&gt;
        /// </summary>

        public override bool InnerHtmlAllowed
        {
            get
            {
                return !HtmlData.HtmlChildrenNotAllowed(_NodeNameID);

            }
        }

        /// <summary>
        /// Gets a value indicating whether text content is allowed as a child of this element.
        /// </summary>

        public override bool InnerTextAllowed
        {
            get
            {
                return HtmlData.ChildrenAllowed(_NodeNameID);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this element may have children. When false, it means this is
        /// a void element.
        /// </summary>

        public override bool ChildrenAllowed
        {
            get
            {
                return HtmlData.ChildrenAllowed(_NodeNameID);
            }
        }


        /// <summary>
        /// The child node at the specified index.
        /// </summary>
        ///
        /// <param name="attribute">
        /// The zero-based index of the child node to access.
        /// </param>
        ///
        /// <returns>
        /// IDomObject, the element at the specified index within this node's children.
        /// </returns>

        public override string this[string attribute]
        {
            get
            {
                return GetAttribute(attribute);
            }
            set
            {
                SetAttribute(attribute, value);
            }
        }

        /// <summary>
        /// The child node at the specified index.
        /// </summary>
        ///
        /// <param name="index">
        /// The zero-based index of the child node to access.
        /// </param>
        ///
        /// <returns>
        /// IDomObject, the element at the specified index within this node's children.
        /// </returns>

        public override IDomObject this[int index]
        {
            get
            {
                return ChildNodes[index];
            }
        }


        /// <summary>
        /// Gets or sets a value indicating whether the element is checked.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/XUL/Property/checked
        /// </url>

        public override bool Checked
        {
            get
            {
                return HasAttribute(HtmlData.CheckedAttrId);
            }
            set
            {
                SetAttribute(HtmlData.CheckedAttrId, value ? "" : null);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the only should be read.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/XUL/Property/readOnly
        /// </url>

        public override bool ReadOnly
        {
            get
            {
                return HasAttribute(HtmlData.ReadonlyAttrId);
            }
            set
            {
                SetAttribute(HtmlData.ReadonlyAttrId, value ? "" : null);
            }
        }

        /// <summary>
        /// Returns text of the inner HTML. When setting, any children will be removed.
        /// </summary>

        public override string InnerHTML
        {
            get
            {
                if (!HasChildren)
                {
                    return String.Empty;
                }
                else
                {
                    var formatter = new FormatDefault();
                    StringBuilder sb = new StringBuilder();
                    StringWriter writer = new StringWriter(sb);
                    formatter.RenderChildren(this, writer);

                    return sb.ToString();
                }
            }
            set
            {
                if (!InnerHtmlAllowed)
                {
                    throw new InvalidOperationException(String.Format("You can't set the innerHTML for a {0} element.", NodeName));
                }
                ChildNodes.Clear();


                CQ csq = CQ.CreateFragment(value, NodeName);
                ChildNodes.AddRange(csq.Document.ChildNodes);
            }
        }

        /// <summary>
        /// Gets or sets the text content of a node and its descendants.
        /// </summary>

        public override string InnerText
        {
            get
            {
                if (!HasChildren)
                {
                    return String.Empty;
                }
                else
                {
                    var formatter = OutputFormatters.Default;
                    StringBuilder sb = new StringBuilder();
                    StringWriter sw = new StringWriter(sb);

                    foreach (IDomObject elm in ChildNodes.Where(item => item.NodeType == NodeType.TEXT_NODE))
                    {
                        formatter.Render(elm, sw);
                    }

                    return sb.ToString();
                }
            }
            set
            {
                if (!InnerTextAllowed)
                {
                    throw new InvalidOperationException(String.Format("You can't set the InnerText for a {0} element.", NodeName));
                }

                ChildNodes.Clear();
                ChildNodes.Add(new DomText(value));
            }
        }

        

        /// <summary>
        /// The index excluding text nodes.
        /// </summary>

        public override int ElementIndex
        {
            get
            {
                int index = -1;
                IDomElement el = this;
                while (el != null)
                {
                    el = el.PreviousElementSibling;
                    index++;
                }
                return index;
            }
        }

        /// <summary>
        /// The object to which this index refers.
        /// </summary>

        public override IDomObject IndexReference
        {
            get
            {
                return this;
            }
        }

        /// <summary>
        /// Returns true if this element is a block-type element.
        /// </summary>

        public override bool IsBlock
        {
            get
            {
                return HtmlData.IsBlock(_NodeNameID);
            }
        }

        /// <summary>
        /// A sequence of all unique class names defined on this element.
        /// </summary>

        public override IEnumerable<string> Classes
        {
            get
            {
                if (!HasClasses) {
                    yield break;
                } else {
                    foreach (var id in _Classes)
                    {
                        yield return HtmlData.TokenName(id);
                    }
                }
            }
        }

        #endregion
        
        #region public methods


        /// <summary>
        /// Returns the HTML for this element, but ignoring children/innerHTML.
        /// </summary>
        ///
        /// <returns>
        /// A string of HTML
        /// </returns>

        public override string ElementHtml()
        {
            var formatter = new FormatDefault();
            StringWriter writer = new StringWriter();
            formatter.RenderElement(this, writer, false);
            return writer.ToString();
        }

        /// <summary>
        /// Gets or sets the outer HTML.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en-US/docs/DOM/element.outerHTML
        /// </url>

        public override string OuterHTML
        {
            get
            {
                var formatter = new FormatDefault();
                StringWriter writer = new StringWriter();
                formatter.RenderElement(this, writer, true);
                return writer.ToString();
            }
            set
            {
                CQ csq = CQ.CreateFragment(value, NodeName);
                int index = this.Index;
                var parent = ParentNode;

                Remove();

                // enumerate collection first - it will change when nodes are moved to a new DOM
                var nodesToAdd = csq.Document.ChildNodes.ToList();
                
                foreach (var node in nodesToAdd)
                {
                    parent.ChildNodes.Insert(index++, node);
                }
            }
        }

        /// <summary>
        /// Returns all the keys that should be in the index for this item (keys for class, tag,
        /// attributes, and id)
        /// </summary>
        ///
        /// <returns>
        /// An enumerator that allows foreach to be used to process index keys in this collection.
        /// </returns>

        public override IEnumerable<ushort[]> IndexKeys()
        {

            ushort[] path = NodePath;

            yield return new ushort[] { HtmlData.indexSeparator }.Concat(path).ToArray();
            
            yield return IndexKey('+',_NodeNameID, path);

            string id = Id;
            
            if (!String.IsNullOrEmpty(id))
            {
                yield return IndexKey('#', HtmlData.TokenizeCaseSensitive(id), path);
            }

            if (HasClasses)
            {
                foreach (ushort clsId in _Classes)
                {
                    yield return IndexKey('.', clsId, path);
                }
            }

            // index attributes

            if (HasClasses)
            {
                yield return IndexKey('!', HtmlData.ClassAttrId, path);
            }
            if (HasStyles)
            {
                yield return IndexKey('!', HtmlData.tagSTYLE, path);
            }

            foreach (ushort token in IndexAttributesTokens())
            {
                yield return IndexKey('!', token, path); ;
            }
        }

        /// <summary>
        /// Makes a deep copy of this object.
        /// </summary>
        ///
        /// <returns>
        /// A copy of this object.
        /// </returns>

        public override DomElement Clone()
        {
            var clone = DomElement.Create(_NodeNameID);

            if (HasAttributes)
            {
                clone._InnerAttributes = InnerAttributes.Clone();
            }
            if (HasClasses)
            {
                clone._Classes = new List<ushort>(_Classes);
            }
            if (HasStyles)
            {
                clone.Style = Style.Clone();
            }
            // will not create ChildNodes lazy object unless results are returned (this is why we don't use AddRange)

            var childNodes = (ChildNodeList)clone.ChildNodes;
            foreach (IDomObject child in CloneChildren())
            {
                childNodes.AddAlways(child);
            }

            return clone;
        }

        /// <summary>
        /// Enumerates clone children in this collection.
        /// </summary>
        ///
        /// <returns>
        /// An enumerator that allows foreach to be used to process clone children in this collection.
        /// </returns>

        public override IEnumerable<IDomObject> CloneChildren()
        {
            if (ChildNodesInternal!=null)
            {
                foreach (IDomObject obj in ChildNodes)
                {
                    yield return obj.Clone();
                }
            }
            yield break;
        }

        /// <summary>
        /// Query if 'name' has style.
        /// </summary>
        ///
        /// <param name="name">
        /// .
        /// </param>
        ///
        /// <returns>
        /// true if style, false if not.
        /// </returns>

        public override bool HasStyle(string name)
        {
            return HasStyles &&
                Style.HasStyle(name);
        }

        /// <summary>
        /// Query if 'name' has class.
        /// </summary>
        ///
        /// <param name="name">
        /// .
        /// </param>
        ///
        /// <returns>
        /// true if class, false if not.
        /// </returns>

        public override bool HasClass(string name)
        {
            return HasClasses
                && _Classes.Contains(HtmlData.TokenizeCaseSensitive(name));
        }

        /// <summary>
        /// Adds the class.
        /// </summary>
        ///
        /// <param name="name">
        /// .
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public override bool AddClass(string name)
        {
            bool result=false;
            bool hadClasses = HasClasses;

            foreach (string cls in name.SplitClean(CharacterData.charsHtmlSpaceArray))
            {
                
                if (!HasClass(cls))
                {
                    if (_Classes == null)
                    {
                        _Classes = new List<ushort>();
                    }
                    ushort tokenId = HtmlData.TokenizeCaseSensitive(cls);
                    
                    _Classes.Add(tokenId);
                    if (IsIndexed)
                    {
                        Document.DocumentIndex.AddToIndex(IndexKey('.', tokenId), this);
                    }
                    
                    result = true;
                }
            }
            if (result && !hadClasses && !IsDisconnected)
            {
                // Must index the attributes for search just on attribute too
                Document.DocumentIndex.AddToIndex(AttributeIndexKey(HtmlData.ClassAttrId), this);
            }
            return result;
        }

        /// <summary>
        /// Removes the class described by name.
        /// </summary>
        ///
        /// <param name="name">
        /// .
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public override bool RemoveClass(string name)
        {
            bool result = false;
            bool hasClasses = HasClasses;
            foreach (string cls in name.SplitClean())
            {
                if (HasClass(cls))
                {
                    ushort tokenId = HtmlData.TokenizeCaseSensitive(cls);
                    _Classes.Remove(tokenId);
                    if (!IsDisconnected)
                    {
                        Document.DocumentIndex.RemoveFromIndex(IndexKey('.',tokenId));
                    }

                    result = true;
                }
            }
            if (!HasClasses && hasClasses && !IsDisconnected)
            {
                Document.DocumentIndex.RemoveFromIndex(AttributeIndexKey(HtmlData.ClassAttrId));
            }

            return result;
        }

       

        /// <summary>
        /// Query if 'tokenId' has attribute.
        /// </summary>
        ///
        /// <param name="name">
        /// .
        /// </param>
        ///
        /// <returns>
        /// true if attribute, false if not.
        /// </returns>

        public override bool HasAttribute(string name)
        {
            return HasAttribute(HtmlData.Tokenize(name));
        }

        /// <summary>
        /// Set the value of a named attribute.
        /// </summary>
        ///
        /// <param name="name">
        /// .
        /// </param>
        /// <param name="value">
        /// .
        /// </param>

        public override void SetAttribute(string name, string value)
        {
            SetAttribute(HtmlData.Tokenize(name), value);
        }

        /// <summary>
        /// Set the value of a named attribute.
        /// </summary>
        ///
        /// <param name="tokenId">
        /// .
        /// </param>
        /// <param name="value">
        /// .
        /// </param>

        protected void SetAttribute(ushort tokenId, string value)
        {
            switch (tokenId)
            {
                case HtmlData.ClassAttrId:
                    ClassName = value;
                    return;
                case HtmlData.IDAttrId:
                    Id = value;
                    break;
                case HtmlData.tagSTYLE:
                    Style = new CSSStyleDeclaration(value, false);
                    return;
                default:
                    // Uncheck any other radio buttons
                    if (tokenId == HtmlData.CheckedAttrId
                        && _NodeNameID == HtmlData.tagINPUT
                        && Type == "radio"
                        && !String.IsNullOrEmpty(Name)
                        && value != null
                        && Document != null)
                    {
                        var radios = Document.QuerySelectorAll("input[type='radio'][name='" + Name + "']:checked");
                        foreach (var item in radios)
                        {
                            item.Checked = false;
                        }
                    }
                    break;
            }

            SetAttributeRaw(tokenId, value);

        }

        /// <summary>
        /// Sets an attribute with no value.
        /// </summary>
        ///
        /// <param name="name">
        /// The attribute name
        /// </param>

        public override void SetAttribute(string name)
        {
            SetAttribute(HtmlData.Tokenize(name));
        }

        /// <summary>
        /// Sets an attribute with no value.
        /// </summary>
        ///
        /// <param name="tokenId">
        /// The token
        /// </param>

        public void SetAttribute(ushort tokenId)
        {
            if (tokenId == HtmlData.ClassAttrId || tokenId == HtmlData.tagSTYLE)
            {
                throw new InvalidOperationException("You can't set class or style attributes as a boolean property.");
            }
            
            AttributeAddToIndex(tokenId);
            InnerAttributes.SetBoolean(tokenId);
        }

        /// <summary>
        /// Used by DomElement to (finally) set the ID value.
        /// </summary>
        ///
        /// <param name="tokenId">
        /// THe attribute token
        /// </param>
        /// <param name="value">
        /// The attribute value
        /// </param>

        protected void SetAttributeRaw(ushort tokenId, string value)
        {
            if (value == null)
            {
                InnerAttributes.Unset(tokenId);
                AttributeRemoveFromIndex(tokenId);
            }
            else
            {
                AttributeAddToIndex(tokenId);
                InnerAttributes[tokenId] = value;
            }
        }

        /// <summary>
        /// Removes the attribute described by name.
        /// </summary>
        ///
        /// <param name="name">
        /// The attribute name to remove
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public override bool RemoveAttribute(string name)
        {
            return RemoveAttribute(HtmlData.Tokenize(name));

        }

        /// <summary>
        /// Removes the attribute described by name.
        /// </summary>
        ///
        /// <param name="tokenId">
        /// The token for the attribute
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public bool RemoveAttribute(ushort tokenId)
        {
            if (!HasAttributes)
            {
                return false;
            }


            switch (tokenId)
            {
                case HtmlData.ClassAttrId:
                    if (HasClasses)
                    {
                        SetClassName(null);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case HtmlData.IDAttrId:
                    if (HasInnerAttributes && InnerAttributes.ContainsKey(HtmlData.IDAttrId))
                    {
                        Id = null;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case HtmlData.tagSTYLE:
                    if (HasStyles)
                    {
                        foreach (var style in Style.Keys)
                        {
                            Style.Remove(style);
                        }
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                default:



                    bool success = InnerAttributes.Remove(tokenId);
                    if (success)
                    {
                        AttributeRemoveFromIndex(tokenId);
                    }
                    return success;
            }
            
        }

        /// <summary>
        /// Gets an attribute value, or returns null if the value is missing. If a valueless attribute is
        /// found, this will also return null. HasAttribute should be used to test for such attributes.
        /// Attributes with an empty string value will return String.Empty.
        /// </summary>
        ///
        /// <param name="name">
        /// .
        /// </param>
        ///
        /// <returns>
        /// The attribute.
        /// </returns>

        public override string GetAttribute(string name)
        {
            return GetAttribute(name, null);
        }

        /// <summary>
        /// Gets an attribute value, or returns null if the value is missing. If a valueless attribute is
        /// found, this will also return null. HasAttribute should be used to test for such attributes.
        /// Attributes with an empty string value will return String.Empty.
        /// </summary>
        ///
        /// <param name="tokenId">
        /// .
        /// </param>
        ///
        /// <returns>
        /// The attribute.
        /// </returns>

        internal string GetAttribute(ushort tokenId)
        {
            return GetAttribute(tokenId, null);
        }

        /// <summary>
        /// Return an attribute value identified by name. If it doesn't exist, return the provided
        /// default value.
        /// </summary>
        ///
        /// <param name="name">
        /// The attribute name.
        /// </param>
        /// <param name="defaultValue">
        /// .
        /// </param>
        ///
        /// <returns>
        /// The attribute.
        /// </returns>

        public override string GetAttribute(string name, string defaultValue)
        {
            return GetAttribute(HtmlData.Tokenize(name), defaultValue);
        }

        /// <summary>
        /// Return an attribute value identified by a token ID. If it doesn't exist, return the provided
        /// default value.
        /// </summary>
        ///
        /// <param name="tokenId">
        /// .
        /// </param>
        /// <param name="defaultValue">
        /// .
        /// </param>
        ///
        /// <returns>
        /// The attribute.
        /// </returns>

        internal string GetAttribute(ushort tokenId, string defaultValue)
        {

            string value = null;
            if (TryGetAttribute(tokenId, out value))
            {
                //IMPORTANT: Even though we need to distinguish between null and empty string values internally to
                // render the same way it was brought over (e.g. either "checked" or "checked=''") --- accessing the
                // attribute value is never null for attributes that exist.
                return value ?? "";
            }
            else
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Try get attribute.
        /// </summary>
        ///
        /// <param name="tokenId">
        /// .
        /// </param>
        /// <param name="value">
        /// .
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public bool TryGetAttribute(ushort tokenId, out string value)
        {
            switch (tokenId)
            {
                case HtmlData.ClassAttrId:
                    value = ClassName;
                    return true;
                case HtmlData.tagSTYLE:
                    value = Style.ToString();
                    return true;
                default:
                    if (HasInnerAttributes) {
                        return InnerAttributes.TryGetValue(tokenId, out value);
                    }
                    value = null;
                    return false;
            }
        }

        /// <summary>
        /// Try get attribute.
        /// </summary>
        ///
        /// <param name="name">
        /// .
        /// </param>
        /// <param name="value">
        /// .
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public override bool TryGetAttribute(string name, out string value)
        {
            return TryGetAttribute(HtmlData.Tokenize(name), out value);
        }

        /// <summary>
        /// Convert this object into a string representation.
        /// </summary>
        ///
        /// <returns>
        /// This object as a string.
        /// </returns>

        public override string ToString()
        {
            return ElementHtml();
        }

        // ICSSStyleDeclations

        /// <summary>
        /// Add a single style in the form "styleName: value".
        /// </summary>
        ///
        /// <param name="style">
        /// .
        /// </param>

        public override void AddStyle(string style)
        {
            AddStyle(style, true);
        }

        /// <summary>
        /// Add a single style in the form "styleName: value".
        /// </summary>
        ///
        /// <param name="style">
        /// .
        /// </param>
        /// <param name="strict">
        /// true to strict.
        /// </param>

        public override void AddStyle(string style, bool strict)
        {
            Style.AddStyles(style, strict);
        }

        /// <summary>
        /// Removes the style described by name.
        /// </summary>
        ///
        /// <param name="name">
        /// .
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public override bool RemoveStyle(string name)
        {
            return _Style != null ? _Style.RemoveStyle(name) : false;
        }

        /// <summary>
        /// Sets the styles.
        /// </summary>
        ///
        /// <param name="styles">
        /// The styles.
        /// </param>

        public void SetStyles(string styles)
        {
            SetStyles(styles, true);
        }

        /// <summary>
        /// Sets the styles.
        /// </summary>
        ///
        /// <param name="styles">
        /// The styles.
        /// </param>
        /// <param name="strict">
        /// true to strict.
        /// </param>

        public void SetStyles(string styles, bool strict)
        {
            Style.SetStyles(styles, strict);
        }

        /// <summary>
        /// Sets a style.
        /// </summary>
        ///
        /// <exception cref="NotImplementedException">
        /// Thrown when the requested operation is unimplemented.
        /// </exception>
        ///
        /// <param name="name">
        /// .
        /// </param>
        /// <param name="value">
        /// .
        /// </param>

        public void SetStyle(string name, string value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets a style.
        /// </summary>
        ///
        /// <exception cref="NotImplementedException">
        /// Thrown when the requested operation is unimplemented.
        /// </exception>
        ///
        /// <param name="name">
        /// .
        /// </param>
        /// <param name="value">
        /// .
        /// </param>
        /// <param name="strict">
        /// true to strict.
        /// </param>

        public void SetStyle(string name, string value, bool strict)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a style.
        /// </summary>
        ///
        /// <exception cref="NotImplementedException">
        /// Thrown when the requested operation is unimplemented.
        /// </exception>
        ///
        /// <param name="name">
        /// .
        /// </param>
        ///
        /// <returns>
        /// The style.
        /// </returns>

        public string GetStyle(string name)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region private methods

        private void SetNodeName(string nodeName)
        {
            if (_NodeNameID < 1)
            {
                _NodeNameID = HtmlData.Tokenize(nodeName);
            }
            else
            {
                throw new InvalidOperationException("You can't change the tag of an element once it has been created.");
            }

        }

        /// <summary>
        /// Attribute index key.
        /// </summary>
        ///
        /// <param name="attrName">
        /// Name of the attribute.
        /// </param>
        ///
        /// <returns>
        /// .
        /// </returns>

        public ushort[] AttributeIndexKey(string attrName)
        {
            return AttributeIndexKey(HtmlData.Tokenize(attrName));
        }

        /// <summary>
        /// Attribute index key.
        /// </summary>
        ///
        /// <param name="attrId">
        /// Identifier for the attribute.
        /// </param>
        ///
        /// <returns>
        /// .
        /// </returns>

        public ushort[] AttributeIndexKey(ushort attrId)
        {
#if DEBUG_PATH
            return "!" + HtmlData.TokenName(attrId).ToLower() + HtmlData.indexSeparator + Path;
#else
            return new ushort[] { '!', attrId, HtmlData.indexSeparator }.Concat(NodePath).ToArray();
#endif
        }

        /// <summary>
        /// Attribute remove from index.
        /// </summary>
        ///
        /// <param name="attrId">
        /// Identifier for the attribute.
        /// </param>

        protected void AttributeRemoveFromIndex(ushort attrId)
        {
            if (!IsDisconnected)
            {
                Document.DocumentIndex.RemoveFromIndex(AttributeIndexKey(attrId));
            }
        }

        /// <summary>
        /// Attribute add to index.
        /// </summary>
        ///
        /// <param name="attrId">
        /// Identifier for the attribute.
        /// </param>

        protected void AttributeAddToIndex(ushort attrId)
        {
            if (!IsDisconnected && !InnerAttributes.ContainsKey(attrId))
            {

                Document.DocumentIndex.AddToIndex(AttributeIndexKey(attrId), this);
            }
        }

        /// <summary>
        /// Sets the class name.
        /// </summary>
        ///
        /// <param name="className">
        /// And sets the value of the class attribute of the specified element.
        /// </param>

        protected void SetClassName(string className)
        {
            
            if (HasClasses) {
                foreach (var cls in Classes.ToList())
                {
                    RemoveClass(cls);
                }
            }
            if (!string.IsNullOrEmpty(className)) 
            {
                AddClass(className);
            }    
        }

        /// <summary>
        /// Query if this object has default value.
        /// </summary>
        ///
        /// <returns>
        /// true if default value, false if not.
        /// </returns>

        protected bool hasDefaultValue()
        {
            return NodeNameID == HtmlData.tagINPUT || NodeNameID == HtmlData.tagTEXTAREA;
        }

        /// <summary>
        /// Index key.
        /// </summary>
        ///
        /// <param name="prefix">
        /// The prefix.
        /// </param>
        /// <param name="keyTokenId">
        /// Identifier for the key token.
        /// </param>
        ///
        /// <returns>
        /// A string
        /// </returns>

        protected ushort[] IndexKey(char prefix, ushort keyTokenId)
        {
            return IndexKey(prefix, keyTokenId, NodePath);
        }

        /// <summary>
        /// Index key.
        /// </summary>
        ///
        /// <param name="prefix">
        /// The prefix.
        /// </param>
        /// <param name="key">
        /// The key.
        /// </param>
        ///
        /// <returns>
        /// A string
        /// </returns>

        protected ushort[] IndexKey(char prefix, string key)
        {
            return IndexKey(prefix, key, NodePath);
        }

        /// <summary>
        /// Index key.
        /// </summary>
        ///
        /// <param name="prefix">
        /// The prefix.
        /// </param>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="path">
        /// Full pathname of the file.
        /// </param>
        ///
        /// <returns>
        /// .
        /// </returns>

        protected ushort[] IndexKey(char prefix, string key, ushort[] path)
        {
            return IndexKey(prefix, HtmlData.Tokenize(key), path);
        }

        /// <summary>
        /// Generates a key that will be used to refernece this item in the index
        /// </summary>
        ///
        /// <param name="prefix">
        /// The prefix.
        /// </param>
        /// <param name="keyTokenId">
        /// Identifier for the key token.
        /// </param>
        /// <param name="path">
        /// Full path to the element.
        /// </param>
        ///
        /// <returns>
        /// A string representing the key for this item in the index
        /// </returns>

        protected ushort[] IndexKey(char prefix, ushort keyTokenId, ushort[] path)
        {
#if DEBUG_PATH
            return prefix + HtmlData.TokenName(keyTokenId) + HtmlData.indexSeparator + path;
#else
            ushort[] key = new ushort[path.Length + 3];
            key[0] = (ushort)prefix;
            key[1] = keyTokenId;
            key[2] = HtmlData.indexSeparator;
            for (int i = 0; i < path.Length; i++)
            {
                key[i + 3] = path[i];
            }
            return key;
#endif
        }

        
        #endregion

        #region internal methods

        /// <summary>
        /// Enumerates all descendant elements in this collection.
        /// </summary>
        ///
        /// <returns>
        /// A sequence of IDomElement objects
        /// </returns>

        internal IEnumerable<IDomElement> DescendantElements()
        {
            foreach (var el in ChildElements)
            {
                yield return el;
                if (el.HasChildren)
                {
                    foreach (var child in ((DomElement)el).DescendantElements())
                    {
                        yield return child;
                    }
                }
            }
        }

        /// <summary>
        /// Query if 'tokenId' has attribute.
        /// </summary>
        ///
        /// <param name="tokenId">
        /// .
        /// </param>
        ///
        /// <returns>
        /// true if attribute, false if not.
        /// </returns>

        internal bool HasAttribute(ushort tokenId)
        {
            switch (tokenId)
            {
                case HtmlData.ClassAttrId:
                    return HasClasses;
                case HtmlData.tagSTYLE:
                    return HasStyles;
                default:
                    return _InnerAttributes != null
                        && InnerAttributes.ContainsKey(tokenId);
            }
        }

        /// <summary>
        /// Gets an attribute value for matching, accounting for default values of special attribute
        /// types.
        /// </summary>
        ///
        /// <param name="attributeId">
        /// Identifier for the attribute.
        /// </param>
        /// <param name="value">
        /// The matched value
        /// </param>
        ///
        /// <returns>
        /// The attribute for matching.
        /// </returns>

        internal virtual bool TryGetAttributeForMatching(ushort attributeId, out string value)
        {
            return TryGetAttribute(attributeId, out value);
        }

        /// <summary>
        /// Return the first ancestor of the specified tag
        /// </summary>
        ///
        /// <param name="tagID">
        /// Identifier for the tag.
        /// </param>
        ///
        /// <returns>
        /// An IDomContainer
        /// </returns>

        internal IDomElement Closest(ushort tagID)
        {
            IDomContainer parent = ParentNode;
            while (parent != null)
            {
                if (parent.NodeNameID == tagID)
                {
                    return (IDomElement)parent;
                }
                parent = parent.ParentNode;
            }
            return null;

        }

        /// <summary>
        /// Sets a boolean property by creating or removing it
        /// </summary>
        ///
        /// <param name="tagId">
        /// Identifier for the tag.
        /// </param>
        /// <param name="value">
        /// The value to set
        /// </param>

        internal void SetProp(ushort tagId, bool value)
        {
            if (value)
            {
                SetAttribute(tagId);
            }
            else
            {
                RemoveAttribute(tagId);
            }
        }

        internal void SetProp(string tagName, bool value) {
            SetProp(HtmlData.Tokenize(tagName), value);
        }


        /// <summary>
        /// Returns all child elements of a specific tag, cast to a type
        /// </summary>
        ///
        /// <typeparam name="T">
        /// Generic type parameter.
        /// </typeparam>
        /// <param name="nodeNameId">
        /// Backing field for NodeNameID property.
        /// </param>
        ///
        /// <returns>
        /// An enumerator.
        /// </returns>

        internal IEnumerable<T> ChildElementsOfTag<T>(ushort nodeNameId)
        {
            return ChildElementsOfTag<T>(this, nodeNameId);
        }

        private IEnumerable<T> ChildElementsOfTag<T>(IDomElement parent, ushort nodeNameId)
        {
            foreach (var el in ChildNodes)
            {
                if (el.NodeType == NodeType.ELEMENT_NODE &&
                    el.NodeNameID == nodeNameId)
                {
                    yield return (T)el;
                }
                if (el.HasChildren)
                {
                    foreach (var child in ((DomElement)el).ChildElementsOfTag<T>(nodeNameId))
                    {
                        yield return child;
                    }
                }
            }
        }
        #endregion

        #region explicit members for IAttributesCollection

        /// <summary>
        /// Get the named attribute value
        /// </summary>
        ///
        /// <param name="attributeName">
        /// The name of the attribute
        /// </param>
        ///
        /// <returns>
        /// A string of the attribute value
        /// </returns>

        string IAttributeCollection.this[string attributeName]
        {
            get
            {
                return GetAttribute(attributeName);
            }
            set
            {
                SetAttribute(attributeName, value);
            }
        }

        /// <summary>
        /// The number of attributes in this attribute collection. This includes special attributes such
        /// as "class", "id", and "style".
        /// </summary>
        ///
        /// <returntype>
        /// int
        /// </returntype>

        int IAttributeCollection.Length
        {
            get {
                int otherAttributes = (HasClasses ? 1 : 0) + (HasStyles ? 1 : 0);

                return otherAttributes + (!HasInnerAttributes ? 0 :
                    InnerAttributes.Count);
            }
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>

        IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
        {
            return AttributesCollection().GetEnumerator();
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        ///
        /// <returns>
        /// The enumerator.
        /// </returns>

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return AttributesCollection().GetEnumerator() ;
        }        

        /// <summary>
        /// Enumerate the attributes + class &amp; style.
        /// </summary>
        ///
        /// <returns>
        /// An enumerator that allows foreach to be used to process attributes collection in this
        /// collection.
        /// </returns>

        protected IEnumerable<KeyValuePair<string, string>> AttributesCollection()
        {
            if (HasClasses)
            {
                yield return new KeyValuePair<string, string>("class", ClassName);
            }
            if (HasStyles)
            {
                yield return new KeyValuePair<string, string>("style", Style.ToString());
            }
            if (HasInnerAttributes)
            {
                foreach (var kvp in InnerAttributes)
                {
                    yield return kvp;
                }
            }
        }

        /// <summary>
        /// Return a sequence of tokens for each non-class, non-style attribute that should be
        /// added to the attribute index.
        /// </summary>
        ///
        /// <returns>
        /// An enumerator of ushort.
        /// </returns>

        protected virtual IEnumerable<ushort> IndexAttributesTokens()
        {
            if (HasInnerAttributes)
            {
                foreach (var token in InnerAttributes.GetAttributeIds())
                {
                    yield return token;
                }
            }
        }




        #endregion

    }
}
