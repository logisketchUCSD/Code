using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Files
{
    public class SettingsReader
    {

        private static Dictionary<string, Dictionary<string, string>> loadedSettings = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// Calls readSettings(AppDomain.CurrentDomain.BaseDirectory + "settings.txt")
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> readSettings()
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;

            // when running tests, base directory often doesn't contain trailing '\'
            if (dir[dir.Length - 1] != '\\')
                dir += "\\";

            return readSettings(dir + "settings.txt");
        }

        /// <summary>
        /// Get the settings we will use for our recognition proccess.
        /// </summary>
        /// <param name="settingsFilename">the file to read</param>
        /// <returns>a dictionary of setting name -> setting value</returns>
        public static Dictionary<string, string> readSettings(string settingsFilename)
        {
            if (loadedSettings.ContainsKey(settingsFilename))
                return loadedSettings[settingsFilename];

            Console.WriteLine("Loading settings from " + settingsFilename);
            Dictionary<string, string> entries = new Dictionary<string, string>();
            Dictionary<string, string> filenames = new Dictionary<string, string>();
            StreamReader reader = new StreamReader(settingsFilename);

            string line;

            while ((line = reader.ReadLine()) != null)
            {
                if (line != "" && line[0] != '%' && line.Contains("="))
                {
                    int index = line.IndexOf("=");
                    string key = line.Substring(0, index - 1);
                    string value = line.Substring(index + 2, line.Length - (index + 2));

                    if (!entries.ContainsKey(key))
                        entries.Add(key, value);
                }
            }

            reader.Close();
            Console.WriteLine("Settings loaded");

            if (entries.ContainsKey("Directory"))
            {
                string dir = Path.GetDirectoryName(settingsFilename);
                dir += "\\" + entries["Directory"];
                entries.Remove("Directory");

                foreach (KeyValuePair<string, string> pair in entries)
                {
                    if (!pair.Key.Contains("Directory"))
                        filenames.Add(pair.Key, dir + pair.Value);
                    else
                        filenames.Add(pair.Key, "\\" + pair.Value);
                }
            }
            else
                foreach (KeyValuePair<string, string> pair in entries)
                    filenames.Add(pair.Key, pair.Value);

            loadedSettings[settingsFilename] = filenames;

            return filenames;
        }
    }
}
