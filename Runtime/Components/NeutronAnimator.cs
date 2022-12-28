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

using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Neutron.Core.Enums;

namespace Neutron.Core
{
    public class NeutronAnimator : NeutronObject
    {
        private const int SEPARATOR_HEIGHT = 1;
        private const int SEPARATOR = -(20 - SEPARATOR_HEIGHT);

        [InfoBox("Trigger type parameters are not supported!", EInfoBoxType.Warning)]
        [Required][Space(2)] public Animator animator;
        [HorizontalLine(below: true, height: SEPARATOR_HEIGHT)][Space(SEPARATOR)] public List<AnimatorParameter> parameters;
        [SerializeField][Range(0, 1f)] private float syncInterval = 0.1f;
        [SerializeField] private AuthorityMode authority = AuthorityMode.Mine;
        [SerializeField] private Channel channel = Channel.Unreliable;
        [SerializeField] private Target target = Target.Others;
        [SerializeField] private SubTarget subTarget = SubTarget.None;
        [SerializeField] private CacheMode cacheMode = CacheMode.None;

        protected override bool OnSerializeViewAuthority
        {
            get
            {
                return authority switch
                {
                    AuthorityMode.Mine => IsMine,
                    AuthorityMode.Server => IsServer,
                    AuthorityMode.Client => IsClient,
                    AuthorityMode.Free => IsFree,
                    _ => default,
                };
            }
        }

        protected override Channel OnSerializeViewChannel => channel;
        protected override Target OnSerializeViewTarget => target;
        protected override SubTarget OnSerializeViewSubTarget => subTarget;
        protected override CacheMode OnSerializeViewCacheMode => cacheMode;

        private void Start()
        {
            if (OnSerializeViewAuthority)
                OnSerializeView(new(syncInterval));
        }

        protected internal override void OnSerializeView(ByteStream parameters, bool isWriting, RemoteStats stats)
        {
            for (int i = 0; i < this.parameters.Count; i++)
            {
                AnimatorParameter parameter = this.parameters[i];
                switch (parameter.ParameterType)
                {
                    case AnimatorControllerParameterType.Float:
                        if (isWriting)
                            parameters.Write(animator.GetFloat(parameter.ParameterName));
                        else if (!isWriting)
                            animator.SetFloat(parameter.ParameterName, parameters.ReadFloat());
                        break;
                    case AnimatorControllerParameterType.Int:
                        if (isWriting)
                            parameters.Write(animator.GetInteger(parameter.ParameterName));
                        else if (!isWriting)
                            animator.SetInteger(parameter.ParameterName, parameters.ReadInt());
                        break;
                    case AnimatorControllerParameterType.Bool:
                        if (isWriting)
                            parameters.Write(animator.GetBool(parameter.ParameterName));
                        else if (!isWriting)
                            animator.SetBool(parameter.ParameterName, parameters.ReadBool());
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        break;
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