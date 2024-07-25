// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Linq;


internal class Program
{
    public static void Main(string[] args)
    {
        try
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            string MountPath = "";

            if (args.Length == 1 && args[0].Contains(".txt"))
            {
                //custom ordering
                args = File.ReadAllLines(args[0]).ToList().Select(s => s.Replace("\"", string.Empty)).ToArray();
            }
            else
            {
                //no point merging a single file
                if (args.Length < 2) { return; }
                //alpahbetical ordered merging
                Array.Sort(args, (x, y) => String.Compare(x.Split("\\").Last(), y.Split("\\").Last()));
            }
            //get exe location
            Console.Write($"");

            //start with unpaking all the files
            var proc = new Process() { StartInfo = new ProcessStartInfo() { RedirectStandardOutput = true, UseShellExecute = false, FileName = path + @"Resources\UnrealPak\UnrealPak.exe" } };
            int r = 0;
            List<string> reglist = new();
            Dictionary<string, string> PakReg = new();
            string game = "";
            foreach (string arg in args)
            {

                //Console.Write("Checking: " + arg.Split("\\").Last() + "    ");
                proc.StartInfo.Arguments = arg + " -List ";
                proc.Start();
                var AR = ContainsAssetRegistry(proc);
                while (!proc.WaitForExit(100)) ;
                proc.Close();

                Log($"Extracting: [{arg.Split("\\").Last()}]{(AR ? " Asset Registry Detected!" : "")}\n");
                proc.StartInfo.Arguments = arg + " -Extract " + path + @"Extracted\ -extracttomountpoint";
                proc.Start();
                ReadProcess(proc);
                while (!proc.WaitForExit(100)) ;
                proc.Close();

                game = Directory.GetDirectories(path + @"Extracted\")[0].Split("\\").Last();
                //if AssetRegisty exists keep track and store them on the side until done unpaking
                if (File.Exists(path + $@"Extracted\{game}\AssetRegistry.bin"))
                {
                    //Console.WriteLine("");
                    string reg = "";
                    if (r > 0)
                    {
                        PakReg.Add("AssetRegistry" + r + ".bin", arg.Split("\\").Last());
                        reg = path + $@"Extracted\{game}\Registries\AssetRegistry" + r + ".bin";
                        if (!Directory.Exists(path + $@"Extracted\{game}\Registries"))
                        {
                            // Log("Duplicate Registries Found, Creating Folder\n");
                            Directory.CreateDirectory(path + $@"Extracted\{game}\Registries");
                        }
                        //Log("Moving Registry\n");
                        File.Move(path + $@"Extracted\{game}\AssetRegistry.bin", reg, true);
                    }
                    else
                    {
                        PakReg.Add("AssetRegistry.bin", arg.Split("\\").Last());
                        reg = path + $@"Extracted\{game}\AssetRegistryBase.bin";
                        File.Move(path + $@"Extracted\{game}\AssetRegistry.bin", reg, true);
                    }
                    r++;
                    reglist.Add(reg);
                    //Console.WriteLine("Success\n");
                }
                else
                {
                    //Console.WriteLine("\nSuccess\n");
                }

            }
            Console.WriteLine(""); //Spacer
            if (reglist.Count >= 1)
            {
                string final = path + $@"Extracted\{game}\AssetRegistry.bin";
                File.Move(reglist.First(), final, true);
                if (reglist.Count > 1)
                {
                    proc.StartInfo.FileName = path + @"Resources\ARH\AssetRegistryHelper.exe";
                    foreach (string reg in reglist.Skip(1))
                    {
                        Log("Merging Asset Registry from [" + PakReg[reg.Split("\\").Last()] + "] into Asset Registry from [" + PakReg[final.Split("\\").Last()] + "]\n");
                        proc.StartInfo.Arguments = final + " -Merge " + reg + " " + final + " -Overwrite";
                        proc.Start();
                        ReadProcess(proc);
                        while (!proc.WaitForExit(100)) ;
                        proc.Close();
                        //Console.WriteLine("\n");
                    }
                }
            }
            proc.StartInfo.FileName = path + @"Resources\UnrealPak\UnrealPak.exe";
            if (Directory.Exists(path + $@"Extracted\{game}\Registries"))
                Directory.Delete(path + $@"Extracted\{game}\Registries", true);
            var dir = Directory.GetDirectories(path + @"Extracted");
            if (dir.Length > 0)
            {
                string mount = dir[0].Split("\\").Last();
                File.WriteAllText(path + @"filelist.txt", "\"../../Extracted/" + mount + "/*\" \"../../../" + mount + "/\"");
            }
            Console.WriteLine(""); //Spacer
            Log("Output Pak:\n" + Path.GetDirectoryName(args[0]) + @"\Merged.pak" + "\n\n");
            proc.StartInfo.Arguments = Path.GetDirectoryName(args[0]) + @"\Merged.pak -create=" + path + @"filelist.txt" + " -compress";
            proc.Start();
            //ReadProcess(proc);
            while (!proc.WaitForExit(100)) ;
            if (File.Exists(path + @"filelist.txt"))
                File.Delete(path + @"filelist.txt");
            proc.Close();
            if (Directory.Exists(path + @"Extracted"))
                Directory.Delete(path + @"Extracted", true);

            Log($"Merged {args.Length} pak files and {r} Asset Registries\n");
            Log($"Press Enter to Terminate...");
            Console.ReadLine();
        }
        catch (Exception e) { Console.WriteLine(e.Message); Console.Read(); }
    }

    private static bool CanLogLine(string line)
    {
        string[] blacklist = { "Warning", "Extracted", "seconds", "crypto", "Overwriting", "filelist.txt", "Merged", "Added" };
        return !blacklist.Any(x => line.Contains(x));
    }
    private static void ReadProcess(Process p)
    {
        string line = "";
        while ((line = p.StandardOutput.ReadLine()) != null)
        {
            if (CanLogLine(line))
                Console.WriteLine(line);
        }
        //Console.Read();
    }
    private static bool ContainsAssetRegistry(Process p)
    {
        bool Found = false;
        string line = "";
        while ((line = p.StandardOutput.ReadLine()) != null)
        {
            if (line.Contains("AssetRegistry.bin"))
                Found = true;
        }
        return Found;
    }
    private static void Log(string s)
    {
        Console.Write("PakMerger: " + s);
    }
}