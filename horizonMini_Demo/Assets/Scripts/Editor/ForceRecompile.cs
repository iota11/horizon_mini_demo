using UnityEditor;
using UnityEngine;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Force Unity to recompile all scripts
    /// </summary>
    public class ForceRecompile
    {
        [MenuItem("HorizonMini/Force Recompile Scripts")]
        public static void RecompileScripts()
        {
            Debug.Log("Forcing Unity to recompile all scripts...");

            // Request script compilation
            AssetDatabase.Refresh();
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();

            Debug.Log("Script recompilation requested. Please wait...");
        }
    }
}
