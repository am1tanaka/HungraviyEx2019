﻿/*
 * MIT License
 * Copyright (c) 2018 Yu Tanaka
 * https://github.com/am1tanaka/OpenSimpleFramework201801/blob/master/LICENSE
 */

using System.Collections;
using UnityEngine;

namespace HungraviyEx2019
{
    /// <summary>
    /// BGMやSEを制御するクラスです。
    /// </summary>
    public class SoundController : MonoBehaviour
    {
        /// <summary>
        /// 自分のインスタンス
        /// </summary>
        public static SoundController Instance
        {
            get;
            private set;
        }

        [TooltipAttribute("SE用のオーディオソース")]
        public AudioSource audioSE;
        [TooltipAttribute("BGM用のオーディオソース")]
        public AudioSource audioBGM;

        /// <summary>
        /// 効果音リスト。この並びと<c>SEList</c>にセットするAudioClipの並びを合わせます。
        /// </summary>
        [SerializeField]
        public enum SeType
        {
            Blackhole,
            Sucked,
            Press,
            ToItem,
            Eat,
            Pomp,
            Start,
            Click,
            Miss,
            FallBlock,
        };
        [TooltipAttribute("効果音リスト"), SerializeField]
        private AudioClip[] seList = null;

        /// <summary>
        /// BGMの列挙子。この並びと<c>BGMList</c>にセットするAudioClipの並びを合わせます。
        /// </summary>
        public enum BgmType
        {
            Stage1,
            Stage2,
            Stage3,
            Clear,
            GameOver,
            Ending
        }
        [Tooltip("BGMリスト"), SerializeField]
        private AudioClip[] bgmList = null;

        /// <summary>
        /// フェードに合わせたボリューム調整をする時、true
        /// </summary>
        static bool useFade = false;

        static float _seVolume = 1f;
        /// <summary>
        /// SEのボリューム
        /// </summary>
        public static float SeVolume
        {
            get
            {
                return _seVolume;
            }
            set
            {
                _seVolume = Mathf.Clamp01(value);
            }
        }
        static float _bgmVolume = 1f;
        /// <summary>
        /// BGMのボリューム
        /// </summary>
        public static float BgmVolume
        {
            get
            {
                return _bgmVolume;
            }
            set
            {
                _bgmVolume = Mathf.Clamp01(value);
            }
        }

        /// <summary>
        /// フェードアウトを表すフラグ。trueの時、フェードアウト中。
        /// </summary>
        static bool isFadingOut;

        /// <summary>
        /// Fadeのα値に合わせてフェードさせる時true。falseの時は、独自に指定の秒数でフェードアウト
        /// </summary>
        public static bool useFadeAlpha = true;

        /// <summary>
        /// Fadeのα値を利用しない時、独自に秒数の経過でフェードさせます。
        /// </summary>
        static float targetFadeSeconds;

        /// <summary>
        /// フェードを開始してからの経過秒数
        /// </summary>
        static float fadeSeconds;

        static AudioListener myAudioListener = null;

        #region System

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            isFadingOut = false;
            useFade = false;
            myAudioListener = GetComponentInChildren<AudioListener>();
        }

        private void FixedUpdate()
        {
            fadeSeconds += Time.fixedDeltaTime;
            if (fadeSeconds > targetFadeSeconds)
            {
                fadeSeconds = targetFadeSeconds;
            }
        }

        private void LateUpdate()
        {
            SetVolume();
        }

        #endregion System

        #region Service Methods

        /// <summary>
        /// 指定の効果音を鳴らします。
        /// </summary>
        /// <param name="snd">再生したい効果音</param>
        public static void Play(SeType snd)
        {
            Instance.audioSE.PlayOneShot(Instance.seList[(int)snd]);
        }

        /// <summary>
        /// 効果音を停止します。
        /// </summary>
        public static void Stop()
        {
            Instance.audioSE.Stop();
        }

        /// <summary>
        /// 指定のBGMを再生します。
        /// </summary>
        /// <param name="bgm">再生したいBGM</param>
        /// <param name="isFadeIn">フェードインさせたい時trueを設定。デフォルトはfalse。</param>
        public static void PlayBGM(BgmType bgm, bool isFadeIn = false)
        {
            useFade = isFadeIn;
            isFadingOut = false;
            useFadeAlpha = true;

            // 同じ曲が設定されていて再生中ならなにもしない
            if (Instance.audioBGM.clip == Instance.bgmList[(int)bgm])
            {
                if (Instance.audioBGM.isPlaying)
                {
                    SetVolume();
                    return;
                }
            }
            else
            {
                // 違う曲の場合
                // 曲が設定されていたら、曲を停止
                if (Instance.audioBGM.clip != null)
                {
                    Instance.audioBGM.Stop();
                }

                // 曲を設定
                Instance.audioBGM.clip = Instance.bgmList[(int)bgm];
            }

            // 再生開始
            SetVolume();
            Instance.audioBGM.Play();
        }

        /// <summary>
        /// BGMを停止します。
        /// フェードアウトさせる場合は、先にFadeをフェードアウトさせておくこと。
        /// </summary>
        /// <param name="isFadeOut">trueを設定すると、フェードアウトしたのち停止。falseだとすぐ停止。</param>
        public static void StopBGM(bool isFadeOut = false, float seconds = 0f)
        {
            // フェードの指定がないなら、すぐに停止
            if (!isFadeOut)
            {
                isFadingOut = false;
                useFade = false;
                Instance.audioBGM.Stop();
                return;
            }

            // フェードアウトを開始していないなら、フェードアウト開始
            if (!isFadingOut)
            {
                useFade = true;
                isFadingOut = true;
                useFadeAlpha = true;
                if (seconds > 0f)
                {
                    useFadeAlpha = false;
                    targetFadeSeconds = seconds;
                    fadeSeconds = 0f;
                }

                Instance.StartCoroutine(FadeOutBGM());
            }
        }

        /// <summary>
        /// BGMのフェードアウトが完了するのを待って、BGMを停止する。
        /// 途中でフラグがfalseになったら、次の再生が始まっているかも知れないので
        /// BGMの停止をせずにすぐにコルーチンを終了する。
        /// </summary>
        static IEnumerator FadeOutBGM()
        {
            while ((Fade.FadeState == Fade.FadeStateType.Out)
                || (!useFadeAlpha && (fadeSeconds < targetFadeSeconds)))
            {
                // フェードアウトが途中でキャンセルされたらコルーチンを強制終了
                if (!isFadingOut)
                {
                    yield break;
                }
                yield return null;
            }
        }

        /// <summary>
        /// オーディオリスナの有効・無効切り替え
        /// </summary>
        /// <param name="flag"></param>
        public static void SetAudioListener(bool flag)
        {
            myAudioListener.enabled = flag;
        }

        #endregion Service Methods

        #region Private Methods

        /// <summary>
        /// 現在のフェードのα値からボリュームを設定します。
        /// フェード未使用の場合はボリュームを最大にします。
        /// </summary>
        static void SetVolume()
        {
            float newvol = _bgmVolume;

            if (useFade)
            {
                if (useFadeAlpha)
                {
                    newvol = _bgmVolume * (1f - Fade.NowColor.a);
                }
                else
                {
                    newvol = _bgmVolume * (1f - (fadeSeconds / targetFadeSeconds));
                }
            }

            if (Instance.audioBGM.volume != newvol)
            {
                Instance.audioBGM.volume = newvol;
            }
            if (Instance.audioSE.volume != _seVolume)
            {
                Instance.audioSE.volume = _seVolume;
            }
        }

        #endregion Private Methods

    }
}

