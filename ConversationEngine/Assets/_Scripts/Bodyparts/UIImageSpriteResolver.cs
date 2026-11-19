using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D.Animation;

[ExecuteAlways]
[RequireComponent(typeof(Image), typeof(SpriteResolver))]
public class UIImageSpriteResolver : MonoBehaviour
{
    private Image uiImage;
    private SpriteResolver spriteResolver;
    private SpriteLibrary spriteLibrary;

    void OnEnable()
    {
        uiImage = GetComponent<Image>();
        spriteResolver = GetComponent<SpriteResolver>();
        spriteLibrary = GetComponent<SpriteLibrary>();
    }

    void LateUpdate()
    {
        if (uiImage == null) uiImage = GetComponent<Image>();
        if (spriteResolver == null) spriteResolver = GetComponent<SpriteResolver>();
        if (spriteLibrary == null) spriteLibrary = GetComponent<SpriteLibrary>();

        if (spriteLibrary == null || spriteLibrary.spriteLibraryAsset == null) return;

        string categoria = spriteResolver.GetCategory();
        string etiqueta = spriteResolver.GetLabel();

        if (!string.IsNullOrEmpty(categoria) && !string.IsNullOrEmpty(etiqueta))
        {
            Sprite spriteActual = spriteLibrary.GetSprite(categoria, etiqueta);
            if (spriteActual != null && uiImage.sprite != spriteActual)
            {
                uiImage.sprite = spriteActual;
            }
        }
    }
}
