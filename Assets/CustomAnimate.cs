using Naninovel.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.FX
{
    public class CustomAnimate : Animate
    {
        protected virtual List<float?> SizeX { get; } = new List<float?>();
        protected virtual List<float?> SizeY { get; } = new List<float?>();

        public override void SetSpawnParameters(string[] parameters)
        {
            SpawnedPath = gameObject.name;
            KeyCount = 1 + parameters.Max(s => string.IsNullOrEmpty(s) ? 0 : s.Count(c => c == AnimateActor.KeyDelimiter));

            // Required parameters.
            ActorId = parameters.ElementAtOrDefault(0);
            Loop = bool.Parse(parameters.ElementAtOrDefault(1));

            // Optional parameters.
            var cameraConfig = Engine.GetService<ICameraManager>().Configuration;
            for (int paramIdx = 2; paramIdx < 14; paramIdx++)
            {
                var keys = parameters.ElementAtOrDefault(paramIdx)?.Split(AnimateActor.KeyDelimiter);
                if (keys is null || keys.Length == 0 || keys.All(s => s == string.Empty)) continue;

                void AssignKeys<T> (List<T> parameter, Func<string, T> parseKey = default)
                {
                    var defaultKeys = Enumerable.Repeat<T>(default, KeyCount);
                    parameter.AddRange(defaultKeys);

                    for (int keyIdx = 0; keyIdx < keys.Length; keyIdx++)
                        if (!string.IsNullOrEmpty(keys[keyIdx]))
                            parameter[keyIdx] = parseKey is null ? (T)(object)keys[keyIdx] : parseKey(keys[keyIdx]);
                }

                if (paramIdx == 2) AssignKeys(Appearance);
                if (paramIdx == 3) AssignKeys(Transition);
                if (paramIdx == 4) AssignKeys(Visibility, k => bool.TryParse(k, out var result) ? (bool?)result : null);
                if (paramIdx == 5) AssignKeys(PositionX, k => k.AsInvariantFloat());
                if (paramIdx == 6) AssignKeys(PositionY, k => k.AsInvariantFloat());
                if (paramIdx == 7) AssignKeys(PositionZ, k => k.AsInvariantFloat());
                if (paramIdx == 8) AssignKeys(RotationZ, k => k.AsInvariantFloat());
                if (paramIdx == 9) AssignKeys(SizeX, k => k.AsInvariantFloat());
                if (paramIdx == 10) AssignKeys(SizeY, k => k.AsInvariantFloat());
                if (paramIdx == 11) AssignKeys(TintColor);
                if (paramIdx == 12) AssignKeys(EasingTypeName);
                if (paramIdx == 13) AssignKeys(Duration, k => k.AsInvariantFloat());
            }

            // Fill missing durations.
            var lastDuration = 0f;
            for (int keyIdx = 0; keyIdx < KeyCount; keyIdx++)
                if (!Duration.IsIndexValid(keyIdx)) continue;
                else if (!Duration[keyIdx].HasValue)
                    Duration[keyIdx] = lastDuration;
                else lastDuration = Duration[keyIdx].Value;
        }
        
        struct CustomBackData
        {
            public Vector2 originSize;
            public Vector2 size;
            public Vector2 position;
        }
        
        protected override async UniTask AnimateKey (IActor actor, int keyIndex, CancellationToken cancellationToken)
        {
            tasks.Clear();
            
            if (!Duration.IsIndexValid(keyIndex)) return;

            CustomBackData data;
            data.position.x = PositionX.ElementAtOrDefault(keyIndex) ?? actor.Position.x;
            data.position.y = PositionY.ElementAtOrDefault(keyIndex) ?? actor.Position.y;

            data.size.x = SizeX.ElementAtOrDefault(keyIndex) ?? actor.Scale.x;
            data.size.y = SizeY.ElementAtOrDefault(keyIndex) ?? actor.Scale.y;
          
           
            data.originSize = Engine.GetConfiguration<CameraConfiguration>().ReferenceResolution;
        
            
            var duration = Duration[keyIndex] ?? 0f;
            var easingType = EasingType.Linear;
            if (EasingTypeName.ElementAtOrDefault(keyIndex) != null && !Enum.TryParse(EasingTypeName[keyIndex], true, out easingType))
                Debug.LogWarning($"Failed to parse `{EasingTypeName}` easing.");

            if (Appearance.ElementAtOrDefault(keyIndex) != null)
            {
                var transitionName = !string.IsNullOrEmpty(Transition.ElementAtOrDefault(keyIndex)) ? Transition[keyIndex] : TransitionType.Crossfade;
                var transition = new Transition(transitionName);
                tasks.Add(actor.ChangeAppearanceAsync(Appearance[keyIndex], duration, easingType, transition, cancellationToken));
            }

            if (Visibility.ElementAtOrDefault(keyIndex).HasValue)
                tasks.Add(actor.ChangeVisibilityAsync(Visibility[keyIndex] ?? false, duration, easingType, cancellationToken));

            if (PositionX.ElementAtOrDefault(keyIndex).HasValue || PositionY.ElementAtOrDefault(keyIndex).HasValue ||
                PositionZ.ElementAtOrDefault(keyIndex).HasValue)
            {
                Vector2 pos = AttemptPosition(data);
                Vector3 result = new Vector3(
                    PositionX.ElementAtOrDefault(keyIndex) != null ? pos.x : actor.Position.x,
                    PositionY.ElementAtOrDefault(keyIndex) != null ? pos.y : actor.Position.y,
                    PositionZ.ElementAtOrDefault(keyIndex) ?? actor.Position.z);
                tasks.Add(actor.ChangePositionAsync(result, duration, easingType,
                    cancellationToken));
            }

            if (RotationZ.ElementAtOrDefault(keyIndex).HasValue)
                tasks.Add(actor.ChangeRotationZAsync(RotationZ[keyIndex] ?? 0f, duration, easingType, cancellationToken));

            if (Scale.ElementAtOrDefault(keyIndex).HasValue)
            {
                Vector3 scale = AttemptScale(data);
                Vector3 result = new Vector3((SizeX[keyIndex] != null ? scale.x : 1f),
                    (SizeY[keyIndex] != null ? scale.y : 1f), 1);
                tasks.Add(actor.ChangeScaleAsync(result, duration, easingType, cancellationToken));
            }
        

            if (TintColor.ElementAtOrDefault(keyIndex) != null)
            {
                if (ColorUtility.TryParseHtmlString(TintColor[keyIndex], out var color))
                    tasks.Add(actor.ChangeTintColorAsync(color, duration, easingType, cancellationToken));
                else Debug.LogWarning($"Failed to parse `{TintColor}` color to apply tint animation for `{actor.Id}` actor. See the API docs for supported color formats.");
            }

            await UniTask.WhenAll(tasks);
        }
        

        private Vector3 AttemptScale(CustomBackData customBackData)
        {
            return new Vector3
            {
                [0] = customBackData.originSize.x / customBackData.size.x,
                [1] = customBackData.originSize.y / customBackData.size.y,
                [2] = 1.0f
            };
        }

        private Vector3 AttemptPosition(CustomBackData customBackData)
        {
            Vector2 referenceSize = Engine.GetConfiguration<CameraConfiguration>().ReferenceSize;
            
            Vector3 cale = AttemptScale(customBackData);
            
            float offsetCenterX = (customBackData.originSize.x * (float)cale.x) / 2;
            float offsetCenterX1 = customBackData.originSize.x - (customBackData.size.x / 2);
            float offsetX = offsetCenterX1 - offsetCenterX;
            
            float offsetCenterY = (customBackData.originSize.y * (float) cale.y) / 2;
            float offsetCenterY1 = customBackData.originSize.y - (customBackData.size.y / 2);
            float offsetY = offsetCenterY1 - offsetCenterY;

            float centerX = customBackData.position.x + (customBackData.size.x / 2) + offsetX;
            float centerY = (customBackData.originSize.y - customBackData.position.y) - (customBackData.size.y / 2.0f) - offsetY;
            
            float x = 1.0f - (centerX / customBackData.originSize.x);
            float y = centerY / customBackData.originSize.y;
            
            Vector2 originPosition = -referenceSize / 2f;
            
            return new Vector3
            {
                [0] = (originPosition + Vector2.Scale(new Vector2(x,0.0f), referenceSize)).x,
                [1] = -(originPosition + Vector2.Scale(new Vector2(0.0f,y), referenceSize)).y,
                [2] = 100
            };
        }
    }
    
}