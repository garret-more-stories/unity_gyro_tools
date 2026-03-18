#if UNITY_EDITOR
using UnityEditor;
using System.IO;

// Revise this code if building doesn't work
namespace MoreStories.GyroTools.Editor
{

    [InitializeOnLoad]
    public class AddLibraryToProject
    {
        const string PluginsLocations = "Packages/unity_gyro_tools/Runtime/Plugins";
        const string ProjectPlugins   = "Assets/Plugins";
        static AddLibraryToProject() =>

            CopyAllFromTo(Directory.Exists(PluginsLocations) ? 
                            PluginsLocations : 
                            UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                                typeof(AddLibraryToProject).Assembly).resolvedPath.
                                Replace(Directory.GetCurrentDirectory() + "/","") + "/Runtime/Plugins", 
                          ProjectPlugins);


        static void CopyAllFromTo(string sourceDirectory, string destinationDirectory)
        {
            foreach (string directoryPath in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                string destDirectory = directoryPath.Replace(sourceDirectory, destinationDirectory);
                if(!Directory.Exists(destDirectory))
                {
                    Directory.CreateDirectory(destDirectory);
                }
                
            }

            foreach (string filePath in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                string destFilePath = filePath.Replace(sourceDirectory, destinationDirectory);
                if(!File.Exists(destFilePath))
                {
                    File.Copy(filePath, destFilePath, true); 
                }
                
            }
            AssetDatabase.Refresh();
        }
    }
}
#endif