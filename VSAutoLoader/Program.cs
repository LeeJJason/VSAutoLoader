using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace CSAutoLoader
{
    class Program
    {
        private static HashSet<string> excludeFolders = new HashSet<string>();
        private static HashSet<string> excludeFiles = new HashSet<string>();
        private static HashSet<string> includeFiles = new HashSet<string>();

        private static bool Check(string file)
        {
            foreach (var item in excludeFolders)
            {
                if (file.Contains(item))
                    return false;
            }

            foreach (var item in includeFiles)
            {
                if (file.EndsWith(item))
                    return true;
            }

            foreach (var item in excludeFiles)
            {
                if (file.EndsWith(item))
                    return false;
            }

            if (file.EndsWith(".cs"))
                return true;
            return false;
        }

        static void LoadConfig()
        {
            excludeFolders.Add(string.Format("{0}{1}{0}", Path.DirectorySeparatorChar, "bin"));
            excludeFolders.Add(string.Format("{0}{1}{0}", Path.DirectorySeparatorChar, "obj"));

            includeFiles.Add("App.config");
            includeFiles.Add(".loader");
            string file = ".loader";
            if (File.Exists(file))
            {
                StreamReader reader = new StreamReader(File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read));
                string line = null;
                string state = null;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        switch (line)
                        {
                            case "ExcludeFolder":
                            case "ExcludeFile":
                            case "IncludeFile":
                                state = line;
                                continue;
                        }

                        switch (state)
                        {
                            case "ExcludeFolder":
                            case "ExcludeFile":
                            case "IncludeFile":
                                HashSet<string> filter = state == "ExcludeFolder" ? excludeFolders :
                                    (state == "ExcludeFile" ? excludeFiles : includeFiles);
                                string[] ss = line.Split(';');
                                for (int i = 0; i < ss.Length; ++i)
                                {
                                    filter.Add(state == "ExcludeFolder" ? string.Format("{0}{1}{0}", Path.DirectorySeparatorChar, ss[i]) : ss[i]);
                                }
                                break;
                        }
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            LoadConfig();
            string path = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            string[] files = Directory.GetFiles(path, "*.csproj", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; ++i)
            {
                Parser(files[i]);
            }
        }

        static void Parser(string file)
        {
            XDocument document = XDocument.Load(file);
            var root = document.Root;
            XName name = root.Name.Namespace + "ItemGroup";
            var ItemGroups = document.Root.Elements(name);
            XElement compileGoup = null, noneGroup = null, contentGroup = null;
            foreach (var group in ItemGroups)
            {
                var item = group.FirstNode as XElement;
                if (item != null)
                {
                    switch (item.Name.LocalName)
                    {
                        case "Compile":
                            compileGoup = group;
                            group.RemoveAll();
                            break;
                        case "None":
                            noneGroup = group;
                            group.RemoveAll();
                            break;
                        case "Content":
                            contentGroup = group;
                            group.RemoveAll();
                            break;
                    }
                }
            }
            string path = Path.GetDirectoryName(file);
            LoadFromPath(path, path, root, compileGoup, noneGroup, contentGroup);
            document.Save(file);

        }

        private static void LoadFromPath(string path, string basePath, XElement root, XElement compileGoup, XElement noneGroup, XElement contentGroup)
        {
            string[] files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; ++i)
            {
                string file = files[i];//.Replace('\\', '/');
                if (Check(file))
                {
                    file = file.Substring(basePath.Length + 1);
                    if (file.EndsWith(".cs"))
                    {
                        compileGoup = CheckGroup(root, compileGoup);

                        AddItem(compileGoup, "Compile", file);
                    }
                    else if (file.EndsWith(".config"))
                    {
                        noneGroup = CheckGroup(root, noneGroup);

                        AddItem(noneGroup, "None", file);
                    }
                    else
                    {
                        contentGroup = CheckGroup(root, contentGroup);

                        AddItem(contentGroup, "Content", file);
                    }
                }
            }

            string[] paths = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < paths.Length; ++i)
            {
                path = paths[i];
                if (Directory.GetFiles(path, "*.csproj", SearchOption.TopDirectoryOnly).Length > 0)
                {
                    continue;
                }
                LoadFromPath(path, basePath, root, compileGoup, noneGroup, contentGroup);
            }
        }

        private static void AddItem(XElement group, string name, string file)
        {
            group.Add(new XElement(group.Name.Namespace + name, new XAttribute("Include", file)));
        }

        private static XElement CheckGroup(XElement root, XElement group)
        {
            if (group == null)
            {
                group = new XElement(root.Name.Namespace + "ItemGroup");
                root.Add(group);
            }
            return group;
        }

    }
}
