﻿using Assets.SimpleLocalization;
using DG.Tweening;
using Nekoyume.Game;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace Nekoyume.UI
{
    public class SpeechBubble : HudWidget
    {
        public enum ImageType : int
        {
            Normal,
            Emphasis,
        }

        public string localizationKey;
        public Transform bubbleContainer;
        public Image[] bubbleImages;
        public TextMeshProUGUI textSize;
        public TextMeshProUGUI text;
        public float speechSpeedInterval = 0.02f;
        public float speechWaitTime = 1.0f;
        public float bubbleTweenTime = 0.2f;

        public float speechBreakTime;
        public float destroyTime = 4.0f;

        public int SpeechCount { get; private set; }
        private Coroutine _coroutine;

        public void Init()
        {
            SpeechCount = LocalizationManager.LocalizedCount(localizationKey);
            gameObject.SetActive(false);
        }

        public void Clear()
        {
            StopAllCoroutines();
            DOTween.Kill(this);
            gameObject.SetActive(false);
        }

        public void UpdatePosition(GameObject target, Vector3 offset = new Vector3())
        {
            var targetPosition = target.transform.position + offset;
            RectTransform.anchoredPosition = targetPosition.ToCanvasPosition(ActionCamera.instance.Cam, MainCanvas.instance.Canvas);
        }

        public bool SetKey(string value)
        {
            localizationKey = value;
            SpeechCount = LocalizationManager.LocalizedCount(localizationKey);
            return SpeechCount > 0;
        }

        public void SetBubbleImage(int index)
        {
            for (int i = 0; i < bubbleImages.Length; ++i)
            {
                bubbleImages[i].gameObject.SetActive(index == i);
            }
        }

        public void Hide()
        {
            text.text = "";
            gameObject.SetActive(false);
        }

        private void BeforeSpeech()
        {
            if (!(_coroutine is null))
            {
                StopCoroutine(_coroutine);
            }

            gameObject.SetActive(true);
        }

        public IEnumerator CoShowText()
        {
            if (SpeechCount == 0)
                yield break;
            BeforeSpeech();
            var speech = LocalizationManager.Localize($"{localizationKey}{Random.Range(0, SpeechCount)}");
            _coroutine = StartCoroutine(ShowText(speech));
            yield return _coroutine;
        }

        public IEnumerator CoShowText(string speech)
        {
            BeforeSpeech();
            _coroutine = StartCoroutine(ShowText(speech));
            yield return _coroutine;
        }

        private IEnumerator ShowText(string speech)
        {
            text.text = "";
            var breakTime = speechBreakTime;
            if (!string.IsNullOrEmpty(speech))
            {
                if (speech.StartsWith("!"))
                {
                    breakTime /= 2;
                    speech = speech.Substring(1);
                    SetBubbleImage(1);
                }
                else
                    SetBubbleImage(0);

                textSize.text = speech;
                textSize.rectTransform.DOScale(0.0f, 0.0f);
                textSize.rectTransform.DOScale(1.0f, bubbleTweenTime).SetEase(Ease.OutBack);

                var tweenScale = DOTween.Sequence();
                tweenScale.Append(bubbleContainer.DOScale(1.1f, 1.4f));
                tweenScale.Append(bubbleContainer.DOScale(1.0f, 1.4f));
                tweenScale.SetLoops(3);
                tweenScale.Play();

                var tweenMoveBy = DOTween.Sequence();
                tweenMoveBy.Append(textSize.transform.DOBlendableLocalMoveBy(new Vector3(0.0f, 6.0f), 1.4f));
                tweenMoveBy.Append(textSize.transform.DOBlendableLocalMoveBy(new Vector3(0.0f, -6.0f), 1.4f));
                tweenMoveBy.SetLoops(3);
                tweenMoveBy.Play();

                yield return new WaitForSeconds(bubbleTweenTime);
                for (var i = 1; i <= speech.Length; ++i)
                {
                    text.text = i == speech.Length ? $"{speech.Substring(0, i)}" : $"{speech.Substring(0, i)}<alpha=#00>{speech.Substring(i)}";
                    yield return new WaitForSeconds(speechSpeedInterval);

                    // check destroy
                    if (!gameObject)
                    {
                        break;
                    }
                }

                yield return new WaitForSeconds(speechWaitTime);

                text.text = "";
                textSize.rectTransform.DOScale(0.0f, bubbleTweenTime).SetEase(Ease.InBack);
                yield return new WaitForSeconds(bubbleTweenTime);
            }

            yield return new WaitForSeconds(breakTime);

            bubbleContainer.DOKill();
            textSize.transform.DOKill();
            gameObject.SetActive(false);
        }
    }
}
