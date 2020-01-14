using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HungraviyEx2019
{
    [RequireComponent(typeof(AudioSource))]
    public class Blackhole : MonoBehaviour
    {
        public static Blackhole instance = null;

        [Tooltip("最高速度"), SerializeField]
        float speedMax = 10f;
        [Tooltip("音のフェードアウト秒数"), SerializeField]
        float seFadeOutSeconds = 0.5f;

        /// <summary>
        /// エネルギーがあって、マウスをクリックしている時、true
        /// </summary>
        public static bool IsSpawn { get; private set; }

        Rigidbody2D rb;
        Animator anim;
        Camera myCamera = null;
        /// <summary>
        /// 前回の発生状況
        /// </summary>
        bool lastSpawned;
        /// <summary>
        /// SpawnかSpawnedの時true、DestroyかHidedの時falseにするようにアニメから設定するフラグ
        /// </summary>
        bool animSpawn;

        /// <summary>
        /// クリア中の発生時、true。ぐらびぃの口からclearOffsetを加えた座標に発生させる。
        /// </summary>
        bool isClear;
        /// <summary>
        /// クリア時のぐらびぃからのオフセット座標
        /// </summary>
        Vector3 clearOffset;

        AudioSource audioSource;
        float seFadeOutTime;
        bool isSeFadeOut = false;

        private void Awake()
        {
            instance = this;
            rb = GetComponent<Rigidbody2D>();
            anim = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();
            animSpawn = false;
            isSeFadeOut = false;
        }

        /// <summary>
        /// クリア処理開始
        /// </summary>
        /// <param name="ofs"></param>
        public void ClearStart(Vector3 ofs)
        {
            clearOffset = ofs;
            isClear = true;
            IsSpawn = false;
            anim.SetBool("Spawn", true);
            PlaySe();
        }

        public void ClearDone()
        {
            isClear = false;
            anim.SetBool("Spawn", false);
            StopSe();
        }

        private void FixedUpdate()
        {
            if (isSeFadeOut)
            {
                seFadeOutTime += Time.fixedDeltaTime;
                if (seFadeOutTime >= seFadeOutSeconds)
                {
                    isSeFadeOut = false;
                    audioSource.Stop();
                }
                else
                {
                    audioSource.volume = SoundController.SeVolume * (1f-(seFadeOutSeconds / seFadeOutSeconds));
                }
            }

            if (!isClear && !Graviy.CanMove)
            {
                anim.SetBool("Spawn", false);
                StopSe();
                return;
            }

            Vector3 target = Graviy.MouthPosition + clearOffset;
            if (!isClear)
            {
                target = GetTargetWithControl();
            }
            Vector3 move = (target - transform.position) / Time.fixedDeltaTime;
            move = Vector3.ClampMagnitude(move, speedMax);
            rb.velocity = move;
        }

        Vector3 GetTargetWithControl()
        {
            bool mouseClicked = Input.GetMouseButton(0);
            IsSpawn = (mouseClicked && (Graviy.Energy > 0));
            anim.SetBool("Spawn", IsSpawn);

            // 動かす
            if (myCamera == null)
            {
                myCamera = Camera.main;
            }
            Vector3 mpos = Input.mousePosition;
            mpos.z = 1;
            Vector3 target = myCamera.ScreenToWorldPoint(mpos);

            if (!lastSpawned)
            {
                PlaySe();
                transform.position = target;
                rb.velocity = Vector3.zero;
                lastSpawned = true;
                return transform.position;
            }

            // 発生アニメでなければ移動なし
            if (!animSpawn)
            {
                lastSpawned = false;
                StopSe();
                return transform.position;
            }

            return target;
        }

        /// <summary>
        /// アニメがSpawn状態に設定する。
        /// </summary>
        public void AnimSpawn()
        {
            animSpawn = true;
        }

        /// <summary>
        /// アニメが非表示の状態に設定する。
        /// </summary>
        public void AnimHide()
        {
            animSpawn = false;
        }

        /// <summary>
        /// ブラックホールの効果音が始まっていなければ開始します。
        /// </summary>
        void PlaySe()
        {
            if (!lastSpawned || isSeFadeOut)
            {
                isSeFadeOut = false;
                audioSource.volume = SoundController.SeVolume;
                audioSource.Play();
            }
        }

        /// <summary>
        /// ブラックホールの効果音をフェードアウトさせる
        /// </summary>
        void StopSe()
        {
            if (!isSeFadeOut && audioSource.isPlaying)
            {
                isSeFadeOut = true;
                seFadeOutTime = 0f;
            }
        }


    }
}