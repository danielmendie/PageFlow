using Microsoft.AspNetCore.Components;
using PageFlow.Blazor.CLI.Helpers;
using System.Reflection;
using System.Text;

namespace PageFlow.Blazor.CLI.Commands
{
    public class GenerateRoutesCommand
    {
        public static async Task<int> RunAsync(CommandOptions options)
        {
            UtilityHelper.Log("---------- PageFlow Started ----------");

            if (string.IsNullOrWhiteSpace(options.Path))
                options.Path = Directory.GetCurrentDirectory();

            UtilityHelper.Log($"Project directory: {options.Path}", options.Verbose);
            var projectInfo = ComponentHelper.GetProjectAssemblyInfo(options.Path);

            if (options.Force || projectInfo == null)
            {
                UtilityHelper.Log("Build started...", options.Verbose);
                var ok = UtilityHelper.BuildProject(options.Path, configuration: "Debug");
                if (!ok)
                {
                    UtilityHelper.Log("dotnet build failed. Aborting route generation.");
                    return 1;
                }
                UtilityHelper.Log("Build completed", options.Verbose);
            }

            UtilityHelper.Log($"Searching for routable components...", options.Verbose);
            var routes = ComponentHelper.GetRoutableComponents(options.Path);

            if (!routes.Any())
            {
                UtilityHelper.Log("No routable components found");
            }
            else
            {
                UtilityHelper.Log($"Found {routes.Count()} components");
                UtilityHelper.Log($"Searching for base components...", options.Verbose);
                await GeneratePageRoutes(routes, options);
            }

            UtilityHelper.Log("---------- PageFlow Completed ----------");
            return 0;
        }

        static List<string> ProjectNameSpaces = [];
        private static async Task GeneratePageRoutes(IEnumerable<string> routeFilePaths, CommandOptions option)
        {
            try
            {
                var basePath = ComponentHelper.GetBaseComponentByAttribute(option);
                if (option.DryRun)
                {
                    UtilityHelper.Log($"Dry Run - skipping {routeFilePaths.Count()} route file updates.");
                    return;
                }

                #region CodeGenerator

                List<PageFlowInfo> routeComponents = [];
                foreach (var componentPath in routeFilePaths)
                {
                    //read file content
                    UtilityHelper.Log($"Processing route for: {componentPath}", option.Verbose);
                    var fileContent = File.ReadAllText(componentPath);

                    var (componentType, nameSpace) = await ComponentHelper.ExtractComponentTypeAsync(fileContent, option.Path);
                    if (componentType == null)
                    {
                        UtilityHelper.Log($"Failed to extract at path: {componentPath}", option.Verbose);
                        continue;
                    }

                    var className = componentType.Name;
                    var fullyQualifiedName = componentType.FullName;

                    var attributes = componentType.GetCustomAttributes(typeof(PageFlowRoutableComponentAttribute), false);
                    if (attributes.Length != 0)
                    {
                        foreach (PageFlowRoutableComponentAttribute attribute in attributes.Cast<PageFlowRoutableComponentAttribute>())
                        {
                            int appId = attribute.AppId;
                            bool isDefault = attribute.IsDefault;
                            string pageName = attribute.PageName;

                            routeComponents.Add(new PageFlowInfo
                            {
                                ComponentType = componentType,
                                AppId = appId,
                                IsDefault = isDefault,
                                Component = className,
                                PageName = pageName
                            });
                        }

                        if (!string.IsNullOrWhiteSpace(nameSpace))
                            ProjectNameSpaces.Add(nameSpace);
                    }
                }

                var sb = new StringBuilder();
                var totalComs = routeComponents.Count;
                var loopComs = 1;
                foreach (var component in routeComponents)
                {
                    var properties = component.ComponentType.GetProperties()
                        .Where(p => p.GetCustomAttributes<ParameterAttribute>().Any())
                        .Select(p => p);

                    sb.AppendLine($"new PageFlowInfo");
                    sb.AppendLine("{");
                    sb.AppendLine($"AppId = {component.AppId},");
                    sb.AppendLine($"PageName =  \"{component.PageName}\",");
                    sb.AppendLine($"Component =  nameof({component.Component}),");
                    sb.AppendLine($"ComponentType =  typeof({component.Component}),");
                    if (component.IsDefault)
                    {
                        sb.AppendLine($"IsDefault = true,");
                    }
                    if (properties.Any())
                    {
                        sb.AppendLine("Params = new()");
                        sb.AppendLine("{");

                        var totalProps = properties.Count();
                        var loopProps = 1;
                        foreach (var property in properties)
                        {
                            sb.AppendLine("{");
                            sb.AppendLine($"\"{property.Name}\", \"\"");
                            if (loopProps != totalProps)
                            {
                                sb.AppendLine("},");
                            }
                            else
                            {
                                sb.AppendLine("}");
                            }

                            loopProps++;
                        }

                        sb.AppendLine("}");
                    }

                    if (loopComs != totalComs)
                    {
                        sb.AppendLine("},");
                    }
                    else
                    {
                        sb.AppendLine("}");
                    }

                    loopComs++;
                }

                CreatePageRouteRegistry(basePath);
                CreateVirtualOnAppNavigationSetup(sb.ToString(), basePath);
                AppendUIComponent(option.Path);

                #endregion

            }
            catch (Exception ex)
            {
                UtilityHelper.Log($"******* {ex.Message} ");
            }
        }

        #region Helpers

        private protected static void CreateVirtualOnAppNavigationSetup(string code, string filePath)
        {
            filePath = filePath.Replace('/', '\\');

            if (!File.Exists(filePath))
            {
                UtilityHelper.Log($"File not found: {filePath}");
                return;
            }

            var lines = File.ReadAllLines(filePath);

            var methodSignature = "protected override async Task OnAppFlowSetup()";
            var startIndex = -1;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(methodSignature))
                {
                    startIndex = i;
                    break;
                }
            }

            if (startIndex != -1)
            {
                int endIndex = UtilityHelper.FindMatchingClosingBrace(lines, startIndex);

                if (endIndex != -1)
                {
                    lines = lines.Take(startIndex)
                                 .Concat(lines.Skip(endIndex + 1))
                                 .ToArray();
                }
            }

            int insertIndex = startIndex != -1 ? startIndex : lines.Length - 1;

            var sb = new StringBuilder();
            sb.AppendLine("    protected override async Task OnAppFlowSetup()");
            sb.AppendLine("    {");
            sb.AppendLine("        PageFlowRegistry.Routes.Clear();");
            sb.AppendLine("        PageFlowRegistry.Routes.AddRange(new List<PageFlowInfo>()");
            sb.AppendLine("        {");
            sb.AppendLine($"            {code}");
            sb.AppendLine("        });");
            sb.AppendLine("        await base.OnAppFlowSetup();");
            sb.AppendLine("    }");

            var newMethodLines = sb.ToString()
                                   .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            if (startIndex != -1)
            {
                // method existed: insert at the original position
                lines = lines.Take(insertIndex)
                             .Concat(newMethodLines)
                             .Concat(lines.Skip(insertIndex))
                             .ToArray();
            }
            else
            {
                // method did not exist: insert before final "}"
                lines = lines.Take(lines.Length - 1)
                             .Concat(newMethodLines)
                             .Concat(new[] { "}" })
                             .ToArray();
            }

            File.WriteAllLines(filePath, lines);
            UtilityHelper.FormatCode(filePath, ProjectNameSpaces);
        }


        private protected static void CreatePageRouteRegistry(string filePath)
        {
            filePath = filePath.Replace('/', '\\');

            if (File.Exists(filePath))
            {
                string[] lines = File.ReadAllLines(filePath);

                // Find the starting line of the GetPageRoutes method
                int startIndex = -1;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("PageFlowRegistry PageFlowRegistry { get; set; }"))
                    {
                        startIndex = i;
                        break;
                    }
                }

                bool proceed = true;
                if (startIndex != -1) proceed = false;
                if (startIndex != -1)
                {
                    int endIndex = UtilityHelper.FindMatchingClosingBrace(lines, startIndex);

                    if (endIndex != -1)
                    {
                        lines = lines.Take(startIndex).Concat(lines.Skip(endIndex + 1)).ToArray();
                    }
                }

                if (proceed)
                {
                    int insertIndex = startIndex;

                    StringBuilder newMethodCode = new StringBuilder();
                    newMethodCode.AppendLine("[Inject] public PageFlowRegistry PageFlowRegistry { get; set; } = default!;");

                    if (startIndex != -1)
                    {
                        lines = lines.Take(insertIndex).Concat(newMethodCode.ToString().Split('\n')).Concat(lines.Skip(insertIndex)).ToArray();
                    }
                    else
                    {
                        lines = lines.Take(lines.Length - 1).Concat(newMethodCode.ToString().Split('\n')).Concat(new[] { "}" }).ToArray();
                    }

                    File.WriteAllLines(filePath, lines);

                    UtilityHelper.FormatCode(filePath, ProjectNameSpaces, true);
                }
            }
            else
            {
                UtilityHelper.Log($"File not found: {filePath}");
            }
        }

        private protected static void AppendUIComponent(string directory)
        {
            var indexPath = ComponentHelper.GetIndexComponentByAttribute(directory);

            if (indexPath is null)
            {
                return;
            }

            indexPath = indexPath.EndsWith(".cs") ? indexPath.Replace(".cs", "") : indexPath;
            var lines = File.ReadAllLines(indexPath).ToList();
            const string UIRender = "<PageFlow.Blazor.FlowUI />";

            var exists = lines.Any(l => string.Equals(l.Trim(), UIRender, StringComparison.OrdinalIgnoreCase));
            if (exists)
            {
                return;
            }

            // Append as a new line at the bottom
            lines.Add(UIRender);
            File.WriteAllLines(indexPath, lines);
        }

        #endregion
    }
}
