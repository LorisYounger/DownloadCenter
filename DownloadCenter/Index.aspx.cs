using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using LinePutScript;
using System.IO;
using System.Configuration;
using System.Text;

namespace DownloadCenter
{
    public partial class Index : System.Web.UI.Page
    {
        public static string rootPath = null;
        protected void Page_Load(object sender, EventArgs e)
        {
            if (rootPath == null)
            {
                if (ConfigurationManager.AppSettings["urlrewrite"].ToLower() == "true")
                {
                    rootPath = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host + '/';
                }
                else
                {
                    rootPath = HttpContext.Current.Request.Path + "?rootPath=";
                }
            }
            string showpath = Page.Request.QueryString["rootPath"];
            if (string.IsNullOrEmpty(showpath))
                showpath = "";
            string permissionkey = Page.Request.QueryString["permission"];
            int permission = 0;
            if (!string.IsNullOrEmpty(permissionkey))
                for (int i = 255; i > 0; i--)
                    if (ConfigurationManager.AppSettings[$"permission_{i}"] == permissionkey)
                    {
                        permission = i;
                        break;
                    }

            ShowFile(showpath.Replace('/', '\\'), permission);
        }
        private void ShowFile(string pathname, int permission)
        {
            if (Application["permission#" + permission + ":|" + pathname.ToLower()] != null)
            {
                Table.Text = (string)Application["permission#" + permission + ":|" + pathname.ToLower()];
                Title = (string)Application["title:|permission#" + permission + ":|" + pathname.ToLower()];
                h1title.InnerText = (string)Application["h1title:|permission#" + permission + ":|" + pathname.ToLower()];
                CurrentFolder.InnerText = (string)Application["current:|permission#" + permission + ":|" + pathname.ToLower()];
                h2readme.InnerText = (string)Application["h2readme:|permission#" + permission + ":|" + pathname.ToLower()];
                ReadMe.Text = (string)Application["h2text:|permission#" + permission + ":|" + pathname.ToLower()];
                return;
            }
            string localpath = Path.GetDirectoryName(Page.Request.PhysicalPath);
            string lpspath = localpath + '\\' + pathname + "\\index.lps";
            if (!File.Exists(lpspath))
            {
                lpspath = localpath + '\\' + pathname + ".lps";
                if (!File.Exists(lpspath))
                    if (pathname.Contains('\\'))
                    {
                        lpspath = localpath + '\\' + pathname.Replace("\\", "__") + ".lps";
                        if (!File.Exists(lpspath))
                        {
                            lpspath = null;
                        }
                    }
                    else
                    {
                        lpspath = null;
                    }
            }
            //以后会更新进lineput的功能,支持多行编辑
            LpsDocument lps = null; DirectoryInfo thisdi = null;
            int.TryParse(ConfigurationManager.AppSettings[$"permission_auto"], out int per_auto);
            string name = ConfigurationManager.AppSettings["webname"];
            if (lpspath != null)
            {
                lps = new LpsDocument(File.ReadAllText(lpspath).Replace("\r", "").Replace(":\n|", "/n").Replace(":\n:", ""));

                if (lps.First()[(gint)"permission"] > permission)
                {
                    ShowFile("401", permission);
                    return;
                }

                Title = lps.First()[(gstr)"name"] + '-' + name;
                h1title.InnerText = lps.First()[(gstr)"name"] + " - " + name;
                CurrentFolder.InnerText = lps.First()[(gstr)"fullname"];
                h2readme.InnerText = lps["readme"][(gstr)"title"];
                switch (lps["readme"][(gstr)"type"])
                {
                    case "txt":
                    case "plain":
                    default:
                        ReadMe.Text = $"<p>{lps["readme"].Text.Replace("  ", "&nbsp;&nbsp;").Replace("\n", "<br />")}</p>";
                        break;
                    case "html":
                        ReadMe.Text = lps["readme"].Text;
                        break;
                    case "md":
                    case "markdown":
                        ReadMe.Text = Westwind.Web.Markdown.Markdown.Parse(lps["readme"].Text);
                        break;
                }
            }
            else if (Directory.Exists(localpath + '\\' + pathname))
            {//虽然说找不到定义文本,但是找得到目录,所以通过目录进行                
                if (permission < per_auto)
                {
                    ShowFile("401", permission);
                    return;
                }
                thisdi = new DirectoryInfo(localpath + '\\' + pathname);

                Title = thisdi.Name + '-' + name;
                h1title.InnerText = thisdi.Name + " - " + name;
                CurrentFolder.InnerText = pathname;
                h2readme.InnerText = thisdi.Name;
                ReadMe.Text = $"<p>创建时间:{thisdi.CreationTime}</p><p>上次修改时间:{thisdi.LastWriteTime}</p><p>上次访问时间:{thisdi.LastAccessTime}</p>";
            }
            else
            {
                ShowFile("404", permission);
                return;
            }



            StringBuilder table = new StringBuilder();
            DirectoryInfo currdi = new DirectoryInfo(localpath + '\\' + pathname);
            int id = 0;
            //先加index
            if (pathname != "" && pathname != "\\")
            {
                //看看有没有上级目录
                //只要上级不是index,就有上级目录
                if (lps != null)
                {
                    table.AppendLine($"<tr class=\"bgcA\"><td>{++id}</td><td><a href=\"{HttpContext.Current.Request.Path}\">{name}</a></td><td>返回主目录</td><td>{Directory.GetLastWriteTime(lpspath)}</td><td>Index</td><td>-</td></tr>");

                    string[] path = lps.First()[(gstr)"fullname"].Split('\\');
                    if (lps.First()[(gstr)"fullname"].StartsWith("\\") && path.Last() != "")
                    {
                        string upperpath = "";
                        for (int i = 0; i < path.Length - 2; i++)
                        {
                            upperpath += '\\' + path[i];
                        }
                        DateTime time = DateTime.MinValue;
                        if (File.Exists(localpath + '\\' + upperpath + "\\index.lps"))
                        {
                            time = File.GetLastWriteTime(localpath + '\\' + upperpath + "\\index.lps");
                        }
                        else if (Directory.Exists(localpath + '\\' + upperpath))
                        {
                            time = Directory.GetLastWriteTime(localpath + '\\' + upperpath);
                        }
                        else if (Directory.Exists(localpath + '\\' + pathname.Replace("/", "__") + ".lps"))
                        {
                            time = Directory.GetLastWriteTime(localpath + '\\' + pathname.Replace("/", "__") + ".lps");
                        }
                        if (currdi.Exists && currdi.Parent.FullName != new FileInfo(HttpContext.Current.Request.PhysicalPath).Directory.FullName)
                            table.AppendLine($"<tr class=\"bgcC\"><td>{++id}</td><td><a href=\"{rootPath}{upperpath}\">{upperpath}</a></td><td>返回上一级</td><td>{(time == DateTime.MinValue ? "-" : time.ToShortDateString() + ' ' + time.ToShortTimeString())}</td><td>Upper</td><td>-</td></tr>");
                    }
                }
                else if (thisdi != null)
                {
                    table.AppendLine($"<tr class=\"bgcA\"><td>{++id}</td><td><a href=\"{HttpContext.Current.Request.Path}\">{name}</a></td><td>返回主目录</td><td>{thisdi.LastWriteTime}</td><td>Index</td><td>-</td></tr>");
                    if (thisdi.Parent.FullName != new FileInfo(HttpContext.Current.Request.PhysicalPath).Directory.FullName)
                        table.AppendLine($"<tr class=\"bgcC\"><td>{++id}</td><td><a href=\"{rootPath}{thisdi.Parent.FullName.Replace(localpath, "")}\">{thisdi.Parent.Name}</a></td><td>返回上一级</td><td>{thisdi.Parent.LastWriteTime}</td><td>Upper</td><td>-</td></tr>");
                }
            }
            //然后是自定义的文件夹
            //自定义的文件夹和自动扫描的文件夹应该加在一起,所以新建一个类拿去用

            List<ItemFolder> items = new List<ItemFolder>();

            if (lps != null)
            {
                foreach (Line item in lps.FindAllLine("folder"))
                {
                    if (permission >= item[(gint)"permission"])
                        items.Add(new ItemFolder(item, localpath));
                }
                if (currdi.Exists && lps.First()[(gbol)"autofolder"])
                    foreach (DirectoryInfo di in currdi.GetDirectories())
                    {
                        if (lps.FindLine("disable") == null
                            || ((lps["disable"].Find("is") == null || lps["disable"]["is"].GetInfos().FirstOrDefault(x => x.ToLower() == di.Name.ToLower()) == null)
                            && (lps["disable"].Find("like") == null || lps["disable"]["like"].GetInfos().FirstOrDefault(x => di.Name.ToLower().Contains(x.ToLower())) == null)
                             && (lps["disable"].Find("startwith") == null || lps["disable"]["startwith"].GetInfos().FirstOrDefault(x => di.Name.ToLower().StartsWith(x.ToLower())) == null)
                             && (lps["disable"].Find("endwith") == null || lps["disable"]["endwith"].GetInfos().FirstOrDefault(x => di.Name.ToLower().EndsWith(x.ToLower())) == null)
                             && items.FirstOrDefault(x => x.Name.ToLower() == di.Name.ToLower()) == null))
                            items.Add(new ItemFolder(di, localpath));
                    }
            }
            else if (thisdi != null)
            {
                if (permission >= per_auto)
                    foreach (DirectoryInfo di in currdi.GetDirectories())
                        items.Add(new ItemFolder(di, localpath));
            }


            items.Sort();
            foreach (ItemFolder item in items)
                table.AppendLine(item.ToString(++id, Page));
            items.Clear();

            //然后是自定义的文件和扫描的文件
            if (lps != null)
            {
                foreach (Line item in lps.FindAllLine("file"))
                {
                    if (permission >= item[(gint)"permission"])
                        items.Add(new ItemFolder(item, localpath));
                }
                if (thisdi != null || currdi.Exists && lps.First()[(gbol)"autofile"])
                    foreach (FileInfo di in currdi.GetFiles())
                    {
                        if (lps.FindLine("disable") == null
                            || ((lps["disable"].Find("is") == null || lps["disable"]["is"].GetInfos().FirstOrDefault(x => x.ToLower() == di.Name.ToLower()) == null)
                            && (lps["disable"].Find("like") == null || lps["disable"]["like"].GetInfos().FirstOrDefault(x => di.Name.ToLower().Contains(x.ToLower())) == null)
                             && (lps["disable"].Find("startwith") == null || lps["disable"]["startwith"].GetInfos().FirstOrDefault(x => di.Name.ToLower().StartsWith(x.ToLower())) == null)
                             && (lps["disable"].Find("endwith") == null || lps["disable"]["endwith"].GetInfos().FirstOrDefault(x => di.Name.ToLower().EndsWith(x.ToLower())) == null)
                             && items.FirstOrDefault(x => x.Name.ToLower() == di.Name.ToLower()) == null))
                            items.Add(new ItemFolder(di, localpath));
                    }
            }
            else if (thisdi != null)
            {
                if (permission >= per_auto)
                    foreach (FileInfo di in currdi.GetFiles())
                        items.Add(new ItemFolder(di, localpath));
            }


            items.Sort();
            foreach (ItemFolder item in items)
                table.AppendLine(item.ToString(++id, Page));

            Table.Text = table.ToString();

            //储存缓存
            Application["permission#" + permission + ":|" + pathname.ToLower()] = Table.Text;
            Application["title:|permission#" + permission + ":|" + pathname.ToLower()] = Title;
            Application["h1title:|permission#" + permission + ":|" + pathname.ToLower()] = h1title.InnerText;
            Application["current:|permission#" + permission + ":|" + pathname.ToLower()] = CurrentFolder.InnerText;
            Application["h2readme:|permission#" + permission + ":|" + pathname.ToLower()] = h2readme.InnerText;
            Application["h2text:|permission#" + permission + ":|" + pathname.ToLower()] = ReadMe.Text;
        }
        public class ItemFolder : IComparable<ItemFolder>
        {
            public string Name;
            public string Link;
            public string LinkType;
            public DateTime LastWriteTime;
            public string Comments;
            readonly string classstr;
            public ItemFolderType Type;
            public long Size = -1;
            public enum ItemFolderType
            {
                Raw_File,
                Raw_Folder,
                Link_File,
                Link_Folder,
            }
            public ItemFolder(DirectoryInfo di, string localpath)
            {
                Name = di.Name;
                Link = di.FullName.Replace(localpath, "");
                LastWriteTime = di.LastWriteTime;
                Comments = "-";
                classstr = " class=\"bgcD\"";
                Type = ItemFolderType.Raw_Folder;
                LinkType = "inlink";
            }
            public ItemFolder(FileInfo fi, string localpath)
            {
                Name = fi.Name;
                string path = HttpContext.Current.Request.Path;
                Link = path.Substring(0, path.LastIndexOf('/')) + fi.FullName.Replace(localpath, "").Replace('\\', '/');
                LastWriteTime = fi.LastWriteTime;
                Comments = "-";
                classstr = "";
                Type = ItemFolderType.Raw_Folder;
                Size = fi.Length;
                LinkType = "link";
            }
            public ItemFolder(Line di, string localpath)
            {
                Name = di[(gstr)"name"];
                Link = di[(gstr)"link"];
                LastWriteTime = DateTime.MinValue;
                Comments = di[(gstr)"comments"];
                Size = di.GetInt("size", -1);
                LinkType = di.GetString("linktype", "").ToLower();

                string nowpath = localpath + '\\' + Link;
                if (di.Find("time") != null)
                {
                    if (di[(gstr)"time"] != "")
                        DateTime.TryParse(di[(gstr)"time"], out LastWriteTime);
                }
                else if (di.Name.ToLower() == "folder")
                {
                    if (File.Exists(nowpath + "\\index.lps"))
                    {
                        LastWriteTime = File.GetLastWriteTime(nowpath + "\\index.lps");
                    }
                    else if (Directory.Exists(nowpath))
                    {
                        LastWriteTime = Directory.GetLastWriteTime(nowpath);
                    }
                    else if (Directory.Exists(nowpath + '\\' + Link.Replace("/", "__") + ".lps"))
                    {
                        LastWriteTime = Directory.GetLastWriteTime(nowpath + '\\' + Link.Replace("/", "__") + ".lps");
                    }
                    classstr = " class=\"bgcD\"";
                    Type = ItemFolderType.Link_Folder;
                }
                else if (di.Name.ToLower() == "file")
                {
                    if (File.Exists(nowpath))
                    {
                        LastWriteTime = File.GetLastWriteTime(nowpath);
                    }
                    classstr = " class=\"bgcD\"";
                    Type = ItemFolderType.Link_File;
                }
            }
            public string ToString(int id, Page page)
            {
                switch (LinkType)
                {
                    case "button":
                    case "function":
                        return $"<tr{classstr}><td>{id}</td><td><button class=\"buttonlink\" onclick=\"{Link}()\">{Name}</button></td><td>{Comments}</td><td>{(LastWriteTime == DateTime.MinValue ? "-" : LastWriteTime.ToShortDateString() + ' ' + LastWriteTime.ToShortTimeString())}</td><td>{Type}</td><td>{SizeToString(Size)}</td></tr>";
                    case "inlink":
                        string per = page.Request.QueryString["permission"];
                        if (string.IsNullOrEmpty(per))
                        {
                            per = "";
                        }
                        else
                        {
                            per = "&permission=" + per;
                        }
                        return $"<tr{classstr}><td>{id}</td><td><a href=\"{rootPath}{Link}{per}\">{Name}</a></td><td>{Comments}</td><td>{(LastWriteTime == DateTime.MinValue ? "-" : LastWriteTime.ToShortDateString() + ' ' + LastWriteTime.ToShortTimeString())}</td><td>{Type}</td><td>{SizeToString(Size)}</td></tr>";
                    default:
                        return $"<tr{classstr}><td>{id}</td><td><a href=\"{Link}\">{Name}</a></td><td>{Comments}</td><td>{(LastWriteTime == DateTime.MinValue ? "-" : LastWriteTime.ToShortDateString() + ' ' + LastWriteTime.ToShortTimeString())}</td><td>{Type}</td><td>{SizeToString(Size)}</td></tr>";

                }
            }

            public int CompareTo(ItemFolder other)
            {
                return Name.CompareTo(other.Name);
            }

            public static string SizeToString(long size)
            {
                if (size < 0)
                    return "-";
                else if (size < 1000)
                    return size.ToString() + "b";
                else if (size < 1000000)
                    return (size / 1000.0).ToString("f2") + "kb";
                else if (size < 1000000000)
                    return (size / 1000000.0).ToString("f2") + "mb";
                else if (size < 1000000000000)
                    return (size / 1000000000.0).ToString("f2") + "gb";
                else
                    return (size / 1000000000000.0).ToString("f2") + "tb";
            }
        }
    }
}