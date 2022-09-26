#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;

namespace Neutron.Editor
{
    public class ActiveBuildTargetListener : IActiveBuildTargetChanged
    {
        public int callbackOrder { get { return 0; } }
        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {

        }
    }
}
#endif