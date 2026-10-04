using UnityEngine;
using UnityEngine.UI;

namespace AetherWild
{
    // Scene-referenced presentation data; no runtime AssetDatabase/Editor paths.
    [CreateAssetMenu(menuName="AetherWild/Production art")]
    public sealed class ProductionArt : ScriptableObject
    {
        public Sprite maeIdle, maeWalkA, maeWalkB, maeCast, maeHit, maeDefeat;
        public Sprite healthFrame, shieldFrame, hudPanel, primaryButton, secondaryButton;
        public Sprite sigilSlot, sigilSelected, title;

        // Decorative child only: the existing rectangular Graphic keeps the whole touch target.
        public static Image Skin(Graphic target,Sprite sprite,bool sliced=true)
        {
            var image=new GameObject("Production skin",typeof(RectTransform),typeof(Image)).GetComponent<Image>();
            var rect=image.rectTransform;
            if(target is Text)
            {
                // Text must be rendered after its decoration, not beneath a child Image.
                image.transform.SetParent(target.transform.parent,false);
                image.transform.SetSiblingIndex(target.transform.GetSiblingIndex());
                var source=target.rectTransform;
                rect.anchorMin=source.anchorMin;rect.anchorMax=source.anchorMax;
                rect.pivot=source.pivot;rect.anchoredPosition=source.anchoredPosition;rect.sizeDelta=source.sizeDelta;
            }
            else
            {
                image.transform.SetParent(target.transform,false);
                image.transform.SetAsFirstSibling();
                rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
                rect.offsetMin=rect.offsetMax=Vector2.zero;
            }
            image.sprite=sprite;image.color=Color.white;image.raycastTarget=false;
            image.type=sliced?Image.Type.Sliced:Image.Type.Simple;
            image.pixelsPerUnitMultiplier=12;
            image.preserveAspect=!sliced;
            return image;
        }
        public static void TextOutline(Text text)
        {
            var shadow=text.gameObject.AddComponent<Shadow>();
            shadow.effectColor=new Color(0,0,0,.95f);
            shadow.effectDistance=new Vector2(1,-1);
        }
    }
}
