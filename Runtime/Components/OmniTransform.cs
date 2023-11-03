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

using UnityEngine;
using static Omni.Core.Enums;

namespace Omni.Core
{
    [DisallowMultipleComponent]
    public class OmniTransform : OmniObject
    {
        [SerializeField] private float lerpSpeed = 1f;
        [SerializeField][Range(0, 1f)] private float syncInterval = 0.1f;
        [SerializeField] private AuthorityMode authority = AuthorityMode.Mine;
        [SerializeField] private Channel channel = Channel.Unreliable;
        [SerializeField] private Target target = Target.Others;
        [SerializeField] private SubTarget subTarget = SubTarget.None;
        [SerializeField] private CacheMode cacheMode = CacheMode.None;

        private Vector3 netPos;
        private Quaternion netRot;

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

        protected override Channel OnSerializeViewChannel => channel;
        protected override Target OnSerializeViewTarget => target;
        protected override SubTarget OnSerializeViewSubTarget => subTarget;
        protected override CacheMode OnSerializeViewCacheMode => cacheMode;

        private void Start()
        {
            if (OnSerializeViewAuthority)
                OnSerializeView(new(syncInterval));
        }

        private void Update()
        {
            if (OnSerializeViewAuthority)
            {
                netPos = transform.position;
                netRot = transform.rotation;
            }
            else if (!OnSerializeViewAuthority)
            {
                Vector3 pos = Vector3.Lerp(transform.position, netPos, Time.deltaTime * lerpSpeed);
                Quaternion rot = Quaternion.Lerp(transform.rotation, netRot, Time.deltaTime * lerpSpeed);
                transform.SetPositionAndRotation(pos, rot);
            }
        }

        protected internal override void OnSerializeView(ByteStream parameters, bool isWriting, RemoteStats stats)
        {
            if (isWriting)
            {
                parameters.Write(netPos);
                parameters.Write(netRot);
            }
            else if (!isWriting)
            {
                netPos = parameters.ReadVector3();
                netRot = parameters.ReadQuaternion();
            }
        }
    }
}