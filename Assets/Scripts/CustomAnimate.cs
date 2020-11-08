using Naninovel.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using OVDEN.Helper;
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
        
        protected override async UniTask AnimateKey (IActor actor, int keyIndex, CancellationToken cancellationToken)
        {
            tasks.Clear();
            
            if (!Duration.IsIndexValid(keyIndex)) return;

            Vector2 cropPosition = new Vector2
            {
                [0] = PositionX.ElementAtOrDefault(keyIndex) ?? actor.Position.x,
                [1] = PositionY.ElementAtOrDefault(keyIndex) ?? actor.Position.y
            };
            Vector2 cropSize = new Vector2
            {
                [0] = SizeX.ElementAtOrDefault(keyIndex) ?? actor.Scale.x,
                [1] = SizeY.ElementAtOrDefault(keyIndex) ?? actor.Scale.y
            };
            Crop crop = new Crop(cropPosition,cropSize);
            
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
                Vector2 pos = crop.AttemptPositionVector();
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
                Vector3 scale = crop.AttemptScaleVector();
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
    }
}