using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ClickOnceOrbita
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (Environment.SpecialFolder folder in Enum.GetValues(typeof(Environment.SpecialFolder)))
            {
                Console.WriteLine(folder.ToString() + " : /n" + Environment.GetFolderPath(folder) + "/n");
            }

            Console.WriteLine("\n\n\n");

            var uris = new string[] 
            {
                "http://example.com/app/hello.exe",
                "file:///example/app/hello.exe",
                "https://example.com/app/hello.exe",
                "c:/example/app/hello.exe",
                "c:\\example\\app\\hello.exe",
                "\\\\\\example\\app\\hello.exe",
                "ftp://example.com/app/hello.exe",
                "http://example.com:666/app/hello.exe"
            };
            foreach (var uri in uris)
                TestURI(new Uri(uri));

            //System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
            //httpClient.GetAsync(uris[0]);

            //Update(@"C:\share\TestWeb.application");

            //if (args.Any())
            //{
            //    Update(args.First());
            //}

            Console.ReadKey();
        }

        static void TestURI(Uri uri)
        {
            Console.Write(
                $"uri.OriginalString:   {uri.OriginalString}\n" +   //
                //$"uri.AbsolutePath:     {uri.AbsolutePath}\n" +
                $"uri.AbsoluteUri:      {uri.AbsoluteUri}\n" +      //
                //$"uri.Fragment:         {uri.Fragment}\n" +
                //$"uri.Host:             {uri.Host}\n" +
                $"uri.LocalPath:        {uri.LocalPath}\n" +        //
                $"uri.Port:             {uri.Port}\n" +
                $"uri.Scheme:           {uri.Scheme}\n" +           //
                $"uri.Segments.Last():  {uri.Segments.Last()}\n" +  //
                $"uri.IsFile:           {uri.IsFile}\n\n");

            if (uri.Scheme == "file")
            {
                Console.WriteLine("open {0}\norigin: {1}\n", uri.LocalPath, uri.OriginalString);
            }
            else if (uri.Scheme == "http" || uri.Scheme == "https")
            {
                Console.WriteLine("download {0}\norigin: {1}\n", uri.AbsoluteUri, uri.OriginalString);
            }
            else
            {
                Console.WriteLine("No supported scheme '{0}'\n", uri.Scheme);
            }
        }

        static void Update(string uri)
        {
            try
            {
#if DEBUG
                DEBUG($"APP: {uri}\n\n"); 
#endif
                string exepath = UpdateApp(new Uri(uri), new Version(2017, 0));
                RunExe(exepath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadKey();
            }
        }

        const string asmv1 = "urn:schemas-microsoft-com:asm.v1";
        const string asmv2 = "urn:schemas-microsoft-com:asm.v2";
        const string dsig = "http://www.w3.org/2000/09/xmldsig#";

        // issue #2 apps path :: ~/orbita/
        static string AppsPath
        {
            get
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Orbita\\");
#if DEBUG
                DEBUG($"AppsPath: {path}");
#endif
                Directory.CreateDirectory(path);
                return path;
            }
        }

        static string AttrValue(XDocument xml, string nodename, string xmlns, string attributename)
        {
            return xml.Descendants(XName.Get(nodename, xmlns)).First().Attribute(attributename).Value;
        }

        static XDocument XDocumentLoad(string url)
        {
            var uri = new Uri(url);
            if (uri.Scheme == "file")
            {
                Console.WriteLine("open {0}\norigin: {1}\n", uri.LocalPath, uri.OriginalString);
            }
            else if (uri.Scheme == "http" || uri.Scheme == "https")
            {
                Console.WriteLine("download {0}\norigin: {1}\n", uri.AbsoluteUri, uri.OriginalString);
            }
            else
            {
                Console.WriteLine("No supported scheme '{0}'\n", uri.Scheme);
            }
            return XDocument.Load(url/*tmp_xml*/);

            //            string tmp_xml = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "tmp.xml");

            //            using (var client = System.Net.WebClient()/*var process = new Process()*/)
            //            {
            //                try
            //                {
            //#if DEBUG
            //                    DEBUG($"WGET tmp app file");
            //#endif              
            //                    if (url[0] == 'h')
            //                    client.DownloadFile(url, tmp_xml);
            //                    //{
            //                    //    process.StartInfo.FileName = "wget";
            //                    //    process.StartInfo.Arguments = $"{url} -O {tmp_xml}";
            //                    //    process.Start();
            //                    //}
            //                    else
            //                        File.Copy((new Uri(url)).LocalPath, tmp_xml);
            //#if DEBUG
            //                    DEBUG($"OK");
            //#endif
            //                }
            //                catch (Exception ex)
            //                {
            //                    throw new Exception($"Ошибка загрузки {url}, {ex}");
            //                }
            //            }
            //DEBUG("Load xml");
            //    return XDocument.Load(url/*tmp_xml*/);
            //DEBUG("ok xml");
        }

        static string UpdateApp(Uri uri_application, Version version)
        {
            var xml_application = XDocumentLoad(uri_application.AbsoluteUri);
            var _uri_application = new Uri(AttrValue(xml_application, "deploymentProvider", asmv2, "codebase"));

            if (_uri_application != uri_application)
            {
                try
                {
                    var _xml = XDocumentLoad(_uri_application.AbsoluteUri);
                    xml_application = _xml;
                    uri_application = _uri_application;
                    //  issue #4
                    //  recursive Load xml?
                }
                catch
                {
                    //  logging this:
                    //  "resourse not found"
                    //  "use local manifests"
                }
            }

            var _version = Version.Parse(AttrValue(xml_application, "assemblyIdentity", asmv1, "version"));

            var uri_manifest = new Uri(uri_application, AttrValue(xml_application, "dependentAssembly", asmv2, "codebase"));

            var appdir = Path.Combine(AppsPath, Path.GetFileNameWithoutExtension(uri_application.LocalPath));

            var local_manifest = Path.Combine(appdir, Path.GetFileName(uri_manifest.LocalPath));
#if DEBUG
            DEBUG($"Local app manifest: {local_manifest}");
#endif
            string appfile = Path.Combine(AppsPath, Path.GetFileName(uri_application.LocalPath));
#if DEBUG
            DEBUG($"Local deploy manifest: {local_manifest}");
#endif
            if (File.Exists(appfile))
            {
                version = Version.Parse(AttrValue(XDocumentLoad(appfile), "assemblyIdentity", asmv1, "version"));
            }

            if (_version <= version)
            {
                return Path.Combine(appdir, AttrValue(XDocumentLoad(local_manifest), "commandLine", asmv2, "file"));
            }

            var version_manifest = Version.Parse(AttrValue(xml_application, "assemblyIdentity", asmv2, "version"));

            var xml_manifest = XDocumentLoad(uri_manifest.AbsoluteUri);

            if (version_manifest != Version.Parse(AttrValue(xml_manifest, "assemblyIdentity", asmv1, "version")))
                throw new Exception(
                    $"Версия манифеста приложения  не совадает " +
                    $"с указанной в манифесте развернывания.\n\n" +
                    $"{Version.Parse(AttrValue(xml_manifest, "assemblyIdentity", asmv1, "version"))} : {uri_manifest}\n" +
                    $"{version_manifest} : {uri_application}");

            var files = new List<string>();

            files.AddRange(
                from node
                in xml_manifest.Descendants(XName.Get("file", asmv2))
                select node.Attribute("name").Value
                );

            files.AddRange(
                from node
                in xml_manifest.Descendants(XName.Get("dependentAssembly", asmv2))
                where node.Attribute("codebase") != null
                select node.Attribute("codebase").Value
                );
#if DEBUG
            DEBUG($"\nDOWNLOAD files to {appdir}\n");
#endif
            DownloadFiles(files, appdir, uri_manifest, xml_manifest);

            if (File.Exists(appfile))
                File.Move(appfile, $"{appfile}.undo");
            try
            {
                xml_application.Save(appfile);
                if (File.Exists($"{appfile}.undo"))
                    File.Delete($"{appfile}.undo");
            }
            catch
            {
                if (File.Exists(appfile))
                    File.Delete(appfile);
                if (File.Exists($"{appfile}.undo"))
                    File.Move($"{appfile}.undo", appfile);
            }

            CreateShortcut(appfile);

            return Path.Combine(appdir, AttrValue(XDocumentLoad(local_manifest), "commandLine", asmv2, "file"));
        }

        static void RunExe(string exefile)
        {
            Process process = new Process();
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                    process.StartInfo.FileName = "mono";
                    process.StartInfo.Arguments = $"{exefile} &";
                    break;
                case PlatformID.MacOSX:
                    process.StartInfo.FileName = "mono";
                    process.StartInfo.Arguments = $"{exefile} &";
                    break;
                default:
                    process.StartInfo.FileName = exefile;
                    break;
            }
            process.Start();
        }

        static void DownloadFiles(List<string> files, string appdir, Uri uri_manifest, XDocument xml_manifest)
        {
            XDocument xml_local = null;
            if (File.Exists(Path.Combine(appdir, Path.GetFileName(uri_manifest.LocalPath))))
                xml_local = XDocumentLoad(Path.Combine(appdir, Path.GetFileName(uri_manifest.LocalPath)));

            foreach (var file in files)
            {
                var newfile = Path.Combine(appdir, file);
                if (File.Exists(newfile))
                {
                    if (xml_local != null)
                    {
                        try
                        {
                            var hsh1 = xml_manifest.Descendants(XName.Get("file", asmv2))
                                 .Where(node => node.Attribute("name").Value == file)
                                  .Descendants(XName.Get("DigestValue", dsig)).First().Value;
                            var hsh2 = xml_local.Descendants(XName.Get("file", asmv2))
                                 .Where(node => node.Attribute("name").Value == file)
                                  .Descendants(XName.Get("DigestValue", dsig)).First().Value;

                            if (hsh1 == hsh2)
                            {
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            var hsh1 = xml_manifest.Descendants(XName.Get("dependentAssembly", asmv2))
                                 .Where(node => node.Attribute("codebase") != null && node.Attribute("codebase").Value == file)
                            .Descendants(XName.Get("DigestValue", dsig)).First().Value;
                            var hsh2 = xml_local.Descendants(XName.Get("dependentAssembly", asmv2))
                                 .Where(node => node.Attribute("codebase") != null && node.Attribute("codebase").Value == file)
                                  .Descendants(XName.Get("DigestValue", dsig)).First().Value;

                            if (hsh1 == hsh2)
                            {
                                continue;
                            }
                        }
                    }
                    //резервирование старых файлов
                    File.Move(newfile, $"{newfile}.undo");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(newfile));
//DOWNLOAD CLIENT
                var u = new Uri(uri_manifest, file);
                using (var client = new System.Net.WebClient())
                {
                    try
                    {
#if DEBUG
                        DEBUG($"WGET: {file}");
#endif
                        client.DownloadFile(u, newfile);
#if DEBUG
                        DEBUG($"OK");
#endif
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        DEBUG($"ERR wget file");
#endif
                        //восстановление резервных копий старых файлов 
                        foreach (var _file in files)
                        {
                            var _newfile = Path.Combine(appdir, _file);
                            if (File.Exists($"{_newfile}.undo"))
                            {
                                if (File.Exists(_newfile))
                                    File.Delete(_newfile);
                                File.Move($"{_newfile}.undo", _newfile);
                            }
                        }

                        throw new Exception($"Ошибка загрузки {file}");
                    }
                }
            }

            foreach (var file in files)
            {
                var newfile = Path.Combine(appdir, file);
                if (File.Exists($"{newfile}.undo"))
                    File.Delete($"{newfile}.undo");
            }

            var manifestfile = Path.Combine(appdir, Path.GetFileName(uri_manifest.LocalPath));
            if (File.Exists(manifestfile))
                File.Move(manifestfile, $"{manifestfile}.undo");
            try
            {
                xml_manifest.Save(manifestfile);
                if (File.Exists($"{manifestfile}.undo"))
                    File.Delete($"{manifestfile}.undo");
            }
            catch
            {
                if (File.Exists(manifestfile))
                    File.Delete(manifestfile);
                if (File.Exists($"{manifestfile}.undo"))
                    File.Move($"{manifestfile}.undo", manifestfile);
            }
        }

        static void CreateShortcut(string appfile)
        {
            //  echo "mono {appspath}/clickonce.exe appfile" > ~/Desktop/link.sh
            //  chmod a+x ~/Desktop/link.sh

            File.AppendAllText(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    Path.GetFileNameWithoutExtension(appfile) + ".bat"),
                $"{Path.Combine(AppsPath, "ClickOnceOrbita.exe")} {appfile}", Encoding.UTF8);
        }

        static void DEBUG(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}
