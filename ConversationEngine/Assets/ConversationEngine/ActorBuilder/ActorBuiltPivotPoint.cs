using UnityEngine;
namespace ConversationActorBuilder
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
        #endregion

        #region Properties
        public int PivotId => pivotId;
        public string PivotName => pivotName;
        public Vector2 BaseLocalPosition => baseLocalPosition;
        public Vector2 ActiveLocalPosition => activeLocalPosition;
        public bool HasOverridePosition => hasOverridePosition;
        #endregion

        #region Setup
        public void ConfigureBase(int id, string name, Vector2 localPosition)
        {
            pivotId = id;
            pivotName = name ?? "";
            baseLocalPosition = localPosition;
            activeLocalPosition = localPosition;
            hasOverridePosition = false;
            transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
        }
        #endregion

        #region Overrides
        public void SetOverride(Vector2 localPosition)
        {
            hasOverridePosition = true;
            activeLocalPosition = localPosition;
            transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
        }
        public void ClearOverride()
        {
            hasOverridePosition = false;
            activeLocalPosition = baseLocalPosition;
            transform.localPosition = new Vector3(baseLocalPosition.x, baseLocalPosition.y, 0f);
        }
        #endregion
    }
}
