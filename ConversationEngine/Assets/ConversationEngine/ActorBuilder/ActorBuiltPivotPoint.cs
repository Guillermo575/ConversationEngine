using UnityEngine;
namespace ConversationEngine.ActorBuilder
{
    public class ActorBuiltPivotPoint : MonoBehaviour
    {
        #region Metadata
        [SerializeField] private int pivotId;
        [SerializeField] private string pivotName;
        #endregion

        #region Positions
        [SerializeField] private Vector2 baseLocalPosition;
        [SerializeField] private Vector2 activeLocalPosition;
        [SerializeField] private bool hasOverridePosition;
        [SerializeField] private Vector2 positionMultiplier = Vector2.one;
        #endregion

        #region Properties
        public int PivotId => pivotId;
        public string PivotName => pivotName;
        public Vector2 BaseLocalPosition => baseLocalPosition;
        public Vector2 ActiveLocalPosition => activeLocalPosition;
        public bool HasOverridePosition => hasOverridePosition;
        public Vector2 PositionMultiplier => positionMultiplier;
        #endregion

        #region Setup
        public void ConfigureBase(int id, string name, Vector2 localPosition)
        {
            pivotId = id;
            pivotName = name ?? "";
            baseLocalPosition = localPosition;
            activeLocalPosition = localPosition;
            hasOverridePosition = false;
            ApplyTransformPosition(activeLocalPosition);
        }
        public void SetPositionMultiplier(Vector2 multiplier)
        {
            positionMultiplier = new Vector2(Mathf.Approximately(multiplier.x, 0f) ? 1f : multiplier.x, Mathf.Approximately(multiplier.y, 0f) ? 1f : multiplier.y);
            ApplyTransformPosition(activeLocalPosition);
        }
        #endregion

        #region Overrides
        public void SetOverride(Vector2 localPosition)
        {
            hasOverridePosition = true;
            activeLocalPosition = localPosition;
            ApplyTransformPosition(activeLocalPosition);
        }
        public void ClearOverride()
        {
            hasOverridePosition = false;
            activeLocalPosition = baseLocalPosition;
            ApplyTransformPosition(activeLocalPosition);
        }
        private void ApplyTransformPosition(Vector2 logicalLocalPosition)
        {
            transform.localPosition = new Vector3(logicalLocalPosition.x * positionMultiplier.x, logicalLocalPosition.y * positionMultiplier.y, 0f);
        }
        #endregion
    }
}
