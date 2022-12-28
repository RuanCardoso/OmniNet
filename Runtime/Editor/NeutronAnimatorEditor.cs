/*===========================================================
    Author: Ruan Cardoso
    -
    Country: Brazil(Brasil)
    -
    Contact: cardoso.ruan050322@gmail.com
    -
    Support: neutron050322@gmail.com
    -
    Unity Minor Version: 2021.3 LTS
    -
    License: Open Source (MIT)
    ===========================================================*/

#if UNITY_EDITOR
using NaughtyAttributes.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Neutron.Core.Inspector
{
    [CustomEditor(typeof(NeutronAnimator))]
    [CanEditMultipleObjects]
    public class NeutronAnimatorEditor : NaughtyInspector
    {
        private NeutronAnimator animatorTarget;

        protected override void OnEnable()
        {
            base.OnEnable();
            animatorTarget = (NeutronAnimator)target;
            if (animatorTarget.animator == null)
                animatorTarget.animator = animatorTarget.GetComponent<Animator>();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (animatorTarget.animator != null && !Application.isPlaying)
            {
                AnimatorController controller = (AnimatorController)animatorTarget.animator.runtimeAnimatorController;
                if (controller != null)
                {
                    AnimatorControllerParameter[] parameters = controller.parameters;
                    List<AnimatorParameter> targetOf = animatorTarget.parameters;
                    targetOf.RemoveAll(x => IsNull(parameters, x.ParameterName, x.ParameterType));
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        AnimatorControllerParameter animatorControllerParameter = parameters[i];
                        AnimatorParameter animatorParameter = new(animatorControllerParameter.name, animatorControllerParameter.type, AnimatorParameter.Sync.Enabled);
                        if (!targetOf.Contains(animatorParameter)) targetOf.Add(animatorParameter);
                        else
                        {
                            AnimatorParameter parameter = targetOf.FirstOrDefault(x => x.Equals(animatorParameter));
                            if (parameter != null)
                            {
                                parameter.ParameterName = animatorParameter.ParameterName;
                                parameter.ParameterType = animatorParameter.ParameterType;
                            }
                            else Logger.PrintError("Neutron Animator -> Invalid parameter!");
                        }
                    }
                }
            }
        }

        public bool IsNull(AnimatorControllerParameter[] parameters, string name, AnimatorControllerParameterType type) => parameters.FirstOrDefault(f => f.name == name && f.type == type) == null;
    }
}
#endif