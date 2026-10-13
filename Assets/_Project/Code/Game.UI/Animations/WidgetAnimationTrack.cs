using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Game.UI.Animations
{
    /// <summary>
    /// Timeline track for WidgetAnimation
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(WidgetAnimationShot))]
    [TrackBindingType(typeof(BaseWidgetAnimation))]
    public class WidgetAnimationTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<WidgetAnimationMixerBehaviour>.Create(graph, inputCount);
        }
    }
    
    public class WidgetAnimationShot : PlayableAsset
    {
        public WidgetAnimationType WidgetAnimationType;
        public bool Instant;
        
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<WidgetAnimationBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.WidgetAnimationType = WidgetAnimationType;
            behaviour.Instant = Instant;
            return playable;
        }
    }
    
    public class WidgetAnimationBehaviour : PlayableBehaviour
    {
        public WidgetAnimationType WidgetAnimationType;
        public bool Instant;
    }
    
    public class WidgetAnimationMixerBehaviour : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var trackBinding = playerData as BaseWidgetAnimation;
            if (trackBinding == null)
            {
                return;
            }

            var inputCount = playable.GetInputCount();
            for (var i = 0; i < inputCount; i++)
            {
                var inputPlayable = (ScriptPlayable<WidgetAnimationBehaviour>) playable.GetInput(i);
                var inputBehaviour = inputPlayable.GetBehaviour();
                // trackBinding.Play(inputBehaviour.WidgetAnimationType, inputBehaviour.Instant);
            }
        }
    }
}