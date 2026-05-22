using System.Collections.Generic;
using UnityEngine;

namespace PongLegends
{
    // Drop audio clips in Assets/Resources/Audio/ — they load automatically by filename.
    //
    // Expected clip names:
    //   hit_paddle       – ball hits a paddle (punch/kick impact)
    //   hit_wall         – ball hits top/bottom wall
    //   point            – a point is scored
    //   game_over        – winner announced
    //   ability_paparazzi   – Johnny Pong (camera shutter)
    //   ability_uppercut    – Ryu Pong    (uppercut whoosh / "SHORYUKEN!")
    //   ability_lightning   – Tele-Pong   (electric crackle)
    //   ability_ironshield  – Tank Pong   (metal clank)
    //   ability_shadowclone – Shadow Pong (ninja woosh)
    //   ability_glitch      – Pixel Pong  (digital glitch)
    //   ability_fireball    – Inferno Pong (fire whoosh / "HADOUKEN!")
    //   ability_ice         – Frost Pong  (ice shard / freeze)
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        private AudioSource _source;
        private readonly Dictionary<string, AudioClip> _clips = new();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            foreach (var clip in Resources.LoadAll<AudioClip>("Audio"))
                _clips[clip.name] = clip;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // Plays a clip by name. Silently no-ops if the clip isn't found.
        public static void Play(string name)
        {
            if (Instance == null)
                new GameObject("SoundManager").AddComponent<SoundManager>();
            if (Instance._clips.TryGetValue(name, out var clip))
                Instance._source.PlayOneShot(clip);
        }
    }
}
