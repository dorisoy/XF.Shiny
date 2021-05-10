﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Statiq.Common;


namespace Docs.Shortcodes
{
    public class PackageInfoShortcode : IShortcode
    {
        public async Task<IEnumerable<ShortcodeResult>> ExecuteAsync(KeyValuePair<string, string>[] args, string content, IDocument document, IExecutionContext context)
        {
            var packageName = args.FirstOrDefault().Value;
            var package = Utils.GetPackage(packageName);

            var sb = new StringBuilder();
            sb
                .AppendLine("|Area|Info|")
                .AppendLine("|----|----|");

            AppendIf(sb, "NuGet", Utils.ToNugetShield(package.Name, true));

            var scount = package.Services?.Length ?? 0;
            switch (scount)
            {
                case 0:
                    break;

                case 1:
                    RenderService(sb, package.Services!.First());
                    break;

                default:
                    if (args.Length != 2)
                        throw new ArgumentException($"Package '{packageName}' has multiple services and thus requires a secondary argument of the service you want to render");

                    var sn = args[1].Value;
                    var service = package
                        .Services!
                        .FirstOrDefault(x => x.Name.Equals(sn));

                    if (service == null)
                        throw new ArgumentException("No service found called " + sn);

                    RenderService(sb, service);
                    break;
            }
            return new[] { new ShortcodeResult(sb.ToString()) };
        }


        static void AppendIf(StringBuilder sb, string category, string? value)
        {
            if (!value.IsNullOrEmpty())
                sb.AppendLine($"|**{category}**|{value}|");
        }


        static void RenderService(StringBuilder sb, PackageService service)
        {
            AppendIf(sb, "Service", service.Name);
            AppendIf(sb, "Startup", service.Startup);

            if (!service.BgDelegate.IsNullOrEmpty())
            {
                var bg = service.BgDelegate;
                if (service.BgDelegateRequired)
                    bg += " (Required)";

                AppendIf(sb, "Delegate", bg);
            }
            AppendIf(sb, "Static", service.Static);

            if (service.Platforms != null)
            {
                foreach (var platform in service.Platforms)
                {
                    RenderPlatform(sb, platform);
                }
            }
        }


        static void RenderPlatform(StringBuilder sb, PlatformSupport platform)
        {
            var value = platform.MinVersion ?? DefaultPlatformVersion(platform.Platform);

            if (platform.RequiresConfig)
            {
                value += $" ([OS Configuration Required](xref:{platform.Platform}))";
            }
            sb.AppendLine($"| - *{platform.Platform}*|{value}|");
        }


        static string DefaultPlatformVersion(Platform platform) => platform switch
        {
            Platform.Android => "8.0",
            Platform.iOS => "9",
            Platform.Uwp => "17763"
        };
    }
}
