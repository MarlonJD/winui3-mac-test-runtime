using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using WinUI3.MacCompatibility;

namespace WinUI3.MacXaml;

public sealed class MacXamlCompiler
{
    private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
    private static readonly HashSet<string> CommonFrameworkProperties = new(StringComparer.Ordinal)
    {
        "Background",
        "DataContext",
        "Foreground",
        "HorizontalAlignment",
        "Style",
        "Tag",
        "Uid",
        "VerticalAlignment",
        "Visibility"
    };

    private static readonly HashSet<string> ControlProperties = new(StringComparer.Ordinal)
    {
        "IsEnabled"
    };

    private static readonly Dictionary<string, HashSet<string>> ElementProperties = new(StringComparer.Ordinal)
    {
        ["Application"] = new(StringComparer.Ordinal) { "Resources" },
        ["Window"] = new(StringComparer.Ordinal) { "Content", "Resources", "SystemBackdrop", "Title" },
        ["Page"] = new(StringComparer.Ordinal) { "Content", "Resources" },
        ["UserControl"] = new(StringComparer.Ordinal) { "Content", "Resources" },
        ["StackPanel"] = new(StringComparer.Ordinal) { "Orientation", "Padding", "Spacing" },
        ["Grid"] = new(StringComparer.Ordinal) { "ColumnDefinitions", "ColumnSpacing" },
        ["Border"] = new(StringComparer.Ordinal) { "Child", "CornerRadius" },
        ["ScrollViewer"] = new(StringComparer.Ordinal) { "Content", "VerticalScrollBarVisibility" },
        ["ContentControl"] = new(StringComparer.Ordinal) { "Content" },
        ["ItemsControl"] = new(StringComparer.Ordinal) { "Items" },
        ["TextBlock"] = new(StringComparer.Ordinal) { "Text" },
        ["TextBox"] = new(StringComparer.Ordinal) { "Text" },
        ["Button"] = new(StringComparer.Ordinal) { "Command", "CommandParameter", "Content" },
        ["AppBarButton"] = new(StringComparer.Ordinal) { "Command", "CommandParameter", "Content", "Icon", "Label" },
        ["ToggleButton"] = new(StringComparer.Ordinal) { "Command", "CommandParameter", "Content", "IsChecked" },
        ["CheckBox"] = new(StringComparer.Ordinal) { "Command", "CommandParameter", "Content", "IsChecked" },
        ["RadioButton"] = new(StringComparer.Ordinal) { "Command", "CommandParameter", "Content", "GroupName", "IsChecked" },
        ["ComboBox"] = new(StringComparer.Ordinal) { "Items", "PlaceholderText", "SelectedIndex" },
        ["Image"] = new(StringComparer.Ordinal) { "Source" },
        ["ListView"] = new(StringComparer.Ordinal) { "Items", "SelectedIndex" },
        ["ProgressRing"] = new(StringComparer.Ordinal) { "IsActive" },
        ["ProgressBar"] = new(StringComparer.Ordinal) { "IsIndeterminate", "Maximum", "Minimum", "Value" },
        ["InfoBar"] = new(StringComparer.Ordinal) { "IsOpen", "Message", "Severity", "Title" },
        ["CommandBar"] = new(StringComparer.Ordinal) { "PrimaryCommands" },
        ["FontIcon"] = new(StringComparer.Ordinal) { "FontSize", "Glyph" },
        ["Frame"] = new(StringComparer.Ordinal) { "Content" },
        ["NavigationView"] = new(StringComparer.Ordinal)
        {
            "CompactPaneLength",
            "Content",
            "IsBackButtonVisible",
            "IsSettingsVisible",
            "MenuItems",
            "OpenPaneLength",
            "PaneDisplayMode",
            "PaneFooter"
        },
        ["NavigationViewItem"] = new(StringComparer.Ordinal) { "Content", "Icon" },
        ["ResourceDictionary"] = new(StringComparer.Ordinal)
    };

    private static readonly Dictionary<string, HashSet<string>> ElementEvents = new(StringComparer.Ordinal)
    {
        ["Button"] = new(StringComparer.Ordinal) { "Click" },
        ["AppBarButton"] = new(StringComparer.Ordinal) { "Click" },
        ["ToggleButton"] = new(StringComparer.Ordinal) { "Click" },
        ["CheckBox"] = new(StringComparer.Ordinal) { "Click" },
        ["RadioButton"] = new(StringComparer.Ordinal) { "Click" },
        ["NavigationView"] = new(StringComparer.Ordinal) { "SelectionChanged" }
    };

    private static readonly Dictionary<string, HashSet<string>> ElementPropertyElements = new(StringComparer.Ordinal)
    {
        ["Application"] = new(StringComparer.Ordinal) { "Resources" },
        ["Window"] = new(StringComparer.Ordinal) { "Resources" },
        ["Page"] = new(StringComparer.Ordinal) { "Resources" },
        ["UserControl"] = new(StringComparer.Ordinal) { "Resources" },
        ["NavigationView"] = new(StringComparer.Ordinal) { "MenuItems", "PaneFooter", "Content" },
        ["NavigationViewItem"] = new(StringComparer.Ordinal) { "Icon", "Content" },
        ["Border"] = new(StringComparer.Ordinal) { "Child" },
        ["ScrollViewer"] = new(StringComparer.Ordinal) { "Content" },
        ["ContentControl"] = new(StringComparer.Ordinal) { "Content" },
        ["ItemsControl"] = new(StringComparer.Ordinal) { "Items" },
        ["ListView"] = new(StringComparer.Ordinal) { "Items" },
        ["ComboBox"] = new(StringComparer.Ordinal) { "Items" },
        ["Frame"] = new(StringComparer.Ordinal) { "Content" },
        ["CommandBar"] = new(StringComparer.Ordinal) { "PrimaryCommands" },
        ["Button"] = new(StringComparer.Ordinal) { "Content" },
        ["AppBarButton"] = new(StringComparer.Ordinal) { "Content", "Icon" },
        ["ToggleButton"] = new(StringComparer.Ordinal) { "Content" },
        ["CheckBox"] = new(StringComparer.Ordinal) { "Content" },
        ["RadioButton"] = new(StringComparer.Ordinal) { "Content" }
    };

    private static readonly HashSet<string> SupportedAttachedProperties = new(StringComparer.Ordinal)
    {
        "AutomationProperties.Name",
        "AutomationProperties.HelpText",
        "Grid.Column"
    };

    public XamlCompilationResult CompileFile(string xamlPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xamlPath);

        var document = XDocument.Load(xamlPath, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
        return CompileDocument(document, xamlPath);
    }

    public XamlCompilationResult CompileText(string xaml, string filePath = "inline.xaml")
    {
        ArgumentNullException.ThrowIfNull(xaml);

        var document = XDocument.Parse(xaml, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
        return CompileDocument(document, filePath);
    }

    private static XamlCompilationResult CompileDocument(XDocument document, string filePath)
    {
        if (document.Root is null)
        {
            return Failure("XAML0001", "XAML document does not have a root element.", filePath, null);
        }

        var diagnostics = new List<XamlDiagnostic>();
        var root = document.Root;
        var xClass = ReadXamlAttribute(root, "Class");
        if (string.IsNullOrWhiteSpace(xClass))
        {
            diagnostics.Add(CreateDiagnostic("XAML0002", "Root element must declare x:Class.", "Error", filePath, root));
            return new XamlCompilationResult(false, string.Empty, diagnostics);
        }

        var className = CSharpClassName.Parse(xClass);
        var context = new GenerationContext(filePath, diagnostics);
        var rootType = MapType(root, context);
        if (rootType is null)
        {
            return new XamlCompilationResult(false, string.Empty, diagnostics);
        }

        var source = new StringBuilder();
        source.AppendLine("// <auto-generated />");
        source.AppendLine("#nullable enable");
        source.AppendLine();
        source.AppendLine($"namespace {className.Namespace}");
        source.AppendLine("{");
        source.AppendLine();
        source.AppendLine($"public sealed partial class {className.Name} : {rootType}");
        source.AppendLine("{");

        var model = XamlObjectModel.FromRoot(root, context);
        foreach (var namedElement in model.NamedElements)
        {
            source.AppendLine($"    public {namedElement.TypeName} {namedElement.FieldName} {{ get; private set; }} = null!;");
        }

        source.AppendLine();
        source.AppendLine("    public void InitializeComponent()");
        source.AppendLine("    {");

        var writer = new XamlCodeWriter(source);
        writer.WriteResources(model.AllResources());
        writer.WriteRoot(model);

        source.AppendLine("    }");
        source.AppendLine("}");
        source.AppendLine("}");

        return new XamlCompilationResult(
            diagnostics.All(diagnostic => diagnostic.Severity != "Error"),
            source.ToString(),
            diagnostics);
    }

    private static XamlCompilationResult Failure(string code, string message, string filePath, XElement? element)
    {
        return new XamlCompilationResult(
            false,
            string.Empty,
            new[] { CreateDiagnostic(code, message, "Error", filePath, element) });
    }

    private static string? MapType(XElement element, GenerationContext context)
    {
        var typeName = element.Name.LocalName;
        var mapped = typeName switch
        {
            "Application" => "Microsoft.UI.Xaml.Application",
            "Window" => "Microsoft.UI.Xaml.Window",
            "Page" => "Microsoft.UI.Xaml.Controls.Page",
            "UserControl" => "Microsoft.UI.Xaml.Controls.UserControl",
            "StackPanel" => "Microsoft.UI.Xaml.Controls.StackPanel",
            "ScrollViewer" => "Microsoft.UI.Xaml.Controls.ScrollViewer",
            "ContentControl" => "Microsoft.UI.Xaml.Controls.ContentControl",
            "ItemsControl" => "Microsoft.UI.Xaml.Controls.ItemsControl",
            "TextBlock" => "Microsoft.UI.Xaml.Controls.TextBlock",
            "TextBox" => "Microsoft.UI.Xaml.Controls.TextBox",
            "Button" => "Microsoft.UI.Xaml.Controls.Button",
            "AppBarButton" => "Microsoft.UI.Xaml.Controls.AppBarButton",
            "ToggleButton" => "Microsoft.UI.Xaml.Controls.ToggleButton",
            "CheckBox" => "Microsoft.UI.Xaml.Controls.CheckBox",
            "RadioButton" => "Microsoft.UI.Xaml.Controls.RadioButton",
            "ComboBox" => "Microsoft.UI.Xaml.Controls.ComboBox",
            "Image" => "Microsoft.UI.Xaml.Controls.Image",
            "ListView" => "Microsoft.UI.Xaml.Controls.ListView",
            "ProgressRing" => "Microsoft.UI.Xaml.Controls.ProgressRing",
            "ProgressBar" => "Microsoft.UI.Xaml.Controls.ProgressBar",
            "InfoBar" => "Microsoft.UI.Xaml.Controls.InfoBar",
            "CommandBar" => "Microsoft.UI.Xaml.Controls.CommandBar",
            "Grid" => "Microsoft.UI.Xaml.Controls.Grid",
            "Border" => "Microsoft.UI.Xaml.Controls.Border",
            "FontIcon" => "Microsoft.UI.Xaml.Controls.FontIcon",
            "Frame" => "Microsoft.UI.Xaml.Controls.Frame",
            "NavigationView" => "Microsoft.UI.Xaml.Controls.NavigationView",
            "NavigationViewItem" => "Microsoft.UI.Xaml.Controls.NavigationViewItem",
            "ResourceDictionary" => "Microsoft.UI.Xaml.ResourceDictionary",
            _ => null
        };

        if (mapped is null)
        {
            context.Diagnostics.Add(CreateCompatibilityDiagnostic(
                "XAML1001",
                "XAML element",
                typeName,
                CompatibilityCatalog.Current.FindXamlElement(typeName),
                context.FilePath,
                element));
        }

        return mapped;
    }

    private static string? ReadXamlAttribute(XElement element, string localName)
    {
        return element.Attribute(XamlNamespace + localName)?.Value;
    }

    private static XamlDiagnostic CreateDiagnostic(
        string code,
        string message,
        string severity,
        string? filePath,
        XElement? element)
    {
        var lineInfo = element as IXmlLineInfo;
        return new XamlDiagnostic(
            code,
            message,
            severity,
            filePath,
            lineInfo?.HasLineInfo() == true ? lineInfo.LineNumber : null,
            lineInfo?.HasLineInfo() == true ? lineInfo.LinePosition : null);
    }

    private static XamlDiagnostic CreateCompatibilityDiagnostic(
        string code,
        string kind,
        string construct,
        CompatibilityCatalogEntry? catalogEntry,
        string? filePath,
        XElement? element)
    {
        var message = catalogEntry is null
            ? $"Unsupported {kind} '{construct}' is not present in the WinUI compatibility catalog."
            : $"Unsupported {kind} '{construct}' is cataloged as {catalogEntry.Status}.";
        return CreateDiagnostic(code, message, "Error", filePath, element);
    }

    private sealed record CSharpClassName(string Namespace, string Name)
    {
        public static CSharpClassName Parse(string xClass)
        {
            var lastDot = xClass.LastIndexOf('.');
            if (lastDot < 0)
            {
                return new CSharpClassName("GlobalNamespace", xClass);
            }

            return new CSharpClassName(xClass[..lastDot], xClass[(lastDot + 1)..]);
        }
    }

    private sealed class GenerationContext
    {
        public GenerationContext(string filePath, List<XamlDiagnostic> diagnostics)
        {
            FilePath = filePath;
            Diagnostics = diagnostics;
        }

        public string FilePath { get; }

        public List<XamlDiagnostic> Diagnostics { get; }
    }

    private sealed record NamedElement(string FieldName, string TypeName);

    private sealed record ResourceEntry(
        string Key,
        string? Value,
        StyleResource? Style,
        IReadOnlyList<ThemeDictionaryResource>? ThemeDictionaries = null);

    private sealed record ThemeDictionaryResource(string Theme, IReadOnlyList<ResourceEntry> Entries);

    private sealed record StyleResource(string? TargetType, IReadOnlyList<SetterResource> Setters);

    private sealed record SetterResource(string Property, string Value);

    private sealed record PropertyElementChildren(string PropertyName, IReadOnlyList<XamlObjectModel> Children);

    private sealed class XamlObjectModel
    {
        public XamlObjectModel(
            XElement element,
            string typeName,
            string variableName,
            string? name,
            IReadOnlyDictionary<string, string> properties,
            IReadOnlyDictionary<string, string> events,
            IReadOnlyList<ResourceEntry> resources,
            IReadOnlyList<XamlObjectModel> children,
            IReadOnlyList<PropertyElementChildren> propertyChildren,
            IReadOnlyList<NamedElement> namedElements)
        {
            Element = element;
            TypeName = typeName;
            VariableName = variableName;
            Name = name;
            Properties = properties;
            Events = events;
            Resources = resources;
            Children = children;
            PropertyChildren = propertyChildren;
            NamedElements = namedElements;
        }

        public XElement Element { get; }

        public string TypeName { get; }

        public string VariableName { get; }

        public string? Name { get; }

        public IReadOnlyDictionary<string, string> Properties { get; }

        public IReadOnlyDictionary<string, string> Events { get; }

        public IReadOnlyList<ResourceEntry> Resources { get; }

        public IReadOnlyList<XamlObjectModel> Children { get; }

        public IReadOnlyList<PropertyElementChildren> PropertyChildren { get; }

        public IReadOnlyList<NamedElement> NamedElements { get; }

        public IReadOnlyList<ResourceEntry> AllResources()
        {
            var resources = new List<ResourceEntry>(Resources);
            AddChildResources(resources, Children);
            foreach (var propertyChildren in PropertyChildren)
            {
                AddChildResources(resources, propertyChildren.Children);
            }

            return resources;
        }

        private static void AddChildResources(List<ResourceEntry> resources, IReadOnlyList<XamlObjectModel> children)
        {
            foreach (var child in children)
            {
                resources.AddRange(child.Resources);
                AddChildResources(resources, child.Children);
                foreach (var propertyChildren in child.PropertyChildren)
                {
                    AddChildResources(resources, propertyChildren.Children);
                }
            }
        }

        public static XamlObjectModel FromRoot(XElement root, GenerationContext context)
        {
            var counter = 0;
            var namedElements = new List<NamedElement>();
            return FromElement(root, "this", context, namedElements, ref counter);
        }

        private static XamlObjectModel FromElement(
            XElement element,
            string variableName,
            GenerationContext context,
            List<NamedElement> namedElements,
            ref int counter)
        {
            var typeName = MapType(element, context) ?? "object";
            var name = ReadXamlAttribute(element, "Name") ?? element.Attribute("Name")?.Value;
            var properties = new Dictionary<string, string>();
            var events = new Dictionary<string, string>();
            var resources = ReadResources(element, context);
            var children = new List<XamlObjectModel>();
            var propertyChildren = new List<PropertyElementChildren>();

            foreach (var attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration)
                {
                    continue;
                }

                var localName = attribute.Name.LocalName;
                if (attribute.Name.Namespace == XamlNamespace)
                {
                    if (localName == "Name")
                    {
                        continue;
                    }

                    if (localName == "Uid")
                    {
                        properties["Uid"] = attribute.Value;
                        continue;
                    }

                    if (localName == "Class")
                    {
                        continue;
                    }

                    context.Diagnostics.Add(CreateCompatibilityDiagnostic(
                        "XAML1004",
                        "XAML directive",
                        $"x:{localName}",
                        CompatibilityCatalog.Current.FindXamlDirective($"x:{localName}"),
                        context.FilePath,
                        element));
                    continue;
                }

                if (SupportedAttachedProperties.Contains(localName) &&
                    localName is "AutomationProperties.Name" or "AutomationProperties.HelpText")
                {
                    properties[localName] = attribute.Value;
                    continue;
                }

                if (SupportedAttachedProperties.Contains(localName) && localName == "Grid.Column")
                {
                    properties[localName] = attribute.Value;
                    continue;
                }

                if (localName == "Name")
                {
                    continue;
                }

                if (localName.Contains('.', StringComparison.Ordinal))
                {
                    context.Diagnostics.Add(CreateCompatibilityDiagnostic(
                        "XAML1005",
                        "attached property",
                        $"{element.Name.LocalName}.{localName}",
                        CompatibilityCatalog.Current.FindXamlAttachedProperty(localName),
                        context.FilePath,
                        element));
                    continue;
                }

                if (IsSupportedEvent(element.Name.LocalName, localName))
                {
                    events[localName] = attribute.Value;
                    continue;
                }

                if (LooksLikeEvent(localName))
                {
                    context.Diagnostics.Add(CreateCompatibilityDiagnostic(
                        "XAML1006",
                        "event",
                        $"{element.Name.LocalName}.{localName}",
                        CompatibilityCatalog.Current.FindXamlEvent(element.Name.LocalName, localName),
                        context.FilePath,
                        element));
                    continue;
                }

                if (!IsSupportedProperty(element.Name.LocalName, localName))
                {
                    context.Diagnostics.Add(CreateCompatibilityDiagnostic(
                        "XAML1002",
                        "XAML property",
                        $"{element.Name.LocalName}.{localName}",
                        CompatibilityCatalog.Current.FindXamlProperty(element.Name.LocalName, localName),
                        context.FilePath,
                        element));
                    continue;
                }

                if (IsUnsupportedMarkupExtension(attribute.Value, out var markupName, out var markupEntry))
                {
                    context.Diagnostics.Add(CreateCompatibilityDiagnostic(
                        "XAML1007",
                        "markup extension",
                        markupName,
                        markupEntry,
                        context.FilePath,
                        element));
                    continue;
                }

                properties[localName] = attribute.Value;
            }

            var textContent = ReadElementTextContent(element);
            if (!string.IsNullOrWhiteSpace(textContent))
            {
                if (element.Name.LocalName == "TextBlock")
                {
                    properties.TryAdd("Text", textContent.Trim());
                }
                else if (element.Name.LocalName == "Button")
                {
                    properties.TryAdd("Content", textContent.Trim());
                }
            }

            foreach (var child in element.Elements())
            {
                if (IsResourceChild(child))
                {
                    continue;
                }

                if (IsPropertyElement(child))
                {
                    var propertyName = ReadPropertyElementName(child);
                    if (!IsSupportedPropertyElement(element.Name.LocalName, propertyName))
                    {
                        context.Diagnostics.Add(CreateCompatibilityDiagnostic(
                            "XAML1003",
                            "property element",
                            child.Name.LocalName,
                            CompatibilityCatalog.Current.FindXamlPropertyElement(
                                element.Name.LocalName,
                                propertyName),
                            context.FilePath,
                            child));
                        continue;
                    }

                    var values = new List<XamlObjectModel>();
                    foreach (var propertyChild in child.Elements())
                    {
                        counter++;
                        var childVariable = $"__element{counter.ToString(CultureInfo.InvariantCulture)}";
                        values.Add(FromElement(propertyChild, childVariable, context, namedElements, ref counter));
                    }

                    propertyChildren.Add(new PropertyElementChildren(propertyName, values));
                    continue;
                }

                counter++;
                var normalChildVariable = $"__element{counter.ToString(CultureInfo.InvariantCulture)}";
                children.Add(FromElement(child, normalChildVariable, context, namedElements, ref counter));
            }

            if (name is not null)
            {
                namedElements.Add(new NamedElement(name, typeName));
            }

            return new XamlObjectModel(element, typeName, variableName, name, properties, events, resources, children, propertyChildren, namedElements);
        }

        private static bool IsUnsupportedMarkupExtension(
            string value,
            out string name,
            out CompatibilityCatalogEntry? entry)
        {
            name = string.Empty;
            entry = null;
            var trimmed = value.Trim();
            if (trimmed.Length < 2 || trimmed[0] != '{' || trimmed[^1] != '}')
            {
                return false;
            }

            if (trimmed.StartsWith("{}", StringComparison.Ordinal))
            {
                // {} is the XAML escape for a literal value that begins with a brace.
                return false;
            }

            var inner = trimmed[1..^1].TrimStart();
            var separator = inner.IndexOf(' ', StringComparison.Ordinal);
            name = (separator < 0 ? inner : inner[..separator]).Trim();
            if (name is "StaticResource" or "ThemeResource" or "Binding")
            {
                // Recognized markup extensions handled by the code writer.
                return false;
            }

            entry = name.StartsWith("x:", StringComparison.Ordinal)
                ? CompatibilityCatalog.Current.FindXamlDirective(name)
                : CompatibilityCatalog.Current.FindByApi(name);
            return true;
        }

        private static bool IsSupportedProperty(string elementName, string propertyName)
        {
            if (CommonFrameworkProperties.Contains(propertyName))
            {
                return true;
            }

            if (IsControl(elementName) && ControlProperties.Contains(propertyName))
            {
                return true;
            }

            return ElementProperties.TryGetValue(elementName, out var properties) && properties.Contains(propertyName);
        }

        private static bool IsSupportedEvent(string elementName, string eventName)
        {
            return ElementEvents.TryGetValue(elementName, out var events) && events.Contains(eventName);
        }

        private static bool LooksLikeEvent(string localName)
        {
            return localName is "Click" or "SelectionChanged";
        }

        private static bool IsSupportedPropertyElement(string elementName, string propertyName)
        {
            return ElementPropertyElements.TryGetValue(elementName, out var properties) && properties.Contains(propertyName);
        }

        private static bool IsControl(string elementName)
        {
            return elementName is
                "Button" or
                "AppBarButton" or
                "ToggleButton" or
                "CheckBox" or
                "RadioButton" or
                "TextBox" or
                "Image" or
                "ItemsControl" or
                "ListView" or
                "ComboBox" or
                "ScrollViewer" or
                "ProgressRing" or
                "ProgressBar" or
                "InfoBar" or
                "CommandBar" or
                "FontIcon" or
                "Frame" or
                "NavigationView" or
                "NavigationViewItem";
        }

        private static bool IsPropertyElement(XElement element)
        {
            return element.Name.LocalName.Contains('.', StringComparison.Ordinal);
        }

        private static bool IsResourceChild(XElement element)
        {
            return element.Name.LocalName.EndsWith(".Resources", StringComparison.Ordinal) ||
                element.Name.LocalName == "ResourceDictionary";
        }

        private static string ReadPropertyElementName(XElement element)
        {
            var localName = element.Name.LocalName;
            var dot = localName.IndexOf('.');
            return dot < 0 ? localName : localName[(dot + 1)..];
        }

        private static IReadOnlyList<ResourceEntry> ReadResources(XElement element, GenerationContext context)
        {
            var resourcesElement = element
                .Elements()
                .FirstOrDefault(child => child.Name.LocalName == element.Name.LocalName + ".Resources");
            if (resourcesElement is null)
            {
                return Array.Empty<ResourceEntry>();
            }

            var dictionary = resourcesElement.Elements().FirstOrDefault(child => child.Name.LocalName == "ResourceDictionary");
            if (dictionary is null)
            {
                context.Diagnostics.Add(CreateDiagnostic(
                    "XAML2001",
                    "Resources must use a ResourceDictionary element in Phase 1.",
                    "Error",
                    context.FilePath,
                    resourcesElement));
                return Array.Empty<ResourceEntry>();
            }

            return ReadDictionaryEntries(dictionary, context, allowThemeDictionaries: true);
        }

        private static IReadOnlyList<ResourceEntry> ReadDictionaryEntries(
            XElement dictionary,
            GenerationContext context,
            bool allowThemeDictionaries)
        {
            var resources = new List<ResourceEntry>();
            foreach (var resource in dictionary.Elements())
            {
                if (resource.Name.LocalName == "ResourceDictionary.MergedDictionaries")
                {
                    if (resource.Elements().Any(element => element.Name.LocalName == "XamlControlsResources"))
                    {
                        resources.Add(new ResourceEntry("XamlControlsResources", "present", null));
                    }

                    continue;
                }

                if (resource.Name.LocalName == "ResourceDictionary.ThemeDictionaries")
                {
                    resources.Add(new ResourceEntry(
                        "ResourceDictionary.ThemeDictionaries",
                        "present",
                        null,
                        allowThemeDictionaries ? ReadThemeDictionaries(resource, context) : Array.Empty<ThemeDictionaryResource>()));
                    continue;
                }

                if (resource.Name.LocalName == "XamlControlsResources")
                {
                    resources.Add(new ResourceEntry("XamlControlsResources", "present", null));
                    continue;
                }

                var key = ReadXamlAttribute(resource, "Key");
                if (string.IsNullOrWhiteSpace(key))
                {
                    context.Diagnostics.Add(CreateDiagnostic(
                        "XAML2002",
                        "Resource entries must declare x:Key.",
                        "Error",
                        context.FilePath,
                        resource));
                    continue;
                }

                resources.Add(resource.Name.LocalName == "Style"
                    ? new ResourceEntry(key, null, ReadStyle(resource, context))
                    : new ResourceEntry(key, ReadResourceValue(resource), null));
            }

            return resources;
        }

        private static IReadOnlyList<ThemeDictionaryResource> ReadThemeDictionaries(
            XElement themeDictionaries,
            GenerationContext context)
        {
            var dictionaries = new List<ThemeDictionaryResource>();
            foreach (var dictionary in themeDictionaries.Elements().Where(element => element.Name.LocalName == "ResourceDictionary"))
            {
                var themeKey = ReadXamlAttribute(dictionary, "Key");
                if (string.IsNullOrWhiteSpace(themeKey))
                {
                    context.Diagnostics.Add(CreateDiagnostic(
                        "XAML2002",
                        "Theme dictionaries must declare x:Key.",
                        "Error",
                        context.FilePath,
                        dictionary));
                    continue;
                }

                dictionaries.Add(new ThemeDictionaryResource(
                    themeKey,
                    ReadDictionaryEntries(dictionary, context, allowThemeDictionaries: false)));
            }

            return dictionaries;
        }

        private static string ReadResourceValue(XElement resource)
        {
            return resource.Name.LocalName switch
            {
                "SolidColorBrush" => resource.Attribute("Color")?.Value ?? resource.Value.Trim(),
                _ => resource.Value.Trim()
            };
        }

        private static StyleResource ReadStyle(XElement styleElement, GenerationContext context)
        {
            var targetType = styleElement.Attribute("TargetType")?.Value;
            var setters = new List<SetterResource>();
            foreach (var setter in styleElement.Elements().Where(element => element.Name.LocalName == "Setter"))
            {
                var property = setter.Attribute("Property")?.Value;
                var value = setter.Attribute("Value")?.Value;
                if (string.IsNullOrWhiteSpace(property) || value is null)
                {
                    context.Diagnostics.Add(CreateDiagnostic(
                        "XAML2003",
                        "Style setters must declare Property and Value.",
                        "Error",
                        context.FilePath,
                        setter));
                    continue;
                }

                setters.Add(new SetterResource(property, value));
            }

            return new StyleResource(targetType, setters);
        }

        private static string? ReadElementTextContent(XElement element)
        {
            var text = string.Concat(
                element
                    .Nodes()
                    .OfType<XText>()
                    .Select(node => node.Value));
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
    }

    private sealed class XamlCodeWriter
    {
        private readonly StringBuilder source;

        public XamlCodeWriter(StringBuilder source)
        {
            this.source = source;
        }

        public void WriteResources(IReadOnlyList<ResourceEntry> resources)
        {
            source.AppendLine("        var __resources = new Microsoft.UI.Xaml.ResourceDictionary();");
            WriteResourceEntries(resources, "__resources", "__style");

            source.AppendLine("        this.Resources.Clear();");
            source.AppendLine("        foreach (var __resource in __resources)");
            source.AppendLine("        {");
            source.AppendLine("            this.Resources[__resource.Key] = __resource.Value;");
            source.AppendLine("        }");
            source.AppendLine("        foreach (var __themeDictionary in __resources.ThemeDictionaries)");
            source.AppendLine("        {");
            source.AppendLine("            this.Resources.ThemeDictionaries[__themeDictionary.Key] = __themeDictionary.Value;");
            source.AppendLine("        }");
        }

        private void WriteResourceEntries(
            IReadOnlyList<ResourceEntry> resources,
            string dictionaryVariable,
            string stylePrefix)
        {
            foreach (var (resource, index) in resources.Select((resource, index) => (resource, index)))
            {
                if (resource.ThemeDictionaries is { } themeDictionaries)
                {
                    source.AppendLine($"        {dictionaryVariable}[{Literal(resource.Key)}] = {Literal(resource.Value ?? string.Empty)};");
                    foreach (var (themeDictionary, themeIndex) in themeDictionaries.Select((themeDictionary, themeIndex) => (themeDictionary, themeIndex)))
                    {
                        var themeVariable = $"__themeDictionary{index.ToString(CultureInfo.InvariantCulture)}_{themeIndex.ToString(CultureInfo.InvariantCulture)}";
                        source.AppendLine($"        var {themeVariable} = new Microsoft.UI.Xaml.ResourceDictionary();");
                        WriteResourceEntries(themeDictionary.Entries, themeVariable, $"{stylePrefix}{index.ToString(CultureInfo.InvariantCulture)}_{themeIndex.ToString(CultureInfo.InvariantCulture)}");
                        source.AppendLine($"        {dictionaryVariable}.ThemeDictionaries[{Literal(themeDictionary.Theme)}] = {themeVariable};");
                    }

                    continue;
                }

                if (resource.Style is { } style)
                {
                    var styleVariable = $"{stylePrefix}{index.ToString(CultureInfo.InvariantCulture)}";
                    source.AppendLine($"        var {styleVariable} = new Microsoft.UI.Xaml.Style {{ TargetType = {Literal(style.TargetType ?? string.Empty)} }};");
                    foreach (var setter in style.Setters)
                    {
                        source.AppendLine($"        {styleVariable}.Setters.Add(new Microsoft.UI.Xaml.Setter({Literal(setter.Property)}, {RenderValue(setter.Property, setter.Value)}));");
                    }

                    source.AppendLine($"        {dictionaryVariable}[{Literal(resource.Key)}] = {styleVariable};");
                    continue;
                }

                source.AppendLine($"        {dictionaryVariable}[{Literal(resource.Key)}] = {Literal(resource.Value ?? string.Empty)};");
            }
        }

        public void WriteRoot(XamlObjectModel model)
        {
            WriteProperties(model);
            WriteEvents(model);
            foreach (var child in model.Children)
            {
                WriteElement(child);
                AttachChild(model, child);
            }

            foreach (var propertyChildren in model.PropertyChildren)
            {
                foreach (var child in propertyChildren.Children)
                {
                    WriteElement(child);
                    AttachPropertyChild(model, propertyChildren.PropertyName, child);
                }
            }
        }

        private void WriteElement(XamlObjectModel model)
        {
            source.AppendLine($"        var {model.VariableName} = new {model.TypeName}();");
            WriteProperties(model);
            WriteEvents(model);

            if (model.Name is not null)
            {
                source.AppendLine($"        {model.Name} = {model.VariableName};");
            }

            foreach (var child in model.Children)
            {
                WriteElement(child);
                AttachChild(model, child);
            }

            foreach (var propertyChildren in model.PropertyChildren)
            {
                foreach (var child in propertyChildren.Children)
                {
                    WriteElement(child);
                    AttachPropertyChild(model, propertyChildren.PropertyName, child);
                }
            }
        }

        private void WriteProperties(XamlObjectModel model)
        {
            if (model.Name is not null)
            {
                source.AppendLine($"        {model.VariableName}.Name = {Literal(model.Name)};");
            }

            foreach (var property in model.Properties)
            {
                if (property.Key == "AutomationProperties.Name")
                {
                    source.AppendLine($"        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName({model.VariableName}, {Literal(property.Value)});");
                    continue;
                }

                if (property.Key == "AutomationProperties.HelpText")
                {
                    source.AppendLine($"        Microsoft.UI.Xaml.Automation.AutomationProperties.SetHelpText({model.VariableName}, {Literal(property.Value)});");
                    continue;
                }

                if (property.Key == "Grid.Column")
                {
                    source.AppendLine($"        Microsoft.UI.Xaml.Controls.Grid.SetColumn({model.VariableName}, {RenderValue(property.Key, property.Value)});");
                    continue;
                }

                if (property.Key == "Style" && TryReadResourceReference(property.Value, out var styleKey))
                {
                    source.AppendLine($"        {model.VariableName}.Style = Microsoft.UI.Xaml.ResourceOperations.ResolveStyle(__resources, {Literal(styleKey)}, {Literal(property.Key)});");
                    source.AppendLine($"        Microsoft.UI.Xaml.StyleOperations.Apply({model.VariableName}, {model.VariableName}.Style as Microsoft.UI.Xaml.Style);");
                    continue;
                }

                if (TryReadBinding(property.Value, out var bindingPath, out var bindingMode))
                {
                    source.AppendLine($"        Microsoft.UI.Xaml.Data.BindingOperations.SetBinding({model.VariableName}, {Literal(property.Key)}, new Microsoft.UI.Xaml.Data.Binding({Literal(bindingPath)}, Microsoft.UI.Xaml.Data.BindingMode.{bindingMode}));");
                    continue;
                }

                source.AppendLine($"        {model.VariableName}.{property.Key} = {RenderValue(property.Key, property.Value)};");
            }
        }

        private void WriteEvents(XamlObjectModel model)
        {
            foreach (var routedEvent in model.Events)
            {
                source.AppendLine($"        {model.VariableName}.{routedEvent.Key} += {routedEvent.Value};");
            }
        }

        private void AttachChild(XamlObjectModel parent, XamlObjectModel child)
        {
            if (parent.Element.Name.LocalName is "Window" or "Page" or "UserControl")
            {
                source.AppendLine($"        {parent.VariableName}.Content = {child.VariableName};");
                return;
            }

            if (parent.Element.Name.LocalName == "Border")
            {
                source.AppendLine($"        {parent.VariableName}.Child = {child.VariableName};");
                return;
            }

            if (parent.Element.Name.LocalName is "ScrollViewer" or "ContentControl" or "Frame")
            {
                source.AppendLine($"        {parent.VariableName}.Content = {child.VariableName};");
                return;
            }

            if (parent.Element.Name.LocalName == "StackPanel")
            {
                source.AppendLine($"        {parent.VariableName}.Children.Add({child.VariableName});");
                return;
            }

            if (parent.Element.Name.LocalName == "Grid")
            {
                source.AppendLine($"        {parent.VariableName}.Children.Add({child.VariableName});");
                return;
            }

            if (parent.Element.Name.LocalName == "NavigationView")
            {
                if (child.Element.Name.LocalName == "NavigationViewItem")
                {
                    source.AppendLine($"        {parent.VariableName}.MenuItems.Add({child.VariableName});");
                    return;
                }

                source.AppendLine($"        {parent.VariableName}.Content = {child.VariableName};");
                return;
            }

            if (parent.Element.Name.LocalName is "ItemsControl" or "ListView" or "ComboBox")
            {
                source.AppendLine($"        {parent.VariableName}.Items.Add({child.VariableName});");
                return;
            }

            if (parent.Element.Name.LocalName == "CommandBar")
            {
                source.AppendLine($"        {parent.VariableName}.PrimaryCommands.Add({child.VariableName});");
                return;
            }

            if (parent.Element.Name.LocalName is "Button" or "AppBarButton" or "ToggleButton" or "CheckBox" or "RadioButton")
            {
                source.AppendLine($"        {parent.VariableName}.Content = {child.VariableName};");
            }
        }

        private void AttachPropertyChild(XamlObjectModel parent, string propertyName, XamlObjectModel child)
        {
            switch (propertyName)
            {
                case "MenuItems":
                    source.AppendLine($"        {parent.VariableName}.MenuItems.Add({child.VariableName});");
                    break;
                case "PaneFooter":
                    source.AppendLine($"        {parent.VariableName}.PaneFooter = {child.VariableName};");
                    break;
                case "Icon":
                    source.AppendLine($"        {parent.VariableName}.Icon = {child.VariableName};");
                    break;
                case "Content":
                    source.AppendLine($"        {parent.VariableName}.Content = {child.VariableName};");
                    break;
                case "Child":
                    source.AppendLine($"        {parent.VariableName}.Child = {child.VariableName};");
                    break;
                case "Items":
                    source.AppendLine($"        {parent.VariableName}.Items.Add({child.VariableName});");
                    break;
                case "PrimaryCommands":
                    source.AppendLine($"        {parent.VariableName}.PrimaryCommands.Add({child.VariableName});");
                    break;
            }
        }

        private static string RenderValue(string propertyName, string value)
        {
            if (TryReadResourceReference(value, out var key))
            {
                if (IsStringProperty(propertyName))
                {
                    return $"Microsoft.UI.Xaml.ResourceOperations.ResolveString(__resources, {Literal(key)}, {Literal(propertyName)})";
                }

                return $"Microsoft.UI.Xaml.ResourceOperations.Resolve(__resources, {Literal(key)}, {Literal(propertyName)})";
            }

            if (propertyName == "Visibility" &&
                (string.Equals(value, "Visible", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "Collapsed", StringComparison.OrdinalIgnoreCase)))
            {
                return $"Microsoft.UI.Xaml.Visibility.{value}";
            }

            if (propertyName == "HorizontalAlignment" &&
                (string.Equals(value, "Left", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "Center", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "Right", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "Stretch", StringComparison.OrdinalIgnoreCase)))
            {
                return $"Microsoft.UI.Xaml.HorizontalAlignment.{value}";
            }

            if (propertyName == "VerticalAlignment" &&
                (string.Equals(value, "Top", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "Center", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "Bottom", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "Stretch", StringComparison.OrdinalIgnoreCase)))
            {
                return $"Microsoft.UI.Xaml.VerticalAlignment.{value}";
            }

            if (propertyName == "Orientation" &&
                (string.Equals(value, "Vertical", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "Horizontal", StringComparison.OrdinalIgnoreCase)))
            {
                return $"Microsoft.UI.Xaml.Controls.Orientation.{value}";
            }

            if (propertyName == "VerticalScrollBarVisibility" &&
                (string.Equals(value, "Disabled", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "Auto", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "Hidden", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "Visible", StringComparison.OrdinalIgnoreCase)))
            {
                return $"Microsoft.UI.Xaml.Controls.ScrollBarVisibility.{value}";
            }

            if (propertyName == "Severity" &&
                (string.Equals(value, "Informational", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "Success", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "Warning", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "Error", StringComparison.OrdinalIgnoreCase)))
            {
                return $"Microsoft.UI.Xaml.Controls.InfoBarSeverity.{value}";
            }

            if (bool.TryParse(value, out var boolean))
            {
                return boolean ? "true" : "false";
            }

            if (IsDoubleProperty(propertyName) && double.TryParse(value, CultureInfo.InvariantCulture, out var number))
            {
                return number.ToString(CultureInfo.InvariantCulture);
            }

            if (IsIntProperty(propertyName) && int.TryParse(value, CultureInfo.InvariantCulture, out var integer))
            {
                return integer.ToString(CultureInfo.InvariantCulture);
            }

            return Literal(value);
        }

        private static bool TryReadResourceReference(string value, out string key)
        {
            if ((value.StartsWith("{StaticResource ", StringComparison.Ordinal) ||
                    value.StartsWith("{ThemeResource ", StringComparison.Ordinal)) &&
                value.EndsWith('}'))
            {
                var markerLength = value.StartsWith("{StaticResource ", StringComparison.Ordinal)
                    ? "{StaticResource ".Length
                    : "{ThemeResource ".Length;
                key = value[markerLength..^1].Trim();
                return !string.IsNullOrWhiteSpace(key);
            }

            key = string.Empty;
            return false;
        }

        private static bool TryReadBinding(string value, out string path, out string mode)
        {
            mode = "OneWay";
            if (value.StartsWith("{Binding Path=", StringComparison.Ordinal) && value.EndsWith('}'))
            {
                path = value["{Binding Path=".Length..^1].Trim();
                var comma = path.IndexOf(',');
                if (comma >= 0)
                {
                    var options = path[(comma + 1)..];
                    path = path[..comma].Trim();
                    mode = ReadBindingMode(options);
                }

                return !string.IsNullOrWhiteSpace(path);
            }

            if (value.StartsWith("{Binding ", StringComparison.Ordinal) && value.EndsWith('}'))
            {
                path = value["{Binding ".Length..^1].Trim();
                return !string.IsNullOrWhiteSpace(path);
            }

            path = string.Empty;
            return false;
        }

        private static string ReadBindingMode(string options)
        {
            foreach (var part in options.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (part.StartsWith("Mode=", StringComparison.OrdinalIgnoreCase))
                {
                    var value = part["Mode=".Length..];
                    return string.Equals(value, "TwoWay", StringComparison.OrdinalIgnoreCase) ? "TwoWay" : "OneWay";
                }
            }

            return "OneWay";
        }

        private static bool IsDoubleProperty(string propertyName)
        {
            return propertyName is
                "CompactPaneLength" or
                "OpenPaneLength" or
                "Spacing" or
                "ColumnSpacing" or
                "FontSize" or
                "Minimum" or
                "Maximum" or
                "Value";
        }

        private static bool IsIntProperty(string propertyName)
        {
            return propertyName is "Grid.Column" or "SelectedIndex";
        }

        private static bool IsStringProperty(string propertyName)
        {
            return propertyName is
                "Title" or
                "Text" or
                "Glyph" or
                "Padding" or
                "ColumnDefinitions" or
                "IsBackButtonVisible" or
                "PaneDisplayMode";
        }

        private static string Literal(string value)
        {
            var escaped = value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal)
                .Replace("\r", "\\r", StringComparison.Ordinal)
                .Replace("\n", "\\n", StringComparison.Ordinal)
                .Replace("\t", "\\t", StringComparison.Ordinal);
            return "\"" + escaped + "\"";
        }
    }
}
