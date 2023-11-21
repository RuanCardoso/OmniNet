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

using System;
using System.Collections.Generic;
using UnityEngine;
using static Omni.Core.Enums;

namespace Omni.Core
{
    [DisallowMultipleComponent]
    public class OmniAnimator : OmniObject
    {
        private const int SEPARATOR_HEIGHT = 1;
        private const int SEPARATOR = -(20 - SEPARATOR_HEIGHT);

        [InfoBox("Warning: Triggers are not supported.", EInfoBoxType.Warning)]
        [Required("Animator is required.")][Space(2)] public Animator animator;
        [HorizontalLine(below: true, height: SEPARATOR_HEIGHT)][Space(SEPARATOR)] public List<AnimatorParameter> parameters;
        [SerializeField][Range(0, 1f)] private float syncInterval = 0.1f;
        [SerializeField] private AuthorityMode authority = AuthorityMode.Mine;
        [SerializeField] private DataDeliveryMode deliveryMode = DataDeliveryMode.Unsecured;
        [SerializeField] private DataTarget target = DataTarget.BroadcastExcludingSelf;
        [SerializeField] private DataProcessingOption processingOption = DataProcessingOption.DoNotProcessOnServer;
        [SerializeField] private DataCachingOption cachingOption = DataCachingOption.None;

        protected override bool OnSerializeViewAuthority
        {
            get
            {
                return authority switch
                {
                    AuthorityMode.Mine => IsMine,
                    AuthorityMode.Server => IsServer,
                    AuthorityMode.Client => IsClient,
                    AuthorityMode.Custom => IsCustom,
                    _ => default,
                };
            }
        }

        protected override DataDeliveryMode OnSerializeViewChannel => deliveryMode;
        protected override DataTarget OnSerializeViewTarget => target;
        protected override DataProcessingOption OnSerializeViewSubTarget => processingOption;
        protected override DataCachingOption OnSerializeViewCacheMode => cachingOption;

        private void Start()
        {
            if (OnSerializeViewAuthority)
            {
                OnSerializeView(new WaitForSeconds(syncInterval));
            }
        }

        protected internal override void OnSerializeView(DataIOHandler IOHandler, bool isWriting, RemoteStats stats)
        {
            for (int i = 0; i < this.parameters.Count; i++)
            {
                AnimatorParameter parameter = this.parameters[i];
                if (parameter.SyncMode == AnimatorParameter.Sync.Disabled)
                {
                    continue;
                }
                else
                {
                    switch (parameter.ParameterType)
                    {
                        case AnimatorControllerParameterType.Float:
                            if (isWriting)
                            {
                                IOHandler.Write(animator.GetFloat(parameter.ParameterName));
                            }
                            else if (!isWriting)
                            {
                                animator.SetFloat(parameter.ParameterName, IOHandler.ReadFloat());
                            }
                            break;
                        case AnimatorControllerParameterType.Int:
                            if (isWriting)
                            {
                                IOHandler.Write(animator.GetInteger(parameter.ParameterName));
                            }
                            else if (!isWriting)
                            {
                                animator.SetInteger(parameter.ParameterName, IOHandler.ReadInt());
                            }
                            break;
                        case AnimatorControllerParameterType.Bool:
                            if (isWriting)
                            {
                                IOHandler.Write(animator.GetBool(parameter.ParameterName));
                            }
                            else if (!isWriting)
                            {
                                animator.SetBool(parameter.ParameterName, IOHandler.ReadBool());
                            }
                            break;
                        case AnimatorControllerParameterType.Trigger:
                            break;
                    }
                }
            }
        }
    }

    [Serializable]
    public class AnimatorParameter : IEquatable<AnimatorParameter>
    {
        public enum Sync : int
        {
            Enabled,
            Disabled
        }

        [SerializeField] private string parameterName;
        [SerializeField] private AnimatorControllerParameterType parameterType;
        [SerializeField] private Sync syncMode;

        public string ParameterName
        {
            get => parameterName;
            set => parameterName = value;
        }

        public AnimatorControllerParameterType ParameterType
        {
            get => parameterType;
            set => parameterType = value;
        }

        public Sync SyncMode
        {
            get => syncMode;
            set => syncMode = value;
        }

        public AnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType, Sync syncMode)
        {
            this.parameterName = parameterName;
            this.syncMode = syncMode;
            this.parameterType = parameterType;
        }

        public override int GetHashCode() => parameterName.GetHashCode() ^ parameterType.GetHashCode();
        public bool Equals(AnimatorParameter other) => parameterName == other.ParameterName && parameterType == other.parameterType;
        public override bool Equals(object obj)
        {
            AnimatorParameter other = obj as AnimatorParameter;
            return parameterName == other.ParameterName && parameterType == other.parameterType;
        }
    }
}