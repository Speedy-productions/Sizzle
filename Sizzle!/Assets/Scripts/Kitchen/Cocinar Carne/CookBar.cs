using UnityEngine;
using UnityEngine.UI;

public class CookingProgressUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] Image progressFillImage;   // círculo de progreso
    [SerializeField] Image readyIcon;           // icono "lista"
    [SerializeField] Image burnedIcon;          // icono "quemada"
    [SerializeField] Sprite fallbackCircleSprite;

    [Header("Colores")]
    [SerializeField] Color startYellow = new(1.00f, 0.93f, 0.10f);
    [SerializeField] Color readyGreen = new(0.10f, 0.85f, 0.20f);
    [SerializeField] Color burnYellow = new(1.00f, 0.85f, 0.10f);
    [SerializeField] Color burnedRed = new(0.90f, 0.10f, 0.10f);

    [Header("Tiempos visuales")]
    [Range(0.05f, 0.95f)]
    [SerializeField] float postYellowPortion = 0.55f; // tramo verde?amarillo antes del rojo

    [Header("Animación")]
    [SerializeField] float lerpSpeed = 6f;

    [Header("Radial 360")]
    [SerializeField] bool clockwise = true;
    [SerializeField] Image.Origin360 origin360 = Image.Origin360.Top;

    float targetFill = 0f;
    float cookFrac = 0.5f;
    Color targetColor;
    bool readyShown = false;
    bool burnedShown = false;

    void Awake()
    {
        if (!progressFillImage)
        {
            Debug.LogError("[CookingProgressUI] Asigna la Image del progreso.");
            enabled = false; return;
        }

        if (!progressFillImage.sprite && fallbackCircleSprite)
            progressFillImage.sprite = fallbackCircleSprite;

        // Configuración del radial
        progressFillImage.material = null;
        progressFillImage.type = Image.Type.Filled;
        progressFillImage.fillMethod = Image.FillMethod.Radial360;
        progressFillImage.fillOrigin = (int)origin360;
        progressFillImage.fillClockwise = clockwise;
        progressFillImage.preserveAspect = true;
        progressFillImage.raycastTarget = false;

        progressFillImage.fillAmount = 0f;
        progressFillImage.color = startYellow;
        targetColor = startYellow;

        if (readyIcon) readyIcon.enabled = false;
        if (burnedIcon) burnedIcon.enabled = false;
    }

    void Update()
    {
        progressFillImage.fillAmount = Mathf.Lerp(progressFillImage.fillAmount, targetFill, lerpSpeed * Time.deltaTime);
        progressFillImage.color = Color.Lerp(progressFillImage.color, targetColor, lerpSpeed * Time.deltaTime);
    }

    // t = 0..1 (0 cruda, cookFrac lista, 1 quemada)
    public void SetProgress(float t, float cookFraction)
    {
        t = Mathf.Clamp01(t);
        targetFill = t;
        cookFrac = Mathf.Clamp01(cookFraction);

        if (t <= cookFrac)
        {
            // amarillo ? verde
            float k = Mathf.InverseLerp(0f, cookFrac, t);
            targetColor = Color.Lerp(startYellow, readyGreen, k);

            // al llegar a verde, muestra check y oculta "quemada"
            if (!readyShown && k >= 0.98f)
            {
                if (readyIcon) readyIcon.enabled = true;
                if (burnedIcon) burnedIcon.enabled = false;
                readyShown = true;
                burnedShown = false;
            }
        }
        else
        {
            // verde ? amarillo ? rojo
            float post = Mathf.InverseLerp(cookFrac, 1f, t);
            if (post <= postYellowPortion)
            {
                float k = Mathf.InverseLerp(0f, postYellowPortion, post);
                targetColor = Color.Lerp(readyGreen, burnYellow, k);
            }
            else
            {
                float k = Mathf.InverseLerp(postYellowPortion, 1f, post);
                targetColor = Color.Lerp(burnYellow, burnedRed, k);

                // al llegar al rojo, muestra icono de quemada y oculta el de lista
                if (!burnedShown && k >= 0.98f)
                {
                    if (burnedIcon) burnedIcon.enabled = true;
                    if (readyIcon) readyIcon.enabled = false;
                    burnedShown = true;
                    readyShown = false;
                }
            }
        }
    }

    // Reset visual (se usa al iniciar el otro lado)
    public void ResetUI(float cookFraction)
    {
        cookFrac = Mathf.Clamp01(cookFraction);
        targetFill = 0f;
        if (progressFillImage)
        {
            progressFillImage.fillAmount = 0f;
            progressFillImage.color = startYellow;
        }
        targetColor = startYellow;
        readyShown = burnedShown = false;
        if (readyIcon) readyIcon.enabled = false;
        if (burnedIcon) burnedIcon.enabled = false;
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
        if (!visible)
        {
            if (readyIcon) readyIcon.enabled = false;
            if (burnedIcon) burnedIcon.enabled = false;
            readyShown = burnedShown = false;
        }
    }
}
