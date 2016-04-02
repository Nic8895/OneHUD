﻿using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;
using AGData;
using AGServerInterface;

namespace AGServer.Servers.DataHandlers.Startup
{
    class StartupDataHandler
    {
        public static StartupDataHandlerResult ProcessStartupRequest(TelemetryData telemetry, Dictionary<string, IGame> plugins, NameValueCollection postData)
        {
            StartupDataHandlerResult result = new StartupDataHandlerResult() { Result = false };
            result.Plugins = new List<SartupDataHandlerPlugin>();
            result.Pages = new List<StartupDataPageInfo>();
            result.Widgets = new List<StartupDataWidgetInfo>();

            result.Version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

            foreach (var key in plugins)
            {
                IGame game = key.Value;
                SartupDataHandlerPlugin plugin = new SartupDataHandlerPlugin();
                plugin.PluginName = "";
                plugin.GameShortName = game.Name;
                plugin.GameLongName = game.DisplayName;
                plugin.PluginVersion = game.Version;
                result.Plugins.Add(plugin);
            }

            string basePath = Directory.GetCurrentDirectory();
            basePath += @"\Web\js\pages";
            string[] pageJsonFiles = Directory.GetFiles(basePath, "*.js");

            for (int i = 0; i < pageJsonFiles.Length; i++)
            {
                string pageText = File.ReadAllText(pageJsonFiles[i]);

                string pageName = ParseVariable(pageText, "_name");
                string pageIcon = ParseVariable(pageText, "_icon");
                string pageDescription = ParseVariable(pageText, "_description");

                if (pageName != null && pageIcon != null && pageDescription != null)
                {
                    StartupDataPageInfo pageInfo = new StartupDataPageInfo();
                    pageInfo.Name = pageName;
                    pageInfo.Icon = pageIcon;
                    pageInfo.Description = pageDescription;
                    result.Pages.Add(pageInfo);
                }
            }

            basePath = Directory.GetCurrentDirectory();
            basePath += @"\Web\js\widgets";
            DirSearch(basePath, result);

            return result;
        }

        static void DirSearch(string dir, StartupDataHandlerResult result)
        {
            try
            {
                foreach (string f in Directory.GetFiles(dir))
                {
                    string widgetJS = File.ReadAllText(f);
                    string widgetName = ParseVariable(widgetJS, "_name");
                    string widgetIcon = ParseVariable(widgetJS, "_icon");
                    string widgetDescription = ParseVariable(widgetJS, "_description");

                    StartupDataWidgetInfo widget = new StartupDataWidgetInfo() { Name = widgetName, Icon = widgetIcon, Description = widgetDescription };
                    result.Widgets.Add(widget);

                }

                foreach (string d in Directory.GetDirectories(dir))
                {
                    DirSearch(d, result);
                }

            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static string ParseVariable(string text, string varName)
        {
            string result = null;
            Regex regex = new Regex(@"var " + varName  + " = '(.)+';");
            Match lineFound = regex.Match(text);
            if (lineFound.Success)
            {
                regex = new Regex(@"'(.)+';");
                Match stringFound = regex.Match(lineFound.Value);
                if (stringFound.Success)
                {
                    result = stringFound.Value.Substring(1, stringFound.Value.Length - 2);
                }
            }

            return result;
        }
    }
}