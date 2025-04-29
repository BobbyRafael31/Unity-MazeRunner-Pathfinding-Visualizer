using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
public class MemoryProfilerSetup
{
    [MenuItem("Tools/Pathfinding/Enable Memory Profiler")]
    public static void EnableMemoryProfiler()
    {
        string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(
            EditorUserBuildSettings.selectedBuildTargetGroup);
        
        if (!definesString.Contains("ENABLE_MEMORY_PROFILER"))
        {
            if (definesString.Length > 0)
                definesString += ";";
                
            definesString += "ENABLE_MEMORY_PROFILER";
            
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup, definesString);
            
            Debug.Log("Memory Profiler enabled! (Added ENABLE_MEMORY_PROFILER symbol)");
            Debug.Log("Please restart the editor for this to take effect.");
        }
        else
        {
            Debug.Log("Memory Profiler is already enabled.");
        }
    }
    
    [MenuItem("Tools/Pathfinding/Disable Memory Profiler")]
    public static void DisableMemoryProfiler()
    {
        string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(
            EditorUserBuildSettings.selectedBuildTargetGroup);
        
        if (definesString.Contains("ENABLE_MEMORY_PROFILER"))
        {
            definesString = definesString.Replace("ENABLE_MEMORY_PROFILER", "");
            definesString = definesString.Replace(";;", ";"); // Fix double semicolons
            
            // Remove leading or trailing semicolons
            if (definesString.StartsWith(";"))
                definesString = definesString.Substring(1);
            if (definesString.EndsWith(";"))
                definesString = definesString.Substring(0, definesString.Length - 1);
            
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup, definesString);
            
            Debug.Log("Memory Profiler disabled! (Removed ENABLE_MEMORY_PROFILER symbol)");
            Debug.Log("Please restart the editor for this to take effect.");
        }
        else
        {
            Debug.Log("Memory Profiler is already disabled.");
        }
    }
}
#endif