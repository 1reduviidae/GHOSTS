﻿// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ghosts.Domain.Code;
using NLog;
using Exception = System.Exception;
using System.Threading;

namespace Ghosts.Client.Infrastructure
{
    /// <summary>
    /// Lists and deletes files that were created by ghosts client, so as to avoid high disk usage
    /// </summary>
    public static class FileListing
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private static readonly string _fileName = ApplicationDetails.InstanceFiles.FilesCreated;
        private static readonly object _locked = new object();
        private static readonly object _safetyLocked = new object();

        public static void Add(string path)
        {
            try
            {
                if (!File.Exists(_fileName))
                    File.Create(_fileName);

                if (!Monitor.IsEntered(_safetyLocked)) //checking if safety net is currently flushing cache
                {
                    lock (_locked) //if a thread has entered, the others will wait
                    {
                        var writer = new StreamWriter(_fileName, true);
                        writer.WriteLine(path);
                        writer.Flush();
                        writer.Close();
                    }
                }
                else //sleep if safety net is being safe
                {
                    Thread.Sleep(5000);
                }
            }
            catch (Exception e)
            {
                _log.Trace(e);
            }
        }

        /// <summary>
        /// Deletes all files in the "ApplicationDetails.InstanceFiles.FilesCreated" cache file
        /// </summary>
        public static void FlushList()
        {
            //check if flushing
            if (Program.Configuration.OfficeDocsMaxAgeInHours == -1)
                return;

            if(!File.Exists(_fileName))
                return;

            //locking thread to make sure files can't write to the log
            lock (_safetyLocked)
            {
                _log.Trace("Flushing list...");
                try
                {
                    var deletedFiles = new List<string>();

                    using (var reader = new StreamReader(_fileName))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {

                            FileInfo file;

                            try
                            {
                                file = new FileInfo(line);
                            }
                            catch (Exception e)
                            {
                                _log.Trace($"Error with file in deleted list {line}: {e}");
                                deletedFiles.Add(line);
                                continue;
                            }

                            var creationTime = file.CreationTime.Hour;
                            _log.Trace($"Delete evaluation for {file.FullName} {file.CreationTime}");

                            if (!file.Exists || (creationTime <= Program.Configuration.OfficeDocsMaxAgeInHours))
                                continue;

                            try
                            {
                                _log.Trace($"Deleting: {file.FullName}");
                                file.Delete();
                                deletedFiles.Add(file.FullName);
                            }
                            catch (Exception e)
                            {
                                _log.Debug($"Could not delete file {_fileName}: {e}");
                            }
                        }
                    }

                    if (deletedFiles.Count < 0) return;
                    
                    var lines = File.ReadAllLines(_fileName).ToList();
                    foreach (var line in lines.ToArray())
                    {
                        if (deletedFiles.Contains(line))
                        {
                            lines.Remove(line);
                        }
                    }
                    File.WriteAllLines(_fileName, lines);
                }
                catch (Exception e)
                {
                    _log.Error($"Error flushing {_fileName}: {e}");
                }
            }
        }
    }
}
