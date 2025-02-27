﻿// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using Ghosts.Client.Infrastructure;
using Ghosts.Domain;
using OpenQA.Selenium.Chrome;
using System;
using System.IO;

namespace Ghosts.Client.Handlers
{
    public class BrowserChrome : BaseBrowserHandler
    {
        private string GetInstallLocation()
        {
            var path = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
            if (File.Exists(path))
            {
                return path;
            }

            path = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";
            return path;
        }

        public BrowserChrome(TimelineHandler handler)
        {
            BrowserType = HandlerType.BrowserChrome;
            try
            {
                var options = new ChromeOptions();
                options.AddArguments("disable-infobars");
                options.AddArguments("disable-logging");
                options.AddArguments("--disable-logging");
                options.AddArgument("--log-level=3");
                options.AddArgument("--silent");

                options.AddUserProfilePreference("download.default_directory", @"%homedrive%%homepath%\\Downloads");
                options.AddUserProfilePreference("disable-popup-blocking", "true");
                options.BinaryLocation = GetInstallLocation();

                if (handler.HandlerArgs != null)
                {
                    if (handler.HandlerArgs.ContainsKey("executable-location") &&
                        !string.IsNullOrEmpty(handler.HandlerArgs["executable-location"]))
                    {
                        options.BinaryLocation = handler.HandlerArgs["executable-location"];
                    }

                    if (handler.HandlerArgs.ContainsKey("isheadless") && handler.HandlerArgs["isheadless"] == "true")
                    {
                        options.AddArguments("headless");
                    }

                    if (handler.HandlerArgs.ContainsKey("incognito") && handler.HandlerArgs["incognito"] == "true")
                    {
                        options.AddArguments("--incognito");
                    }

                    if (handler.HandlerArgs.ContainsKey("blockstyles") && handler.HandlerArgs["blockstyles"] == "true")
                    {
                        options.AddUserProfilePreference("profile.managed_default_content_settings.stylesheets", 2);
                    }

                    if (handler.HandlerArgs.ContainsKey("blockimages") && handler.HandlerArgs["blockimages"] == "true")
                    {
                        options.AddUserProfilePreference("profile.managed_default_content_settings.images", 2);
                    }

                    if (handler.HandlerArgs.ContainsKey("blockflash") && handler.HandlerArgs["blockflash"] == "true")
                    {
                        // ?
                    }

                    if (handler.HandlerArgs.ContainsKey("blockscripts") &&
                        handler.HandlerArgs["blockscripts"] == "true")
                    {
                        options.AddUserProfilePreference("profile.managed_default_content_settings.javascript", 1);
                    }
                }

                options.AddUserProfilePreference("profile.default_content_setting_values.notifications", 2);
                options.AddUserProfilePreference("profile.managed_default_content_settings.cookies", 2);
                options.AddUserProfilePreference("profile.managed_default_content_settings.plugins", 2);
                options.AddUserProfilePreference("profile.managed_default_content_settings.popups", 2);
                options.AddUserProfilePreference("profile.managed_default_content_settings.geolocation", 2);
                options.AddUserProfilePreference("profile.managed_default_content_settings.media_stream", 2);

                if (!string.IsNullOrEmpty(Program.Configuration.ChromeExtensions))
                {
                    options.AddArguments($"--load-extension={Program.Configuration.ChromeExtensions}");
                }

                Driver = new ChromeDriver(options);
                base.Driver = Driver;

                Driver.Navigate().GoToUrl(handler.Initial);

                if (handler.Loop)
                {
                    while (true)
                    {
                        ExecuteEvents(handler);
                    }
                }
                else
                {
                    ExecuteEvents(handler);
                }
            }
            catch (Exception e)
            {
                _log.Error(e);
            }
            finally
            {
                ProcessManager.KillProcessAndChildrenByName(ProcessManager.ProcessNames.Chrome);
                ProcessManager.KillProcessAndChildrenByName(ProcessManager.ProcessNames.ChromeDriver);
            }
        }
    }
}
