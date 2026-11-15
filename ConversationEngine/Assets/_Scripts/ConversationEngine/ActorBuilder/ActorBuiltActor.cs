using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace ConversationEngine.ActorBuilder
{
    public class ActorBuiltActor : MonoBehaviour
    {
        #region Runtime Data
        [SerializeField] private List<ActorBuiltBodyPart> bodyParts = new List<ActorBuiltBodyPart>();
        private readonly Dictionary<int, ActorBuiltBodyPart> bodyPartById = new Dictionary<int, ActorBuiltBodyPart>();
        private readonly Dictionary<int, ActorBuiltPivotPoint> pivotById = new Dictionary<int, ActorBuiltPivotPoint>();
        #endregion

        #region Setup
        public void RegisterBodyPart(ActorBuiltBodyPart bodyPart)
        {
            if (bodyPart == null) return;
            if (bodyParts.Any(x => x == bodyPart)) return;
            bodyParts.Add(bodyPart);
            bodyPartById[bodyPart.BodyPartId] = bodyPart;
            RegisterBodyPartPivots(bodyPart);
        }
        public void RebuildCache()
        {
            bodyPartById.Clear();
            pivotById.Clear();
            for (int i = 0; i < bodyParts.Count; i++)
            {
                ActorBuiltBodyPart bodyPart = bodyParts[i];
                if (bodyPart == null) continue;
                bodyPartById[bodyPart.BodyPartId] = bodyPart;
                RegisterBodyPartPivots(bodyPart);
            }
        }
        private void RegisterBodyPartPivots(ActorBuiltBodyPart bodyPart)
        {
            List<ActorBuiltPivotPoint> pivots = bodyPart.GetPivots();
            for (int i = 0; i < pivots.Count; i++)
            {
                ActorBuiltPivotPoint pivot = pivots[i];
                if (pivot == null) continue;
                if (pivotById.ContainsKey(pivot.PivotId)) continue;
                pivotById[pivot.PivotId] = pivot;
            }
        }
        #endregion

        #region Access
        public Transform GetAttachTarget(int pivotId)
        {
            switch (pivotId)
            {
                case 0:
                    return transform;
                default:
                    if (pivotById.TryGetValue(pivotId, out ActorBuiltPivotPoint pivotPoint) && pivotPoint != null) return pivotPoint.transform;
                    return transform;
            }
        }
        public bool SetBodyPartResource(int bodyPartId, string resourceId)
        {
            if (!bodyPartById.TryGetValue(bodyPartId, out ActorBuiltBodyPart bodyPart) || bodyPart == null) return false;
            bodyPart.SetCurrentResource(resourceId);
            return true;
        }
        #endregion
    }
}
