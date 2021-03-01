﻿using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;

namespace XrmToolBox.AttachmentsDownloader
{
    // Do not forget to update version number and author (company attribute) in AssemblyInfo.cs class
    // To generate Base64 string for Images below, you can use https://www.base64-image.de/
    [Export(typeof(IXrmToolBoxPlugin)),
        ExportMetadata("Name", "Attachments Downloader"),
        ExportMetadata("Description", "Downlaod the attachments related to any entity in Dynamics 365 CRM. Use fetch xml to filter out records."),
        // Please specify the base64 content of a 32x32 pixels image
        ExportMetadata("SmallImageBase64", "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"),
        // Please specify the base64 content of a 80x80 pixels image
        ExportMetadata("BigImageBase64", "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"),
        ExportMetadata("BackgroundColor", "Purple"),
        ExportMetadata("PrimaryFontColor", "White"),
        ExportMetadata("SecondaryFontColor", "White")]
    public class MyPlugin : PluginBase
    {
        public override IXrmToolBoxPluginControl GetControl()
        {
            return new MyPluginControl();
        }

        /// <summary>
        /// Constructor 
        /// </summary>
        public MyPlugin()
        {
            // If you have external assemblies that you need to load, uncomment the following to 
            // hook into the event that will fire when an Assembly fails to resolve
            // AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(AssemblyResolveEventHandler);
        }

        /// <summary>
        /// Event fired by CLR when an assembly reference fails to load
        /// Assumes that related assemblies will be loaded from a subfolder named the same as the Plugin
        /// For example, a folder named Sample.XrmToolBox.MyPlugin 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Assembly AssemblyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            Assembly loadAssembly = null;
            Assembly currAssembly = Assembly.GetExecutingAssembly();

            // base name of the assembly that failed to resolve
            var argName = args.Name.Substring(0, args.Name.IndexOf(","));

            // check to see if the failing assembly is one that we reference.
            List<AssemblyName> refAssemblies = currAssembly.GetReferencedAssemblies().ToList();
            var refAssembly = refAssemblies.Where(a => a.Name == argName).FirstOrDefault();

            // if the current unresolved assembly is referenced by our plugin, attempt to load
            if (refAssembly != null)
            {
                // load from the path to this plugin assembly, not host executable
                string dir = Path.GetDirectoryName(currAssembly.Location).ToLower();
                string folder = Path.GetFileNameWithoutExtension(currAssembly.Location);
                dir = Path.Combine(dir, folder);

                var assmbPath = Path.Combine(dir, $"{argName}.dll");

                if (File.Exists(assmbPath))
                {
                    loadAssembly = Assembly.LoadFrom(assmbPath);
                }
                else
                {
                    throw new FileNotFoundException($"Unable to locate dependency: {assmbPath}");
                }
            }

            return loadAssembly;
        }
    }
}