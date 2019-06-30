using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MinecraftClient.API
{
    class PluginLoader
    {
        public static List<IPlugin> LoadPlugins(string pluginFolder)
        {
            List<IPlugin> plugins = new List<IPlugin>();
            foreach (string file in Directory.GetFiles(pluginFolder, "*.dll"))
            {
                try
                {
                    plugins.Add(LoadPlugin(file));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error loading plugin: " + Path.GetFileName(file));
                    Console.WriteLine(e.ToString());
                }
            }

            return plugins;
        }

        public static IPlugin LoadPlugin(string path)
        {
            Assembly assembly = Assembly.Load(AssemblyName.GetAssemblyName(path));
            foreach (Type type in assembly.GetTypes())
            {
                string IPluginFullName = typeof(IPlugin).FullName;
                if (!type.IsInterface && !type.IsAbstract && type.GetInterface(IPluginFullName) != null)
                    return (IPlugin)Activator.CreateInstance(type);
            }

            return null;
        }
    }
}
