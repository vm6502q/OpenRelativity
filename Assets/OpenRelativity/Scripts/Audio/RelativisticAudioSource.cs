using System.Collections.Generic;
using UnityEngine;
using OpenRelativity.Objects;

namespace OpenRelativity.Audio
{
    public class RelativisticAudioSource : RelativisticBehavior
    {
        public Transform AudioSourceTransform;
        public AudioSource[] audioSources;
        public float[] pitches;

        public RelativisticObject relativisticObject { get; protected set; }

        protected RelativisticAudioSystem audioSystem;

        protected class RelativisticAudioSourcePlayTimeHistoryPoint
        {
            public Vector4 sourceWorldSpaceTimePos { get; set; }
            public int audioSourceIndex { get; set; }

            public RelativisticAudioSourcePlayTimeHistoryPoint(Vector4 sourceWorldSTPos, int audioSource)
            {
                sourceWorldSpaceTimePos = sourceWorldSTPos;
                audioSourceIndex = audioSource;
            }
        }

        protected List<RelativisticAudioSourcePlayTimeHistoryPoint> playTimeHistory;

        protected class RelativisticAudioSourceVelocityHistoryPoint
        {
            public float WorldSoundTime { get; set; }
            public Vector3 viw { get; set; }

            public RelativisticAudioSourceVelocityHistoryPoint(float t, Vector3 v)
            {
                WorldSoundTime = t;
                viw = v;
            }
        }

        protected List<RelativisticAudioSourceVelocityHistoryPoint> velocityHistory;

        protected Vector3 oldViw;

        public Vector3 piw
        {
            get
            {
                return relativisticObject.piw;
            }
        }

        public Vector3 viw
        {
            get
            {
                return ((velocityHistory != null) && (velocityHistory.Count > 0)) ? velocityHistory[0].viw : relativisticObject.viw;
            }
        }

        public Matrix4x4 metric;

        protected float tisw
        {
            get
            {
                return relativisticObject.GetTisw();
            }
        }

        protected Vector3 listenerPiw
        {
            get
            {
                return RelativisticAudioSystem.PlayerAudioListener.piw;
            }
        }

        protected Vector3 listenerViw
        {
            get
            {
                return RelativisticAudioSystem.PlayerAudioListener.viw;
            }
        }

        public Vector3 soundVelocity
        {
            get
            {
                Vector3 dispUnit = (listenerPiw - piw).normalized;
                Matrix4x4 m = metric;

                return (audioSystem.RapidityOfSound * dispUnit).RapidityToVelocity(m)
                    .AddVelocity(audioSystem.WorldSoundMediumRapidity.RapidityToVelocity(m));
            }
        }
        protected Vector3 soundPosition
        {
            get
            {
                return piw + (tisw - soundLightDelayTime) * viw;
            }
        }

        protected float soundLightDelayTime
        {
            get
            {
                // TODO: Does this need handling for a generalized metric?
                return (relativisticObject.opticalPiw -
                    (piw + tisw * Vector3.Project(relativisticObject.viw.AddVelocity(soundVelocity), relativisticObject.viw.normalized))
                    ).magnitude / state.SpeedOfLight;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            audioSystem = RelativisticAudioSystem.Instance;
            relativisticObject = GetComponent<RelativisticObject>();
            audioSources = AudioSourceTransform.GetComponents<AudioSource>();
            playTimeHistory = new List<RelativisticAudioSourcePlayTimeHistoryPoint>();

            oldViw = viw;
            velocityHistory = new List<RelativisticAudioSourceVelocityHistoryPoint>();

            pitches = new float[audioSources.Length];
            for (int i = 0; i < audioSources.Length; i++)
            {
                pitches[i] = audioSources[i].pitch;

                // Turn off built-in Doppler
                audioSources[i].dopplerLevel = 0;
            }
        }

        private void Update()
        {
            metric = relativisticObject.GetMetric();

            if (relativisticObject.viw != oldViw)
            {
                velocityHistory.Add(new RelativisticAudioSourceVelocityHistoryPoint(state.TotalTimeWorld - soundLightDelayTime, viw));
                oldViw = relativisticObject.viw;
            }

            if (velocityHistory.Count == 0)
            {
                velocityHistory.Add(new RelativisticAudioSourceVelocityHistoryPoint(state.TotalTimeWorld - soundLightDelayTime, viw));
            }

            while (velocityHistory.Count > 1)
            {
                if (velocityHistory[1].WorldSoundTime >= state.TotalTimeWorld)
                {
                    velocityHistory.RemoveAt(0);
                }
                else
                {
                    break;
                }
            }

            AudioSourceTransform.position = soundPosition;

            audioSystem.WorldSoundDopplerShift(this);

            if (playTimeHistory.Count == 0)
            {
                return;
            }

            while (playTimeHistory[0].sourceWorldSpaceTimePos.w >= (state.TotalTimeWorld - audioSystem.WorldSoundVelocityDelay(this)))
            {
                audioSources[playTimeHistory[0].audioSourceIndex].Play();
                playTimeHistory.RemoveAt(0);
            }
        }

        public void ShiftPitches(float frequencyFactor)
        {
            for (int i = 0; i < pitches.Length; i++)
            {
                audioSources[i].pitch = frequencyFactor * pitches[i];
            }
        }

        public void PlayOnWorldClock(int audioSourceIndex = 0)
        {
            playTimeHistory.Add(new RelativisticAudioSourcePlayTimeHistoryPoint(piw, audioSourceIndex));
        }
    }
}