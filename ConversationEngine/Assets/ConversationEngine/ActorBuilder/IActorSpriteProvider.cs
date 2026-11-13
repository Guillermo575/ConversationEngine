using UnityEngine;
namespace ConversationEngine.ActorBuilder
{
    public interface IActorSpriteProvider
    {
        Sprite LoadSprite(string resourceGuid, string resourcePath);
    }
}
