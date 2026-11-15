using System.Collections.Generic;
using System.Linq;
using ConversationEngine.ConversationScheme;
using UnityEngine;
namespace ConversationEngine.ActorBuilder
{
    public class ActorBuiltBodyPart : MonoBehaviour
    {
        #region Types
        [System.Serializable]
        private class ResourceRuntimeData
        {
            public string ResourceId;
            public GameObject ResourceObject;
            public List<PivotPoint> OverridePivotPoints = new List<PivotPoint>();
        }
        #endregion

        #region Metadata
        [SerializeField] private int bodyPartId;
        [SerializeField] private string bodyPartName;
        [SerializeField] private int attachToPivotId;
        [SerializeField] private string currentResourceId;
        #endregion

        #region Runtime Data
        [SerializeField] private List<ActorBuiltPivotPoint> pivotPoints = new List<ActorBuiltPivotPoint>();
        [SerializeField] private List<ResourceRuntimeData> resources = new List<ResourceRuntimeData>();
        #endregion

        #region Properties
        public int BodyPartId => bodyPartId;
        public string BodyPartName => bodyPartName;
        public int AttachToPivotId => attachToPivotId;
        public string CurrentResourceId => currentResourceId;
        #endregion

        #region Setup
        public void ConfigureMetadata(int id, string name, int attachPivotId)
        {
            bodyPartId = id;
            bodyPartName = name ?? "";
            attachToPivotId = attachPivotId;
        }
        public void RegisterPivot(ActorBuiltPivotPoint pivotPoint)
        {
            if (pivotPoint == null) return;
            if (pivotPoints.Any(x => x != null && x.PivotId == pivotPoint.PivotId)) return;
            pivotPoints.Add(pivotPoint);
        }
        public void RegisterResource(string resourceId, GameObject resourceObject, List<PivotPoint> overridePivotPoints)
        {
            ResourceRuntimeData resourceData = new ResourceRuntimeData
            {
                ResourceId = resourceId ?? "",
                ResourceObject = resourceObject,
                OverridePivotPoints = overridePivotPoints == null ? new List<PivotPoint>() : overridePivotPoints.Select(x => x == null ? null : new PivotPoint { Id = x.Id, Name = x.Name, X = x.X, Y = x.Y }).Where(x => x != null).ToList()
            };
            resources.Add(resourceData);
        }
        #endregion

        #region Queries
        public ActorBuiltPivotPoint GetPivot(int pivotId)
        {
            return pivotPoints.FirstOrDefault(x => x != null && x.PivotId == pivotId);
        }
        public List<ActorBuiltPivotPoint> GetPivots()
        {
            return pivotPoints.Where(x => x != null).ToList();
        }
        #endregion

        #region Resource State
        public void SetCurrentResource(string resourceId)
        {
            if (resources.Count == 0)
            {
                currentResourceId = "";
                ApplyPivotOverrides(null);
                return;
            }
            ResourceRuntimeData targetResource = null;
            if (!string.IsNullOrEmpty(resourceId)) targetResource = resources.FirstOrDefault(x => x != null && x.ResourceId == resourceId);
            if (targetResource == null) targetResource = resources.FirstOrDefault(x => x != null);
            if (targetResource == null)
            {
                currentResourceId = "";
                ApplyPivotOverrides(null);
                return;
            }
            currentResourceId = targetResource.ResourceId ?? "";
            for (int i = 0; i < resources.Count; i++)
            {
                ResourceRuntimeData entry = resources[i];
                if (entry?.ResourceObject == null) continue;
                bool shouldBeActive = ReferenceEquals(entry, targetResource);
                entry.ResourceObject.SetActive(shouldBeActive);
            }
            ApplyPivotOverrides(targetResource.OverridePivotPoints);
        }
        private void ApplyPivotOverrides(List<PivotPoint> overridePivots)
        {
            for (int i = 0; i < pivotPoints.Count; i++)
            {
                ActorBuiltPivotPoint pivot = pivotPoints[i];
                if (pivot == null) continue;
                PivotPoint overridePivot = null;
                if (overridePivots != null) overridePivot = overridePivots.FirstOrDefault(x => x != null && x.Id == pivot.PivotId);
                if (overridePivot == null) pivot.ClearOverride();
                else pivot.SetOverride(new Vector2(overridePivot.X, overridePivot.Y));
            }
        }
        #endregion
    }
}
