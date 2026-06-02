using System.Text.RegularExpressions;

namespace LegacyLens.Core.LegacyAspNet;

public sealed class LegacyAspNetArtifactScanner
{
    private static readonly Regex MvcControllerClassRegex = new(
        @"\bclass\s+(?<name>[A-Za-z_][A-Za-z0-9_]*Controller)\s*:\s*(?<baseTypes>[^{]+)\{",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    private static readonly Regex ClassWithBaseTypesRegex = new(
        @"\bclass\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*:\s*(?<baseTypes>[^{]+)\{",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    private static readonly Regex MvcActionMethodRegex = new(
        @"(?<attributes>(?:\s*\[[^\]]+\]\s*)*)\bpublic\s+(?:async\s+)?(?:virtual\s+|override\s+)?(?<returnType>[A-Za-z_][A-Za-z0-9_\.]*(?:<[^>]+>)?)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\(",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    private static readonly Regex AttributeNameRegex = new(
        @"\[\s*(?<name>[A-Za-z_][A-Za-z0-9_\.]*)(?:Attribute)?(?:\s*\(|\s*\])",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    private static readonly HashSet<string> MvcActionReturnTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ActionResult",
        "System.Web.Mvc.ActionResult",
        "ViewResult",
        "System.Web.Mvc.ViewResult",
        "JsonResult",
        "System.Web.Mvc.JsonResult",
        "PartialViewResult",
        "System.Web.Mvc.PartialViewResult",
        "RedirectResult",
        "System.Web.Mvc.RedirectResult",
        "RedirectToRouteResult",
        "System.Web.Mvc.RedirectToRouteResult",
        "FileResult",
        "System.Web.Mvc.FileResult",
        "ContentResult",
        "System.Web.Mvc.ContentResult",
        "HttpStatusCodeResult",
        "System.Web.Mvc.HttpStatusCodeResult"
    };

    private static readonly HashSet<string> MvcRouteAttributeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Route",
        "RouteAttribute",
        "RoutePrefix",
        "RoutePrefixAttribute"
    };

    private static readonly HashSet<string> MvcActionAttributeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "HttpGet",
        "HttpGetAttribute",
        "HttpPost",
        "HttpPostAttribute",
        "HttpPut",
        "HttpPutAttribute",
        "HttpDelete",
        "HttpDeleteAttribute",
        "HttpPatch",
        "HttpPatchAttribute",
        "AcceptVerbs",
        "AcceptVerbsAttribute",
        "Authorize",
        "AuthorizeAttribute",
        "AllowAnonymous",
        "AllowAnonymousAttribute",
        "ValidateAntiForgeryToken",
        "ValidateAntiForgeryTokenAttribute",
        "OutputCache",
        "OutputCacheAttribute"
    };

    public IReadOnlyList<DiscoveredLegacyAspNetArtifact> Scan(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Root path cannot be empty.", nameof(rootPath));
        }

        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException($"Root path does not exist: {rootPath}");
        }

        var artifacts = new List<DiscoveredLegacyAspNetArtifact>();

        AddFileBasedArtifacts(rootPath, artifacts);
        AddSourceLevelArtifacts(rootPath, artifacts);

        return artifacts
            .OrderBy(x => x.Kind)
            .ThenBy(x => x.FilePath)
            .ThenBy(x => x.Name)
            .ToList();
    }

    private static void AddFileBasedArtifacts(
        string rootPath,
        List<DiscoveredLegacyAspNetArtifact> artifacts)
    {
        foreach (var filePath in Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories))
        {
            var fileName = Path.GetFileName(filePath);
            var extension = Path.GetExtension(filePath);

            if (extension.Equals(".aspx", StringComparison.OrdinalIgnoreCase))
            {
                artifacts.Add(CreateArtifact(
                    LegacyAspNetArtifactKind.WebFormsPage,
                    filePath));

                continue;
            }

            if (extension.Equals(".ascx", StringComparison.OrdinalIgnoreCase))
            {
                artifacts.Add(CreateArtifact(
                    LegacyAspNetArtifactKind.WebFormsUserControl,
                    filePath));

                continue;
            }

            if (extension.Equals(".master", StringComparison.OrdinalIgnoreCase))
            {
                artifacts.Add(CreateArtifact(
                    LegacyAspNetArtifactKind.MasterPage,
                    filePath));

                continue;
            }

            if (extension.Equals(".asmx", StringComparison.OrdinalIgnoreCase))
            {
                artifacts.Add(CreateArtifact(
                    LegacyAspNetArtifactKind.AsmxWebService,
                    filePath));

                continue;
            }

            if (extension.Equals(".ashx", StringComparison.OrdinalIgnoreCase))
            {
                artifacts.Add(CreateArtifact(
                    LegacyAspNetArtifactKind.HttpHandler,
                    filePath));

                continue;
            }

            if (fileName.Equals("Global.asax", StringComparison.OrdinalIgnoreCase))
            {
                artifacts.Add(CreateArtifact(
                    LegacyAspNetArtifactKind.GlobalAsax,
                    filePath));
            }
        }
    }

    private static void AddSourceLevelArtifacts(
        string rootPath,
        List<DiscoveredLegacyAspNetArtifact> artifacts)
    {
        foreach (var sourceFilePath in Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories))
        {
            string source;

            try
            {
                source = File.ReadAllText(sourceFilePath);
            }
            catch
            {
                continue;
            }

            AddMvcControllerArtifacts(sourceFilePath, source, artifacts);
            AddRouteConfigArtifact(sourceFilePath, source, artifacts);
            AddAreaRegistrationArtifacts(sourceFilePath, source, artifacts);
        }
    }

    private static void AddMvcControllerArtifacts(
        string sourceFilePath,
        string source,
        List<DiscoveredLegacyAspNetArtifact> artifacts)
    {
        foreach (Match match in MvcControllerClassRegex.Matches(source))
        {
            var controllerName = match.Groups["name"].Value;
            var baseTypes = match.Groups["baseTypes"].Value;

            if (!InheritsFromMvcController(baseTypes))
            {
                continue;
            }

            artifacts.Add(new DiscoveredLegacyAspNetArtifact
            {
                Kind = LegacyAspNetArtifactKind.MvcController,
                FilePath = sourceFilePath,
                Name = controllerName
            });

            AddMvcControllerAttributeArtifacts(sourceFilePath, controllerName, source, match, artifacts);
            AddMvcActionArtifacts(sourceFilePath, controllerName, source, match, artifacts);
        }
    }

    private static void AddMvcControllerAttributeArtifacts(
        string sourceFilePath,
        string controllerName,
        string source,
        Match controllerMatch,
        List<DiscoveredLegacyAspNetArtifact> artifacts)
    {
        var attributes = GetAttributeBlockBefore(source, controllerMatch.Index);

        foreach (var attributeName in GetAttributeNames(attributes))
        {
            if (!MvcRouteAttributeNames.Contains(attributeName))
            {
                continue;
            }

            artifacts.Add(new DiscoveredLegacyAspNetArtifact
            {
                Kind = LegacyAspNetArtifactKind.MvcRouteAttribute,
                FilePath = sourceFilePath,
                Name = $"{controllerName} [{NormalizeAttributeName(attributeName)}]"
            });
        }
    }

    private static void AddMvcActionArtifacts(
        string sourceFilePath,
        string controllerName,
        string source,
        Match controllerMatch,
        List<DiscoveredLegacyAspNetArtifact> artifacts)
    {
        var classBody = GetClassBody(source, controllerMatch);

        if (string.IsNullOrWhiteSpace(classBody))
        {
            return;
        }

        foreach (Match methodMatch in MvcActionMethodRegex.Matches(classBody))
        {
            var actionName = methodMatch.Groups["name"].Value;
            var returnType = methodMatch.Groups["returnType"].Value;
            var attributes = methodMatch.Groups["attributes"].Value;

            if (!IsMvcActionReturnType(returnType))
            {
                continue;
            }

            if (IsIgnoredMvcMethod(actionName))
            {
                continue;
            }

            var actionQualifiedName = $"{controllerName}.{actionName}";

            artifacts.Add(new DiscoveredLegacyAspNetArtifact
            {
                Kind = LegacyAspNetArtifactKind.MvcAction,
                FilePath = sourceFilePath,
                Name = actionQualifiedName
            });

            AddMvcActionAttributeArtifacts(
                sourceFilePath,
                actionQualifiedName,
                attributes,
                artifacts);
        }
    }

    private static void AddMvcActionAttributeArtifacts(
        string sourceFilePath,
        string actionQualifiedName,
        string attributes,
        List<DiscoveredLegacyAspNetArtifact> artifacts)
    {
        foreach (var attributeName in GetAttributeNames(attributes))
        {
            if (MvcRouteAttributeNames.Contains(attributeName))
            {
                artifacts.Add(new DiscoveredLegacyAspNetArtifact
                {
                    Kind = LegacyAspNetArtifactKind.MvcRouteAttribute,
                    FilePath = sourceFilePath,
                    Name = $"{actionQualifiedName} [{NormalizeAttributeName(attributeName)}]"
                });

                continue;
            }

            if (MvcActionAttributeNames.Contains(attributeName))
            {
                artifacts.Add(new DiscoveredLegacyAspNetArtifact
                {
                    Kind = LegacyAspNetArtifactKind.MvcActionAttribute,
                    FilePath = sourceFilePath,
                    Name = $"{actionQualifiedName} [{NormalizeAttributeName(attributeName)}]"
                });
            }
        }
    }

    private static void AddRouteConfigArtifact(
        string sourceFilePath,
        string source,
        List<DiscoveredLegacyAspNetArtifact> artifacts)
    {
        if (!Path.GetFileName(sourceFilePath).Equals("RouteConfig.cs", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!LooksLikeAspNetRouteConfig(source))
        {
            return;
        }

        artifacts.Add(CreateArtifact(
            LegacyAspNetArtifactKind.RouteConfig,
            sourceFilePath));
    }

    private static void AddAreaRegistrationArtifacts(
        string sourceFilePath,
        string source,
        List<DiscoveredLegacyAspNetArtifact> artifacts)
    {
        foreach (Match match in ClassWithBaseTypesRegex.Matches(source))
        {
            var className = match.Groups["name"].Value;
            var baseTypes = match.Groups["baseTypes"].Value;

            if (!InheritsFromAreaRegistration(baseTypes))
            {
                continue;
            }

            if (!LooksLikeAspNetMvcAreaRegistration(source))
            {
                continue;
            }

            artifacts.Add(new DiscoveredLegacyAspNetArtifact
            {
                Kind = LegacyAspNetArtifactKind.AreaRegistration,
                FilePath = sourceFilePath,
                Name = className
            });
        }
    }

    private static string GetClassBody(string source, Match classMatch)
    {
        var openingBraceIndex = source.IndexOf('{', classMatch.Index);

        if (openingBraceIndex < 0)
        {
            return string.Empty;
        }

        var depth = 0;

        for (var i = openingBraceIndex; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
                continue;
            }

            if (source[i] != '}')
            {
                continue;
            }

            depth--;

            if (depth == 0)
            {
                return source.Substring(openingBraceIndex + 1, i - openingBraceIndex - 1);
            }
        }

        return string.Empty;
    }

    private static string GetAttributeBlockBefore(string source, int declarationStartIndex)
    {
        var declarationLineStartIndex = source.LastIndexOf('\n', Math.Max(0, declarationStartIndex - 1));

        declarationLineStartIndex = declarationLineStartIndex < 0
            ? 0
            : declarationLineStartIndex + 1;

        var prefix = source[..declarationLineStartIndex];
        var lines = prefix.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var attributeLines = new Stack<string>();

        for (var i = lines.Length - 1; i >= 0; i--)
        {
            var trimmed = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                if (attributeLines.Count == 0)
                {
                    continue;
                }

                break;
            }

            if (!trimmed.StartsWith("[", StringComparison.Ordinal))
            {
                break;
            }

            attributeLines.Push(lines[i]);
        }

        return string.Join(Environment.NewLine, attributeLines);
    }

    private static IEnumerable<string> GetAttributeNames(string attributes)
    {
        foreach (Match match in AttributeNameRegex.Matches(attributes))
        {
            var name = match.Groups["name"].Value;

            if (!string.IsNullOrWhiteSpace(name))
            {
                yield return name;
            }
        }
    }

    private static bool IsMvcActionReturnType(string returnType)
    {
        if (MvcActionReturnTypes.Contains(returnType))
        {
            return true;
        }

        if (returnType.StartsWith("Task<", StringComparison.OrdinalIgnoreCase) &&
            returnType.EndsWith(">", StringComparison.Ordinal))
        {
            var innerReturnType = returnType[5..^1].Trim();

            return MvcActionReturnTypes.Contains(innerReturnType);
        }

        return false;
    }

    private static bool IsIgnoredMvcMethod(string methodName)
    {
        return methodName.Equals("Dispose", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("Initialize", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("OnActionExecuting", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("OnActionExecuted", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("OnAuthorization", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("OnException", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("OnResultExecuting", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("OnResultExecuted", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeAttributeName(string attributeName)
    {
        const string attributeSuffix = "Attribute";

        return attributeName.EndsWith(attributeSuffix, StringComparison.OrdinalIgnoreCase)
            ? attributeName[..^attributeSuffix.Length]
            : attributeName;
    }

    private static bool InheritsFromMvcController(string baseTypes)
    {
        return baseTypes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(IsMvcControllerBaseType);
    }

    private static bool InheritsFromAreaRegistration(string baseTypes)
    {
        return baseTypes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(IsAreaRegistrationBaseType);
    }

    private static bool IsMvcControllerBaseType(string baseType)
    {
        return baseType.Equals("Controller", StringComparison.OrdinalIgnoreCase) ||
               baseType.Equals("System.Web.Mvc.Controller", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAreaRegistrationBaseType(string baseType)
    {
        return baseType.Equals("AreaRegistration", StringComparison.OrdinalIgnoreCase) ||
               baseType.Equals("System.Web.Mvc.AreaRegistration", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeAspNetRouteConfig(string source)
    {
        return source.Contains("RouteCollection", StringComparison.OrdinalIgnoreCase) ||
               source.Contains("routes.MapRoute", StringComparison.OrdinalIgnoreCase) ||
               source.Contains("System.Web.Routing", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeAspNetMvcAreaRegistration(string source)
    {
        return source.Contains("AreaName", StringComparison.OrdinalIgnoreCase) ||
               source.Contains("RegisterArea", StringComparison.OrdinalIgnoreCase) ||
               source.Contains("AreaRegistrationContext", StringComparison.OrdinalIgnoreCase) ||
               source.Contains("context.MapRoute", StringComparison.OrdinalIgnoreCase);
    }

    private static DiscoveredLegacyAspNetArtifact CreateArtifact(
        LegacyAspNetArtifactKind kind,
        string filePath)
    {
        return new DiscoveredLegacyAspNetArtifact
        {
            Kind = kind,
            FilePath = filePath,
            Name = Path.GetFileName(filePath)
        };
    }
}