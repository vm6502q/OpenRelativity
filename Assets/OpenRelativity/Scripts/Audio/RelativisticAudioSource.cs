using OpenRelativity.Objects;
using System.Collections.Generic;
using UnityEngine;

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

        public class RelativisticAudioSourcePVHistoryPoint
        {
            public float WorldSoundTime;
            public Vector3 piw { get; set; }
            public Vector3 viw { get; set; }

            public RelativisticAudioSourcePVHistoryPoint(float w, Vector3 p, Vector3 v)
            {
                WorldSoundTime = w;
                piw = p;
                viw = v;
            }
        }

        public List<RelativisticAudioSourcePVHistoryPoint> pvHistory { get; protected set; }

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
                return relativisticObject.viw;
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

                // TODO: Use the generalized metric for RapidityToVelocity, with speed-of-sound delay.

                return (audioSystem.RapidityOfSound * dispUnit).RapidityToVelocity()
                    .AddVelocity(audioSystem.WorldSoundMediumRapidity.RapidityToVelocity());
            }
        }
        protected Vector3 soundPosition
        {
            get
            {
                return piw + tisw * Vector3.Project(viw.AddVelocity(soundVelocity), viw.normalized);
            }
        }

        protected float soundDelayTime
        {
            get
            {
                // TODO: Does this need handling for a generalized metric?
                return (relativisticObject.opticalPiw
                    - (relativisticObject.piw + tisw * Vector3.Project(viw.AddVelocity(soundVelocity), viw.normalized))).magnitude
                    / state.SpeedOfLight;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            audioSystem = RelativisticAudioSystem.Instance;
            relativisticObject = GetComponent<RelativisticObject>();
            audioSources = AudioSourceTransform.GetComponents<AudioSource>();
            playTimeHistory = new List<RelativisticAudioSourcePlayTimeHistoryPoint>();
            pvHistory = new List<RelativisticAudioSourcePVHistoryPoint>();

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

            AudioSourceTransform.position = soundPosition;

            float soundTime = state.TotalTimeWorld - soundDelayTime;

            pvHistory.Add(new RelativisticAudioSourcePVHistoryPoint(state.TotalTimeWorld + soundDelayTime, piw, viw));

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
            playTimeHistory.Add(new RelativisticAudioSourcePlayTimeHistoryPoint(relativisticObject.piw, audioSourceIndex));
        }
    }
}