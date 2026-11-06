using UnityEngine;
namespace ConversationActorBuilder
{
    public interface IActorSpriteProvider
    {
        Sprite LoadSprite(string resourceGuid, string resourcePath);
    }
}
