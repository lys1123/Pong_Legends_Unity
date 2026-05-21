using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PongLegends
{
    public class SpecialMeterUI : MonoBehaviour
    {
        [SerializeField] private Image fillImage;

        private Coroutine _flashRoutine;

        public void SetFill(float amount)
        {
            if (fillImage != null)
                fillImage.fillAmount = Mathf.Clamp01(amount);
        }

        public void SetCharged(bool charged)
        {
            if (charged)
            {
                SetFill(1f);
                if (_flashRoutine == null)
                    _flashRoutine = StartCoroutine(Flash());
            }
            else
            {
                if (_flashRoutine != null)
                {
                    StopCoroutine(_flashRoutine);
                    _flashRoutine = null;
                }
                SetFill(0f);
                if (fillImage != null)
                {
                    Color c = fillImage.color;
                    c.a = 1f;
                    fillImage.color = c;
                }
            }
        }

        public void SetAccentColor(Color color)
        {
            if (fillImage != null)
                fillImage.color = color;
        }

        private IEnumerator Flash()
        {
            while (true)
            {
                yield return Fade(1f, 0.2f, 0.3f);
                yield return Fade(0.2f, 1f, 0.3f);
            }
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (fillImage != null)
                {
                    Color c = fillImage.color;
                    c.a = Mathf.Lerp(from, to, elapsed / duration);
                    fillImage.color = c;
                }
                yield return null;
            }
        }
    }
}
