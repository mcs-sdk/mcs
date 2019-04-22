using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

namespace BuildTools
{
	class MainClass
	{

		//helper bool that tells us if we are running in a unix environment or windows
		protected static bool IsUnix;

		//this is where the mono runtime installs pdb2mdb into
		protected static string PDB2MDBPath = "c:\\Program Files (x86)\\Mono\\bin\\pdb2mdb.bat";

		public static void Main (string[] args)
		{	
			//see platform ids: https://msdn.microsoft.com/en-us/library/3a8hyw88(v=vs.110).aspx
			//FYI: OSX is 6
			int platformId = (int)System.Environment.OSVersion.Platform;
			IsUnix = ((platformId == 4) || (platformId == 6) || (platformId == 128));
			Console.WriteLine ("Is Unix: " + (IsUnix ? "Yes" : "No"));

			if(Array.IndexOf(args, "-release") > -1){
				Console.WriteLine("BUILDING RELEASE CANDIDATE");
				MainClass.ReleasePostProcessor ();
				//MainClass.AdjustMetaFiles (Directory.GetCurrentDirectory () + "/release_candidate/scripts/Plugins/MCS_Importer.dll.meta");
			}else{
				Console.WriteLine("BUILDING DEBUG FOR UNITY");
				MainClass.DebugPostProcesor();

                //does the .meta file exist, this is something that unity will create if the .dll is in the unity project folder, otherwise it won't be there

                string dllMetaFile = Directory.GetCurrentDirectory() + "/build/Unity/Assets/MCS/Code/Plugins/MCS_Importer.dll.meta";
                if (File.Exists(dllMetaFile))
                {
                    MainClass.AdjustMetaFiles(dllMetaFile);
                }
			}

		}
		
		public static void AdjustMetaFiles(string path){
			
			int counter = 0;
			List<string> list = new List<string>();
			using (StreamReader reader = new StreamReader(path))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					if (counter == 1) {
						line = "      enabled: 0";
						counter++;
					} else if (counter == 3) {
						line = "      enabled: 1";
						counter++;
					}
					if (line.Contains ("Any:") || line.Contains ("Editor:")) 
					{
						counter++;
					}
					list.Add(line); // Add to list.
				}
			}
			File.Delete (path);
			using (StreamWriter writer = new StreamWriter(path, true))
			{
				foreach(var line in list)
				{
					writer.WriteLine(line);
				}
			}
		}

		public static bool ConvertPDB2MDB(String src){
			Process p = new Process ();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.FileName = PDB2MDBPath;
			p.StartInfo.Arguments = src;
			p.Start ();
			string output = p.StandardOutput.ReadToEnd ();
			p.WaitForExit ();

			Console.WriteLine ("pdb2mdb ("+p.ExitCode+") => " + output);
			return p.ExitCode == 0; //true if it worked
		}

		public static void DebugPostProcesor(){

			/****************************************/
			/**************** COPY DLL **************/
			/****************************************/

			//add files to copy as needed



			string[] files_required = {"MCS_Core.dll", "MCS_Importer.dll", "MCS_Core.dll.mdb", "MCS_Importer.dll.mdb", "ICSharpCode.SharpZipLib.dll", "MCS_Utilities.dll", "MCS_Utilities.dll.mdb" };
            string[] files_optional = { };
            //current directory should be the solution directoy
            string source_directory = Directory.GetCurrentDirectory() + "/build/Debug/";

            if (!Directory.Exists(source_directory))
            {
                Console.WriteLine("Failed to find source directoy: " + source_directory);
                Environment.Exit(1);
            }

			string destination_directory = Directory.GetCurrentDirectory() + "/build/Unity/Assets/MCS/Code/Plugins/";

			if (!IsUnix) {


                //we are running windows, so we need to convert the .pdb files to .mdb now


                if (!File.Exists(PDB2MDBPath))
                {
                    Console.WriteLine("Could not locate pdb2mdb at path: " + PDB2MDBPath);
                    Environment.Exit(2);
                }

                if (!File.Exists(source_directory + "MCS_Core.dll"))
                {
                    Console.WriteLine("Could not locate dll to convert at path: " + source_directory + "MCS_Core.dll");
                    Environment.Exit(2);
                }

                if (!ConvertPDB2MDB (source_directory + "MCS_Core.dll")) {

					Console.WriteLine ("Failed to convert pdb to mdb, make sure you have the mono runtime installed and then check the PDB2MDBPath in BuildTools/Program.cs");
                    Environment.Exit(3);
                }

                if (!File.Exists(source_directory + "MCS_Utilities.dll"))
                {
                    Console.WriteLine("Could not locate dll to convert at path: " + source_directory + "MCS_Utilities.dll");
                    Environment.Exit(2);
                }

                if (!ConvertPDB2MDB(source_directory + "MCS_Utilities.dll"))
                {

                    Console.WriteLine("Failed to convert pdb to mdb, make sure you have the mono runtime installed and then check the PDB2MDBPath in BuildTools/Program.cs");
                    Environment.Exit(3);
                }

                if (!ConvertPDB2MDB (source_directory + "MCS_Importer.dll")) {
					Console.WriteLine ("Failed to convert pdb to mdb, make sure you have the mono runtime installed and then check the PDB2MDBPath in BuildTools/Program.cs");
                    Environment.Exit(3);
                }
            }


			if (!Directory.Exists(destination_directory))
				Directory.CreateDirectory(destination_directory);

			Console.WriteLine ("Attempting to Copy DLLs...");
			foreach (string file in files_required) {
				string source_file = source_directory + file;
				string destination_file = destination_directory + file;
				File.Copy (source_file, destination_file, true);
				//Console.WriteLine (source_file + ":" + destination_file);
			}
            foreach(string file in files_optional)
            {
                try
                {
                    string source_file = source_directory + file;
                    string destination_file = destination_directory + file;
                    File.Copy(source_file, destination_file, true);
                } catch
                {
                    Console.WriteLine("Unable to copy optional file: " + file);
                }
            }
			Console.WriteLine ("DLLs copied.");


			/****************************************/
			/************** COPY SCRIPTS ************/
			/****************************************/

			Console.WriteLine("Attempting to copy loose Scripts...");
			destination_directory = Directory.GetCurrentDirectory() + "/build/Unity/Assets/MCS/Code/Scripts/";
			if (!Directory.Exists(destination_directory))
				Directory.CreateDirectory(destination_directory);

			source_directory = Directory.GetCurrentDirectory() + "/Scripts/";
			string[] folders_to_duplicate = new string[2] {"Editor", "AttachmentPoints"};

			foreach(string folder in folders_to_duplicate){
				//individual folders
				if(!Directory.Exists(destination_directory + folder))
					Directory.CreateDirectory(destination_directory + folder);
				//individual files
				string source_path = source_directory + folder;
				string destination_path = destination_directory + folder;
				foreach (string newPath in Directory.GetFiles(source_path, "*.*", SearchOption.AllDirectories))
					File.Copy(newPath, newPath.Replace(source_path, destination_path), true);
			};
			Console.WriteLine("Scripts copied");

		}

		public static void ReleasePostProcessor(){
			
			/****************************************/
			/**************** COPY DLL **************/
			/****************************************/

			string release_directory = Directory.GetCurrentDirectory () + "/release_candidate/scripts/";

			//add files to copy as needed
			string[] dlls_to_copy = new string[3] {"MCS_Core.dll", "MCS_Importer.dll", "MCS_Utilities.dll" };
			//current directory should be the solution directoy
			string source_directory = Directory.GetCurrentDirectory() + "/build/Release/";
			string destination_directory = release_directory + "Plugins/";

			if (!Directory.Exists(destination_directory))
				Directory.CreateDirectory(destination_directory);

			Console.WriteLine ("Attempting to Copy DLLs...");
			foreach (string file in dlls_to_copy) {
				string source_file = source_directory + file;
				string destination_file = destination_directory + file;
				File.Copy (source_file, destination_file, true);
				//Console.WriteLine (source_file + ":" + destination_file);
			}
			Console.WriteLine ("DLLs copied.");


			/****************************************/
			/************** COPY SCRIPTS ************/
			/****************************************/

			Console.WriteLine("Attempting to copy loose Scripts...");
			destination_directory = release_directory;
			if (!Directory.Exists(destination_directory))
				Directory.CreateDirectory(destination_directory);

			source_directory = Directory.GetCurrentDirectory() + "/Scripts/";
			string[] folders_to_duplicate = new string[2] {"Editor", "AttachmentPoints"};

			foreach(string folder in folders_to_duplicate){
				//individual folders
				if(!Directory.Exists(destination_directory + folder))
					Directory.CreateDirectory(destination_directory + folder);
				//individual files
				string source_path = source_directory + folder;
				string destination_path = destination_directory + folder;
				foreach (string newPath in Directory.GetFiles(source_path, "*.*", SearchOption.AllDirectories))
					File.Copy(newPath, newPath.Replace(source_path, destination_path), true);
			};
			Console.WriteLine("Scripts copied");

		}

		public static void DeepCopy(DirectoryInfo source, DirectoryInfo target)
		{

			// Recursively call the DeepCopy Method for each Directory
			foreach (DirectoryInfo dir in source.GetDirectories()) {
				DeepCopy (dir, target.CreateSubdirectory (dir.Name));
			}

			// Go ahead and copy each file in "source" to the "target" directory
			foreach (FileInfo file in source.GetFiles())
				file.CopyTo(Path.Combine(target.FullName, file.Name), true);

		} 

	}
}
