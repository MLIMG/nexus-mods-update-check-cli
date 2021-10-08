using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nexus_Mods_Update_Check
{
    class Program
    {
        public static string version = "1.0.1";
        public static string cliDir = Directory.GetCurrentDirectory() + "/data/";
        public static string modListFile = cliDir + "mod.list";
        //public static string modDir = cliDir + "downloads/";
        public static List<List<string>> modList = new List<List<string>>();

        static void Main(string[] args)
        {

            if (!Directory.Exists(cliDir))
            {
                Directory.CreateDirectory(cliDir);
            }

            /*if (!Directory.Exists(modDir))
            {
                Directory.CreateDirectory(modDir);
            }*/

            if (!File.Exists(modListFile))
            {
                string line = "Mod Name\tMod Version\tFile ID\tURL";
                File.AppendAllText(modListFile, line);
            }

            modList = loadModList();

            Console.Clear();

            switch (args[0])
            {
                case "-h":
                    Console.WriteLine("");
                    Console.WriteLine("Info:");
                    Console.WriteLine("This update checker do not dwonload and deploy mods.\n" +
                        "It helps you to track mod updates by cli and will open the associated download page.\n" +
                        "'-check' will check for updates and brings you to the download pages if updates are available.\n" +
                        "In the next step download, extract and deploy your updated mods manually.\n" +
                        "Finally run '-finalize' to update the mod list to the latest mod versions.");
                    Console.WriteLine("");
                    Console.WriteLine("Commands:");
                    Console.WriteLine("-v\t\t\t\tVersion info.");
                    Console.WriteLine("");
                    Console.WriteLine("-check all\t\t\tCheck mod updates for all mods in your mod list.");
                    Console.WriteLine("-check <game> <id>\t\tCheck mod update for a mod by id.");
                    Console.WriteLine("");
                    Console.WriteLine("-add <game> <id>\t\tAdd mod by id to the mod list.");
                    Console.WriteLine("-add <url>\t\t\tAdd mod by url to the mod list.");
                    Console.WriteLine("");
                    Console.WriteLine("-remove <game> <id>\t\tRemove mod by id from the mod list.");
                    Console.WriteLine("-remove <url>\t\t\tRemove mod by url from the mod list.");
                    Console.WriteLine("");
                    Console.WriteLine("-list all\t\t\tList all mods in mod list.");
                    Console.WriteLine("-list <game>\t\t\tList all mods for selected game.");
                    Console.WriteLine("");
                    Console.WriteLine("-finalize all\t\t\tThis will update all the mod versions inside the mod list.\n\t\t\t\tPlease make sure that you have to download and deploy them manually.");
                    Console.WriteLine("-finalize <game> <id>\t\t\tThis will update the selected mod versions inside the mod list.\n\t\t\t\tPlease make sure that you have to download and deploy them manually.");
                    Console.WriteLine("");
                    Console.WriteLine("-download all\t\t\tOpen the download page for all mods in the mod list.");
                    Console.WriteLine("-download <game> <id>\t\tOpen the download page for selected mod in the mod list.");
                    break;

                case "-v":
                    Console.WriteLine("");
                    Console.WriteLine("You running Nexus Mods Update Check version " + version);
                    break;

                case "-check":
                    switch (args[1])
                    {
                        case "all":
                            if(modList.Count != 0)
                            {
                                checkAll();
                            } 
                            else
                            {
                                Console.WriteLine("");
                                Console.WriteLine("Mod list dosnt exists. Please add mods to the list. For help use '-h'");
                            }
                            break;

                        default:
                            checkById(args[1], args[2]);
                            break;
                    }
                    break;

                case "-add":
                    if(args[1].Contains("http") && args[1].Contains("://"))
                    {
                        addByUrl(args[1]);
                    } else
                    {
                        addByGamId(args[1], args[2]);
                    }
                    break;

                case "-remove":
                    if (args[1].Contains("http") && args[1].Contains("://"))
                    {
                        removeByUrl(args[1]);
                    }
                    else
                    {
                        removeByGamId(args[1], args[2]);
                    }
                    break;

                case "-list":

                    Console.WriteLine("");
                    switch(args[1])
                    {
                        case "all":
                            foreach (var mod in modList)
                            {
                                Console.WriteLine("Mod name:\t\t" + mod[0]);
                                Console.WriteLine("Version:\t\t" + mod[1]);
                                Console.WriteLine("Game:\t\t\t" + mod[3].Split('/')[3]);
                                Console.WriteLine("URL:\t\t\t" + mod[3]);
                                Console.WriteLine("ID:\t\t\t" + mod[3].Split('/').Last<string>());
                                Console.WriteLine("");
                            }
                            break;

                        default:
                            foreach (var mod in modList)
                            {
                                if (mod[3].Split('/')[3].Equals(args[1]))
                                {
                                    Console.WriteLine("Mod name:\t\t" + mod[0]);
                                    Console.WriteLine("Version:\t\t" + mod[1]);
                                    Console.WriteLine("Game:\t\t\t" + mod[3].Split('/')[3]);
                                    Console.WriteLine("URL:\t\t\t" + mod[3]);
                                    Console.WriteLine("ID:\t\t\t" + mod[3].Split('/').Last<string>());
                                    Console.WriteLine("");
                                }
                            }
                            break;
                    }
                    break;

                case "-finalize":
                    switch (args[1])
                    {
                        case "all":
                            foreach (var dat in modList)
                            {
                                removeByUrl(dat[3]);
                                modList = new List<List<string>>();
                                modList = loadModList();
                                addByUrl(dat[3]);
                            }
                            break;

                        default:
                            var url = @"https://www.nexusmods.com/" + args[1] + "/mods/" + args[2];
                            removeByUrl(url);
                            modList = new List<List<string>>();
                            modList = loadModList();
                            addByUrl(url);
                            break;
                    }
                    break;

                case "-download":
                    switch (args[1])
                    {
                        case "all":
                            downloadAll();
                            break;

                        default:
                            var url = @"https://www.nexusmods.com/" + args[1] + "/mods/" + args[2];
                            downloadByGameId(url);
                            break;
                    }
                    break;
            }
        }

        public static List<List<string>> loadModList()
        {
            string text = File.ReadAllText(modListFile);
            int count = 0;
            foreach (var val in text.Split(new string[] {"\n"}, StringSplitOptions.RemoveEmptyEntries))
            {
                List<string> tmpDat = new List<string>();
                count++;
                if (count == 1) continue;
                var pairs = val.Split(new string[] { "\t" }, StringSplitOptions.None);
                foreach(var str in pairs)
                {
                    tmpDat.Add(str);
                }
                modList.Add(tmpDat);
            }
            return modList;
        }

        public static bool modExistsInList(string modURL)
        {
            bool modExists = false;
            foreach (var dat in modList)
            {
                if (modURL.Equals(dat[3]))
                {
                    modExists = true;
                    break;
                }
            }
            return modExists;
        }

        public static List<string> getModByUrlFromList(string modURL)
        {
            foreach (var dat in modList)
            {
                if (modURL.Equals(dat[3]))
                {
                    return dat;
                }
            }
            return new List<string>();
        }

        public static void addByUrl(string url)
        {
            List<string> modData = getModData(url + "?tab=files");
            Console.WriteLine("Add mod to list...");
            Console.WriteLine("");
            Console.WriteLine("Mod name:\t\t" + modData[0]);
            Console.WriteLine("Version:\t\t" + modData[1]);
            if (!modExistsInList(modData[3]))
            {
                string line = "\n";
                foreach (var dat in modData)
                {
                    line += dat + "\t";
                }

                File.AppendAllText(modListFile, line);
                Console.WriteLine("Message:\t\tMod added");
            } 
            else
            {
                Console.WriteLine("Message:\t\tMod already exists. Skip...");
            }
        }

        public static void addByGamId(string game, string id)
        {
            var url = @"https://www.nexusmods.com/" + game + "/mods/" + id;
            addByUrl(url);
        }

        public static void removeByUrl(string url)
        {
            List<string> modData = getModData(url + "?tab=files");
            List<string> modLocData = getModByUrlFromList(modData[3]);

            Console.WriteLine("Remove mod from list...");
            Console.WriteLine("");
            Console.WriteLine("Mod name:\t\t" + modLocData[0]);
            Console.WriteLine("Version:\t\t" + modLocData[1]);
            if (modExistsInList(modData[3]))
            {

                string modtext = File.ReadAllText(modListFile);
                string text = "";
                foreach (var val in modtext.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (val.Contains(modData[3])) continue;
                    text += "\n" + val;
                }
                File.WriteAllText(modListFile, text.Trim());
                Console.WriteLine("Message:\t\tMod has removed from the list.");
            }
            else
            {
                Console.WriteLine("Message:\t\tMod not found in list. Skip...");
            }
        }

        public static void removeByGamId(string game, string id)
        {
            var url = @"https://www.nexusmods.com/" + game + "/mods/" + id;
            removeByUrl(url);
        }

        public static void checkById(string game,string id)
        {
            Console.WriteLine("Check for updates...");
            Console.WriteLine("");

            var url = @"https://www.nexusmods.com/" + game + "/mods/" + id + "?tab=files";
            List<string> modData = getModData(url);
            List<string> modLocData = getModByUrlFromList(modData[3]);

            Console.WriteLine("Mod name:\t\t" + modData[0]);

            if (modExistsInList(modData[3]))
            {
                Console.WriteLine("Installed version:\t" + modLocData[1]);
                Console.WriteLine("Available version:\t" + modData[1]);

                if (!modLocData[1].Equals(modData[1]))
                {
                    System.Diagnostics.Process.Start(modLocData[3] + "&file_id=" + modData[2]);
                    Console.WriteLine("Message:\t\tUpdate available... Open default browser to download page...");
                }
                else
                {
                    Console.WriteLine("Message:\t\tMod is up to date.");
                }
            }
            else
            {
                Console.WriteLine("Message:\t\tMod not found in modlist...");
            }
            Console.WriteLine("");
        }

        public static void checkAll()
        {
            Console.WriteLine("Check for updates...");
            Console.WriteLine("");
            for (int i = 0; i < modList.Count; i++)
            {
                modList[i][3] = modList[i][3] + "?tab=files";
                List<string> modData = getModData(modList[i][3]);

                Console.WriteLine("Mod name:\t\t" + modList[i][0]);
                Console.WriteLine("Installed version:\t" + modList[i][1]);
                Console.WriteLine("Available version:\t" + modData[1]);

                if (!modList[i][1].Equals(modData[1]))
                {
                    System.Diagnostics.Process.Start(modList[i][3] + "&file_id=" + modData[2]);
                    Console.WriteLine("Message:\t\tUpdate available... Open default browser to download page...");
                } else
                {
                    Console.WriteLine("Message:\t\tMod is up to date.");
                }
                Console.WriteLine("");
            }
        }

        public static void downloadAll()
        {
            Console.WriteLine("Download all Mods...");
            Console.WriteLine("");
            for (int i = 0; i < modList.Count; i++)
            {
                modList[i][3] = modList[i][3] + "?tab=files";
                List<string> modData = getModData(modList[i][3]);

                Console.WriteLine("Mod name:\t\t" + modList[i][0]);
                Console.WriteLine("Installed version:\t" + modList[i][1]);
                Console.WriteLine("Available version:\t" + modData[1]);

                System.Diagnostics.Process.Start(modList[i][3] + "&file_id=" + modData[2]);
                Console.WriteLine("Message:\t\tOpen default browser to download page...");

                Console.WriteLine("");
            }
        }

        public static void downloadByGameId(string url)
        {
            Console.WriteLine("Download selected mod...");
            Console.WriteLine("");

            if (modExistsInList(url))
            {
                url = url + "?tab=files";
                List<string> modData = getModData(url);
                List<string> modLocData = getModByUrlFromList(modData[3]);

                Console.WriteLine("Mod name:\t\t" + modLocData[0]);
                Console.WriteLine("Installed version:\t" + modLocData[1]);
                Console.WriteLine("Available version:\t" + modData[1]);

                System.Diagnostics.Process.Start(url + "&file_id=" + modData[2]);
                Console.WriteLine("Message:\t\tOpen default browser to download page...");
            } 
            else
            {
                Console.WriteLine("Message:\t\tMod not found in mod list...");
            }

        }

        public static List<string> getModData(string url)
        {
            List<string> modData = new List<string>();

            HtmlWeb web = new HtmlWeb();

            var htmlDoc = web.Load(url);

            //Load Mod Name
            var node_modnameHolder = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@id, 'pagetitle')]");
            HtmlDocument doc_pagetitle = new HtmlDocument();
            doc_pagetitle.LoadHtml(node_modnameHolder.OuterHtml);
            var node_h1 = doc_pagetitle.DocumentNode.SelectSingleNode("//h1");
            string ModName = node_h1.InnerText;

            //Load Version
            var node_versionHolder = htmlDoc.DocumentNode.SelectSingleNode("//li[contains(@class, 'stat-version')]");
            HtmlDocument doc_version = new HtmlDocument();
            doc_version.LoadHtml(node_versionHolder.OuterHtml);
            var node_version = doc_version.DocumentNode.SelectSingleNode("//div[@class = 'stat']");
            string version = node_version.InnerText;

            //load latest file
            var node_files = htmlDoc.DocumentNode.SelectSingleNode("//dt[contains(@class, 'file-expander-header')]");
            string file_id = node_files.Attributes["data-id"].Value;

            //populate data
            modData.Add(ModName);
            modData.Add(version);
            modData.Add(file_id);
            if (url.Contains("?")) url = url.Split('?')[0];
            modData.Add(url);

            return modData;
        }

    }
}
