namespace WinUI3.MacRunner.Tests;

/// <summary>
/// Writes a small, self-contained set of macOS runtime artifacts
/// (<c>tree.json</c>, <c>accessibility.json</c>, <c>interactions.json</c>) whose
/// accessibility/tree structures are 1:1, mirroring what the direct ingestion
/// run emits under <c>mac-runtime-direct</c>. Tests load the adapter over this
/// directory instead of depending on out-of-repo QA output.
/// </summary>
internal static class ArtifactAdapterFixture
{
    public static string Write()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "winui3-mac-runner-flaui-adapter-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        File.WriteAllText(Path.Combine(root, "tree.json"), TreeJson);
        File.WriteAllText(Path.Combine(root, "accessibility.json"), AccessibilityJson);
        File.WriteAllText(Path.Combine(root, "interactions.json"), InteractionsJson);

        return root;
    }

    private const string TreeJson = """
        {
          "schemaVersion": "0.2",
          "generatedAt": "1970-01-01T00:00:00+00:00",
          "root": {
            "type": "Microsoft.UI.Xaml.Window",
            "properties": { "title": "Fixture", "isActive": true },
            "children": [
              {
                "type": "Microsoft.UI.Xaml.Controls.NavigationView",
                "name": "RootNavigation",
                "properties": { "automationId": "root-nav", "isEnabled": true },
                "children": [
                  {
                    "type": "Microsoft.UI.Xaml.Controls.NavigationViewItem",
                    "name": "MessagesNavigationItem",
                    "properties": { "automationId": "shell-nav-messages", "isSelected": true, "isEnabled": true },
                    "children": [],
                    "layout": {
                      "x": 12, "y": 48, "width": 224, "height": 40,
                      "desiredWidth": 224, "desiredHeight": 40,
                      "margin": { "left": 0, "top": 0, "right": 0, "bottom": 0 },
                      "padding": { "left": 0, "top": 0, "right": 0, "bottom": 0 },
                      "horizontalAlignment": "Stretch", "verticalAlignment": "Stretch", "visibility": "Visible"
                    }
                  },
                  {
                    "type": "Microsoft.UI.Xaml.Controls.Button",
                    "name": "LogoutButton",
                    "properties": { "automationId": "shell-logout", "isEnabled": true, "content": "Log out" },
                    "children": [],
                    "layout": {
                      "x": 16, "y": 720, "width": 200, "height": 32,
                      "desiredWidth": 200, "desiredHeight": 32,
                      "margin": { "left": 0, "top": 0, "right": 0, "bottom": 0 },
                      "padding": { "left": 0, "top": 0, "right": 0, "bottom": 0 },
                      "horizontalAlignment": "Left", "verticalAlignment": "Bottom", "visibility": "Visible"
                    }
                  },
                  {
                    "type": "Microsoft.UI.Xaml.Controls.CheckBox",
                    "name": "RememberCheckBox",
                    "properties": { "automationId": "remember", "isChecked": true, "isEnabled": true },
                    "children": []
                  },
                  {
                    "type": "Microsoft.UI.Xaml.Controls.TextBox",
                    "name": "SearchBox",
                    "properties": { "automationId": "search", "text": "hello", "isFocused": true, "isEnabled": true },
                    "children": []
                  },
                  {
                    "type": "Microsoft.UI.Xaml.Controls.Expander",
                    "name": "DetailsExpander",
                    "properties": { "automationId": "details", "isExpanded": true, "isEnabled": false },
                    "children": []
                  }
                ]
              }
            ]
          }
        }
        """;

    private const string AccessibilityJson = """
        {
          "schemaVersion": "0.3",
          "generatedAt": "1970-01-01T00:00:00+00:00",
          "root": {
            "role": "window",
            "isFocused": false,
            "children": [
              {
                "role": "navigation",
                "name": "RootNavigation",
                "automationId": "root-nav",
                "isFocused": false,
                "isFocusable": true,
                "isEnabled": true,
                "isSelected": true,
                "value": "MessagesNavigationItem",
                "children": [
                  {
                    "role": "navigation-item",
                    "name": "MessagesNavigationItem",
                    "automationId": "shell-nav-messages",
                    "label": "MessagesNavigationItem",
                    "isFocused": false,
                    "isFocusable": true,
                    "isEnabled": true,
                    "isSelected": true,
                    "children": []
                  },
                  {
                    "role": "button",
                    "name": "LogoutButton",
                    "automationId": "shell-logout",
                    "label": "Log out",
                    "helpText": "Signs out the current user",
                    "isFocused": false,
                    "isFocusable": true,
                    "isEnabled": true,
                    "children": []
                  },
                  {
                    "role": "checkbox",
                    "name": "RememberCheckBox",
                    "automationId": "remember",
                    "label": "Remember me",
                    "isFocused": false,
                    "isFocusable": true,
                    "isEnabled": true,
                    "isChecked": true,
                    "children": []
                  },
                  {
                    "role": "textbox",
                    "name": "SearchBox",
                    "automationId": "search",
                    "label": "Search",
                    "isFocused": true,
                    "isFocusable": true,
                    "isEnabled": true,
                    "value": "hello",
                    "children": []
                  },
                  {
                    "role": "button",
                    "name": "DetailsExpander",
                    "automationId": "details",
                    "label": "Details",
                    "isFocused": false,
                    "isFocusable": true,
                    "isEnabled": false,
                    "isExpanded": true,
                    "children": []
                  }
                ]
              }
            ]
          }
        }
        """;

    private const string InteractionsJson = """
        {
          "schemaVersion": "0.3",
          "steps": [
            {
              "index": 0,
              "type": "assertAccessibilityState",
              "status": "passed",
              "target": "automationId=shell-nav-messages",
              "expected": "true",
              "actual": "True",
              "selector": "automationId=shell-nav-messages",
              "selectorKind": "automationId",
              "targetType": "NavigationViewItem",
              "observedState": {
                "role": "navigation-item",
                "name": "MessagesNavigationItem",
                "isSelected": "True"
              }
            },
            {
              "index": 1,
              "type": "selectNavigation",
              "status": "passed",
              "target": "automationId=shell-nav-messages",
              "selector": "automationId=shell-nav-messages",
              "selectorKind": "automationId",
              "targetType": "NavigationViewItem"
            },
            {
              "index": 2,
              "type": "assertProperty",
              "status": "failed",
              "target": "ContentFrame",
              "expected": "messages",
              "actual": "home",
              "selector": "ContentFrame",
              "selectorKind": "name",
              "targetType": "Frame",
              "message": "Expected 'messages' but found 'home'."
            }
          ]
        }
        """;
}
