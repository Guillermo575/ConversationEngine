using UnityEditor;
using UnityEngine;
namespace ConversationEditor
{
    public class ConversationNodeStyle
    {
        #region Singleton
        private static ConversationNodeStyle SingletonObject;
        private ConversationNodeStyle() { }
        private ConversationNodeStyle CreateSingleton()
        {
            if (SingletonObject == null)
            {
                SingletonObject = this;
                InitializeStyles();
            }
            return SingletonObject;
        }
        public static ConversationNodeStyle GetSingleton()
        {
            if (SingletonObject == null)
            {
                SingletonObject = new ConversationNodeStyle().CreateSingleton();
            }
            return SingletonObject;
        }
        #endregion

        #region Styles
        public GUIStyle nodeStyle;
        public GUIStyle nodeSelectedStyle;
        public GUIStyle nodeDraggingStyle;
        public GUIStyle startNodeStyle;
        public GUIStyle startNodeSelectedStyle;
        public GUIStyle startNodeDraggingStyle;
        public GUIStyle endNodeStyle;
        public GUIStyle endNodeSelectedStyle;
        public GUIStyle endNodeDraggingStyle;
        public GUIStyle functionNodeStyle;
        public GUIStyle functionNodeSelectedStyle;
        public GUIStyle functionNodeDraggingStyle;
        public GUIStyle optionNodeStyle;
        public GUIStyle optionNodeSelectedStyle;
        public GUIStyle optionNodeDraggingStyle;
        public GUIStyle conditionalNodeStyle;
        public GUIStyle conditionalNodeSelectedStyle;
        public GUIStyle conditionalNodeDraggingStyle;
        public GUIStyle nodeHeaderStyle;
        public GUIStyle nodeBodyTextStyle;
        public GUIStyle nodeActorTextStyle;
        #endregion

        #region Variables
        public bool stylesInitialized = false;
        public const int nodeHeaderBaseFontSize = 11;
        public const int nodeBodyBaseFontSize = 12;
        #endregion

        #region Initialize
        public void InitializeStyles()
        {
            if (stylesInitialized) return;
            int borderWidth = 4;
            nodeStyle = new GUIStyle("box");
            nodeStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.2f, 0.6f, 0.3f, 0.9f), Color.black, borderWidth);
            nodeStyle.border = new RectOffset(borderWidth, borderWidth, borderWidth, borderWidth);
            nodeStyle.padding = new RectOffset(borderWidth + 4, borderWidth + 4, borderWidth + 4, borderWidth + 4);
            nodeStyle.alignment = TextAnchor.UpperLeft;
            nodeStyle.wordWrap = true;
            nodeSelectedStyle = new GUIStyle(nodeStyle);
            nodeSelectedStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.2f, 0.6f, 0.3f, 0.9f), new Color(1f, 0.84f, 0f, 1f), borderWidth);
            nodeDraggingStyle = new GUIStyle(nodeStyle);
            nodeDraggingStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.2f, 0.6f, 0.3f, 0.9f), Color.white, borderWidth);
            startNodeStyle = new GUIStyle(nodeStyle);
            startNodeStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.2f, 0.4f, 0.8f, 0.9f), Color.black, borderWidth);
            startNodeStyle.alignment = TextAnchor.MiddleCenter;
            startNodeStyle.fontSize = 16;
            startNodeStyle.fontStyle = FontStyle.Bold;
            startNodeSelectedStyle = new GUIStyle(startNodeStyle);
            startNodeSelectedStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.2f, 0.4f, 0.8f, 0.9f), new Color(1f, 0.84f, 0f, 1f), borderWidth);
            startNodeDraggingStyle = new GUIStyle(startNodeStyle);
            startNodeDraggingStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.2f, 0.4f, 0.8f, 0.9f), Color.white, borderWidth);
            endNodeStyle = new GUIStyle(nodeStyle);
            endNodeStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.8f, 0.2f, 0.2f, 0.9f), Color.black, borderWidth);
            endNodeStyle.alignment = TextAnchor.MiddleCenter;
            endNodeStyle.fontSize = 16;
            endNodeStyle.fontStyle = FontStyle.Bold;
            endNodeSelectedStyle = new GUIStyle(endNodeStyle);
            endNodeSelectedStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.8f, 0.2f, 0.2f, 0.9f), new Color(1f, 0.84f, 0f, 1f), borderWidth);
            endNodeDraggingStyle = new GUIStyle(endNodeStyle);
            endNodeDraggingStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.8f, 0.2f, 0.2f, 0.9f), Color.white, borderWidth);
            functionNodeStyle = new GUIStyle(nodeStyle);
            functionNodeStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.6f, 0.6f, 0.6f, 0.9f), Color.black, borderWidth);
            functionNodeSelectedStyle = new GUIStyle(functionNodeStyle);
            functionNodeSelectedStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.6f, 0.6f, 0.6f, 0.9f), new Color(1f, 0.84f, 0f, 1f), borderWidth);
            functionNodeDraggingStyle = new GUIStyle(functionNodeStyle);
            functionNodeDraggingStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.6f, 0.6f, 0.6f, 0.9f), Color.white, borderWidth);
            optionNodeStyle = new GUIStyle(nodeStyle);
            optionNodeStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.4f, 0.7f, 0.9f, 0.9f), Color.black, borderWidth);
            optionNodeStyle.padding = new RectOffset(borderWidth + 6, borderWidth + 6, borderWidth + 6, borderWidth + 6);
            optionNodeSelectedStyle = new GUIStyle(optionNodeStyle);
            optionNodeSelectedStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.4f, 0.7f, 0.9f, 0.9f), new Color(1f, 0.84f, 0f, 1f), borderWidth);
            optionNodeDraggingStyle = new GUIStyle(optionNodeStyle);
            optionNodeDraggingStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.4f, 0.7f, 0.9f, 0.9f), Color.white, borderWidth);
            conditionalNodeStyle = new GUIStyle(nodeStyle);
            conditionalNodeStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.9f, 0.8f, 0.2f, 0.9f), Color.black, borderWidth);
            conditionalNodeSelectedStyle = new GUIStyle(conditionalNodeStyle);
            conditionalNodeSelectedStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.9f, 0.8f, 0.2f, 0.9f), new Color(1f, 0.84f, 0f, 1f), borderWidth);
            conditionalNodeDraggingStyle = new GUIStyle(conditionalNodeStyle);
            conditionalNodeDraggingStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.9f, 0.8f, 0.2f, 0.9f), Color.white, borderWidth);
            nodeHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
            nodeHeaderStyle.padding = new RectOffset(borderWidth + 4, borderWidth + 4, borderWidth + 4, borderWidth + 4);
            nodeHeaderStyle.fontSize = nodeHeaderBaseFontSize;
            nodeHeaderStyle.normal.textColor = Color.white;
            nodeHeaderStyle.fontStyle = FontStyle.Bold;
            nodeBodyTextStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
            nodeBodyTextStyle.fontStyle = FontStyle.Bold;
            nodeBodyTextStyle.fontSize = nodeBodyBaseFontSize;
            nodeBodyTextStyle.normal.textColor = Color.white;
            nodeActorTextStyle = new GUIStyle(EditorStyles.label);
            nodeActorTextStyle.wordWrap = true;
            nodeActorTextStyle.fontSize = nodeBodyBaseFontSize;
            nodeActorTextStyle.normal.textColor = Color.white;
            stylesInitialized = true;
        }
        #endregion

        #region Texture Generation
        private Texture2D MakeTextureWithBorder(int width, int height, Color fillColor, Color borderColor, int borderWidth)
        {
            int totalWidth = width + borderWidth * 2;
            int totalHeight = height + borderWidth * 2;
            Color[] pixels = new Color[totalWidth * totalHeight];
            for (int y = 0; y < totalHeight; y++)
            {
                for (int x = 0; x < totalWidth; x++)
                {
                    if (x < borderWidth || x >= totalWidth - borderWidth || y < borderWidth || y >= totalHeight - borderWidth) pixels[y * totalWidth + x] = borderColor;
                    else pixels[y * totalWidth + x] = fillColor;
                }
            }
            Texture2D texture = new Texture2D(totalWidth, totalHeight, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();
            return texture;
        }
        #endregion
    }
}