using System.Collections.Generic;
using UnityEngine;
using KSP;
using ModuleManager;
using System;
using System.Reflection;
using System.Text.RegularExpressions;


/* Extension for ialdabaoth  ModuleManager
 * Code totaly inspired by his
 */


namespace MMSarbianExt
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class MMSarbianExt : MonoBehaviour // or ConfigManager ?
    {


        bool loaded = false;
        public void OnGUI()
        {
            if (loaded)
                return;

            bool MMpresent = false;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {                
                if (assembly.GetName().Name == "ModuleManager")
                    MMpresent = true;
            }
            if (!MMpresent)
            {
                print("MMSarbianExt require ModuleManager to work. Install it");
                loaded = true;
                return;
            }
            print("MMSarbianExt loading cfg patches...");

            foreach (UrlDir.UrlConfig mod in GameDatabase.Instance.root.AllConfigs)
            {
                if (mod.type[0] == '@')
                {
                    char[] sep = new char[] { '[', ']' };
                    string[] splits = mod.name.Split(sep,3);
                    string pattern = splits[1];

                    print("MMSarbianExt " + mod.name + " | " + mod.type + " | "  + splits.Length + " | " + pattern  + " | " + splits[2]);

                    // it's a modification node and it's not one Modulemanager will process
                    if (pattern.Contains("*") || pattern.Contains("?") || (splits.Length > 2 && splits[2].Contains("Has") && !splits[2].Contains("Final")))
                    {
                        String cond = "";
                        if (splits.Length > 2 && splits[2].Length > 5) 
                        {
                            int start = splits[2].IndexOf("Has[") + 4;
                            cond = splits[2].Substring(start, splits[2].LastIndexOf(']') - start);
                            print("MMSarbianExt Cond : " + cond);
                        }
                        foreach (UrlDir.UrlConfig url in GameDatabase.Instance.root.AllConfigs)
                        {
                            if (url.name[0] != '@' && WildcardMatch(url.name, pattern) && CheckCondition(mod.config, cond))
                            {
                                print("Applying node " + mod.url + " to " + url.url);
                                url.config = ConfigManager.ModifyNode(url.config, mod.config);
                            }
                        }
                    }

                    // TODO : :Final nodes

                }
            }


            loaded = true;
        }

        public bool CheckCondition(ConfigNode node, String cond)
        {
            return true;
        }

        private bool WildcardMatch(String s, String wildcard)
        {
            String pattern = "^" + Regex.Escape(wildcard).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
            Regex regex;
            regex = new Regex(pattern);

            return (regex.IsMatch(s));
        } 


    }
}
