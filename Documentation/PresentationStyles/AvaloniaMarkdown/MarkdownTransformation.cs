//===============================================================================================================
// System  : Sandcastle Tools Standard Presentation Styles
// File    : MarkdownTransformation.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/02/2024
// Note    : Copyright 2022-2024, Eric Woodruff, All rights reserved
//
// This file contains the class used to generate a MAML or API HTML topic from the raw topic XML data for the
// Open XML presentation style.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://GitHub.com/EWSoftware/SHFB.  This
// notice, the author's name, and all copyright notices must remain intact in all applications, documentation,
// and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/25/2022  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Sandcastle.Core;
using Sandcastle.Core.PresentationStyle.Transformation;
using Sandcastle.Core.PresentationStyle.Transformation.Elements;
using Sandcastle.Core.PresentationStyle.Transformation.Elements.Html;
using Sandcastle.Core.PresentationStyle.Transformation.Elements.Markdown;
using Sandcastle.Core.Reflection;
using MarkdownGlossaryElement = Sandcastle.Core.PresentationStyle.Transformation.Elements.Markdown.GlossaryElement;

namespace Avalonia.Sandcastle.PresentationStyles.AvaloniaMarkdown
{
    /// <summary>
    /// This class is used to generate a MAML or API markdown topic from the raw topic XML data for the Markdown
    /// presentation style.
    /// </summary>
    public class AvaloniaMarkdownTransformation : TopicTransformationCore
    {
        #region Private data members
        //=====================================================================

        private XDocument pageTemplate;

        private static readonly HashSet<string> spacePreservedElements = new HashSet<string>(
            new[] { "code", "pre", "snippet" }, StringComparer.OrdinalIgnoreCase);

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="resolvePath">The function used to resolve content file paths for the presentation style</param>
        public AvaloniaMarkdownTransformation(Func<string, string> resolvePath) : base(HelpFileFormats.Markdown, resolvePath)
        {
            this.TopicTemplatePath = this.ResolvePath(@"Templates\TopicTemplate.xml");
        }
        #endregion

        #region Topic transformation argument shortcut properties
        //=====================================================================

        /// <summary>
        /// Maximum version parts
        /// </summary>
        private int MaxVersionParts => Int32.TryParse(this.TransformationArguments[nameof(MaxVersionParts)].Value,
            out int maxVersionParts) ? maxVersionParts : 5;

        /// <summary>
        /// Include enumerated type values
        /// </summary>
        private bool IncludeEnumValues => Boolean.TryParse(this.TransformationArguments[nameof(IncludeEnumValues)].Value,
            out bool includeEnumValues) && includeEnumValues;

        /// <summary>
        /// Enumeration member sort order
        /// </summary>
        private EnumMemberSortOrder EnumMemberSortOrder => Enum.TryParse(this.TransformationArguments[nameof(EnumMemberSortOrder)].Value,
            true, out EnumMemberSortOrder sortOrder) ? sortOrder : EnumMemberSortOrder.Value;

        /// <summary>
        /// Flags enumeration value format
        /// </summary>
        private EnumValueFormat FlagsEnumValueFormat => Enum.TryParse(this.TransformationArguments[nameof(FlagsEnumValueFormat)].Value,
            true, out EnumValueFormat format) ? format : EnumValueFormat.IntegerValue;

        /// <summary>
        /// Flags enumeration value separator group size
        /// </summary>
        private int FlagsEnumSeparatorSize => Int32.TryParse(this.TransformationArguments[nameof(FlagsEnumSeparatorSize)].Value,
            out int groupSize) ? groupSize : 0;

        /// <summary>
        /// Include separators for integer enumeration values
        /// </summary>
        private bool IncludeIntegerEnumSeparators => Boolean.TryParse(this.TransformationArguments[nameof(IncludeIntegerEnumSeparators)].Value,
            out bool includeSeparators) && includeSeparators;

        /// <summary>
        /// Base source code URL
        /// </summary>
        private string BaseSourceCodeUrl => this.TransformationArguments[nameof(BaseSourceCodeUrl)].Value;

        /// <summary>
        /// Show parameters on all methods on the member list page, not just on overloads
        /// </summary>
        private bool ShowParametersOnAllMethods => Boolean.TryParse(this.TransformationArguments[nameof(ShowParametersOnAllMethods)].Value,
            out bool showParameters) && showParameters;

        #endregion

        #region TopicTransformationCore implementation
        //=====================================================================

        /// <inheritdoc />
        public override string IconPath { get; set; } = "media/";

        /// <inheritdoc />
        /// <remarks>Not used by this transformation</remarks>
        public override string StyleSheetPath { get; set; }

        /// <inheritdoc />
        /// <remarks>Not used by this transformation</remarks>
        public override string ScriptPath { get; set; }

        /// <inheritdoc />
        protected override void CreateTransformationArguments()
        {
            this.AddTransformationArgumentRange(new[]
            {
                new TransformationArgument(nameof(BibliographyDataFile), true, true, null,
                    "An optional bibliography data XML file.  Specify the filename with a fully qualified or " +
                    "relative path.  If the path is relative or omitted, it is assumed to be relative to the " +
                    "project folder.\r\n\r\n" +
                    "If blank, no bibliography section will be included in the topics.\r\n\r\n" +
                    "For information on the data file's format, see the bibliography element topic in the " +
                    "Sandcastle MAML Guide or XML Comments Guide."),
                new TransformationArgument(nameof(MaxVersionParts), false, true, null,
                    "The maximum number of assembly version parts to show in API member topics.  Set to 2, " +
                    "3, or 4 to limit it to 2, 3, or 4 parts or leave it blank for all parts including the " +
                    "assembly file version value if specified."),
                new TransformationArgument(nameof(IncludeEnumValues), false, true, "True",
                    "Set this to True to include the column for the numeric value of each field in " +
                    "enumerated type topics.  Set it to False to omit the numeric values column."),
                new TransformationArgument(nameof(EnumMemberSortOrder), false, true, "Value",
                    "The sort order for enumeration members.  Set it to Value to sort by value or Name to sort " +
                    "by name."),
                new TransformationArgument(nameof(FlagsEnumValueFormat), false, true, "IntegerValue",
                    "The format of flags enumeration values: IntegerValue, HexValue, or BitFlags"),
                new TransformationArgument(nameof(FlagsEnumSeparatorSize), false, true, "0",
                    "The separator group size for flags enumeration values (0, 4, or 8).  This determines where " +
                    "separators are placed in the formatted value (e.g. 0b0000_0000, 0x1234_ABCD).  If set to " +
                    "zero, no separators will be inserted."),
                new TransformationArgument(nameof(IncludeIntegerEnumSeparators), false, true, "true",
                    "Set this to true to include separators in integer enumeration values (1,000 vs 1000)."),
                new TransformationArgument(nameof(BaseSourceCodeUrl), false, true, null,
                    "If you set the Source Code Base Path property in the Paths category, specify the URL to " +
                    "the base source code folder on your project's website here.  Some examples for GitHub are " +
                    "shown below.\r\n\r\n" +
                    "Important: Be sure to set the Source Code Base Path property and terminate the URL below with " +
                    "a slash if necessary.\r\n\r\n" +
                    "Format: https://github.com/YourUserID/YourProject/blob/BranchNameOrCommitHash/BaseSourcePath/ \r\n\r\n" +
                    "Master branch: https://github.com/JohnDoe/WidgestProject/blob/master/src/ \r\n" +
                    "A different branch: https://github.com/JohnDoe/WidgestProject/blob/dev-branch/src/ \r\n" +
                    "A specific commit: https://github.com/JohnDoe/WidgestProject/blob/c6e41c4fc2a4a335352d2ae8e7e85a1859751662/src/"),
                new TransformationArgument(nameof(ShowParametersOnAllMethods), false, true, "False",
                    "If false, the default, parameters are hidden on all but overloaded methods on the member " +
                    "list pages.  If set to true, parameters are shown on all methods.")
            });
        }

        /// <inheritdoc />
        /// <remarks>This presentation style does not use language specific text</remarks>
        protected override void CreateLanguageSpecificText()
        {
        }

        /// <inheritdoc />
        protected override void CreateElementHandlers()
        {
            this.AddElements(new Element[]
            {
                // MAML document root element types
                new NonRenderedParentElement("topic"),
                new NonRenderedParentElement("codeEntityDocument"),
                new NonRenderedParentElement("developerConceptualDocument"),
                new NonRenderedParentElement("developerErrorMessageDocument"),
                new NonRenderedParentElement("developerGlossaryDocument"),
                new NonRenderedParentElement("developerHowToDocument"),
                new NonRenderedParentElement("developerOrientationDocument"),
                new NonRenderedParentElement("developerReferenceWithSyntaxDocument"),
                new NonRenderedParentElement("developerReferenceWithoutSyntaxDocument"),
                new NonRenderedParentElement("developerSDKTechnologyOverviewArchitectureDocument"),
                new NonRenderedParentElement("developerSDKTechnologyOverviewCodeDirectoryDocument"),
                new NonRenderedParentElement("developerSDKTechnologyOverviewOrientationDocument"),
                new NonRenderedParentElement("developerSDKTechnologyOverviewScenariosDocument"),
                new NonRenderedParentElement("developerSDKTechnologyOverviewTechnologySummaryDocument"),
                new NonRenderedParentElement("developerSampleDocument"),
                new NonRenderedParentElement("developerTroubleshootingDocument"),
                new NonRenderedParentElement("developerUIReferenceDocument"),
                new NonRenderedParentElement("developerWalkthroughDocument"),
                new NonRenderedParentElement("developerWhitePaperDocument"),
                new NonRenderedParentElement("developerXmlReference"),

                // HTML elements (may occur in XML comments)
                new PassthroughElement("a"),
                new PassthroughElement("abbr"),
                new PassthroughElement("acronym"),
                new PassthroughElement("area"),
                new PassthroughElement("article"),
                new PassthroughElement("aside"),
                new PassthroughElement("audio"),
                new MarkdownElement("b", "**", "**", "b"),
                new PassthroughElement("bdi"),
                new PassthroughElement("blockquote"),
                new PassthroughElement("br"),
                new PassthroughElement("canvas"),
                new PassthroughElement("datalist"),
                new PassthroughElement("dd"),
                new PassthroughElement("del"),
                new PassthroughElement("details"),
                new PassthroughElement("dialog"),
                new PassthroughElement("div"),
                new PassthroughElement("dl"),
                new PassthroughElement("dt"),
                new MarkdownElement("em", "*", "*", "em"),
                new PassthroughElement("embed"),
                new PassthroughElement("figcaption"),
                new PassthroughElement("figure"),
                new PassthroughElement("font"),
                new PassthroughElement("footer"),
                new MarkdownElement("h1", "# ", null, "h1"),
                new MarkdownElement("h2", "## ", null, "h2"),
                new MarkdownElement("h3", "### ", null, "h3"),
                new MarkdownElement("h4", "#### ", null, "h4"),
                new MarkdownElement("h5", "##### ", null, "h5"),
                new MarkdownElement("h6", "###### ", null, "h6"),
                new PassthroughElement("header"),
                new MarkdownElement("hr", "---", null, "hr"),
                new MarkdownElement("i", "*", "*", "em"),
                new PassthroughElement("img"),
                new PassthroughElement("ins"),
                new PassthroughElement("keygen"),
                new PassthroughElement("li"),
                new PassthroughElement("main"),
                new PassthroughElement("map"),
                new PassthroughElement("mark"),
                new PassthroughElement("meter"),
                new PassthroughElement("nav"),
                new PassthroughElement("ol"),
                new PassthroughElement("output"),
                new MarkdownElement("p", "\n", "\n", "p"),
                new PassthroughElement("pre"),
                new PassthroughElement("progress"),
                new PassthroughElement("q"),
                new PassthroughElement("rp"),
                new PassthroughElement("rt"),
                new PassthroughElement("ruby"),
                new PassthroughElement("source"),
                new MarkdownElement("strong", "**", "**", "strong"),
                new PassthroughElement("sub"),
                new PassthroughElement("sup"),
                new PassthroughElement("svg"),
                new PassthroughElement("tbody"),
                new PassthroughElement("td"),
                new PassthroughElement("tfoot"),
                new PassthroughElement("th"),
                new PassthroughElement("thead"),
                new PassthroughElement("time"),
                new PassthroughElement("tr"),
                new PassthroughElement("track"),
                new PassthroughElement("u"),
                new PassthroughElement("ul"),
                new PassthroughElement("video"),
                new PassthroughElement("wbr"),

                // Elements common to HTML, MAML, and/or XML comments.  Processing may differ based on the topic
                // type (API or MAML).
                new BibliographyElement(),
                new CiteElement(),
                new CodeElement("code"),
                new PassthroughElement("include"),
                new PassthroughElement("includeAttribute"),
                new MarkupElement(),
                new MarkdownElement("para", "\n", "\n", "p"),
                new ListElement(),
                new ParametersElement(),
                new PassthroughElement("referenceLink"),
                new PassthroughElement("span"),
                new CodeElement("snippet"),
                new SummaryElement(),
                new TableElement(),

                // MAML elements
                new NoteElement("alert")
                {
                    CautionAlertTemplatePath = this.ResolvePath(@"Templates\CautionAlertTemplate.xml"),
                    LanguageAlertTemplatePath = this.ResolvePath(@"Templates\LanguageAlertTemplate.xml"),
                    NoteAlertTemplatePath = this.ResolvePath(@"Templates\noteAlertTemplate.xml"),
                    SecurityAlertTemplatePath = this.ResolvePath(@"Templates\SecurityAlertTemplate.xml"),
                    ToDoAlertTemplatePath = this.ResolvePath(@"Templates\ToDoAlertTemplate.xml")
                },
                new MarkdownElement("application", "**", "**", "strong"),
                new NamedSectionElement("appliesTo"),
                new AutoOutlineElement(),
                new NamedSectionElement("background"),
                new NamedSectionElement("buildInstructions"),
                new CodeEntityReferenceElement(),
                new CodeExampleElement(),
                new MarkdownElement("codeFeaturedElement", "**", "**", "strong"),
                new MarkdownElement("codeInline", "`", "`", "code"),
                new NonRenderedParentElement("codeReference"),
                // Command may contain nested elements and markdown inline code (`text`) doesn't render nested
                // formatting so we use a code element instead.
                new ConvertibleElement("command", "code"),
                new MarkdownElement("computerOutputInline", "`", "`", "code"),
                new NonRenderedParentElement("conclusion"),
                new NonRenderedParentElement("content"),
                new CopyrightElement(),
                new NonRenderedParentElement("corporation"),
                new NonRenderedParentElement("country"),
                new MarkdownElement("database", "**", "**", "strong"),
                new NonRenderedParentElement("date"),
                new ConvertibleElement("definedTerm", "dt", true),
                new ConvertibleElement("definition", "dd"),
                new ConvertibleElement("definitionTable", "dl"),
                new NamedSectionElement("demonstrates"),
                new NonRenderedParentElement("description"),
                new NamedSectionElement("dotNetFrameworkEquivalent"),
                new MarkdownElement("embeddedLabel", "**", "**", "strong"),
                new EntryElement(),
                new MarkdownElement("environmentVariable", "`", "`", "code"),
                new MarkdownElement("errorInline", "*", "*", "em"),
                new NamedSectionElement("exceptions"),
                new ExternalLinkElement(),
                new NamedSectionElement("externalResources"),
                new MarkdownElement("fictitiousUri", "*", "*", "em"),
                new MarkdownElement("foreignPhrase", "*", "*", "em"),
                new MarkdownGlossaryElement(),
                new MarkdownElement("hardware", "**", "**", "strong"),
                new NamedSectionElement("inThisSection"),
                new IntroductionElement(),
                new LanguageKeywordElement(),
                new NamedSectionElement("languageReferenceRemarks"),
                new NonRenderedParentElement("legacy"),
                new MarkdownElement("legacyBold", "**", "**", "strong"),
                new MarkdownElement("legacyItalic", "*", "*", "em"),
                new LegacyLinkElement(),
                new ConvertibleElement("legacyUnderline", "u"),
                new MarkdownElement("lineBreak", null, "  \n", "br"),
                new ConvertibleElement("listItem", "li", true),
                new MarkdownElement("literal", "*", "*", "em"),
                new MarkdownElement("localUri", "*", "*", "em"),
                new NonRenderedParentElement("localizedText"),
                new MarkdownElement("math", "*", "*", "em"),
                new MediaLinkElement(),
                new MediaLinkInlineElement(),
                new MarkdownElement("newTerm", "*", "*", "em"),
                new NamedSectionElement("nextSteps"),
                new MarkdownElement("parameterReference", "*", "*", "em"),
                new MarkdownElement("phrase", "*", "*", "em"),
                new MarkdownElement("placeholder", "*", "*", "em"),
                new NamedSectionElement("prerequisites"),
                new ProcedureElement(),
                new ConvertibleElement("quote", "blockquote"),
                new MarkdownElement("quoteInline", "*", "*", "em"),
                new NamedSectionElement("reference"),
                new NamedSectionElement("relatedSections"),
                new RelatedTopicsElement(),
                new MarkdownElement("replaceable", "*", "*", "em"),
                new NamedSectionElement("requirements"),
                new NamedSectionElement("returnValue"),
                new NamedSectionElement("robustProgramming"),
                new ConvertibleElement("row", "tr"),
                new SectionElement(),
                new NonRenderedParentElement("sections"),
                new NamedSectionElement("security"),
                new NonRenderedParentElement("snippets"),
                new ConvertibleElement("step", "li", true),
                new StepsElement(),
                new ConvertibleElement("subscript", "sub"),
                new ConvertibleElement("subscriptType", "sub"),
                new ConvertibleElement("superscript", "sup"),
                new ConvertibleElement("superscriptType", "sup"),
                new MarkdownElement("system", "**", "**", "strong"),
                new ConvertibleElement("tableHeader", "thead"),
                new NamedSectionElement("textValue"),
                // The title element is ignored.  The section and table elements handle them as needed.
                new IgnoredElement("title"),
                new NonRenderedParentElement("type"),
                new MarkdownElement("ui", "**", "**", "strong"),
                new MarkdownElement("unmanagedCodeEntityReference", "**", "**", "strong"),
                new MarkdownElement("userInput", "**", "**", "strong"),
                new MarkdownElement("userInputLocalizable", "**", "**", "strong"),
                new NamedSectionElement("whatsNew"),

                // XML comments and reflection data file elements
                new MarkdownElement("c", "`", "`", "code"),
                new PassthroughElement("conceptualLink"),
                new NamedSectionElement("example"),
                new ImplementsElement(),
                new NoteElement("note")
                {
                    CautionAlertTemplatePath = this.ResolvePath(@"Templates\CautionAlertTemplate.xml"),
                    LanguageAlertTemplatePath = this.ResolvePath(@"Templates\LanguageAlertTemplate.xml"),
                    NoteAlertTemplatePath = this.ResolvePath(@"Templates\noteAlertTemplate.xml"),
                    SecurityAlertTemplatePath = this.ResolvePath(@"Templates\SecurityAlertTemplate.xml"),
                    ToDoAlertTemplatePath = this.ResolvePath(@"Templates\ToDoAlertTemplate.xml")
                },
                new MarkdownElement("paramref", "name", "*", "*", "em"),
                new PreliminaryElement(),
                new NamedSectionElement("remarks"),
                new ReturnsElement(),
                new SeeElement(),
                // seeAlso should be a top-level element in the comments but may appear within other elements.
                // We'll ignore it if seen as they'll be handled manually by the See Also section processing.
                new IgnoredElement("seealso"),
                // For this presentation style, namespace/assembly info and inheritance hierarchy are part of
                // the definition (syntax) section.
                new SyntaxElement(nameof(BaseSourceCodeUrl))
                {
                    NamespaceAndAssemblyInfoRenderer = RenderApiNamespaceAndAssemblyInformation,
                    InheritanceHierarchyRenderer = RenderApiInheritanceHierarchy
                },
                new TemplatesElement(),
                new ThreadsafetyElement(),
                new MarkdownElement("typeparamref", "name", "*", "*", "em"),
                new ValueElement(),
                new VersionsElement()
            });
        }

        /// <inheritdoc />
        protected override void CreateApiTopicSectionHandlers()
        {
            // API Topic sections will be rendered in this order by default
            this.AddApiTopicSectionHandlerRange(new[]
            {
                new ApiTopicSectionHandler(ApiTopicSectionType.Notices, t => RenderNotices(t)),
                new ApiTopicSectionHandler(ApiTopicSectionType.Summary, t => RenderApiSummarySection(t)),
                new ApiTopicSectionHandler(ApiTopicSectionType.SyntaxSection, t => RenderApiSyntaxSection(t)),
                new ApiTopicSectionHandler(ApiTopicSectionType.Remarks, t => RenderApiRemarksSection(t)),
                new ApiTopicSectionHandler(ApiTopicSectionType.Examples, t => RenderApiExamplesSection(t)),
                new ApiTopicSectionHandler(ApiTopicSectionType.MemberList, t => RenderApiMemberList(t)),
                new ApiTopicSectionHandler(ApiTopicSectionType.Events,
                    t => RenderApiSectionTable(t, "title_events", t.CommentsNode.Elements("event"))),
                new ApiTopicSectionHandler(ApiTopicSectionType.Exceptions,
                    t => RenderApiSectionTable(t, "title_exceptions", this.CommentsNode.Elements("exception"))),
                new ApiTopicSectionHandler(ApiTopicSectionType.Versions, t => RenderApiVersionsSection(t)),
                new ApiTopicSectionHandler(ApiTopicSectionType.Permissions,
                    t => RenderApiSectionTable(t, "title_permissions", t.CommentsNode.Elements("permission"))),
                new ApiTopicSectionHandler(ApiTopicSectionType.ThreadSafety,
                    t => t.RenderNode(t.CommentsNode.Element("threadsafety"))),
                new ApiTopicSectionHandler(ApiTopicSectionType.RevisionHistory,
                    t => RenderApiRevisionHistorySection(t)),
                new ApiTopicSectionHandler(ApiTopicSectionType.Bibliography,
                    t => RenderApiBibliographySection(t)),
                new ApiTopicSectionHandler(ApiTopicSectionType.SeeAlso, t => RenderApiSeeAlsoSection(t))
            });
        }

        /// <inheritdoc />
        protected override XDocument RenderTopic()
        {
            if(pageTemplate == null)
                pageTemplate = LoadTemplateFile(this.TopicTemplatePath, null);

            var document = new XDocument(pageTemplate);

            this.CurrentElement = document.Root;

            if(!this.IsMamlTopic)
            {
                // This is used by the Save Component to get the filename.  It won't end up in the final result.
                document.Root.Add(new XElement("file",
                    new XAttribute("name", this.ReferenceNode.Element("file")?.Attribute("name")?.Value)));
            }

            this.CurrentElement.Add("# ",
                this.IsMamlTopic ? this.MamlTopicTitle() : this.ApiTopicTitle(false, true),
                new XElement("span", new XAttribute("id", "PageHeader"), " "), "\n");
            
            if(!this.IsMamlTopic)
                this.CurrentElement.Add(new XElement("include", new XAttribute("item", "headerText"), "\n"));

            this.OnRenderStarting(document);

            // Add the topic content.  MAML topics are rendered purely off of the element types.  API topics
            // require custom formatting based on the member type in the topic.
            if(this.IsMamlTopic)
                this.RenderNode(this.TopicNode);
            else
            {
                foreach(var section in this.ApiTopicSections)
                {
                    section.RenderSection(this);
                    this.OnSectionRendered(section.SectionType, section.CustomSectionName);
                }
            }

            this.OnRenderCompleted(document);

            return document;
        }

        /// <summary>
        /// This is used to provide additional whitespace handling and normalization for markdown elements
        /// </summary>
        /// <param name="content">The content element to which the text is added</param>
        /// <param name="textNode">The text node to render</param>
        public override void RenderTextNode(XElement content, XText textNode)
        {
            if(content != null && textNode != null)
            {
                string runText = String.Empty, text = textNode.Value;

                // If the content element has an xml:space attribute or the parent of the text node is in the
                // list of elements that should preserve space, just add the text as-is.  Otherwise,normalize the
                // whitespace.
                if(text.Length == 0 || (content.Name != "document" && content.Attribute(Element.XmlSpace) != null) ||
                  spacePreservedElements.Contains(textNode.Parent.Name.LocalName) ||
                  ((textNode.Parent.Name.LocalName == "div" || textNode.Parent.Name.LocalName == "span") &&
                  textNode.Ancestors("syntax").Any()))
                {
                    runText = text;
                }
                else
                {
                    // If there is a preceding non-text sibling that isn't a line break and the text started with
                    // a whitespace, add a leading space.
                    if(Char.IsWhiteSpace(text[0]) && textNode.PreviousNode != null &&
                      !(textNode.PreviousNode is XText) && (!(textNode.PreviousNode is XElement pn) ||
                      pn.Name.LocalName != "lineBreak"))
                    {
                        runText = " ";
                    }

                    runText += text.NormalizeWhiteSpace();

                    // If there is a following non-text sibling and the text ended with a whitespace, add a
                    // trailing space.
                    if(Char.IsWhiteSpace(text[text.Length - 1]) && textNode.NextNode != null &&
                      !(textNode.NextNode is XText))
                    {
                        runText += " ";
                    }
                }

                content.Add(runText);
            }
        }

        /// <inheritdoc />
        /// <remarks>The returned content element is always null and the content should be inserted into the
        /// transformation's current element after adding the title element.</remarks>
        public override (XElement Title, XElement Content) CreateSection(string uniqueId, bool localizedTitle,
          string title, string linkId)
        {
            XElement titleElement = null;

            if(String.IsNullOrWhiteSpace(title))
            {
                if(localizedTitle)
                    throw new ArgumentException("Title cannot be null if it represents a localized item ID", nameof(title));
            }
            else
            {
                XNode titleContent;

                if(localizedTitle)
                    titleContent = new XElement("include", new XAttribute("item", title));
                else
                    titleContent = new XText(title);

                // Wrap the title in a placeholder element.  This container element will be removed by the
                // markdown content generator.
                titleElement = new XElement("SectionTitle",
                    new XAttribute(Element.XmlSpace, "preserve"), "\n\n## ",
                    titleContent, "\n");

                // Special case for the See Also section.  Use the unique ID as the link ID.
                if(uniqueId == "seeAlso")
                    linkId = uniqueId;

                if(!String.IsNullOrWhiteSpace(linkId))
                    titleElement.Add(new XElement("span", new XAttribute("id", linkId), " "));
            }

            return (titleElement, null);
        }

        /// <inheritdoc />
        /// <remarks>The returned content element is always null and the content should be inserted into the
        /// transformation's current element after adding the title element.</remarks>
        public override (XElement Title, XElement Content) CreateSubsection(bool localizedTitle, string title)
        {
            XElement titleElement = null;

            if(String.IsNullOrWhiteSpace(title))
            {
                if(localizedTitle)
                    throw new ArgumentException("Title cannot be null if it represents a localized item ID", nameof(title));
            }
            else
            {
                XNode titleContent;

                if(localizedTitle)
                    titleContent = new XElement("include", new XAttribute("item", title));
                else
                    titleContent = new XText(title);

                // Wrap the title in a placeholder element.  This container element will be removed by the
                // markdown content generator.
                titleElement = new XElement("SectionTitle",
                    new XAttribute(Element.XmlSpace, "preserve"), "\n\n#### ",
                    titleContent, "\n");
            }

            return (titleElement, null);
        }
        #endregion

        #region API topic section handlers
        //=====================================================================

        /// <summary>
        /// This is used to render the preliminary and obsolete API notices
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderNotices(TopicTransformationCore transformation)
        {
            var preliminary = transformation.CommentsNode.Element("preliminary");
            var obsolete = transformation.ReferenceNode.AttributeOfType("T:System.ObsoleteAttribute");

            if(preliminary != null || obsolete != null)
            {
                var currentElement = transformation.CurrentElement;
                var notes = new XElement("blockquote");

                currentElement.Add(notes, "\n");
                transformation.CurrentElement = notes;

                if(preliminary != null)
                    transformation.RenderNode(preliminary);

                if(obsolete != null)
                {
                    if(preliminary != null)
                        notes.Add(new XElement("br"));

                    notes.Add(new XElement("strong",
                        new XElement("include", new XAttribute("item", "boilerplate_obsoleteLong"))));
                }

                transformation.CurrentElement = currentElement;
            }
        }

        /// <summary>
        /// This is used to render the summary section
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiSummarySection(TopicTransformationCore transformation)
        {
            if(transformation.ApiMember.ApiTopicSubgroup != ApiMemberGroup.Overload)
            {
                transformation.CurrentElement.Add("\n");
                transformation.RenderNode(transformation.CommentsNode.Element("summary"));
                transformation.CurrentElement.Add("\n");
            }
            else
            {
                // Render the summary from the first overloads element.  There should only be one.
                var overloads = transformation.ReferenceNode.Descendants("overloads").FirstOrDefault();

                if(overloads != null)
                {
                    var summary = overloads.Element("summary");

                    transformation.CurrentElement.Add("\n");

                    if(summary != null)
                        transformation.RenderNode(summary);
                    else
                        transformation.RenderChildElements(transformation.CurrentElement, overloads.Nodes());

                    transformation.CurrentElement.Add("\n");
                }
            }
        }

        /// <summary>
        /// Render the inheritance hierarchy section
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        /// <param name="content">The content element to which the information is added</param>
        private static void RenderApiInheritanceHierarchy(TopicTransformationCore transformation, XElement content)
        {
            XElement row, td, family = transformation.ReferenceNode.Element("family"),
                implements = transformation.ReferenceNode.Element("implements");
            bool isFirst = true;

            if(family == null && implements == null)
                return;

            var table = new XElement("table");

            content.Add(table, "\n\n");

            if(family != null)
            {
                XElement descendants = family.Element("descendents"), ancestors = family.Element("ancestors");

                td = new XElement("td");
                row = new XElement("tr", new XElement("td", new XElement("strong",
                    new XElement("include", new XAttribute("item", "text_inheritance")))), td);

                table.Add(row);

                if(ancestors != null)
                {
                    // Ancestor types are stored nearest to most distant so reverse them
                    foreach(var typeInfo in ancestors.Elements().Reverse())
                    {
                        if(!isFirst)
                            td.Add("  \u2192  ");

                        transformation.RenderTypeReferenceLink(td, typeInfo, false);
                        isFirst = false;
                    }

                    td.Add("  \u2192  ");
                }

                td.Add(new XElement("referenceLink",
                        new XAttribute("target", transformation.Key),
                        new XAttribute("show-container", false)));

                if(descendants != null)
                {
                    td = new XElement("td");
                    row = new XElement("tr", new XElement("td", new XElement("strong",
                        new XElement("include", new XAttribute("item", "text_derived")))), td);

                    table.Add(row);
                    isFirst = true;

                    foreach(var typeInfo in descendants.Elements().OrderBy(e => e.Attribute("api")?.Value))
                    {
                        if(!isFirst)
                            td.Add(new XElement("br"));

                        transformation.RenderTypeReferenceLink(td, typeInfo, true);
                        isFirst = false;
                    }
                }
            }

            if(implements != null)
            {
                td = new XElement("td");
                row = new XElement("tr", new XElement("td", new XElement("strong",
                    new XElement("include", new XAttribute("item", "text_implements")))), td);

                table.Add(row);
                isFirst = true;

                foreach(var typeInfo in implements.Elements().OrderBy(e => e.Attribute("api")?.Value))
                {
                    if(!isFirst)
                        td.Add(", ");

                    transformation.RenderTypeReferenceLink(td, typeInfo, false);
                    isFirst = false;
                }
            }
        }

        /// <summary>
        /// This is used to render namespace and assembly information for an API topic
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        /// <param name="content">The content element to which the information is added</param>
        private static void RenderApiNamespaceAndAssemblyInformation(TopicTransformationCore transformation,
          XElement content)
        {
            // Only API member pages get namespace/assembly info
            if(transformation.ApiMember.ApiTopicGroup == ApiMemberGroup.List ||
               transformation.ApiMember.ApiTopicGroup == ApiMemberGroup.RootGroup ||
               transformation.ApiMember.ApiTopicGroup == ApiMemberGroup.Root ||
               transformation.ApiMember.ApiTopicGroup == ApiMemberGroup.NamespaceGroup ||
               transformation.ApiMember.ApiTopicGroup == ApiMemberGroup.Namespace)
            {
                return;
            }

            var containers = transformation.ReferenceNode.Element("containers");
            var libraries = containers.Elements("library");

            content.Add("**",
                new XElement("include", new XAttribute("item", "boilerplate_requirementsNamespace")), "** ",
                new XElement("referenceLink",
                    new XAttribute("target", containers.Element("namespace").Attribute("api").Value)), "  ", new XElement("br"));

            int separatorSize = 1;
            bool first = true;

            if(libraries.Count() > 1)
            {
                content.Add("**",
                    new XElement("include", new XAttribute("item", "boilerplate_requirementsAssemblies")), "**");
                separatorSize = 2;
            }
            else
            {
                content.Add("**",
                    new XElement("include", new XAttribute("item", "boilerplate_requirementsAssemblyLabel")), "**");
            }

            string separator = new String(' ', separatorSize);
            int maxVersionParts = ((AvaloniaMarkdownTransformation)transformation).MaxVersionParts;

            foreach(var l in libraries)
            {
                if(!first)
                    content.Add("  ", new XElement("br"));

                content.Add(separator);

                string version = l.Element("assemblydata").Attribute("version").Value,
                    extension = l.Attribute("kind").Value.Equals(
                        "DynamicallyLinkedLibrary", StringComparison.Ordinal) ? "dll" : "exe";
                string[] versionParts = version.Split(VersionNumberSeparators, StringSplitOptions.RemoveEmptyEntries);

                // Limit the version number parts if requested
                if(maxVersionParts > 1 && maxVersionParts < 5)
                    version = String.Join(".", versionParts, 0, maxVersionParts);

                content.Add(new XElement("include",
                        new XAttribute("item", "assemblyNameAndModule"),
                    new XElement("parameter", l.Attribute("assembly").Value),
                    new XElement("parameter", l.Attribute("module").Value),
                    new XElement("parameter", extension),
                    new XElement("parameter", version)));

                first = false;
            }

            // Show XAML XML namespaces for APIs that support XAML.  All topics that have auto-generated XAML
            // syntax get an "XMLNS for XAML" line in the Requirements section.  Topics with boilerplate XAML
            // syntax, e.g. "Not applicable", do NOT get this line.
            var xamlCode = transformation.SyntaxNode.Elements("div").Where(d => d.Attribute("codeLanguage")?.Value.Equals(
                "XAML", StringComparison.Ordinal) ?? false);

            if(xamlCode.Any())
            {
                var xamlXmlNS = xamlCode.Elements("div").Where(d => d.Attribute("class")?.Value == "xamlXmlnsUri");

                content.Add("  ", new XElement("br"), "**",
                    new XElement("include", new XAttribute("item", "boilerplate_xamlXmlnsRequirements")), "** ");

                if(xamlXmlNS.Any())
                {
                    first = true;

                    foreach(var d in xamlXmlNS)
                    {
                        if(!first)
                            content.Add(", ");

                        content.Add(d.Value.NormalizeWhiteSpace());
                        first = false;
                    }
                }
                else
                    content.Add(new XElement("include", new XAttribute("item", "boilerplate_unmappedXamlXmlns")));
            }
        }

        /// <summary>
        /// This is used to render the syntax section
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiSyntaxSection(TopicTransformationCore transformation)
        {
            // Only API member pages get a syntax section
            if(transformation.ApiMember.ApiTopicGroup != ApiMemberGroup.List &&
               transformation.ApiMember.ApiTopicGroup != ApiMemberGroup.RootGroup &&
               transformation.ApiMember.ApiTopicGroup != ApiMemberGroup.Root &&
               transformation.ApiMember.ApiTopicGroup != ApiMemberGroup.NamespaceGroup &&
               transformation.ApiMember.ApiTopicGroup != ApiMemberGroup.Namespace)
            {
                transformation.RenderNode(transformation.SyntaxNode);
            }
        }

        /// <summary>
        /// This is used to render a member list topic (root, root group, namespace group, namespace, enumeration,
        /// type, or type member list).
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiMemberList(TopicTransformationCore transformation)
        {
            switch(transformation.ApiMember)
            {
                case var t when t.ApiTopicGroup == ApiMemberGroup.RootGroup || t.ApiTopicGroup == ApiMemberGroup.Root:
                    RenderApiRootList(transformation);
                    break;

                case var t when t.ApiTopicGroup == ApiMemberGroup.NamespaceGroup:
                    RenderApiNamespaceGroupList(transformation);
                    break;

                case var t when t.ApiTopicGroup == ApiMemberGroup.Namespace:
                    RenderApiNamespaceList(transformation);
                    break;

                case var t when t.ApiTopicSubgroup == ApiMemberGroup.Enumeration:
                    RenderApiEnumerationMembersList(transformation);
                    break;

                case var t when t.ApiTopicGroup == ApiMemberGroup.Type || t.ApiTopicGroup == ApiMemberGroup.List:
                    RenderApiTypeMemberLists(transformation);
                    break;
            }
        }

        /// <summary>
        /// Render the list in a root group or root topic
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiRootList(TopicTransformationCore transformation)
        {
            var elements = transformation.ReferenceNode.Element("elements")?.Elements("element").OrderBy(
                e => e.Element("apidata").Attribute("name").Value).ToList();

            if((elements?.Count ?? 0) == 0)
                return;

            var (title, _) = transformation.CreateSection(elements[0].GenerateUniqueId(), true, "title_namespaces", null);

            transformation.CurrentElement.Add(title);

            var table = new XElement("table");

            transformation.CurrentElement.Add(table);

            foreach(var e in elements)
            {
                string name = e.Element("apidata").Attribute("name").Value;
                var refLink = new XElement("referenceLink",
                    new XAttribute("target", e.Attribute("api").Value),
                    new XAttribute("qualified", "false"));
                var summaryCell = new XElement("td");

                if(name.Length == 0)
                    refLink.Add(new XElement("include", new XAttribute("item", "defaultNamespace")));

                table.Add(new XElement("tr", 
                    new XElement("td", refLink), 
                    summaryCell));

                var summary = e.Element("summary");

                if(summary != null)
                    transformation.RenderChildElements(summaryCell, summary.Nodes());
                else
                    summaryCell.Add(Element.NonBreakingSpace);
            }
        }

        /// <summary>
        /// Render the list in a namespace group topic
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiNamespaceGroupList(TopicTransformationCore transformation)
        {
            var elements = transformation.ReferenceNode.Element("elements")?.Elements("element").OrderBy(e =>
            {
                string name = e.Attribute("api").Value;
                return name.Substring(name.IndexOf(':') + 1);
            }).ToList();

            if((elements?.Count ?? 0) == 0)
                return;

            var (title, _) = transformation.CreateSection(elements[0].GenerateUniqueId(), true,
                "tableTitle_namespace", null);

            transformation.CurrentElement.Add(title);

            var table = new XElement("table");

            transformation.CurrentElement.Add(table);

            foreach(var e in elements)
            {
                var summaryCell = new XElement("td");

                table.Add(new XElement("tr",
                    new XElement("td",
                        new XElement("referenceLink",
                            new XAttribute("target", e.Attribute("api").Value),
                            new XAttribute("qualified", "false"))),
                    summaryCell));

                var summary = e.Element("summary");

                if(summary != null)
                    transformation.RenderChildElements(summaryCell, summary.Nodes());
                else
                    summaryCell.Add(Element.NonBreakingSpace);
            }
        }

        /// <summary>
        /// Render the category lists in a namespace topic
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiNamespaceList(TopicTransformationCore transformation)
        {
            var elements = transformation.ReferenceNode.Element("elements").Elements("element").GroupBy(
                e => e.Element("apidata").Attribute("subgroup").Value).ToDictionary(k => k.Key, v => v);

            foreach(string key in new[] { "class", "structure", "interface", "delegate", "enumeration" })
            {
                if(elements.TryGetValue(key, out var group))
                {
                    var (title, _) = transformation.CreateSection(group.First().GenerateUniqueId(), true,
                        "tableTitle_" + key, null);

                    transformation.CurrentElement.Add(title);

                    var table = new XElement("table");

                    transformation.CurrentElement.Add(table);

                    foreach(var e in group.OrderBy(el => el.Attribute("api").Value))
                    {
                        var summaryCell = new XElement("td");

                        table.Add(new XElement("tr",
                            new XElement("td",
                                new XElement("referenceLink",
                                    new XAttribute("target", e.Attribute("api").Value),
                                    new XAttribute("qualified", "false"))),
                            summaryCell));

                        var summary = e.Element("summary");

                        if(summary != null)
                            transformation.RenderChildElements(summaryCell, summary.Nodes());

                        var obsoleteAttr = e.AttributeOfType("T:System.ObsoleteAttribute");
                        var prelimComment = e.Element("preliminary");

                        if(obsoleteAttr != null || prelimComment != null)
                        {
                            if(!summaryCell.IsEmpty)
                                summaryCell.Add(new XElement("br"));

                            if(obsoleteAttr != null)
                            {
                                summaryCell.Add(new XElement("strong",
                                    new XElement("include", new XAttribute("item", "boilerplate_obsoleteShort"))));
                            }

                            if(prelimComment != null)
                            {
                                if(obsoleteAttr != null)
                                    summaryCell.Add("&#160;&#160;");

                                summaryCell.Add(new XElement("em",
                                    new XElement("include", new XAttribute("item", "preliminaryShort"))));
                            }
                        }

                        if(summaryCell.IsEmpty)
                            summaryCell.Add(Element.NonBreakingSpace);
                    }
                }
            }
        }

        /// <summary>
        /// Render the members of an enumeration
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiEnumerationMembersList(TopicTransformationCore transformation)
        {
            // Convert to this type so that we can access the argument shortcuts easily
            var thisTransform = (AvaloniaMarkdownTransformation)transformation;

            var allMembers = transformation.ReferenceNode.Element("elements")?.Elements("element").ToList();

            if(allMembers == null)
                return;

            List<XElement> fieldMembers = new List<XElement>(), extensionsMethods = new List<XElement>();

            // Enumerations can have extension methods which need to be rendered in a separate section
            foreach(var m in allMembers)
            {
                XElement apiData = m.Element("apidata");

                // Some members such as inherited interface members on a derived interface, contain no
                // metadata and we'll ignore them.
                if(apiData == null)
                    continue;

                if(Enum.TryParse<ApiMemberGroup>(apiData.Attribute("subgroup")?.Value, true, out var subgroup) &&
                  subgroup == ApiMemberGroup.Field)
                {
                    fieldMembers.Add(m);
                }
                else
                    extensionsMethods.Add(m);
            }

            if(fieldMembers.Count != 0)
            {
                // Sort order is configurable for enumeration members
                EnumMemberSortOrder enumMemberSortOrder = thisTransform.EnumMemberSortOrder;

                var elements = fieldMembers.OrderBy(el => enumMemberSortOrder == EnumMemberSortOrder.Name ?
                    el.Element("apidata").Attribute("name").Value :
                    el.Element("value").Value.PadLeft(20, ' ')).ToList();

                var enumValues = elements.Select(e => e.Element("value").Value).ToList();
                bool includeEnumValues = thisTransform.IncludeEnumValues;
                int idx;

                if(includeEnumValues)
                {
                    EnumValueFormat enumFormat = thisTransform.FlagsEnumValueFormat;
                    int groupSize = thisTransform.IncludeIntegerEnumSeparators ? 3 : 0, minWidth = 0;
                    bool signedValues = enumValues.Any(v => v.Length > 0 && v[0] == '-');

                    if(enumFormat != EnumValueFormat.IntegerValue &&
                      thisTransform.ReferenceNode.AttributeOfType("T:System.FlagsAttribute") != null)
                    {
                        groupSize = thisTransform.FlagsEnumSeparatorSize;

                        if(groupSize != 0 && groupSize != 4 && groupSize != 8)
                            groupSize = 0;

                        // Determine the minimum width of the values
                        if(signedValues)
                        {
                            minWidth = enumValues.Select(v => TopicTransformationExtensions.FormatSignedEnumValue(v,
                                enumFormat, 0, 0)).Max(v => v.Length) - 2;
                        }
                        else
                        {
                            minWidth = enumValues.Select(v => TopicTransformationExtensions.FormatUnsignedEnumValue(v,
                                enumFormat, 0, 0)).Max(v => v.Length) - 2;
                        }

                        if(minWidth < 3)
                            minWidth = 2;
                        else
                        {
                            if((minWidth % 4) != 0)
                                minWidth += 4 - (minWidth % 4);
                        }
                    }
                    else
                        enumFormat = EnumValueFormat.IntegerValue;   // Enforce integer format for non-flags enums

                    for(idx = 0; idx < enumValues.Count; idx++)
                    {
                        if(signedValues)
                        {
                            enumValues[idx] = TopicTransformationExtensions.FormatSignedEnumValue(enumValues[idx],
                                enumFormat, minWidth, groupSize);
                        }
                        else
                        {
                            enumValues[idx] = TopicTransformationExtensions.FormatUnsignedEnumValue(enumValues[idx],
                                enumFormat, minWidth, groupSize);
                        }
                    }
                }

                var (title, _) = thisTransform.CreateSection(elements.First().GenerateUniqueId(), true,
                    "topicTitle_enumMembers", null);

                thisTransform.CurrentElement.Add(title);

                var table = new XElement("table");

                thisTransform.CurrentElement.Add(table);

                idx = 0;

                foreach(var e in elements)
                {
                    var summaryCell = new XElement("td");

                    XElement valueCell = null;

                    if(includeEnumValues)
                    {
                        valueCell = new XElement("td", enumValues[idx]);
                        idx++;
                    }

                    table.Add(new XElement("tr",
                        new XElement("td", e.Element("apidata").Attribute("name").Value),
                        valueCell, summaryCell));

                    var summary = e.Element("summary");
                    var remarks = e.Element("remarks");

                    if(summary != null || remarks != null)
                    {
                        if(summary != null)
                            thisTransform.RenderChildElements(summaryCell, summary.Nodes());

                        // Enum members may have additional authored content in the remarks node
                        if(remarks != null)
                            thisTransform.RenderChildElements(summaryCell, remarks.Nodes());
                    }
                    
                    if(e.AttributeOfType("T:System.ObsoleteAttribute") != null)
                    {
                        if(!summaryCell.IsEmpty)
                            summaryCell.Add(new XElement("br"));

                        summaryCell.Add(new XElement("strong",
                            new XElement("include", new XAttribute("item", "boilerplate_obsoleteShort"))));
                    }

                    if(summaryCell.IsEmpty)
                        summaryCell.Add(Element.NonBreakingSpace);
                }
            }

            if(extensionsMethods.Count != 0)
                RenderApiTypeMemberLists(transformation);
        }

        /// <summary>
        /// Render type member lists
        /// </summary>
        /// <remarks>This is used for types and the member list subtopics</remarks>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiTypeMemberLists(TopicTransformationCore transformation)
        {
            var allMembers = transformation.ReferenceNode.Element("elements")?.Elements("element").ToList();

            if((allMembers?.Count ?? 0) == 0)
                return;

            var overloads = allMembers.Where(e => e.Attribute("api").Value.StartsWith("Overload:",
                StringComparison.Ordinal)).ToList();

            // Remove overload topics and add their members to the full member list
            foreach(var overload in overloads)
            {
                allMembers.Remove(overload);
                allMembers.AddRange(overload.Elements("element"));
            }

            var memberGroups = new Dictionary<ApiMemberGroup, List<XElement>>
            {
                { ApiMemberGroup.Constructor, new List<XElement>() },
                { ApiMemberGroup.Property, new List<XElement>() },
                { ApiMemberGroup.Method, new List<XElement>() },
                { ApiMemberGroup.Event, new List<XElement>() },
                { ApiMemberGroup.Operator, new List<XElement>() },
                { ApiMemberGroup.Field, new List<XElement>() },
                { ApiMemberGroup.AttachedProperty, new List<XElement>() },
                { ApiMemberGroup.AttachedEvent, new List<XElement>() },
                { ApiMemberGroup.Extension, new List<XElement>() },
                { ApiMemberGroup.ExplicitInterfaceImplementation, new List<XElement>() },
                // Only used for overloads topics.  It won't appear on normal list pages.
                { ApiMemberGroup.Overload, new List<XElement>() },
            };

            if(transformation.ApiMember.ApiTopicSubgroup != ApiMemberGroup.Overload)
            {
                // Group the members by section type
                foreach(var m in allMembers)
                {
                    XElement apiData = m.Element("apidata"), memberData = m.Element("memberdata"),
                        procedureData = m.Element("proceduredata");

                    // Some members such as inherited interface members on a derived interface, contain no
                    // metadata and we'll ignore them.
                    if(apiData == null)
                        continue;

                    if(!Enum.TryParse<ApiMemberGroup>(apiData.Attribute("subgroup")?.Value, true, out var subgroup))
                        subgroup = ApiMemberGroup.Unknown;

                    if(!Enum.TryParse<ApiMemberGroup>(apiData.Attribute("subsubgroup")?.Value, true, out var subsubgroup))
                        subsubgroup = ApiMemberGroup.Unknown;

                    switch(m)
                    {
                        // The order of checks is important here and doesn't match the order of the rendered
                        // sections.  It minimizes the conditions we need to check in each subsequent case.
                        case var mbr when procedureData?.Attribute("eii")?.Value == "true":
                            memberGroups[ApiMemberGroup.ExplicitInterfaceImplementation].Add(mbr);
                            break;

                        case var mbr when subgroup == ApiMemberGroup.Constructor:
                            memberGroups[ApiMemberGroup.Constructor].Add(mbr);
                            break;

                        case var mbr when subgroup == ApiMemberGroup.Property && subsubgroup == ApiMemberGroup.Unknown:
                            memberGroups[ApiMemberGroup.Property].Add(mbr);
                            break;

                        case var mbr when subgroup == ApiMemberGroup.Method && subsubgroup == ApiMemberGroup.Unknown:
                            memberGroups[ApiMemberGroup.Method].Add(mbr);
                            break;

                        case var mbr when subgroup == ApiMemberGroup.Event && subsubgroup == ApiMemberGroup.Unknown:
                            memberGroups[ApiMemberGroup.Event].Add(mbr);
                            break;

                        case var mbr when subsubgroup == ApiMemberGroup.Operator:
                            memberGroups[ApiMemberGroup.Operator].Add(mbr);
                            break;

                        case var mbr when subgroup == ApiMemberGroup.Field:
                            memberGroups[ApiMemberGroup.Field].Add(mbr);
                            break;

                        case var mbr when subsubgroup == ApiMemberGroup.AttachedProperty:
                            memberGroups[ApiMemberGroup.AttachedProperty].Add(mbr);
                            break;

                        case var mbr when subsubgroup == ApiMemberGroup.AttachedEvent:
                            memberGroups[ApiMemberGroup.AttachedEvent].Add(mbr);
                            break;

                        case var mbr when subsubgroup == ApiMemberGroup.Extension:
                            memberGroups[ApiMemberGroup.Extension].Add(mbr);
                            break;

                        default:
                            // We shouldn't get here, but just in case...
                            Debug.WriteLine("Unhandled member type Subgroup: {0} Sub-subgroup: {1}", subgroup, subsubgroup);

                            if(Debugger.IsAttached)
                                Debugger.Break();
                            break;
                    }
                }
            }
            else
                memberGroups[ApiMemberGroup.Overload].AddRange(allMembers);

            // When called for an enumeration's extension methods, ignore fields as they've already been rendered
            if(transformation.ApiMember.ApiTopicSubgroup == ApiMemberGroup.Enumeration)
                memberGroups[ApiMemberGroup.Field].Clear();

            // Render each section with at least one member
            foreach(var memberType in new[] { ApiMemberGroup.Constructor, ApiMemberGroup.Property,
                ApiMemberGroup.Method, ApiMemberGroup.Event, ApiMemberGroup.Operator, ApiMemberGroup.Field,
                ApiMemberGroup.AttachedProperty, ApiMemberGroup.AttachedEvent, ApiMemberGroup.Extension,
                ApiMemberGroup.ExplicitInterfaceImplementation, ApiMemberGroup.Overload })
            {
                var members = memberGroups[memberType];

                if(members.Count == 0)
                    continue;

                var (title, _) = transformation.CreateSection(members.First().GenerateUniqueId(), true,
                    "tableTitle_" + memberType.ToString(), null);

                transformation.CurrentElement.Add(title);

                var table = new XElement("table");

                transformation.CurrentElement.Add(table);

                // Sort by EII name if present else the member name and then by template count
                foreach(var e in members.OrderBy(el => el.Element("topicdata")?.Attribute("eiiName")?.Value ??
                    el.Element("apidata")?.Attribute("name").Value ?? String.Empty).ThenBy(
                    el => el.Element("templates")?.Elements()?.Count() ?? 0))
                {
                    XElement referenceLink = new XElement("referenceLink",
                            new XAttribute("target", e.Attribute("api").Value));
                    string showParameters = (!((AvaloniaMarkdownTransformation)transformation).ShowParametersOnAllMethods &&
                        transformation.ApiMember.ApiTopicSubgroup != ApiMemberGroup.Overload &&
                        e.Element("memberdata").Attribute("overload") == null &&
                        !(e.Parent.Attribute("api")?.Value ?? String.Empty).StartsWith(
                            "Overload:", StringComparison.Ordinal)) ? "false" : "true";
                    bool isExtensionMethod = e.AttributeOfType("T:System.Runtime.CompilerServices.ExtensionAttribute") != null;

                    var summaryCell = new XElement("td");

                    switch(memberType)
                    {
                        case var t when t == ApiMemberGroup.Operator &&
                          (e.Element("apidata")?.Attribute("name")?.Value == "Explicit" ||
                          e.Element("apidata")?.Attribute("name")?.Value == "Implicit"):
                            referenceLink.Add(new XAttribute("show-parameters", "true"));
                            break;

                        case var t when t == ApiMemberGroup.Operator:
                            break;

                        case var t when t == ApiMemberGroup.Extension:
                            var extensionMethod = new XElement("extensionMethod");

                            foreach(var attr in e.Attributes())
                                extensionMethod.Add(new XAttribute(attr));

                            foreach(var typeEl in new[] { e.Element("apidata"), e.Element("templates"),
                              e.Element("parameters"), e.Element("containers") })
                            {
                                if(typeEl != null)
                                    extensionMethod.Add(new XElement(typeEl));
                            }

                            referenceLink.Add(new XAttribute("display-target", "extension"),
                                new XAttribute("show-parameters", showParameters), extensionMethod);
                            break;

                        default:
                            referenceLink.Add(new XAttribute("show-parameters", showParameters));
                            break;
                    }

                    table.Add(new XElement("tr", new XElement("td", referenceLink), summaryCell));

                    var summary = e.Element("summary");

                    if(summary != null)
                        transformation.RenderChildElements(summaryCell, summary.Nodes());

                    if(transformation.ApiMember.ApiTopicSubgroup != ApiMemberGroup.Overload)
                    {
                        if(memberType == ApiMemberGroup.Extension)
                        {
                            var parameter = new XElement("parameter");

                            summaryCell.Add(new XElement("br"),
                                new XElement("include", new XAttribute("item", "definedBy"),
                                parameter));

                            transformation.RenderTypeReferenceLink(parameter, e.Element("containers").Element("type"), false);
                        }
                        else
                        {
                            if(transformation.ApiMember.TypeTopicId != e.Element("containers").Element("type").Attribute("api").Value)
                            {
                                var parameter = new XElement("parameter");

                                summaryCell.Add(new XElement("br"),
                                    new XElement("include", new XAttribute("item", "inheritedFrom"),
                                    parameter));

                                transformation.RenderTypeReferenceLink(parameter, e.Element("containers").Element("type"), false);
                            }
                            else
                            {
                                if(e.Element("overrides")?.Element("member") != null)
                                {
                                    var parameter = new XElement("parameter");

                                    summaryCell.Add(new XElement("br"),
                                        new XElement("include", new XAttribute("item", "overridesMember"),
                                        parameter));

                                    transformation.RenderTypeReferenceLink(parameter, e.Element("overrides").Element("member"), true);
                                }
                            }
                        }
                    }

                    var obsoleteAttr = e.AttributeOfType("T:System.ObsoleteAttribute");
                    var prelimComment = e.Element("preliminary");

                    if(obsoleteAttr != null || prelimComment != null)
                    {
                        if(!summaryCell.IsEmpty)
                            summaryCell.Add(new XElement("br"));

                        if(obsoleteAttr != null)
                        {
                            summaryCell.Add(new XElement("strong",
                                new XElement("include", new XAttribute("item", "boilerplate_obsoleteShort"))));
                        }

                        if(prelimComment != null)
                        {
                            if(obsoleteAttr != null)
                                summaryCell.Add("&#160;&#160;");

                            summaryCell.Add(new XElement("em",
                                new XElement("include", new XAttribute("item", "preliminaryShort"))));
                        }
                    }

                    if(summaryCell.IsEmpty)
                        summaryCell.Add(Element.NonBreakingSpace);
                }
            }
        }

        /// <summary>
        /// Render a section with a title and a table containing the element content
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        /// <param name="sectionTitleItem">The section title include item</param>
        /// <param name="sectionElements">An enumerable list of the elements to render in the table</param>
        private static void RenderApiSectionTable(TopicTransformationCore transformation, string sectionTitleItem,
          IEnumerable<XElement> sectionElements)
        {
            if(sectionElements.Any())
            {
                var (title, _) = transformation.CreateSection(sectionElements.First().GenerateUniqueId(), true,
                    sectionTitleItem, null);

                transformation.CurrentElement.Add(title);

                var table = new XElement("table");

               transformation.CurrentElement.Add(table);
               
                foreach(var se in sectionElements)
                {
                    var descCell = new XElement("td");
                    
                    table.Add(new XElement("tr",
                        new XElement("td",
                            new XElement("referenceLink",
                                new XAttribute("target", se.Attribute("cref")?.Value ?? String.Empty),
                                new XAttribute("qualified", "false"))),
                        descCell));
                    
                    transformation.RenderChildElements(descCell, se.Nodes());
                }
            }
        }

        /// <summary>
        /// This is used to render the remarks section
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiRemarksSection(TopicTransformationCore transformation)
        {
            // For overloads, render remarks from the first overloads element.  There should only be one.
            if(transformation.ApiMember.ApiTopicSubgroup != ApiMemberGroup.Overload)
                transformation.RenderNode(transformation.CommentsNode.Element("remarks"));
            else
            {
                var overloads = transformation.ReferenceNode.Descendants("overloads").FirstOrDefault();

                if(overloads != null)
                    transformation.RenderNode(overloads.Element("remarks"));
            }
        }

        /// <summary>
        /// This is used to render the examples section
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiExamplesSection(TopicTransformationCore transformation)
        {
            // For overloads, render examples from the overloads element.  There should only be one.
            if(transformation.ApiMember.ApiTopicSubgroup != ApiMemberGroup.Overload)
                transformation.RenderNode(transformation.CommentsNode.Element("example"));
            else
            {
                var overloads = transformation.ReferenceNode.Descendants("overloads").FirstOrDefault();

                if(overloads != null)
                    transformation.RenderNode(overloads.Element("example"));
            }
        }

        /// <summary>
        /// This is used to render the versions section
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiVersionsSection(TopicTransformationCore transformation)
        {
            // Only API member pages get version information
            if(transformation.ApiMember.ApiTopicGroup != ApiMemberGroup.List &&
               transformation.ApiMember.ApiTopicGroup != ApiMemberGroup.RootGroup &&
               transformation.ApiMember.ApiTopicGroup != ApiMemberGroup.Root &&
               transformation.ApiMember.ApiTopicGroup != ApiMemberGroup.NamespaceGroup &&
               transformation.ApiMember.ApiTopicGroup != ApiMemberGroup.Namespace)
            {
                foreach(var v in transformation.ReferenceNode.Elements("versions"))
                    transformation.RenderNode(v);
            }
        }

        /// <summary>
        /// Render the revision history section if applicable
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiRevisionHistorySection(TopicTransformationCore transformation)
        {
            var revisionHistory = transformation.CommentsNode.Element("revisionHistory");

            if(revisionHistory == null || revisionHistory.Attribute("visible")?.Value == "false")
                return;

            var revisions = revisionHistory.Elements("revision").Where(
                h => h.Attribute("visible")?.Value != "false");

            if(revisions.Any())
            {
                var (title, _) = transformation.CreateSection(revisionHistory.GenerateUniqueId(), true,
                    "title_revisionHistory", null);

                transformation.CurrentElement.Add(title);

                var table = new XElement("table",
                    new XElement("thead",
                        new XElement("tr",
                            new XElement("th",
                                new XElement("include", new XAttribute("item", "header_revHistoryDate"))),
                            new XElement("th",
                                new XElement("include", new XAttribute("item", "header_revHistoryVersion"))),
                            new XElement("th",
                                new XElement("include", new XAttribute("item", "header_revHistoryDescription"))))));

                transformation.CurrentElement.Add(table);

                foreach(var rh in revisions)
                {
                    var descCell = new XElement("td");

                    table.Add(new XElement("tr",
                        new XElement("td", rh.Attribute("date")?.Value),
                        new XElement("td", rh.Attribute("version")?.Value),
                        descCell));

                    transformation.RenderChildElements(descCell, rh.Nodes());
                }
            }
        }

        /// <summary>
        /// Render the bibliography section if applicable
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiBibliographySection(TopicTransformationCore transformation)
        {
            if(transformation.ElementHandlerFor("bibliography") is BibliographyElement b)
            {
                if(b.DetermineCitations(transformation).Count != 0)
                {
                    // Use the first citation element as the element for rendering.  It's only needed to create
                    // a unique ID for the section.
                    var cite = transformation.DocumentNode.Descendants("cite").First();

                    b.Render(transformation, cite);
                }
            }
        }

        /// <summary>
        /// This renders the See Also section if applicable
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        private static void RenderApiSeeAlsoSection(TopicTransformationCore transformation)
        {
            // Render the see and seealso links using the see element handler as the processing is the same
            Element seeHandler = transformation.ElementHandlerFor("see"),
                conceptualLinkHandler = transformation.ElementHandlerFor("conceptualLink");

            // Get see also elements from comments excluding those in overloads comments
            List<XElement> seeAlsoNotInOverloads = transformation.CommentsNode.Descendants("seealso").Where(
                    s => !s.Ancestors("overloads").Any()).ToList(),
                seeAlsoHRef = seeAlsoNotInOverloads.Where(s => s.Attribute("href") != null).ToList(),
                seeAlsoCRef = seeAlsoNotInOverloads.Except(seeAlsoHRef).ToList();

            // Combine those with see also elements from element overloads comments
            var elements = transformation.ReferenceNode.Element("elements") ?? new XElement("elements");
            var elementOverloads = elements.Elements("element").SelectMany(e => e.Descendants("overloads")).ToList();

            seeAlsoHRef.AddRange(elementOverloads.Descendants("seealso").Where(s => s.Attribute("href") != null));
            seeAlsoCRef.AddRange(elementOverloads.Descendants("seealso").Where(s => s.Attribute("href") == null));

            // Get unique conceptual links from comments excluding those in overloads comments and combine them
            // with those in element overloads comments.
            var conceptualLinks = transformation.CommentsNode.Descendants("conceptualLink").Where(
                s => !s.Ancestors("overloads").Any()).Concat(
                    elementOverloads.Descendants("conceptualLink")).GroupBy(
                        c => c.Attribute("target")?.Value ?? String.Empty).Where(g => g.Key.Length != 0).Select(
                        g => g.First()).ToList();

            if(seeAlsoCRef.Count != 0 || seeAlsoHRef.Count != 0 || conceptualLinks.Count != 0 ||
              transformation.ApiMember.ApiTopicGroup == ApiMemberGroup.Type ||
              transformation.ApiMember.ApiTopicGroup == ApiMemberGroup.Member ||
              transformation.ApiMember.ApiTopicGroup == ApiMemberGroup.List)
            {
                // This has a fixed ID that matches the one used in MAML topics for the related topics section
                var (title, _) = transformation.CreateSection("seeAlso", true, "title_relatedTopics", null);

                transformation.CurrentElement.Add(title);

                if(seeAlsoCRef.Count != 0 || transformation.ApiMember.ApiTopicGroup == ApiMemberGroup.Type ||
                  transformation.ApiMember.ApiTopicGroup == ApiMemberGroup.Member ||
                  transformation.ApiMember.ApiTopicGroup == ApiMemberGroup.List)
                {
                    var (subtitle, _) = transformation.CreateSubsection(true, "title_seeAlso_reference");

                    if(subtitle != null)
                        transformation.CurrentElement.Add(subtitle);

                    RenderApiAutoGeneratedSeeAlsoLinks(transformation, transformation.CurrentElement);

                    if(seeHandler != null)
                    {
                        foreach(var s in seeAlsoCRef)
                        {
                            seeHandler.Render(transformation, s);
                            transformation.CurrentElement.Add(new XElement("br"));
                        }
                    }
                }

                if((seeAlsoHRef.Count != 0 && seeHandler != null) || (conceptualLinks.Count != 0 &&
                  conceptualLinkHandler != null))
                {
                    var (subtitle, _) = transformation.CreateSubsection(true, "title_seeAlso_otherResources");

                    if(subtitle != null)
                        transformation.CurrentElement.Add(subtitle);

                    if(seeHandler != null)
                    {
                        foreach(var s in seeAlsoHRef)
                        {
                            seeHandler.Render(transformation, s);
                            transformation.CurrentElement.Add("  ", new XElement("br"));
                        }
                    }

                    if(conceptualLinkHandler != null)
                    {
                        foreach(var c in conceptualLinks)
                        {
                            conceptualLinkHandler.Render(transformation, c);
                            transformation.CurrentElement.Add("  ", new XElement("br"));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Render auto-generated see also section links based on the API topic
        /// </summary>
        /// <param name="transformation">The topic transformation to use</param>
        /// <param name="subsection">The subsection to which the links are added</param>
        private static void RenderApiAutoGeneratedSeeAlsoLinks(TopicTransformationCore transformation,
          XElement subsection)
        {
            // Add a link to the containing type on all list and member topics
            if(transformation.ApiMember.ApiTopicGroup == ApiMemberGroup.Member ||
              transformation.ApiMember.ApiTopicGroup == ApiMemberGroup.List)
            {
                subsection.Add(new XElement("referenceLink",
                        new XAttribute("target", transformation.ApiMember.TypeTopicId),
                        new XAttribute("display-target", "format"),
                        new XElement("include",
                            new XAttribute("item", "boilerplate_seeAlsoTypeLink"),
                            new XElement("parameter", "{0}"),
                            new XElement("parameter", transformation.ApiMember.TypeApiSubgroup))), "  ", new XElement("br"));
            }

            // Add a link to the overload topic
            if(!String.IsNullOrWhiteSpace(transformation.ApiMember.OverloadTopicId))
            {
                subsection.Add(new XElement("referenceLink",
                        new XAttribute("target", transformation.ApiMember.OverloadTopicId),
                        new XAttribute("display-target", "format"),
                        new XAttribute("show-parameters", "false"),
                        new XElement("include",
                            new XAttribute("item", "boilerplate_seeAlsoOverloadLink"),
                            new XElement("parameter", "{0}"))), "  ", new XElement("br"));
            }

            // Add a link to the namespace topic
            string namespaceId = transformation.ReferenceNode.Element("containers")?.Element("namespace")?.Attribute("api")?.Value;

            if(!String.IsNullOrWhiteSpace(namespaceId))
            {
                subsection.Add(new XElement("referenceLink",
                        new XAttribute("target", namespaceId),
                        new XAttribute("display-target", "format"),
                        new XElement("include",
                            new XAttribute("item", "boilerplate_seeAlsoNamespaceLink"),
                            new XElement("parameter", "{0}"))));
            }
        }
        #endregion
    }
}
