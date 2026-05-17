using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace TangmenFramework
{
    /// <summary>
    /// 长按按钮组件：挂载到按钮GameObject上，支持长按检测。
    /// 按下超过阈值时间后触发长按事件，松手时触发抬起事件。
    /// 同时提供按下和松开事件，适用于移动端持续输入（如方向键长按移动）。
    /// </summary>
    public class LongPressButton : UIBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Header("长按触发时间（秒）")]
        [SerializeField] private float longPressThreshold = 0.5f;

        /// <summary>是否已经触发了长按（防止长按触发后松手又触发短按）</summary>
        private bool _longPressTriggered;

        /// <summary>按下时刻</summary>
        private float _pointerDownTime;

        /// <summary>是否正在按下中</summary>
        private bool _isPointerDown;

        /// <summary>对外暴露当前是否处于按下状态</summary>
        public bool IsPointerDown => _isPointerDown;

        /// <summary>按下事件（手指按下的瞬间触发）</summary>
        public UnityEvent onPointerDown = new UnityEvent();

        /// <summary>松开事件（手指抬起的瞬间触发）</summary>
        public UnityEvent onPointerUp = new UnityEvent();

        /// <summary>长按触发事件</summary>
        public UnityEvent onLongPress = new UnityEvent();

        /// <summary>短按触发事件（按下后未达到长按阈值就松手时触发）</summary>
        public UnityEvent onShortClick = new UnityEvent();

        protected override void OnDisable()
        {
            if (_isPointerDown)
            {
                _isPointerDown = false;
                onPointerUp?.Invoke();
            }
            _longPressTriggered = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPointerDown = true;
            _longPressTriggered = false;
            _pointerDownTime = Time.unscaledTime;
            onPointerDown?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isPointerDown)
                return;

            _isPointerDown = false;
            onPointerUp?.Invoke();

            // 如果没有触发过长按，说明是短按
            if (!_longPressTriggered)
            {
                onShortClick?.Invoke();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isPointerDown)
                return;

            _isPointerDown = false;
            _longPressTriggered = false;
            onPointerUp?.Invoke();
        }

        private void Update()
        {
            if (_isPointerDown && !_longPressTriggered)
            {
                if (Time.unscaledTime - _pointerDownTime >= longPressThreshold)
                {
                    _longPressTriggered = true;
                    onLongPress?.Invoke();
                }
            }
        }
    }
}
