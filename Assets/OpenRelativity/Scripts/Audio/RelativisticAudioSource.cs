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

        protected class RelativisticAudioSourceHistoryPoint
        {
            public Vector4 sourceWorldSpaceTimePos { get; set; }
            public int audioSourceIndex { get; set; }

            public RelativisticAudioSourceHistoryPoint(Vector4 sourceWorldSTPos, int audioSource)
            {
                sourceWorldSpaceTimePos = sourceWorldSTPos;
                audioSourceIndex = audioSource;
            }
        }

        protected List<RelativisticAudioSourceHistoryPoint> historyPoints;

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
                Matrix4x4 m = metric;

                return (audioSystem.RapidityOfSound * dispUnit).RapidityToVelocity(m)
                    .AddVelocity(audioSystem.WorldSoundMediumRapidity.RapidityToVelocity(m));
            }
        }
        protected Vector3 soundPosition
        {
            get
            {
                return piw + tisw * Vector3.Project(viw.AddVelocity(soundVelocity), viw.normalized);
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            audioSystem = RelativisticAudioSystem.Instance;
            relativisticObject = GetComponent<RelativisticObject>();
            audioSources = AudioSourceTransform.GetComponents<AudioSource>();
            historyPoints = new List<RelativisticAudioSourceHistoryPoint>();

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

            audioSystem.WorldSoundDopplerShift(this);

            if (historyPoints.Count == 0)
            {
                return;
            }

            while (historyPoints[0].sourceWorldSpaceTimePos.w >= (state.TotalTimeWorld - audioSystem.WorldSoundVelocityDelay(this)))
            {
                audioSources[historyPoints[0].audioSourceIndex].Play();
                historyPoints.RemoveAt(0);
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
            historyPoints.Add(new RelativisticAudioSourceHistoryPoint(piw, audioSourceIndex));
        }
    }
}