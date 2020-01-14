using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HungraviyEx2019
{
    [RequireComponent(typeof(BGScroller))]
    public class RightMoveCamera : MonoBehaviour
    {
        /// <summary>
        /// プレイヤーからカメラの座標を求めるためのオフセット値
        /// </summary>
        static Vector3 offset;

        static BGScroller bgScroller = null;

        const int DefaultWidth = 1920;
        const int DefaultHeight = 1080;
        const float DefaultAspect = (float)DefaultWidth / (float)DefaultHeight;

        void Start()
        {
            Camera cam = GetComponent<Camera>();
            if (cam.aspect < DefaultAspect)
            {
                // デフォルトより縦長の時、横幅を画面に入れるようにスクリーンサイズを調整する
                float h = cam.orthographicSize;
                float w = h * DefaultAspect;
                float newOrtho = w / cam.aspect;
                cam.orthographicSize = newOrtho;
            }

            offset = transform.position - Graviy.instance.transform.position;
            bgScroller = GetComponent<BGScroller>();
        }

        void LateUpdate()
        {
            Vector3 next = Graviy.instance.transform.position + offset;
            if (next.x < transform.position.x)
            {
                next.x = transform.position.x;
            }
            transform.position = next;

            Graviy.instance.AdjustLeftPosition();
            bgScroller.UpdateBGPosition();
        }
    }
}