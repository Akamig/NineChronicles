﻿using System;
using UnityEngine;
using System.Collections;
using Spine.Unity;

namespace Nekoyume.Game.Character
{
    public class MecanimStateToAnimationHandle : StateMachineBehaviour
    {
        private static readonly int TransitionHash = Animator.StringToHash("Transition");

        public int layer;
        public string animationClip;
        public float timeScale = 1f;
        public float exitTime = 1f;
        public bool loop;

        private SkeletonAnimationController _controller;
        private SkeletonAnimation _skeletonAnimation;
        private Spine.AnimationState _spineAnimationState;
        private Spine.TrackEntry _trackEntry;
        private float _normalizedTime;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!_controller)
            {
                _controller = animator.GetComponent<SkeletonAnimationController>();
                if (!_controller)
                {
                    throw new NotFoundComponentException<SkeletonAnimationController>();
                }
            }

            if (!_skeletonAnimation)
            {
                _skeletonAnimation = animator.GetComponentInChildren<SkeletonAnimation>();
                if (!_skeletonAnimation)
                {
                    throw new NotFoundComponentException<SkeletonAnimation>();
                }

                _spineAnimationState = _skeletonAnimation.state;
            }

            _trackEntry = _controller.PlayAnimationForState(animationClip, layer);
            if (_trackEntry is null)
            {
                return;
            }

            _trackEntry.TimeScale = timeScale;
            _normalizedTime = 0f;
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (loop)
            {
                return;
            }

            if (_trackEntry is null)
            {
                return;
            }

            _normalizedTime = _trackEntry.AnimationTime / _trackEntry.AnimationEnd;
            if (_normalizedTime >= exitTime)
            {
                animator.SetTrigger(TransitionHash);
            }
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!stateInfo.IsName(nameof(CharacterAnimation.Type.Touch)))
                return;
            
            animator.SetBool(nameof(CharacterAnimation.Type.Touch), false);
        }
    }
}
