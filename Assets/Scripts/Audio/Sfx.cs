// Novi zvučni efekti (izmicanje, eksplozija, lom, nivo...) sa glasnoćom iz podešavanja.
using UnityEngine;

namespace AWizardsFight
{
    public enum SfxId
    {
        Dash,
        Explosion,
        Break,
        LevelUp,
        Pickup,
        Block,
        Slam,
        Summon,
        Footstep,
        Burn
    }

    [DisallowMultipleComponent]
    public sealed class Sfx : MonoBehaviour
    {
        [SerializeField]
        private AudioClip[] clips = new AudioClip[10];
        private readonly float[] lastPlayed = new float[10];
        private AudioSource source;
        private static Sfx instance;

        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
        }

        private void OnEnable() => instance = this;

        private void OnDisable()
        {
            if (instance == this)
                instance = null;
        }

        public static void Play(SfxId id, float volume = 1f)
        {
            if (instance == null || SaveSystem.IsRestoring)
                return;
            int index = (int)id;
            if (index >= instance.clips.Length || instance.clips[index] == null)
                return;
            // Isti zvuk se ne pušta više puta u istom trenutku.
            if (Time.unscaledTime - instance.lastPlayed[index] < .04f)
                return;
            instance.lastPlayed[index] = Time.unscaledTime;
            instance.source.PlayOneShot(instance.clips[index], volume * .5f * PlayerPrefs.GetFloat("EffectsVolume", .8f));
        }
    }
}
