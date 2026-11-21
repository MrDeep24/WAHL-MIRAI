using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace WahlMirai.Tests;

public class LayoutScriptOrderTests
{
    private static string FindViewsSharedFolder()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            var target = Path.Combine(current.FullName, "WahlMirai.Web", "Views", "Shared");
            if (Directory.Exists(target)) return target;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Could not find WahlMirai.Web/Views/Shared directory.");
    }

    [Fact]
    public void AdminLayout_ForwardsScriptsSectionToParentLayout()
    {
        // Arrange
        var sharedFolder = FindViewsSharedFolder();
        var adminLayoutPath = Path.Combine(sharedFolder, "_AdminLayout.cshtml");
        var content = File.ReadAllText(adminLayoutPath);

        // Assert: Debe contener @section Scripts envolviendo RenderSectionAsync("Scripts"
        var pattern = @"@section\s+Scripts\s*\{\s*@await\s+RenderSectionAsync\(\s*""Scripts""\s*,\s*required:\s*false\s*\)\s*\}";
        Assert.Matches(pattern, content);

        // Y no debe contener RenderSectionAsync("Scripts" fuera de @section Scripts
        var stripped = Regex.Replace(content, pattern, string.Empty);
        Assert.DoesNotContain(@"RenderSectionAsync(""Scripts""", stripped);
    }

    [Fact]
    public void ElectorLayout_ForwardsScriptsSectionToParentLayout()
    {
        // Arrange
        var sharedFolder = FindViewsSharedFolder();
        var electorLayoutPath = Path.Combine(sharedFolder, "_ElectorLayout.cshtml");
        var content = File.ReadAllText(electorLayoutPath);

        // Assert: Debe contener @section Scripts envolviendo RenderSectionAsync("Scripts"
        var pattern = @"@section\s+Scripts\s*\{\s*@await\s+RenderSectionAsync\(\s*""Scripts""\s*,\s*required:\s*false\s*\)\s*\}";
        Assert.Matches(pattern, content);

        // Y no debe contener RenderSectionAsync("Scripts" fuera de @section Scripts
        var stripped = Regex.Replace(content, pattern, string.Empty);
        Assert.DoesNotContain(@"RenderSectionAsync(""Scripts""", stripped);
    }

    [Fact]
    public void Layout_RendersScriptsSectionAfterLocaleCore()
    {
        // Arrange
        var sharedFolder = FindViewsSharedFolder();
        var layoutPath = Path.Combine(sharedFolder, "_Layout.cshtml");
        var content = File.ReadAllText(layoutPath);

        // Assert
        var localeIndex = content.IndexOf("locale-core.js", StringComparison.Ordinal);
        var scriptsRenderIndex = content.IndexOf(@"RenderSectionAsync(""Scripts""", StringComparison.Ordinal);

        Assert.True(localeIndex >= 0, "_Layout.cshtml must include locale-core.js");
        Assert.True(scriptsRenderIndex >= 0, "_Layout.cshtml must render the Scripts section");
        Assert.True(scriptsRenderIndex > localeIndex, "Scripts section must be rendered AFTER locale-core.js");
    }
}
