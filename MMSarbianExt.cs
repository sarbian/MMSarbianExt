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
    [KSPAddonFixed(KSPAddon.Startup.Instantly, true, typeof(MMSarbianExt))]
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

                    // it's a modification node and it's not one Modulemanager will process
                    if (pattern.Contains("*") || pattern.Contains("?") || (splits.Length > 2 && splits[2].Contains("HAS") && !splits[2].Contains("Final")))
                    {
                        String cond = "";
                        if (splits.Length > 2 && splits[2].Length > 5) 
                        {
                            int start = splits[2].IndexOf("HAS[") + 4;
                            cond = splits[2].Substring(start, splits[2].LastIndexOf(']') - start);
                        }
                        foreach (UrlDir.UrlConfig url in GameDatabase.Instance.root.AllConfigs)
                        {
                            if (url.name[0] != '@' && WildcardMatch(url.name, pattern) && CheckCondition(url.config, cond))
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

        // Split condiction while not getting lost in embeded brackets
        public List<string> SplitCondition(string cond)
        {
            cond = cond + ",";
            List<string> conds = new List<string>();
            int start = 0;
            int level = 0;
            for (int end = 0; end < cond.Length; end++)
            {
                if (cond[end] == ',' && level == 0)
                {
                    conds.Add(cond.Substring(start, end-start).Trim());
                    start = end + 1;
                }
                else if (cond[end] == '[') level++;
                else if (cond[end] == ']') level--;
            }
            return conds;
        }

        public bool CheckCondition(ConfigNode node, string conds)
        {
            if (conds.Length > 0)
            {
                List<string> condsList = SplitCondition(conds);

                if (condsList.Count == 1)
                {
                    conds = condsList[0];

                    string remainCond = "";
                    if (conds.Contains("HAS["))
                    {
                        int start = conds.IndexOf("HAS[") + 4;
                        remainCond = conds.Substring(start, condsList[0].LastIndexOf(']') - start);
                        conds = conds.Substring(0, start - 5);
                    }

                    string type = conds.Substring(1).Split('[')[0].Trim();
                    string name = conds.Split('[')[1].Replace("]", "").Trim();

                    if (conds[0] == '@' || conds[0] == '!')  // @MODULE[ModuleAlternator] or !MODULE[ModuleAlternator]
                    {
                        bool not = (conds[0] == '!');
                        ConfigNode subNode = ConfigManager.FindConfigNodeIn(node, type, name);
                        if (subNode != null)
                            return not ^ CheckCondition(subNode, remainCond);
                        else
                            return not ^ false;
                    }
                    else if (conds[0] == '#') // #module[Winglet]
                    {
                        if (node.HasValue(type) && node.GetValue(type).Equals(name))
                            return CheckCondition(node, remainCond);
                        else
                            return false;
                    }
                    else
                        return false; // Syntax error
                }
                else  // Multiple condition
                {
                    foreach (string cond in condsList)
                    {
                        if (!CheckCondition(node, cond))
                            return false;
                    }
                    return true;
                }
            }
            else
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


    /// <summary>
    /// KSPAddon with equality checking using an additional type parameter. Fixes the issue where AddonLoader prevents multiple start-once addons with the same start scene.
    /// </summary>
    public class KSPAddonFixed : KSPAddon, IEquatable<KSPAddonFixed>
    {
        private readonly Type type;

        public KSPAddonFixed(KSPAddon.Startup startup, bool once, Type type)
            : base(startup, once)
        {
            this.type = type;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != this.GetType()) { return false; }
            return Equals((KSPAddonFixed)obj);
        }

        public bool Equals(KSPAddonFixed other)
        {
            if (this.once != other.once) { return false; }
            if (this.startup != other.startup) { return false; }
            if (this.type != other.type) { return false; }
            return true;
        }

        public override int GetHashCode()
        {
            return this.startup.GetHashCode() ^ this.once.GetHashCode() ^ this.type.GetHashCode();
        }
    }

}
