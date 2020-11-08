using System.Collections.Generic;
using UnityEngine;
using UniRx.Async;
using System.Globalization;

namespace Naninovel.Commands
{
    [CommandAlias("animateCrop")]
    public class CustomAnimateActor : AnimateActor
    {
        [ParameterAlias("posX")]
        public StringParameter PositionX;
        [ParameterAlias("posY")]
        public StringParameter PositionY;
        
        [ParameterAlias("sizeX")]
        public StringParameter SizeX;
        [ParameterAlias("sizeY")]
        public StringParameter SizeY;
        
        struct CustomBackData
        {
            public Vector2 originSize;
            public Vector2 size;
            public Vector2 position;
        }
        private CustomBackData customBackData;
        
        private const string defaultDuration = "0.35";
        
        public const string prefabPath = "CustomAnimate";
        
        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var spawnManager = Engine.GetService<ISpawnManager>();
            var tasks = new List<UniTask>();

            foreach (var actorId in ActorIds)
            {
                var parameters = new string[14]; // Don't cache it, otherwise parameters will leak across actors on async spawn init.

                parameters[0] = actorId;
                parameters[1] = Loop.Value.ToString(CultureInfo.InvariantCulture);
                parameters[2] = Assigned(Appearance) ? Appearance : null;
                parameters[3] = Assigned(Transition) ? Transition : null;
                parameters[4] = Assigned(Visibility) ? Visibility : null;
                parameters[5] = Assigned(PositionX) ? PositionX : null;
                parameters[6] = Assigned(PositionY) ? PositionY : null;
                parameters[7] = Assigned(PositionZ) ? PositionZ : null;
                parameters[8] = Assigned(Rotation) ? Rotation : null;
                parameters[9] = Assigned(SizeX) ? SizeX : null;
                parameters[10] = Assigned(SizeY) ? SizeY : null;
                parameters[11] = Assigned(TintColor) ? TintColor : null;
                parameters[12] = Assigned(EasingTypeName) ? EasingTypeName : null;
                parameters[13] = Assigned(Duration) ? Duration.Value : defaultDuration;

                var spawnPath = $"{prefabPath}{SpawnConfiguration.IdDelimiter}{actorId}";
                if (spawnManager.IsObjectSpawned(spawnPath))
                    tasks.Add(spawnManager.UpdateSpawnedAsync(spawnPath, cancellationToken, parameters));
                else tasks.Add(spawnManager.SpawnAsync(spawnPath, cancellationToken, parameters));
            }

            await UniTask.WhenAll(tasks);
        }
    }
}