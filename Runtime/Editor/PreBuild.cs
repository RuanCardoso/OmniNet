#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Neutron.Core;

public class PreBuildMessage : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }

    public void OnPreprocessBuild(BuildReport report)
    {
        if (PreBuildUtils.CheckNetworkManagerInScene())
        {
            // Exibe a mensagem antes da construção e verifica a resposta
            bool shouldCancelBuild = EditorUtility.DisplayDialog("Atom", "Please ensure that the active platform in 'Build Settings' matches the platform displayed by Atom. If they don't match, click on 'Request Script Compilation'.", "Continue", "Cancel");

            if (!shouldCancelBuild)
            {
                // Lança uma exceção para cancelar a construção
                throw new BuildFailedException("Operation cancelled by user");
            }
        }
        else
        {
            throw new BuildFailedException("'AtomNetwork object not found!'");
        }
    }
}

[InitializeOnLoad]
public class PreSwitchPlatformMessage : IActiveBuildTargetChanged
{
    public int callbackOrder { get { return 0; } }

    public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
    {
        EditorUtility.DisplayDialog("Atom", "Please ensure that the active platform in 'Build Settings' matches the platform displayed by Atom. If they don't match, click on 'Request Script Compilation'.", "Ok");
    }
}

public class PreBuildUtils
{
    public static bool CheckNetworkManagerInScene()
    {
        if (GameObject.FindObjectOfType<NeutronNetwork>() == null)
        {
            EditorUtility.DisplayDialog("Operation Error", "You need to have the 'AtomNetwork' object in the scene for this operation. Please switch to the scene that contains the 'AtomNetwork' object.", "Ok");
            return false;
        }

        return true;
    }
}
#endif