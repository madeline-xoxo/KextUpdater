﻿using System.Net;
using System.Text.Json;
using System.Text;
using Microsoft.VisualBasic.FileIO;
using System.IO;
using System.IO.Compression;
using Microsoft.Win32;
using System.Runtime.InteropServices;
/*
KextUpdater

A (poorly written) tool made to update kexts (kernel extensions) on a hackintosh.
Easily some of the worst code I have written in my life, but it seems to work sometimes.

Authored by Not a Robot in 2022
*/


Console.Write("Please drag and drop your kexts folder onto the console window: ");
string kextdir = Console.ReadLine();
if (kextdir.EndsWith(" "))
    kextdir = kextdir.Remove(kextdir.Length - 1, 1) + @"/";
//string kextdir = @"C:\Users\Shaun\Downloads\Lenovo-ThinkPad-X1C7-OC-Hackintosh-master(1)\Lenovo-ThinkPad-X1C7-OC-Hackintosh-master\EFI\OC\Kexts";
// if (!Directory.Exists(kextdir))
// {
//     Console.ForegroundColor = ConsoleColor.Red;
//     Console.WriteLine("Invalid directory: exiting...");
//     Console.ForegroundColor = ConsoleColor.Gray;
//     Environment.Exit(1);
// }
HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://raw.githubusercontent.com/NotARobot6969/KextUpdater/master/KextUpdater/kexts.json?time=" + DateTime.Now); // i get that this is a bad
HttpWebResponse response = (HttpWebResponse)request.GetResponse();                                                                                                           // solution, github wont last
string kextJson;                                                                                                                                                            // forever but idc lol
var encoding = Encoding.ASCII;
using (var reader = new StreamReader(response.GetResponseStream(), encoding))
{
    kextJson = reader.ReadToEnd();
}
List<Kexts> kexts = JsonSerializer.Deserialize<List<Kexts>>(kextJson);
List<string> names = new List<string>();

foreach (Kexts kext in kexts)
{
    names.Add(kext.Name);
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.WriteLine(kext.Name + " has been added to the local DB!");
}

List<string> dirs = new List<string>(Directory.EnumerateDirectories(kextdir));
foreach (string curdir in dirs)
{
    bool supported = true;
    string fileName = $"{curdir.Substring(curdir.LastIndexOf(Path.DirectorySeparatorChar) + 1)}";
    string kextName = fileName.Substring(0, fileName.Length - 5);
    if (names.Contains(kextName)) 
    {
        try
        {
            int index = -1;
            foreach (Kexts kext in kexts)
            {
                if (kext.Name == kextName)
                    index = kexts.IndexOf(kext);
            }
            Kexts currentKext = kexts[index];
            string downloadName = "";
            string versionprefix = "";
            List<string> downloadList = kexts[index].Format.Split(',').ToList();
            string seperator = downloadList[0];
            downloadList.RemoveAt(0);
            int versionIndex = 0;
            for (int i = 0; i < downloadList.Count; i++)
            {
                switch (downloadList[i])
                {
                    case "Name":
                        if (currentKext.Name == "BlueToolFixup")
                        {
                            downloadList[i] = seperator + "BcrmPatchRAM";
                        } else
                        {
                            downloadList[i] = seperator + currentKext.Name;
                        }
                        break;
                    case "Version":
                        versionIndex = i;
                        string url = currentKext.URL + "releases/latest";
                        HttpWebRequest versionRequest = (HttpWebRequest)WebRequest.Create(url);
                        versionRequest.Method = "GET";
                        HttpWebResponse versionResponse = (HttpWebResponse)versionRequest.GetResponse();
                        Uri loc = versionResponse.ResponseUri;
                        string pathandquery = loc.PathAndQuery;
                        List<string> paths = pathandquery.Split("/").ToList();
                        downloadList[i] = seperator + versionprefix + paths.Last();
                        break;
                    case "NOSUPPORT":
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine(kextName + " is not supported by this program because " + downloadList[i + 1]);
                        supported = false;
                        break;

                }
                if (downloadList[i].Substring(0, 1) == "(")
                {
                    string replaceString = downloadList[i].Substring(1, downloadList[i].Length - 2);
                    downloadList[i] = seperator + replaceString;
                }
                downloadName += downloadList[i];
            }
            downloadName = downloadName.Substring(1);
            Console.ForegroundColor = ConsoleColor.Cyan;
            // Console.WriteLine("DEBUG: " + downloadName);
            using (var client = new WebClient())
            {
                string tmp = Path.GetTempPath() + @"\.kxttmp";
                if (Directory.Exists(tmp + @"\" + downloadName))
                {
                    Directory.Delete(tmp + @"\" + downloadName, true);
                }
                Directory.CreateDirectory(tmp);
                string downloadUri = currentKext.URL + "releases/download/" + downloadList[versionIndex].Substring(1) + "/" + downloadName + ".zip";
                client.DownloadFile(downloadUri, tmp + @"\" + downloadName + ".zip");
                ZipFile.ExtractToDirectory(tmp + @"\" + downloadName + ".zip", tmp + @"\" + downloadName);
                File.Delete(tmp + @"\" + downloadName + ".zip");
                DirectoryInfo directory = new DirectoryInfo(tmp + @"\" + downloadName);
                foreach (var dir in directory.EnumerateDirectories())
                {
                    if (dir.Name.Contains("dSYM") || !dir.Name.Contains(".kext") || dir.Name == "AppleALCU.kext")
                    {
                        try
                        {
                            Directory.Delete(dir.FullName, true);
                        } catch { }
                    }
                    if (Directory.Exists(dir.FullName))
                    {
                        if (Directory.Exists(kextdir + @"\" + dir.Name))
                            Directory.Delete(kextdir + @"\" + dir.Name, true);
                        if (Directory.Exists(kextdir + dir.Name))
                            Directory.Delete(kextdir + dir.Name, true);
                        // Directory.Move(dir.FullName, kextdir + @"\" + dir.Name); // this causes #1, workaround is to use visual basic func FileSystem.CopyDirectory();
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            FileSystem.CopyDirectory(dir.FullName, kextdir + @"\" + dir.Name);
                        } else
                        {
                            FileSystem.CopyDirectory(dir.FullName, kextdir + dir.Name);
                        }
                        Directory.Delete(dir.FullName, true);
                        if (Directory.Exists(tmp + @"\" + downloadName))
                        Directory.Delete(tmp + @"\" + downloadName, true);
                    }



                }
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Kext " + kextName + " was updated successfully!");

        } catch (Exception ex)
        {
            if (supported)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("Kext " + kextName + " was detected, but was unable to update. Please report which kext this is as a GitHub issue!");
                Console.WriteLine(ex);
            }
        }
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(kextName + " was not found in the database. Please create a pull request to add it or an issue with details!");
    }
}
Console.ForegroundColor = ConsoleColor.Gray;
class Kexts
{
    public string Name { get; set; } = "undefined";
    public string URL { get; set; } = "https://example.com";
    public string Format { get; set; } = "Name,Version,(RELEASE)";
}
