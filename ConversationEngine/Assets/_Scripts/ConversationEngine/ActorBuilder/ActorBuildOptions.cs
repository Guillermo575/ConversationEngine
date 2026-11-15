using UnityEngine;
namespace ConversationEngine.ActorBuilder
{
    public enum ActorRenderTargetType
    {
        World,
        Canvas
    }
    public class ActorBuildOptions
    {
        #region Build Target
        public ActorRenderTargetType TargetType = ActorRenderTargetType.World;
        public Canvas TargetCanvas;
        public string NewCanvasName = "ActorCanvas";
        #endregion

        #region Actor Layout
        public string RootName = "";
        public Vector2 ActorSize = Vector2.zero;
        public float Scale = 1f;
        #endregion

        #region Positions
        public Vector3 WorldPosition = Vector3.zero;
        public Vector2 CanvasAnchoredPosition = Vector2.zero;
        #endregion
    }
}
